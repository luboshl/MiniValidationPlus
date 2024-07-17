using System;

namespace MiniValidationPlus;

/// <summary>
/// Indicates that a property should be ignored during validation when using
/// <see cref="MiniValidatorPlus.TryValidate{TTarget}(TTarget, out System.Collections.Generic.IDictionary{string, string[]})"/>.
/// Note that also recursive validation will be ignored on the property.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class SkipValidationAttribute : Attribute
{
}
