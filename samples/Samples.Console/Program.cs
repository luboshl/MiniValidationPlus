﻿using System.ComponentModel.DataAnnotations;
using MiniValidationPlus;

var nameAndCategory = args.Length > 0 ? args[0] : null;

var widgets = new List<Widget>
{
    new Widget { Name = nameAndCategory, Category = nameAndCategory },
    new WidgetWithCustomValidation { Name = nameAndCategory, Category = nameAndCategory }
};

var allValid = true;
foreach (var widget in widgets)
{
    if (!MiniValidatorPlus.TryValidate(widget, out var errors))
    {
        allValid = false;
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

return allValid ? 0 : 1;

class Widget
{
    [Required, MinLength(3), Display(Name = "Widget name")]
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