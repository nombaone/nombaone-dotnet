using System.Text.Json.Serialization;

namespace NombaOne;

/// <summary>
/// The base type for every resource the SDK returns. Awaiting a call resolves
/// straight to the typed resource; when you also need the request id or raw
/// response headers, read <see cref="RawResponse"/> (or the
/// <see cref="RequestId"/> shortcut) off the returned object.
/// </summary>
public abstract class NombaoneEntity
{
    /// <summary>
    /// Metadata about the HTTP response this object was decoded from — status,
    /// request id, and headers. Populated by the SDK; not part of the wire body.
    /// </summary>
    [JsonIgnore]
    public NombaoneResponse? RawResponse { get; internal set; }

    /// <summary>
    /// The request id for the call that produced this object — quote it to
    /// support. Shortcut for <c>RawResponse?.RequestId</c>.
    /// </summary>
    [JsonIgnore]
    public string? RequestId => RawResponse?.RequestId;
}
