# MiniValidationPlus

👉 with support of non-nullable reference types.

A minimalistic validation library built atop the existing features in .NET's `System.ComponentModel.DataAnnotations` namespace. Adds support for single-line validation calls and recursion with cycle detection.

This project is fork of the original great repo [MiniValidation](https://github.com/DamianEdwards/MiniValidation) from [Damian Edwards](https://github.com/DamianEdwards) and adds support of **non-nullable reference types**. Now validation works more like validation in model binding of ASP.NET Core MVC.

Supports .NET Standard 2.0 compliant runtimes.

## Installation
[![Nuget](https://img.shields.io/nuget/v/MiniValidationPlus)](https://www.nuget.org/packages/MiniValidationPlus/)

Install the library from [NuGet](https://www.nuget.org/packages/MiniValidationPlus):
``` console
❯ dotnet add package MiniValidationPlus
```

### ASP.NET Core 6+ Projects
If installing into an ASP.NET Core 6+ project, consider using the [MinimalApis.Extensions](https://www.nuget.org/packages/MinimalApis.Extensions) package instead, which adds extensions specific to ASP.NET Core, including a validation endpoint filter for .NET 7 apps:
``` console
❯ dotnet add package MinimalApis.Extensions
```

## Example usage

### Validate an object

```csharp
var widget = new Widget { Name = "" };

var isValid = MiniValidatorPlus.TryValidate(widget, out var errors);

class Widget
{
    [Required, MinLength(3)]
    public string Name { get; set; }
    
    // Non-nullable reference types are required automatically
    public string Category { get; set; }

    public override string ToString() => Name;
}
```

### Use services from validators

```csharp
var widget = new Widget { Name = "" };

// Get your serviceProvider from wherever makes sense
var serviceProvider = ...
var isValid = MiniValidatorPlus.TryValidate(widget, serviceProvider, out var errors);

class Widget : IValidatableObject
{
    [Required, MinLength(3)]
    public string Name { get; set; }
    
    // Non-nullable reference types are required automatically
    public string Category { get; set; }

    public override string ToString() => Name;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var disallowedNamesService = validationContext.GetService(typeof(IDisallowedNamesService)) as IDisallowedNamesService;

        if (disallowedNamesService is null)
        {
            throw new InvalidOperationException($"Validation of {nameof(Widget)} requires an {nameof(IDisallowedNamesService)} instance.");
        }

        if (disallowedNamesService.IsDisallowedName(Name))
        {
            yield return new($"Cannot name a widget '{Name}'.", new[] { nameof(Name) });
        }
    }
}
```

### Console app

```csharp
using System.ComponentModel.DataAnnotations;
using MiniValidationPlus;

var nameAndCategory = args.Length > 0 ? args[0] : "";

var widgets = new List<Widget>
{
    new Widget { Name = nameAndCategory, Category = nameAndCategory },
    new WidgetWithCustomValidation { Name = nameAndCategory, Category = nameAndCategory }
};

foreach (var widget in widgets)
{
    if (!MiniValidatorPlus.TryValidate(widget, out var errors))
    {
        Console.WriteLine($"{nameof(Widget)} has errors!");
        foreach (var entry in errors)
        {
            Console.WriteLine($"  {entry.Key}:");
            foreach (var error in entry.Value)
            {
                Console.WriteLine($"  - {error}");
            }
        }
    }
    else
    {
        Console.WriteLine($"{nameof(Widget)} '{widget}' is valid!");
    }
}

class Widget
{
    [Required, MinLength(3)]
    public string Name { get; set; }
    
    // Non-nullable reference types are required automatically
    public string Category { get; set; }

    public override string ToString() => Name;
}

class WidgetWithCustomValidation : Widget, IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.Equals(Name, "Widget", StringComparison.OrdinalIgnoreCase))
        {
            yield return new($"Cannot name a widget '{Name}'.", new[] { nameof(Name) });
        }
    }
}
```
``` console
❯ widget.exe
Widget has errors!
  Name:
  - The Widget name field is required.
  Category:
  - The Category field is required.
Widget has errors!
  Name:
  - The Widget name field is required.
  Category:
  - The Category field is required.

❯ widget.exe Ok
Widget has errors!
  Name:
  - The field Widget name must be a string or array type with a minimum length of '3'.
Widget has errors!
  Name:
  - The field Widget name must be a string or array type with a minimum length of '3'.

❯ widget.exe Widget
Widget 'Widget' is valid!
Widget has errors!
  Name:
  - Cannot name a widget 'Widget'.

❯ widget.exe MiniValidationPlus
Widget 'MiniValidationPlus' is valid!
Widget 'MiniValidationPlus' is valid!
```

### Web app (.NET 6)
```csharp
using System.ComponentModel.DataAnnotations;
using MiniValidationPlus;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Hello World");

app.MapGet("/widgets", () =>
    new[] {
        new Widget { Name = "Shinerizer" },
        new Widget { Name = "Sparklizer" }
    });

app.MapGet("/widgets/{name}", (string name) =>
    new Widget { Name = name });

app.MapPost("/widgets", (Widget widget) =>
    !MiniValidatorPlus.TryValidate(widget, out var errors)
        ? Results.ValidationProblem(errors)
        : Results.Created($"/widgets/{widget.Name}", widget));

app.MapPost("/widgets/custom-validation", (WidgetWithCustomValidation widget) =>
    !MiniValidatorPlus.TryValidate(widget, out var errors)
        ? Results.ValidationProblem(errors)
        : Results.Created($"/widgets/{widget.Name}", widget));

app.Run();

class Widget
{
    [Required, MinLength(3)]
    public string? Name { get; set; }

    public override string? ToString() => Name;
}

class WidgetWithCustomValidation : Widget, IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.Equals(Name, "Widget", StringComparison.OrdinalIgnoreCase))
        {
            yield return new($"Cannot name a widget '{Name}'.", new[] { nameof(Name) });
        }
    }
}
```
