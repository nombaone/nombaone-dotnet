using System.Reflection;

namespace NombaOne.Internal;

/// <summary>
/// The SDK version, surfaced in the <c>User-Agent</c> header of every request.
/// The single source of truth is the <c>&lt;Version&gt;</c> in NombaOne.csproj;
/// this reads it back from the compiled assembly rather than duplicating it.
/// </summary>
internal static class NombaoneVersion
{
    /// <summary>The current SDK semantic version, read from the assembly.</summary>
    internal static string Version { get; } = Resolve();

    /// <summary>The <c>User-Agent</c> value sent on every request.</summary>
    internal static string UserAgent { get; } = "nombaone-dotnet/" + Version;

    private static string Resolve()
    {
        var assembly = typeof(NombaoneVersion).Assembly;

        // <Version> flows into AssemblyInformationalVersion; strip any build
        // metadata a source-link/CI build may append after '+'.
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrEmpty(informational))
        {
            var plus = informational!.IndexOf('+');
            return plus >= 0 ? informational.Substring(0, plus) : informational;
        }

        var version = assembly.GetName().Version;
        return version is null ? "0.0.0" : $"{version.Major}.{version.Minor}.{version.Build}";
    }
}
