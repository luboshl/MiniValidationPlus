#if NET6_0_OR_GREATER

using System.Reflection;

namespace MiniValidationPlus
{
    /// <summary>
    /// Helper for non-nullable reference types.
    /// </summary>
    public class NonNullablePropertyHelper
    {
        private readonly NullabilityInfoContext nullabilityContext = new ();

        /// <summary>
        /// Gets information whether the <paramref name="propertyInfo"/> is non-nullable reference type.
        /// </summary>
        /// <param name="propertyInfo">The property.</param>
        /// <returns><c>True</c> when <paramref name="propertyInfo"/> is non-nullable reference type, <c>False</c> otherwise.</returns>
        public bool IsNonNullableReferenceType(PropertyInfo propertyInfo)
        {
            if (propertyInfo.PropertyType.IsValueType)
            {
                return false;
            }

            var nullabilityInfo = nullabilityContext.Create(propertyInfo);
            return nullabilityInfo.WriteState is not NullabilityState.Nullable;
        }
    }
}
#endif
