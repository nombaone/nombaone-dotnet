// Polyfills for the `required` members feature on target frameworks
// (netstandard2.0) that predate these compiler-recognized attributes.
#if !NET7_0_OR_GREATER
using System;

namespace System.Runtime.CompilerServices
{
    /// <summary>Polyfill — marks a member as required to be set during initialization.</summary>
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property,
        Inherited = false,
        AllowMultiple = false)]
    internal sealed class RequiredMemberAttribute : Attribute
    {
    }

    /// <summary>Polyfill — indicates a compiler feature required to consume a member.</summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    internal sealed class CompilerFeatureRequiredAttribute : Attribute
    {
        public CompilerFeatureRequiredAttribute(string featureName) => FeatureName = featureName;

        public string FeatureName { get; }

        public bool IsOptional { get; init; }

        public const string RefStructs = nameof(RefStructs);
        public const string RequiredMembers = nameof(RequiredMembers);
    }
}

namespace System.Diagnostics.CodeAnalysis
{
    /// <summary>Polyfill — a constructor that sets all required members.</summary>
    [AttributeUsage(AttributeTargets.Constructor, Inherited = false, AllowMultiple = false)]
    internal sealed class SetsRequiredMembersAttribute : Attribute
    {
    }
}
#endif
