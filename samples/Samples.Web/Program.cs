using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http.HttpResults;
using MiniValidationPlus;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.MapSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => "Hello World");

app.MapGet("/widgets", () =>
    new[] {
        new Widget { Name = "Shinerizer" },
        new Widget { Name = "Sparklizer" }
    });

app.MapGet("/widgets/{name}", (string name) =>
    new Widget { Name = name });

app.MapPost("/widgets", Results<ValidationProblem, Created<Widget>> (Widget widget) =>
    !MiniValidatorPlus.TryValidate(widget, out var errors)
        ? TypedResults.ValidationProblem(errors)
        : TypedResults.Created($"/widgets/{widget.Name}", widget));

app.MapPost("/widgets/custom-validation", Results<ValidationProblem, Created<WidgetWithCustomValidation>> (WidgetWithCustomValidation widget) =>
    !MiniValidatorPlus.TryValidate(widget, out var errors)
        ? TypedResults.ValidationProblem(errors)
        : TypedResults.Created($"/widgets/{widget.Name}", widget));

app.Run();

class Widget
{
    [Required, MinLength(3), Display(Name = "Widget name")]
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
