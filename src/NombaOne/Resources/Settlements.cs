using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace NombaOne;

/// <summary>The integer-kobo split of one collection into fee + tenant share.</summary>
public sealed class Settlement : NombaoneEntity
{
    /// <summary>Always <c>"settlement"</c>.</summary>
    [JsonPropertyName("domain")]
    public string Domain { get; init; } = "settlement";

    /// <summary>The settlement id (<c>nbo…stl</c>).</summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>The invoice this settlement came from (<c>nbo…inv</c>), or <c>null</c>.</summary>
    [JsonPropertyName("invoiceReference")]
    public string? InvoiceReference { get; init; }

    /// <summary>The sub-account this settlement lands in.</summary>
    [JsonPropertyName("subAccountRef")]
    public string SubAccountRef { get; init; } = string.Empty;

    /// <summary>The split reference, or <c>null</c>.</summary>
    [JsonPropertyName("splitReference")]
    public string? SplitReference { get; init; }

    /// <summary>Your durable merchant transaction reference.</summary>
    [JsonPropertyName("merchantTxRef")]
    public string MerchantTxRef { get; init; } = string.Empty;

    /// <summary>The gross collected amount, in integer kobo.</summary>
    [JsonPropertyName("grossInKobo")]
    public long GrossInKobo { get; init; }

    /// <summary>The (non-refundable) platform fee, in integer kobo.</summary>
    [JsonPropertyName("platformFeeInKobo")]
    public long PlatformFeeInKobo { get; init; }

    /// <summary>The net amount to the tenant, in integer kobo.</summary>
    [JsonPropertyName("netToTenantInKobo")]
    public long NetToTenantInKobo { get; init; }

    /// <summary>One of <c>pending</c>, <c>settled</c>, <c>reconciled</c>, <c>failed</c>, <c>refunded</c>.</summary>
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    /// <summary>When the settlement was created.</summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>A refund of a settlement's tenant share (the platform fee stays).</summary>
public sealed class Refund : NombaoneEntity
{
    /// <summary>Always <c>"refund"</c>.</summary>
    [JsonPropertyName("domain")]
    public string Domain { get; init; } = "refund";

    /// <summary>The refund id (<c>nbo…ref</c>).</summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>The settlement refunded (<c>nbo…stl</c>).</summary>
    [JsonPropertyName("settlementReference")]
    public string SettlementReference { get; init; } = string.Empty;

    /// <summary>The sub-account debited.</summary>
    [JsonPropertyName("subAccountRef")]
    public string SubAccountRef { get; init; } = string.Empty;

    /// <summary>The refunded amount, in integer kobo.</summary>
    [JsonPropertyName("amountInKobo")]
    public long AmountInKobo { get; init; }

    /// <summary>One of <c>pending</c>, <c>ledger_only</c>, <c>succeeded</c>, <c>failed</c>.</summary>
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    /// <summary>The provider-side reference, or <c>null</c>.</summary>
    [JsonPropertyName("providerReference")]
    public string? ProviderReference { get; init; }

    /// <summary>When the refund was created.</summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>A withdrawal of settled funds to your bank account.</summary>
public sealed class Payout : NombaoneEntity
{
    /// <summary>Always <c>"payout"</c>.</summary>
    [JsonPropertyName("domain")]
    public string Domain { get; init; } = "payout";

    /// <summary>The payout id (<c>nbo…pay</c>).</summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>The sub-account debited.</summary>
    [JsonPropertyName("subAccountRef")]
    public string SubAccountRef { get; init; } = string.Empty;

    /// <summary>The payout amount, in integer kobo.</summary>
    [JsonPropertyName("amountInKobo")]
    public long AmountInKobo { get; init; }

    /// <summary>The destination CBN bank code.</summary>
    [JsonPropertyName("bankCode")]
    public string BankCode { get; init; } = string.Empty;

    /// <summary>The destination account number.</summary>
    [JsonPropertyName("accountNumber")]
    public string AccountNumber { get; init; } = string.Empty;

    /// <summary>The resolved account name, or <c>null</c>.</summary>
    [JsonPropertyName("resolvedAccountName")]
    public string? ResolvedAccountName { get; init; }

    /// <summary>One of <c>pending</c>, <c>ledger_posted</c>, <c>succeeded</c>, <c>failed</c>.</summary>
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    /// <summary>The provider-side reference, or <c>null</c>.</summary>
    [JsonPropertyName("providerReference")]
    public string? ProviderReference { get; init; }

    /// <summary>The failure reason, or <c>null</c>.</summary>
    [JsonPropertyName("failureReason")]
    public string? FailureReason { get; init; }

    /// <summary>When the payout was created.</summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>Your escrow lock and what is actually withdrawable right now.</summary>
public sealed class Escrow : NombaoneEntity
{
    /// <summary>Always <c>"escrow"</c>.</summary>
    [JsonPropertyName("domain")]
    public string Domain { get; init; } = "escrow";

    /// <summary>The amount currently locked, in integer kobo.</summary>
    [JsonPropertyName("lockedInKobo")]
    public long LockedInKobo { get; init; }

    /// <summary>When the lock began.</summary>
    [JsonPropertyName("since")]
    public DateTimeOffset Since { get; init; }

    /// <summary>The total balance, in integer kobo.</summary>
    [JsonPropertyName("balanceInKobo")]
    public long BalanceInKobo { get; init; }

    /// <summary>The minimum withdrawable amount, in integer kobo.</summary>
    [JsonPropertyName("minWithdrawableInKobo")]
    public long MinWithdrawableInKobo { get; init; }

    /// <summary>The amount available to withdraw right now, in integer kobo.</summary>
    [JsonPropertyName("availableInKobo")]
    public long AvailableInKobo { get; init; }
}

/// <summary>Filters for <see cref="SettlementsResource.ListAsync"/>.</summary>
public sealed class SettlementListParams
{
    /// <summary>Filter by status.</summary>
    public string? Status { get; init; }

    /// <summary>Page size, 1–100 (the API default is 20).</summary>
    public int? Limit { get; init; }

    /// <summary>Opaque cursor from a previous page's <c>Pagination.NextCursor</c>.</summary>
    public string? Cursor { get; init; }
}

/// <summary>Parameters for <see cref="SettlementsResource.RefundAsync"/>.</summary>
public sealed class SettlementRefundParams
{
    /// <summary>Amount in integer kobo. Defaults server-side to the full remaining refundable amount.</summary>
    [JsonPropertyName("amountInKobo")]
    public long? AmountInKobo { get; init; }
}

/// <summary>Parameters for <see cref="SettlementsResource.CreatePayoutAsync"/>.</summary>
public sealed class PayoutCreateParams
{
    /// <summary>Amount to withdraw, in integer kobo (₦1.00 = 100).</summary>
    [JsonPropertyName("amountInKobo")]
    public required long AmountInKobo { get; init; }

    /// <summary>The destination CBN 3-digit bank code.</summary>
    [JsonPropertyName("bankCode")]
    public required string BankCode { get; init; }

    /// <summary>The destination account number.</summary>
    [JsonPropertyName("accountNumber")]
    public required string AccountNumber { get; init; }
}

/// <summary>
/// Settlements — where collected money lands, and how it leaves (refunds,
/// payouts) under the escrow lock.
/// </summary>
public sealed class SettlementsResource : NombaoneResource
{
    internal SettlementsResource(Nombaone client)
        : base(client)
    {
    }

    /// <summary>Retrieve a settlement by id.</summary>
    /// <param name="id">The settlement id (<c>nbo…stl</c>).</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <exception cref="NotFoundException">404 <c>SETTLEMENT_NOT_FOUND</c>.</exception>
    public Task<Settlement> RetrieveAsync(string id, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        GetAsync<Settlement>($"/settlements/{Seg(id)}", options, cancellationToken);

    /// <summary>List settlements, newest first.</summary>
    /// <param name="parameters">Optional filters and page size.</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<NombaonePage<Settlement>> ListAsync(SettlementListParams? parameters = null, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        ListAsync<Settlement>("/settlements", BuildListQuery(parameters), options, cancellationToken);

    /// <summary>List settlements as an async stream, fetching pages for you as you iterate.</summary>
    /// <param name="parameters">Optional filters and page size.</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Stops iteration and cancels in-flight fetches.</param>
    public IAsyncEnumerable<Settlement> ListAutoPagingAsync(SettlementListParams? parameters = null, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        ListAutoPagingAsync<Settlement>("/settlements", BuildListQuery(parameters), options, cancellationToken);

    /// <summary>Your escrow lock and available-to-withdraw balance.</summary>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<Escrow> RetrieveEscrowAsync(RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        GetAsync<Escrow>("/settlements/escrow", options, cancellationToken);

    /// <summary>
    /// Refund a settlement's tenant share. The platform fee is never refunded.
    /// Money moves here, so the SDK sends an <c>Idempotency-Key</c> automatically;
    /// pass your own stable <c>options.IdempotencyKey</c> so a retry from a new
    /// process cannot refund twice.
    /// </summary>
    /// <param name="id">The settlement id (<c>nbo…stl</c>).</param>
    /// <param name="parameters">Optional partial-refund amount (kobo).</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <exception cref="ConflictException">409 <c>REFUND_ALREADY_REFUNDED</c>.</exception>
    /// <exception cref="ValidationException">422 <c>REFUND_AMOUNT_EXCEEDS_NET</c>.</exception>
    public Task<Refund> RefundAsync(string id, SettlementRefundParams? parameters = null, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        PostAsync<Refund>($"/settlements/{Seg(id)}/refund", (object?)parameters ?? EmptyBody, options, cancellationToken);

    /// <summary>
    /// Withdraw settled funds to your bank account. <b>Money moves here, and the
    /// <c>Idempotency-Key</c> doubles as the payout's durable merchant reference.</b>
    /// Always pass an explicit, stable <c>options.IdempotencyKey</c> (e.g. your own
    /// payout id) — an auto-generated key protects SDK-level retries, but a
    /// brand-new process retrying with a fresh key would create a second payout.
    /// </summary>
    /// <param name="parameters">The amount (kobo), bank code, and account number.</param>
    /// <param name="options">Per-call options — pass a stable <c>IdempotencyKey</c>.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <exception cref="ConflictException">409 <c>ESCROW_LOCKED</c>.</exception>
    /// <exception cref="ValidationException">422 <c>PAYOUT_EXCEEDS_AVAILABLE</c>.</exception>
    /// <example>
    /// <code>
    /// var payout = await nombaone.Settlements.CreatePayoutAsync(
    ///     new PayoutCreateParams { AmountInKobo = 5_000_000, BankCode = "058", AccountNumber = "0123456789" },
    ///     new RequestOptions { IdempotencyKey = $"payout-{myPayoutRow.Id}" });
    /// </code>
    /// </example>
    public Task<Payout> CreatePayoutAsync(PayoutCreateParams parameters, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        PostAsync<Payout>("/settlements/payout", parameters, options, cancellationToken);

    private static IReadOnlyDictionary<string, string?> BuildListQuery(SettlementListParams? parameters) => new Dictionary<string, string?>
    {
        ["status"] = parameters?.Status,
        ["limit"] = parameters?.Limit?.ToString(CultureInfo.InvariantCulture),
        ["cursor"] = parameters?.Cursor,
    };
}
