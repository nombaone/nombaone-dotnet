using System;

namespace NombaOne;

/// <summary>
/// The base type for everything this SDK throws — API failures, connection
/// problems, webhook-verification failures, and client misconfiguration.
/// Catch this to handle any NombaOne error uniformly.
/// </summary>
/// <example>
/// <code>
/// try
/// {
///     await nombaone.Subscriptions.CreateAsync(subscriptionParams);
/// }
/// catch (NombaoneException ex)
/// {
///     Console.Error.WriteLine(ex.Message);
/// }
/// </code>
/// </example>
public class NombaoneException : Exception
{
    /// <summary>Creates a new <see cref="NombaoneException"/>.</summary>
    /// <param name="message">The human-readable error message.</param>
    /// <param name="innerException">The underlying cause, if any.</param>
    public NombaoneException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// The request never completed at the transport layer — DNS failure, connection
/// reset, TLS error, or a caller-initiated cancellation. A cancellation
/// initiated by your own <see cref="System.Threading.CancellationToken"/> is
/// never retried; genuine network faults are.
/// </summary>
public class NombaoneConnectionException : NombaoneException
{
    /// <summary>Creates a new <see cref="NombaoneConnectionException"/>.</summary>
    /// <param name="message">The human-readable error message.</param>
    /// <param name="innerException">The underlying transport exception, if any.</param>
    public NombaoneConnectionException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// A single request attempt exceeded its per-attempt timeout budget. Retried
/// automatically (up to the configured retry budget); surfaced only once the
/// budget is exhausted. Distinct from a caller cancellation.
/// </summary>
public class NombaoneTimeoutException : NombaoneConnectionException
{
    /// <summary>Creates a new <see cref="NombaoneTimeoutException"/>.</summary>
    /// <param name="message">The human-readable error message.</param>
    /// <param name="innerException">The underlying timeout/cancellation exception, if any.</param>
    public NombaoneTimeoutException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Webhook signature or timestamp verification failed — a missing/malformed
/// header, a stale or future timestamp, an invalid signature, a missing secret,
/// or a non-JSON body. Reject the delivery when this is thrown.
/// </summary>
public class WebhookVerificationException : NombaoneException
{
    /// <summary>Creates a new <see cref="WebhookVerificationException"/>.</summary>
    /// <param name="message">The human-readable error message.</param>
    /// <param name="innerException">The underlying cause, if any.</param>
    public WebhookVerificationException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
