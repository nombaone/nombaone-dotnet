using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NombaOne.Internal;

namespace NombaOne;

/// <summary>
/// The NombaOne API client. Construct it once with your secret key and reach
/// the API through its resource namespaces (<c>Customers</c>,
/// <c>Subscriptions</c>, …). Instances are thread-safe and intended to be
/// long-lived and reused.
/// </summary>
/// <example>
/// <code>
/// using NombaOne;
///
/// var nombaone = new Nombaone(); // reads NOMBAONE_API_KEY
///
/// var subscription = await nombaone.Subscriptions.CreateAsync(new SubscriptionCreateParams
/// {
///     CustomerId = "nbo123456789012cus",
///     PriceId = "nbo123456789012prc",
/// });
/// </code>
/// </example>
public sealed partial class Nombaone : IDisposable
{
    /// <summary>The default host for sandbox keys (<c>nbo_sandbox_…</c>).</summary>
    public const string SandboxBaseUrl = "https://sandbox.api.nombaone.xyz";

    /// <summary>The default host for live keys (<c>nbo_live_…</c>).</summary>
    public const string LiveBaseUrl = "https://api.nombaone.xyz";

    private const string ApiKeyEnvVar = "NOMBAONE_API_KEY";
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
    private const int DefaultMaxRetries = 2;

    private readonly HttpTransport _transport;
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;

    /// <summary>Construct a client, reading the key from the <c>NOMBAONE_API_KEY</c> environment variable.</summary>
    public Nombaone()
        : this(new NombaoneOptions())
    {
    }

    /// <summary>Construct a client with an explicit API key.</summary>
    /// <param name="apiKey">Your secret key (<c>nbo_sandbox_…</c> or <c>nbo_live_…</c>).</param>
    public Nombaone(string apiKey)
        : this(new NombaoneOptions { ApiKey = apiKey })
    {
    }

    /// <summary>Construct a client with full options.</summary>
    /// <param name="options">The client configuration.</param>
    /// <exception cref="NombaoneException">
    /// Thrown when no API key is available, or when the key's prefix is
    /// unrecognized and no <see cref="NombaoneOptions.BaseUrl"/> was supplied.
    /// </exception>
    public Nombaone(NombaoneOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var apiKey = string.IsNullOrEmpty(options.ApiKey)
            ? Environment.GetEnvironmentVariable(ApiKeyEnvVar)
            : options.ApiKey;

        if (string.IsNullOrEmpty(apiKey))
        {
            throw new NombaoneException(
                "Missing API key — set the NOMBAONE_API_KEY environment variable, or pass one: " +
                "new Nombaone(\"nbo_sandbox_…\"). Create keys in the dashboard under API keys.");
        }

        var mode = DeriveMode(apiKey!);
        if (mode is null && string.IsNullOrEmpty(options.BaseUrl))
        {
            throw new NombaoneException(
                "Unrecognized API key format — expected a key starting with \"nbo_sandbox_\" or " +
                "\"nbo_live_\". Copy the key exactly as shown in the dashboard, or pass an explicit " +
                "BaseUrl if you are targeting a custom host.");
        }

        Mode = mode ?? "sandbox";
        BaseUrl = (string.IsNullOrEmpty(options.BaseUrl) ? DefaultBaseUrl(Mode) : options.BaseUrl!)
            .TrimEnd('/');

        if (options.HttpClient is not null)
        {
            _httpClient = options.HttpClient;
            _ownsHttpClient = false;
        }
        else
        {
            // Per-attempt timeouts are enforced via cancellation, so the HttpClient
            // itself must never time out.
            _httpClient = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
            _ownsHttpClient = true;
        }

        _transport = new HttpTransport(
            _httpClient,
            apiKey!,
            BaseUrl,
            options.Timeout ?? DefaultTimeout,
            options.MaxRetries ?? DefaultMaxRetries,
            options.DefaultHeaders);

        InitializeResources();
    }

    /// <summary>The environment this client talks to (<c>"sandbox"</c> or <c>"live"</c>), derived from the key prefix.</summary>
    public string Mode { get; }

    /// <summary>The API origin in use (no <c>/v1</c>).</summary>
    public string BaseUrl { get; }

    /// <summary>Dispose the internally-created <see cref="HttpClient"/> (a caller-supplied one is left untouched).</summary>
    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }

    // Wires up resource namespaces. Extended by each phase (partial method).
    partial void InitializeResources();

    /// <summary>Internal — used by resource classes to issue a single-resource call.</summary>
    internal async Task<T> SendAsync<T>(RequestSpec spec, CancellationToken cancellationToken)
    {
        var result = await _transport.SendAsync<T>(spec, cancellationToken).ConfigureAwait(false);
        return result.Data;
    }

    /// <summary>Internal — used by resource classes that also need the response envelope (pagination, request id).</summary>
    internal Task<TransportResult<T>> SendRawAsync<T>(RequestSpec spec, CancellationToken cancellationToken) =>
        _transport.SendAsync<T>(spec, cancellationToken);

    private static string? DeriveMode(string apiKey)
    {
        if (apiKey.StartsWith("nbo_sandbox_", StringComparison.Ordinal))
        {
            return "sandbox";
        }

        if (apiKey.StartsWith("nbo_live_", StringComparison.Ordinal))
        {
            return "live";
        }

        return null;
    }

    private static string DefaultBaseUrl(string mode) =>
        mode == "live" ? LiveBaseUrl : SandboxBaseUrl;
}
