using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace NombaOne;

/// <summary>One priced item on a subscription.</summary>
public sealed class SubscriptionItem
{
    /// <summary>The item id.</summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>The price this item bills (<c>nbo…prc</c>).</summary>
    [JsonPropertyName("priceId")]
    public string PriceId { get; init; } = string.Empty;

    /// <summary>The quantity billed.</summary>
    [JsonPropertyName("quantity")]
    public int Quantity { get; init; }
}

/// <summary>
/// One customer's recurring relationship with one price. The engine bills it
/// every cycle, retries failures through dunning, and reports every transition
/// as a webhook event. Involuntary churn is <c>status: "canceled"</c> with
/// <c>CancellationReason: "involuntary"</c> — there is no separate churned status.
/// </summary>
public sealed class Subscription : NombaoneEntity
{
    /// <summary>Always <c>"subscription"</c>.</summary>
    [JsonPropertyName("domain")]
    public string Domain { get; init; } = "subscription";

    /// <summary>The subscription id (<c>nbo…sub</c>).</summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>The subscriber (<c>nbo…cus</c>).</summary>
    [JsonPropertyName("customerId")]
    public string CustomerId { get; init; } = string.Empty;

    /// <summary>The price being billed (<c>nbo…prc</c>).</summary>
    [JsonPropertyName("priceId")]
    public string PriceId { get; init; } = string.Empty;

    /// <summary>One of <c>incomplete</c>, <c>incomplete_expired</c>, <c>trialing</c>, <c>active</c>, <c>past_due</c>, <c>paused</c>, <c>canceled</c>.</summary>
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    /// <summary>One of <c>charge_automatically</c>, <c>send_invoice</c>.</summary>
    [JsonPropertyName("collectionMethod")]
    public string CollectionMethod { get; init; } = string.Empty;

    /// <summary>The zero-based index of the current billing period.</summary>
    [JsonPropertyName("currentPeriodIndex")]
    public int CurrentPeriodIndex { get; init; }

    /// <summary>Start of the current period, or <c>null</c>.</summary>
    [JsonPropertyName("currentPeriodStart")]
    public DateTimeOffset? CurrentPeriodStart { get; init; }

    /// <summary>End of the current period, or <c>null</c>.</summary>
    [JsonPropertyName("currentPeriodEnd")]
    public DateTimeOffset? CurrentPeriodEnd { get; init; }

    /// <summary>When the trial started, or <c>null</c>.</summary>
    [JsonPropertyName("trialStart")]
    public DateTimeOffset? TrialStart { get; init; }

    /// <summary>When the trial ends, or <c>null</c>.</summary>
    [JsonPropertyName("trialEnd")]
    public DateTimeOffset? TrialEnd { get; init; }

    /// <summary>Whether the subscription is set to cancel at the period end.</summary>
    [JsonPropertyName("cancelAtPeriodEnd")]
    public bool CancelAtPeriodEnd { get; init; }

    /// <summary>When the subscription was canceled, or <c>null</c>.</summary>
    [JsonPropertyName("canceledAt")]
    public DateTimeOffset? CanceledAt { get; init; }

    /// <summary>When the subscription ended, or <c>null</c>.</summary>
    [JsonPropertyName("endedAt")]
    public DateTimeOffset? EndedAt { get; init; }

    /// <summary>Either <c>"voluntary"</c>, <c>"involuntary"</c>, or <c>null</c>.</summary>
    [JsonPropertyName("cancellationReason")]
    public string? CancellationReason { get; init; }

    /// <summary>The default payment method (<c>nbo…pmt</c>), or <c>null</c>.</summary>
    [JsonPropertyName("defaultPaymentMethodId")]
    public string? DefaultPaymentMethodId { get; init; }

    /// <summary>The priced items on this subscription.</summary>
    [JsonPropertyName("items")]
    public IReadOnlyList<SubscriptionItem> Items { get; init; } = Array.Empty<SubscriptionItem>();

    /// <summary>The most recent invoice (<c>nbo…inv</c>), or <c>null</c>.</summary>
    [JsonPropertyName("latestInvoiceId")]
    public string? LatestInvoiceId { get; init; }

    /// <summary>Always <c>"NGN"</c>.</summary>
    [JsonPropertyName("currency")]
    public string Currency { get; init; } = "NGN";

    /// <summary>The environment (<c>"sandbox"</c> or <c>"live"</c>).</summary>
    [JsonPropertyName("mode")]
    public string Mode { get; init; } = string.Empty;

    /// <summary>When the subscription was created.</summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>A preview of the next cycle's invoice — nothing is charged or stored.</summary>
public sealed class UpcomingInvoice : NombaoneEntity
{
    /// <summary>Always <c>"upcoming_invoice"</c>.</summary>
    [JsonPropertyName("domain")]
    public string Domain { get; init; } = "upcoming_invoice";

    /// <summary>The subscription this previews (<c>nbo…sub</c>).</summary>
    [JsonPropertyName("subscriptionId")]
    public string SubscriptionId { get; init; } = string.Empty;

    /// <summary>The period index this invoice would cover.</summary>
    [JsonPropertyName("periodIndex")]
    public int PeriodIndex { get; init; }

    /// <summary>The previewed period start.</summary>
    [JsonPropertyName("periodStart")]
    public DateTimeOffset PeriodStart { get; init; }

    /// <summary>The previewed period end.</summary>
    [JsonPropertyName("periodEnd")]
    public DateTimeOffset PeriodEnd { get; init; }

    /// <summary>The billing reason for this preview.</summary>
    [JsonPropertyName("billingReason")]
    public string BillingReason { get; init; } = string.Empty;

    /// <summary>Sum before discounts/credits, in integer kobo.</summary>
    [JsonPropertyName("subtotalInKobo")]
    public long SubtotalInKobo { get; init; }

    /// <summary>Total after discounts/credits, in integer kobo.</summary>
    [JsonPropertyName("totalInKobo")]
    public long TotalInKobo { get; init; }

    /// <summary>Amount that would be due, in integer kobo.</summary>
    [JsonPropertyName("amountDueInKobo")]
    public long AmountDueInKobo { get; init; }

    /// <summary>Always <c>"NGN"</c>.</summary>
    [JsonPropertyName("currency")]
    public string Currency { get; init; } = "NGN";

    /// <summary>The previewed line items.</summary>
    [JsonPropertyName("lineItems")]
    public IReadOnlyList<InvoiceLineItem> LineItems { get; init; } = Array.Empty<InvoiceLineItem>();

    /// <summary>The environment (<c>"sandbox"</c> or <c>"live"</c>).</summary>
    [JsonPropertyName("mode")]
    public string Mode { get; init; } = string.Empty;
}

/// <summary>One phase of a subscription schedule.</summary>
public sealed class SchedulePhase
{
    /// <summary>The period index this phase begins at.</summary>
    [JsonPropertyName("startIndex")]
    public int StartIndex { get; init; }

    /// <summary>The price this phase switches to (<c>nbo…prc</c>).</summary>
    [JsonPropertyName("priceId")]
    public string PriceId { get; init; } = string.Empty;

    /// <summary>The quantity for this phase, if set.</summary>
    [JsonPropertyName("quantity")]
    public int? Quantity { get; init; }

    /// <summary>When this phase was consumed, or <c>null</c> if still pending.</summary>
    [JsonPropertyName("consumedAt")]
    public DateTimeOffset? ConsumedAt { get; init; }
}

/// <summary>A queued change that applies at a period boundary instead of mid-cycle.</summary>
public sealed class SubscriptionSchedule : NombaoneEntity
{
    /// <summary>Always <c>"subscription_schedule"</c>.</summary>
    [JsonPropertyName("domain")]
    public string Domain { get; init; } = "subscription_schedule";

    /// <summary>The schedule id (<c>nbo…sch</c>).</summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>The subscription this schedule belongs to (<c>nbo…sub</c>).</summary>
    [JsonPropertyName("subscriptionId")]
    public string SubscriptionId { get; init; } = string.Empty;

    /// <summary>One of <c>active</c>, <c>released</c>, <c>canceled</c>.</summary>
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    /// <summary>The scheduled phases.</summary>
    [JsonPropertyName("phases")]
    public IReadOnlyList<SchedulePhase> Phases { get; init; } = Array.Empty<SchedulePhase>();

    /// <summary>The environment (<c>"sandbox"</c> or <c>"live"</c>).</summary>
    [JsonPropertyName("mode")]
    public string Mode { get; init; } = string.Empty;

    /// <summary>When the schedule was created.</summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>When the schedule was last updated.</summary>
    [JsonPropertyName("updatedAt")]
    public DateTimeOffset UpdatedAt { get; init; }
}

/// <summary>One retry in a recovery (dunning) run.</summary>
public sealed class DunningAttempt : NombaoneEntity
{
    /// <summary>Always <c>"dunning_attempt"</c>.</summary>
    [JsonPropertyName("domain")]
    public string Domain { get; init; } = "dunning_attempt";

    /// <summary>The dunning-attempt id (<c>nbo…dun</c>).</summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>The 1-based attempt number.</summary>
    [JsonPropertyName("attemptNumber")]
    public int AttemptNumber { get; init; }

    /// <summary>One of <c>scheduled</c>, <c>attempting</c>, <c>succeeded</c>, <c>rescheduled</c>, <c>card_update_required</c>, <c>exhausted</c>.</summary>
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    /// <summary>One of <c>reschedule</c>, <c>card_update_required</c>, <c>short_path</c>.</summary>
    [JsonPropertyName("branch")]
    public string Branch { get; init; } = string.Empty;

    /// <summary>The rail this attempt used, or <c>null</c>.</summary>
    [JsonPropertyName("railKey")]
    public string? RailKey { get; init; }

    /// <summary>The failure reason, or <c>null</c>.</summary>
    [JsonPropertyName("failureReason")]
    public string? FailureReason { get; init; }

    /// <summary>The gateway message, or <c>null</c>.</summary>
    [JsonPropertyName("gatewayMessage")]
    public string? GatewayMessage { get; init; }

    /// <summary>The outcome, or <c>null</c>.</summary>
    [JsonPropertyName("outcome")]
    public string? Outcome { get; init; }

    /// <summary>When this attempt was scheduled.</summary>
    [JsonPropertyName("scheduledAt")]
    public DateTimeOffset ScheduledAt { get; init; }

    /// <summary>When this attempt executed, or <c>null</c>.</summary>
    [JsonPropertyName("executedAt")]
    public DateTimeOffset? ExecutedAt { get; init; }

    /// <summary>When the next attempt is scheduled, or <c>null</c>.</summary>
    [JsonPropertyName("nextAttemptAt")]
    public DateTimeOffset? NextAttemptAt { get; init; }

    /// <summary>When this attempt record was created.</summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// Where a subscription stands in recovery. <c>past_due</c> is not canceled —
/// read <see cref="GraceAccessUntil"/> before cutting a subscriber off.
/// </summary>
public sealed class DunningState : NombaoneEntity
{
    /// <summary>Always <c>"dunning_state"</c>.</summary>
    [JsonPropertyName("domain")]
    public string Domain { get; init; } = "dunning_state";

    /// <summary>The subscription in recovery (<c>nbo…sub</c>).</summary>
    [JsonPropertyName("subscriptionRef")]
    public string SubscriptionRef { get; init; } = string.Empty;

    /// <summary>The invoice being recovered (<c>nbo…inv</c>), or <c>null</c>.</summary>
    [JsonPropertyName("invoiceRef")]
    public string? InvoiceRef { get; init; }

    /// <summary>The current recovery status, or <c>"none"</c>.</summary>
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    /// <summary>How many attempts have been used.</summary>
    [JsonPropertyName("attemptsUsed")]
    public int AttemptsUsed { get; init; }

    /// <summary>The maximum number of attempts.</summary>
    [JsonPropertyName("maxAttempts")]
    public int MaxAttempts { get; init; }

    /// <summary>When the next attempt is scheduled, or <c>null</c>.</summary>
    [JsonPropertyName("nextAttemptAt")]
    public DateTimeOffset? NextAttemptAt { get; init; }

    /// <summary>Honor access until this time before cutting a <c>past_due</c> subscriber off; <c>null</c> if none.</summary>
    [JsonPropertyName("graceAccessUntil")]
    public DateTimeOffset? GraceAccessUntil { get; init; }

    /// <summary>The recovery attempts so far.</summary>
    [JsonPropertyName("attempts")]
    public IReadOnlyList<DunningAttempt> Attempts { get; init; } = Array.Empty<DunningAttempt>();
}

/// <summary>Parameters for <see cref="SubscriptionsResource.CreateAsync"/>.</summary>
public sealed class SubscriptionCreateParams
{
    /// <summary>The customer to bill (<c>nbo…cus</c>).</summary>
    [JsonPropertyName("customerId")]
    public required string CustomerId { get; init; }

    /// <summary>The price to bill (<c>nbo…prc</c>) — subscriptions reference a price, not a plan.</summary>
    [JsonPropertyName("priceId")]
    public required string PriceId { get; init; }

    /// <summary>Required for <c>charge_automatically</c> unless <see cref="TrialDays"/> &gt; 0 (the first charge is deferred to trial end).</summary>
    [JsonPropertyName("paymentMethodId")]
    public string? PaymentMethodId { get; init; }

    /// <summary>Defaults to <c>charge_automatically</c> server-side.</summary>
    [JsonPropertyName("collectionMethod")]
    public string? CollectionMethod { get; init; }

    /// <summary>Free-trial days before the first charge.</summary>
    [JsonPropertyName("trialDays")]
    public int? TrialDays { get; init; }

    /// <summary>The quantity to bill. Defaults to <c>1</c> server-side.</summary>
    [JsonPropertyName("quantity")]
    public int? Quantity { get; init; }

    /// <summary>Free-form annotations (arbitrary JSON values).</summary>
    [JsonPropertyName("metadata")]
    public IReadOnlyDictionary<string, object?>? Metadata { get; init; }
}

/// <summary>
/// Parameters for <see cref="SubscriptionsResource.UpdateAsync"/> — metadata /
/// default-payment-method edits only. For a price, quantity, or interval change
/// (which prorates), use <see cref="SubscriptionsResource.ChangeAsync"/>. At
/// least one field must be provided.
/// </summary>
public sealed class SubscriptionUpdateParams
{
    /// <summary>A new default payment method (<c>nbo…pmt</c>).</summary>
    [JsonPropertyName("defaultPaymentMethodId")]
    public string? DefaultPaymentMethodId { get; init; }

    /// <summary>Free-form annotations (arbitrary JSON values).</summary>
    [JsonPropertyName("metadata")]
    public IReadOnlyDictionary<string, object?>? Metadata { get; init; }
}

/// <summary>Filters for <see cref="SubscriptionsResource.ListAsync"/>.</summary>
public sealed class SubscriptionListParams
{
    /// <summary>Filter to one customer (<c>nbo…cus</c>).</summary>
    public string? CustomerId { get; init; }

    /// <summary>Filter by status.</summary>
    public string? Status { get; init; }

    /// <summary>Page size, 1–100 (the API default is 20).</summary>
    public int? Limit { get; init; }

    /// <summary>Opaque cursor from a previous page's <c>Pagination.NextCursor</c>.</summary>
    public string? Cursor { get; init; }
}

/// <summary>Parameters for <see cref="SubscriptionsResource.CancelAsync"/>.</summary>
public sealed class SubscriptionCancelParams
{
    /// <summary>Defaults to <c>now</c> server-side; <c>at_period_end</c> keeps access until the cycle closes.</summary>
    [JsonPropertyName("mode")]
    public string? Mode { get; init; }

    /// <summary>An optional comment recorded with the cancellation.</summary>
    [JsonPropertyName("comment")]
    public string? Comment { get; init; }
}

/// <summary>Parameters for <see cref="SubscriptionsResource.PauseAsync"/>.</summary>
public sealed class SubscriptionPauseParams
{
    /// <summary>Auto-resume after this many days.</summary>
    [JsonPropertyName("maxDays")]
    public int? MaxDays { get; init; }
}

/// <summary>Parameters for <see cref="SubscriptionsResource.ResubscribeAsync"/>.</summary>
public sealed class SubscriptionResubscribeParams
{
    /// <summary>Defaults to the previous price (<c>nbo…prc</c>).</summary>
    [JsonPropertyName("priceId")]
    public string? PriceId { get; init; }

    /// <summary>Defaults to the previous payment method (<c>nbo…pmt</c>).</summary>
    [JsonPropertyName("paymentMethodId")]
    public string? PaymentMethodId { get; init; }
}

/// <summary>
/// Parameters for <see cref="SubscriptionsResource.ChangeAsync"/>. At least one
/// of <see cref="PriceId"/>, <see cref="Quantity"/>, or <see cref="IntervalSwitch"/> is required.
/// </summary>
public sealed class SubscriptionChangeParams
{
    /// <summary>A new price (<c>nbo…prc</c>).</summary>
    [JsonPropertyName("priceId")]
    public string? PriceId { get; init; }

    /// <summary>A new quantity.</summary>
    [JsonPropertyName("quantity")]
    public int? Quantity { get; init; }

    /// <summary>Whether this is an interval switch.</summary>
    [JsonPropertyName("intervalSwitch")]
    public bool? IntervalSwitch { get; init; }

    /// <summary>Defaults to <c>create_prorations</c> server-side; <c>none</c> skips proration.</summary>
    [JsonPropertyName("prorationBehavior")]
    public string? ProrationBehavior { get; init; }
}

/// <summary>
/// Parameters for <see cref="SubscriptionsResource.UpdatePaymentMethodAsync"/>.
/// Provide exactly one of <see cref="PaymentMethodReference"/> or <see cref="CheckoutToken"/>.
/// </summary>
public sealed class SubscriptionUpdatePaymentMethodParams
{
    /// <summary>An already-captured payment method (<c>nbo…pmt</c>).</summary>
    [JsonPropertyName("paymentMethodReference")]
    public string? PaymentMethodReference { get; init; }

    /// <summary>A fresh hosted-checkout token — attaches and swaps atomically.</summary>
    [JsonPropertyName("checkoutToken")]
    public string? CheckoutToken { get; init; }
}

/// <summary>Parameters for <see cref="SubscriptionScheduleResource.CreateAsync"/>.</summary>
public sealed class SubscriptionScheduleCreateParams
{
    /// <summary>The price to switch to at the boundary (<c>nbo…prc</c>).</summary>
    [JsonPropertyName("priceId")]
    public required string PriceId { get; init; }

    /// <summary>The quantity at the boundary.</summary>
    [JsonPropertyName("quantity")]
    public int? Quantity { get; init; }

    /// <summary>Defaults to <c>next_cycle</c> server-side (the only mode today).</summary>
    [JsonPropertyName("effectiveAt")]
    public string? EffectiveAt { get; init; }
}

/// <summary>Parameters for <see cref="SubscriptionsResource.ApplyDiscountAsync"/>.</summary>
public sealed class SubscriptionApplyDiscountParams
{
    /// <summary>A coupon id (<c>nbo…cpn</c>) or its code.</summary>
    [JsonPropertyName("coupon")]
    public required string Coupon { get; init; }
}

/// <summary>Filters for <see cref="SubscriptionsResource.ListEventsAsync"/> and dunning attempt lists.</summary>
public sealed class SubscriptionListEventsParams
{
    /// <summary>Page size, 1–100 (the API default is 20).</summary>
    public int? Limit { get; init; }

    /// <summary>Opaque cursor from a previous page's <c>Pagination.NextCursor</c>.</summary>
    public string? Cursor { get; init; }
}

/// <summary>Scheduled (next-cycle) changes queued against a subscription (<c>…/schedule</c>).</summary>
public sealed class SubscriptionScheduleResource : NombaoneResource
{
    internal SubscriptionScheduleResource(Nombaone client)
        : base(client)
    {
    }

    /// <summary>
    /// Queue a change for the next cycle boundary — the safe way to switch
    /// billing intervals (mid-cycle interval proration is unsupported).
    /// </summary>
    /// <param name="subscriptionId">The subscription id (<c>nbo…sub</c>).</param>
    /// <param name="parameters">The scheduled change.</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <exception cref="ConflictException">409 <c>SUBSCRIPTION_SCHEDULE_CONFLICT</c>.</exception>
    public Task<SubscriptionSchedule> CreateAsync(string subscriptionId, SubscriptionScheduleCreateParams parameters, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        PostAsync<SubscriptionSchedule>($"/subscriptions/{Seg(subscriptionId)}/schedule", parameters, options, cancellationToken);

    /// <summary>Retrieve the subscription's schedule.</summary>
    /// <param name="subscriptionId">The subscription id (<c>nbo…sub</c>).</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <exception cref="NotFoundException">404 <c>SUBSCRIPTION_SCHEDULE_NOT_FOUND</c>.</exception>
    public Task<SubscriptionSchedule> RetrieveAsync(string subscriptionId, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        GetAsync<SubscriptionSchedule>($"/subscriptions/{Seg(subscriptionId)}/schedule", options, cancellationToken);

    /// <summary>Cancel the pending schedule before it applies.</summary>
    /// <param name="subscriptionId">The subscription id (<c>nbo…sub</c>).</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<SubscriptionSchedule> ReleaseAsync(string subscriptionId, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        DeleteAsync<SubscriptionSchedule>($"/subscriptions/{Seg(subscriptionId)}/schedule", options, cancellationToken);
}

/// <summary>Read-only view into a subscription's recovery state (<c>…/dunning</c>).</summary>
public sealed class SubscriptionDunningResource : NombaoneResource
{
    internal SubscriptionDunningResource(Nombaone client)
        : base(client)
    {
    }

    /// <summary>
    /// Where the subscription stands in dunning. Check <c>GraceAccessUntil</c>
    /// before cutting access — <c>past_due</c> usually means "not yet", not "no".
    /// </summary>
    /// <param name="subscriptionId">The subscription id (<c>nbo…sub</c>).</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<DunningState> RetrieveAsync(string subscriptionId, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        GetAsync<DunningState>($"/subscriptions/{Seg(subscriptionId)}/dunning", options, cancellationToken);

    /// <summary>List every recovery attempt, newest first.</summary>
    /// <param name="subscriptionId">The subscription id (<c>nbo…sub</c>).</param>
    /// <param name="parameters">Optional page size and cursor.</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<NombaonePage<DunningAttempt>> ListAttemptsAsync(string subscriptionId, SubscriptionListEventsParams? parameters = null, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        ListAsync<DunningAttempt>($"/subscriptions/{Seg(subscriptionId)}/dunning/attempts", BuildPageQuery(parameters), options, cancellationToken);

    /// <summary>List recovery attempts as an async stream, fetching pages for you as you iterate.</summary>
    /// <param name="subscriptionId">The subscription id (<c>nbo…sub</c>).</param>
    /// <param name="parameters">Optional page size and cursor.</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Stops iteration and cancels in-flight fetches.</param>
    public IAsyncEnumerable<DunningAttempt> ListAttemptsAutoPagingAsync(string subscriptionId, SubscriptionListEventsParams? parameters = null, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        ListAutoPagingAsync<DunningAttempt>($"/subscriptions/{Seg(subscriptionId)}/dunning/attempts", BuildPageQuery(parameters), options, cancellationToken);

    internal static IReadOnlyDictionary<string, string?> BuildPageQuery(SubscriptionListEventsParams? parameters) => new Dictionary<string, string?>
    {
        ["limit"] = parameters?.Limit?.ToString(CultureInfo.InvariantCulture),
        ["cursor"] = parameters?.Cursor,
    };
}

/// <summary>
/// Subscriptions — the core object. Create one against a customer and a price;
/// the engine handles cycles, invoices, retries, and recovery.
/// </summary>
/// <example>
/// <code>
/// var subscription = await nombaone.Subscriptions.CreateAsync(new SubscriptionCreateParams
/// {
///     CustomerId = customer.Id,
///     PriceId = price.Id,
///     PaymentMethodId = paymentMethod.Id,
/// });
/// Console.WriteLine(subscription.Status); // "active"
/// </code>
/// </example>
public sealed class SubscriptionsResource : NombaoneResource
{
    internal SubscriptionsResource(Nombaone client)
        : base(client)
    {
        Schedule = new SubscriptionScheduleResource(client);
        Dunning = new SubscriptionDunningResource(client);
    }

    /// <summary>Scheduled (next-cycle) changes.</summary>
    public SubscriptionScheduleResource Schedule { get; }

    /// <summary>Recovery/dunning state (read-only).</summary>
    public SubscriptionDunningResource Dunning { get; }

    /// <summary>
    /// Create a subscription. This can move money (the first charge), so the SDK
    /// sends an <c>Idempotency-Key</c> automatically and reuses it across its own
    /// retries.
    /// </summary>
    /// <param name="parameters">The subscription to create.</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <exception cref="ValidationException">422 — e.g. a missing payment method without a trial.</exception>
    /// <exception cref="ConflictException">409 <c>SUBSCRIPTION_PAYMENT_METHOD_REQUIRED</c>.</exception>
    public Task<Subscription> CreateAsync(SubscriptionCreateParams parameters, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        PostAsync<Subscription>("/subscriptions", parameters, options, cancellationToken);

    /// <summary>Retrieve a subscription by id.</summary>
    /// <param name="id">The subscription id (<c>nbo…sub</c>).</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <exception cref="NotFoundException">404 <c>SUBSCRIPTION_NOT_FOUND</c>.</exception>
    public Task<Subscription> RetrieveAsync(string id, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        GetAsync<Subscription>($"/subscriptions/{Seg(id)}", options, cancellationToken);

    /// <summary>
    /// Edit metadata or the default payment method. For price/quantity/interval
    /// changes use <see cref="ChangeAsync"/> — those prorate.
    /// </summary>
    /// <param name="id">The subscription id (<c>nbo…sub</c>).</param>
    /// <param name="parameters">The fields to change.</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<Subscription> UpdateAsync(string id, SubscriptionUpdateParams parameters, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        PatchAsync<Subscription>($"/subscriptions/{Seg(id)}", parameters, options, cancellationToken);

    /// <summary>List subscriptions, newest first.</summary>
    /// <param name="parameters">Optional filters and page size.</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<NombaonePage<Subscription>> ListAsync(SubscriptionListParams? parameters = null, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        ListAsync<Subscription>("/subscriptions", BuildListQuery(parameters), options, cancellationToken);

    /// <summary>List subscriptions as an async stream, fetching pages for you as you iterate.</summary>
    /// <param name="parameters">Optional filters and page size.</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Stops iteration and cancels in-flight fetches.</param>
    public IAsyncEnumerable<Subscription> ListAutoPagingAsync(SubscriptionListParams? parameters = null, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        ListAutoPagingAsync<Subscription>("/subscriptions", BuildListQuery(parameters), options, cancellationToken);

    /// <summary>The subscription's audit trail of domain events, newest first.</summary>
    /// <param name="id">The subscription id (<c>nbo…sub</c>).</param>
    /// <param name="parameters">Optional page size and cursor.</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<NombaonePage<DomainEvent>> ListEventsAsync(string id, SubscriptionListEventsParams? parameters = null, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        ListAsync<DomainEvent>($"/subscriptions/{Seg(id)}/events", SubscriptionDunningResource.BuildPageQuery(parameters), options, cancellationToken);

    /// <summary>The subscription's domain events as an async stream, fetching pages for you as you iterate.</summary>
    /// <param name="id">The subscription id (<c>nbo…sub</c>).</param>
    /// <param name="parameters">Optional page size and cursor.</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Stops iteration and cancels in-flight fetches.</param>
    public IAsyncEnumerable<DomainEvent> ListEventsAutoPagingAsync(string id, SubscriptionListEventsParams? parameters = null, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        ListAutoPagingAsync<DomainEvent>($"/subscriptions/{Seg(id)}/events", SubscriptionDunningResource.BuildPageQuery(parameters), options, cancellationToken);

    /// <summary>Pause billing. The subscription keeps its place in the cycle and resumes cleanly.</summary>
    /// <param name="id">The subscription id (<c>nbo…sub</c>).</param>
    /// <param name="parameters">Optional pause settings.</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <exception cref="ConflictException">409 <c>SUBSCRIPTION_ILLEGAL_TRANSITION</c>.</exception>
    public Task<Subscription> PauseAsync(string id, SubscriptionPauseParams? parameters = null, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        PostAsync<Subscription>($"/subscriptions/{Seg(id)}/pause", (object?)parameters ?? EmptyBody, options, cancellationToken);

    /// <summary>Resume a paused subscription.</summary>
    /// <param name="id">The subscription id (<c>nbo…sub</c>).</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<Subscription> ResumeAsync(string id, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        PostAsync<Subscription>($"/subscriptions/{Seg(id)}/resume", EmptyBody, options, cancellationToken);

    /// <summary>Cancel a subscription — immediately (default) or at period end.</summary>
    /// <param name="id">The subscription id (<c>nbo…sub</c>).</param>
    /// <param name="parameters">Optional cancellation settings.</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <example>
    /// <code>
    /// await nombaone.Subscriptions.CancelAsync(subscription.Id, new SubscriptionCancelParams { Mode = "at_period_end" });
    /// </code>
    /// </example>
    public Task<Subscription> CancelAsync(string id, SubscriptionCancelParams? parameters = null, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        PostAsync<Subscription>($"/subscriptions/{Seg(id)}/cancel", (object?)parameters ?? EmptyBody, options, cancellationToken);

    /// <summary>
    /// Start a fresh subscription for a canceled one's customer, reusing the old
    /// price/payment method unless overridden. The subscription must be in a
    /// terminal state.
    /// </summary>
    /// <param name="id">The subscription id (<c>nbo…sub</c>).</param>
    /// <param name="parameters">Optional overrides.</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <exception cref="ConflictException">409 <c>SUBSCRIPTION_NOT_TERMINAL</c>.</exception>
    public Task<Subscription> ResubscribeAsync(string id, SubscriptionResubscribeParams? parameters = null, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        PostAsync<Subscription>($"/subscriptions/{Seg(id)}/resubscribe", (object?)parameters ?? EmptyBody, options, cancellationToken);

    /// <summary>
    /// Change price or quantity mid-cycle, prorating by default. Switching the
    /// billing interval mid-cycle is unsupported
    /// (<c>PRORATION_INTERVAL_SWITCH_UNSUPPORTED</c>) — queue it with
    /// <see cref="SubscriptionScheduleResource.CreateAsync"/> instead.
    /// </summary>
    /// <param name="id">The subscription id (<c>nbo…sub</c>).</param>
    /// <param name="parameters">The change to apply.</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <example>
    /// <code>
    /// await nombaone.Subscriptions.ChangeAsync(subscription.Id, new SubscriptionChangeParams { PriceId = biggerPrice.Id });
    /// </code>
    /// </example>
    public Task<Subscription> ChangeAsync(string id, SubscriptionChangeParams parameters, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        PostAsync<Subscription>($"/subscriptions/{Seg(id)}/change", parameters, options, cancellationToken);

    /// <summary>
    /// Swap the payment method that bills this subscription — the card-update path
    /// during dunning. Provide exactly one of <c>PaymentMethodReference</c> or
    /// <c>CheckoutToken</c>. Returns the attached <see cref="PaymentMethod"/> (not
    /// the subscription).
    /// </summary>
    /// <param name="id">The subscription id (<c>nbo…sub</c>).</param>
    /// <param name="parameters">The new payment method.</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<PaymentMethod> UpdatePaymentMethodAsync(string id, SubscriptionUpdatePaymentMethodParams parameters, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        PostAsync<PaymentMethod>($"/subscriptions/{Seg(id)}/payment-method", parameters, options, cancellationToken);

    /// <summary>Preview the next invoice without charging or storing anything.</summary>
    /// <param name="id">The subscription id (<c>nbo…sub</c>).</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<UpcomingInvoice> RetrieveUpcomingInvoiceAsync(string id, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        GetAsync<UpcomingInvoice>($"/subscriptions/{Seg(id)}/upcoming-invoice", options, cancellationToken);

    /// <summary>Apply a coupon to this subscription only.</summary>
    /// <param name="id">The subscription id (<c>nbo…sub</c>).</param>
    /// <param name="parameters">The coupon to apply.</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<Discount> ApplyDiscountAsync(string id, SubscriptionApplyDiscountParams parameters, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        PostAsync<Discount>($"/subscriptions/{Seg(id)}/discount", parameters, options, cancellationToken);

    /// <summary>Remove the subscription's active discount. Returns the ended discount.</summary>
    /// <param name="id">The subscription id (<c>nbo…sub</c>).</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<Discount> RemoveDiscountAsync(string id, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        DeleteAsync<Discount>($"/subscriptions/{Seg(id)}/discount", options, cancellationToken);

    private static IReadOnlyDictionary<string, string?> BuildListQuery(SubscriptionListParams? parameters) => new Dictionary<string, string?>
    {
        ["customerId"] = parameters?.CustomerId,
        ["status"] = parameters?.Status,
        ["limit"] = parameters?.Limit?.ToString(CultureInfo.InvariantCulture),
        ["cursor"] = parameters?.Cursor,
    };
}
