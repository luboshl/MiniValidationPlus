using System;

namespace MiniValidationPlus;

/// <summary>
/// Configuration parameters for validation.
/// </summary>
/// <param name="ServiceProvider">The service provider to use when creating ValidationContext.</param>
/// <param name="Recurse"><c>true</c> to recursively validate descendant objects; if <c>false</c> only simple values directly on <c>target</c> are validated.</param>
/// <param name="AllowAsync"><c>true</c> to allow asynchronous validation if an object in the graph requires it.</param>
/// <param name="ValidateNonNullableReferenceTypes"><c>true</c> to validate properties of non-nullable reference types as required.</param>
public record ValidationSettings(
    IServiceProvider? ServiceProvider = null,
    bool Recurse = true,
    bool AllowAsync = false,
    bool ValidateNonNullableReferenceTypes = true)
{
    /// <summary>
    /// Default settings.
    /// </summary>
    public static ValidationSettings Default { get; } = new(
        ServiceProvider: null,
        Recurse: true,
        AllowAsync: false,
        ValidateNonNullableReferenceTypes: true);
}
