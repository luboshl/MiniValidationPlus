﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace MiniValidationPlus;

internal class TypeDetailsCache
{
    private static readonly PropertyDetails[] _emptyPropertyDetails = Array.Empty<PropertyDetails>();
    private readonly ConcurrentDictionary<Type, (PropertyDetails[] Properties, bool RequiresAsync)> _cache = new();
    
    public (PropertyDetails[] Properties, bool RequiresAsync) Get(Type? type)
    {
        if (type is null)
        {
            return (_emptyPropertyDetails, false);
        }

        if (!_cache.ContainsKey(type))
        {
            Visit(type);
        }

        return _cache[type];
    }

    private void Visit(Type type)
    {
        var visited = new HashSet<Type>();
        bool requiresAsync = false;
        Visit(type, visited, ref requiresAsync);
    }

    private void Visit(Type type, HashSet<Type> visited, ref bool requiresAsync)
    {
        if (_cache.ContainsKey(type))
        {
            return;
        }

        if (!visited.Add(type))
        {
            return;
        }

        if (DoNotRecurseIntoPropertiesOf(type))
        {
            _cache[type] = (_emptyPropertyDetails, false);
            return;
        }

        if (typeof(IAsyncValidatableObject).IsAssignableFrom(type))
        {
            requiresAsync = true;
        }

        // Find a constructor that matches the Deconstruct method (this will be the primary constuctor for record types)
        ParameterInfo[]? primaryCtorParams = null;
        foreach (var ctor in type.GetConstructors())
        {
            if (ctor.DeclaringType != type) continue;

            // Parameters to Deconstruct are 'byref' so need to call MakeByRefType()
            var deconstructParams = ctor.GetParameters().Select(p => p.ParameterType.IsByRef ? p.ParameterType : p.ParameterType.MakeByRefType()).ToArray();
            if (type.GetMethod("Deconstruct", deconstructParams) is { } deconstruct && deconstruct.DeclaringType == type)
            {
                primaryCtorParams = ctor.GetParameters();
            }
        }

        List<PropertyDetails>? propertiesToValidate = null;
        var hasPropertiesOfOwnType = false;
        var hasValidatableProperties = false;

#if NET6_0_OR_GREATER
        var nonNullablePropertyHelper = new NonNullablePropertyHelper();
#endif

        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy))
        {
            if (property.GetIndexParameters().Length > 0)
            {
                // Ignore indexer properties
                continue;
            }

            var (validationAttributes, displayAttribute, skipRecursionAttribute, skipValidationAttribute)
                = TypeDetailsCache.GetPropertyAttributes(primaryCtorParams, property);
            validationAttributes ??= Array.Empty<ValidationAttribute>();

#if NET6_0_OR_GREATER
            var isNonNullableReferenceType = nonNullablePropertyHelper.IsNonNullableReferenceType(property);
#else
            var isNonNullableReferenceType = false;
#endif

            var hasValidationOnProperty = validationAttributes.Length > 0 || isNonNullableReferenceType;
            var hasSkipRecursionOnProperty = skipRecursionAttribute is not null;
            var hasSkipValidationAttribute = skipValidationAttribute is not null;
            var enumerableType = GetEnumerableType(property.PropertyType);
            if (enumerableType != null && property.PropertyType != typeof(string))
            {
                Visit(enumerableType, visited, ref requiresAsync);
            }

            // Defer fully checking properties that are of the same type we're currently building the cache for.
            // We'll remove them at the end if any other validatable properties are present.
            if (type == property.PropertyType && !hasSkipRecursionOnProperty && !hasSkipValidationAttribute)
            {
                propertiesToValidate ??= new List<PropertyDetails>();
                propertiesToValidate.Add(
                    new PropertyDetails(
                        property.Name,
                        displayAttribute,
                        property.PropertyType,
                        PropertyHelper.MakeNullSafeFastPropertyGetter(property),
                        validationAttributes,
                        true,
                        enumerableType,
                        isNonNullableReferenceType));
                hasPropertiesOfOwnType = true;
                continue;
            }

            Visit(property.PropertyType, visited, ref requiresAsync);
            var propertyTypeHasProperties = _cache.TryGetValue(property.PropertyType, out var typeCache) && typeCache.Properties.Length > 0;
            var propertyTypeIsValidatableObject = typeof(IValidatableObject).IsAssignableFrom(property.PropertyType)
                                                  || typeof(IAsyncValidatableObject).IsAssignableFrom(property.PropertyType);
            var propertyTypeSupportsPolymorphism = !property.PropertyType.IsSealed;
            var enumerableTypeHasProperties = enumerableType != null
                && _cache.TryGetValue(enumerableType, out var enumProperties)
                && enumProperties.Properties.Length > 0;
            var recurse = (enumerableTypeHasProperties || propertyTypeHasProperties
                || propertyTypeIsValidatableObject
                || propertyTypeSupportsPolymorphism)
                && !hasSkipRecursionOnProperty;

            if (!hasSkipValidationAttribute
                && (recurse || hasValidationOnProperty || isNonNullableReferenceType))
            {
                propertiesToValidate ??= new List<PropertyDetails>();
                propertiesToValidate.Add(
                    new PropertyDetails(
                        property.Name,
                        displayAttribute,
                        property.PropertyType,
                        PropertyHelper.MakeNullSafeFastPropertyGetter(property),
                        validationAttributes,
                        recurse,
                        enumerableTypeHasProperties ? enumerableType : null,
                        isNonNullableReferenceType));
                hasValidatableProperties = true;
            }
        }

        if (hasPropertiesOfOwnType && propertiesToValidate != null)
        {
            // Remove properties of same type if there's nothing to validate on them
            for (var i = propertiesToValidate.Count - 1; i >= 0; i--)
            {
                var property = propertiesToValidate[i];
                var enumerableTypeHasProperties = property.EnumerableType != null
                    && _cache.TryGetValue(property.EnumerableType, out var typeCache)
                    && typeCache.Properties.Length > 0;
                var keepProperty = property.Type != type || (hasValidatableProperties || enumerableTypeHasProperties);
                if (!keepProperty)
                {
                    propertiesToValidate.RemoveAt(i);
                }
            }
        }

        _cache[type] = (propertiesToValidate?.ToArray() ?? _emptyPropertyDetails, requiresAsync);
    }

    private static bool DoNotRecurseIntoPropertiesOf(Type type) =>
        type == typeof(object)
        || type.IsPrimitive
        || type.IsArray
        || type.IsPointer
        || type.IsEnum
        || type == typeof(string)
        || type == typeof(decimal)
        || type == typeof(DateTime)
        || type == typeof(DateTimeOffset)
#if NET6_0_OR_GREATER
        || type == typeof(DateOnly)
        || type == typeof(TimeOnly)
#endif
        || type == typeof(Type)
        || type == typeof(Delegate)
        || type == typeof(MethodInfo)
        || type == typeof(MemberInfo)
        || type == typeof(ParameterInfo)
        || type == typeof(Assembly)
        || type == typeof(Uri)
        || type == typeof(CancellationToken)
        || type == typeof(Stream)
    // TODO: Add extension point to add other types to ignore
    ;

    private static (ValidationAttribute[]?, DisplayAttribute?, SkipRecursionAttribute?, SkipValidationAttribute?)
        GetPropertyAttributes(ParameterInfo[]? primaryCtorParameters, PropertyInfo property)
    {
        List<ValidationAttribute>? validationAttributes = null;
        DisplayAttribute? displayAttribute = null;
        SkipRecursionAttribute? skipRecursionAttribute = null;
        SkipValidationAttribute? skipValidationAttribute = null;

        IEnumerable<Attribute>? paramAttributes = null;
        if (primaryCtorParameters is { } ctorParams)
        {
            foreach (var parameter in ctorParams)
            {
                if (string.Equals(parameter.Name, property.Name, StringComparison.Ordinal)
                    && parameter.ParameterType == property.PropertyType)
                {
                    // Matching parameter found
                    paramAttributes = parameter.GetCustomAttributes();
                    break;
                }
            }
        }

        var propertyAttributes = property.GetCustomAttributes();
        var customAttributes = paramAttributes is not null
            ? paramAttributes.Concat(propertyAttributes)
            : propertyAttributes;

        if (TryGetAttributesViaTypeDescriptor(property, out var typeDescriptorAttributes))
        {
            customAttributes = customAttributes
                .Concat(typeDescriptorAttributes.Cast<Attribute>())
                .Distinct();
        }

        foreach (var attr in customAttributes)
        {
            if (attr is ValidationAttribute validationAttr)
            {
                validationAttributes ??= new();
                validationAttributes.Add(validationAttr);
            }
            else if (attr is DisplayAttribute displayAttr)
            {
                displayAttribute = displayAttr;
            }
            else if (attr is SkipRecursionAttribute skipRecursionAttr)
            {
                skipRecursionAttribute = skipRecursionAttr;
            }
            else if (attr is SkipValidationAttribute skipValidationAttr)
            {
                skipValidationAttribute = skipValidationAttr;
            }
        }

        return new(validationAttributes?.ToArray(), displayAttribute, skipRecursionAttribute, skipValidationAttribute);
    }

    private static bool TryGetAttributesViaTypeDescriptor(PropertyInfo property, [NotNullWhen(true)] out IEnumerable<Attribute>? typeDescriptorAttributes)
    {
        var attributes = TypeDescriptor.GetProperties(property.ReflectedType!)
            .Cast<PropertyDescriptor>()
            .FirstOrDefault(x => x.Name == property.Name)
            ?.Attributes;

        if (attributes is { Count: > 0 } tdps)
        {
            typeDescriptorAttributes = tdps.Cast<Attribute>();
            return true;
        }

        typeDescriptorAttributes = null;
        return false;
    }

    private static Type? GetEnumerableType(Type type)
    {
        if (type.IsInterface && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            return type.GetGenericArguments()[0];
        }

        foreach (var intType in type.GetInterfaces())
        {
            if (intType.IsGenericType && intType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return intType.GetGenericArguments()[0];
            }
        }

        return null;
    }
}

internal record PropertyDetails(
    string Name,
    DisplayAttribute? DisplayAttribute,
    Type Type,
    Func<object, object?> PropertyGetter,
    ValidationAttribute[] ValidationAttributes,
    bool Recurse,
    Type? EnumerableType,
    bool IsNonNullableType
)
{
    public object? GetValue(object target) => PropertyGetter(target);

    public bool IsEnumerable => EnumerableType != null;

    public bool HasValidationAttributes => ValidationAttributes.Length > 0;
}
