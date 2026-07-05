#if !NET5_0_OR_GREATER
using System.ComponentModel;

namespace System.Runtime.CompilerServices;

/// <summary>
/// Polyfill enabling C# <c>init</c>-only setters on target frameworks
/// (netstandard2.0) that do not ship this compiler-required type.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
internal static class IsExternalInit
{
}
#endif
