#if NET6_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace MiniValidationPlus.UnitTests;

public class NonNullablePropertyHelperTests
{
    [Fact]
    public void IsNonNullableReferenceType_Identifies_Correct_Properties_Of_Class()
    {
        var type = typeof(ClassModel);

        var nonNullableReferenceTypes = new List<string>();
        var other = new List<string>();

        foreach (var property in type.GetProperties(BindingFlags.Instance
                                                    | BindingFlags.Public
                                                    | BindingFlags.FlattenHierarchy))
        {
            var isNonNullableReferenceType = new NonNullablePropertyHelper().IsNonNullableReferenceType(property);
            if (isNonNullableReferenceType)
            {
                nonNullableReferenceTypes.Add(property.Name);
            }
            else
            {
                other.Add(property.Name);
            }
        }

        Assert.Contains(nameof(ClassModel.StringNonNullable), nonNullableReferenceTypes);
        Assert.Contains(nameof(ClassModel.AnotherNonNullable), nonNullableReferenceTypes);

        Assert.Contains(nameof(ClassModel.IntNonNullable), other);
        Assert.Contains(nameof(ClassModel.IntNullable), other);
        Assert.Contains(nameof(ClassModel.StringNullable), other);
        Assert.Contains(nameof(ClassModel.AnotherNullable), other);
    }

    [Fact]
    public void IsNonNullableReferenceType_Identifies_Correct_Properties_Of_Record()
    {
        var type = typeof(RecordModel);

        var nonNullableReferenceTypes = new List<string>();
        var other = new List<string>();

        foreach (var property in type.GetProperties(BindingFlags.Instance
                                                    | BindingFlags.Public
                                                    | BindingFlags.FlattenHierarchy))
        {
            var isNonNullableReferenceType = new NonNullablePropertyHelper().IsNonNullableReferenceType(property);
            if (isNonNullableReferenceType)
            {
                nonNullableReferenceTypes.Add(property.Name);
            }
            else
            {
                other.Add(property.Name);
            }
        }

        Assert.Contains(nameof(RecordModel.StringNonNullable), nonNullableReferenceTypes);
        Assert.Contains(nameof(RecordModel.AnotherNonNullable), nonNullableReferenceTypes);

        Assert.Contains(nameof(RecordModel.IntNonNullable), other);
        Assert.Contains(nameof(RecordModel.IntNullable), other);
        Assert.Contains(nameof(RecordModel.StringNullable), other);
        Assert.Contains(nameof(RecordModel.AnotherNullable), other);
    }

    private class ClassModel
    {
        public int IntNonNullable { get; set; }
        public int? IntNullable { get; set; }
        public string StringNonNullable { get; set; } = null!;
        public string? StringNullable { get; set; }
        public AnotherModel AnotherNonNullable { get; set; } = new();
        public AnotherModel? AnotherNullable { get; set; }
    }

    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local")]
    private record RecordModel(
        int IntNonNullable,
        int? IntNullable,
        string StringNonNullable,
        string? StringNullable,
        AnotherModel AnotherNonNullable,
        AnotherModel? AnotherNullable);

    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    // ReSharper disable once FunctionRecursiveOnAllPaths
    private class AnotherModel
    {
        public int IntNonNullable { get; set; }
        public int? IntNullable { get; set; }
        public string StringNonNullable { get; set; } = null!;
        public string? StringNullable { get; set; }
        public AnotherModel AnotherNonNullable { get; set; } = new();
        public AnotherModel? AnotherNullable { get; set; }
    }
}
#endif
