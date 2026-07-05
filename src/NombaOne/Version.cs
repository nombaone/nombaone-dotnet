namespace NombaOne.Internal;

/// <summary>
/// The SDK version, surfaced in the <c>User-Agent</c> header of every request.
/// Kept in sync with the <c>&lt;Version&gt;</c> in NombaOne.csproj.
/// </summary>
internal static class NombaoneVersion
{
    /// <summary>The current SDK semantic version.</summary>
    internal const string Version = "0.1.0";

    /// <summary>The <c>User-Agent</c> value sent on every request.</summary>
    internal const string UserAgent = "nombaone-dotnet/" + Version;
}
