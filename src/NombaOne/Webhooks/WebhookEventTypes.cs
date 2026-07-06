namespace NombaOne.Webhooks;

/// <summary>
/// The webhook event types the platform emits, for use in a <c>switch</c> on
/// <see cref="WebhookEvent.Type"/>. The catalog is <b>open</b>: an event type
/// added by the API tomorrow still arrives as a plain string on
/// <see cref="WebhookEvent.Type"/> even if it is not listed here.
/// </summary>
public static class WebhookEventTypes
{
    /// <summary><c>customer.created</c></summary>
    public const string CustomerCreated = "customer.created";
    /// <summary><c>customer.updated</c></summary>
    public const string CustomerUpdated = "customer.updated";

    /// <summary><c>coupon.created</c> — payload carries <c>code</c>.</summary>
    public const string CouponCreated = "coupon.created";

    /// <summary><c>discount.created</c></summary>
    public const string DiscountCreated = "discount.created";
    /// <summary><c>discount.removed</c></summary>
    public const string DiscountRemoved = "discount.removed";

    /// <summary><c>plan.created</c></summary>
    public const string PlanCreated = "plan.created";
    /// <summary><c>plan.updated</c></summary>
    public const string PlanUpdated = "plan.updated";
    /// <summary><c>plan.archived</c></summary>
    public const string PlanArchived = "plan.archived";

    /// <summary><c>price.created</c></summary>
    public const string PriceCreated = "price.created";
    /// <summary><c>price.deactivated</c></summary>
    public const string PriceDeactivated = "price.deactivated";

    /// <summary><c>subscription.created</c> — payload carries <c>status</c>.</summary>
    public const string SubscriptionCreated = "subscription.created";
    /// <summary><c>subscription.updated</c></summary>
    public const string SubscriptionUpdated = "subscription.updated";
    /// <summary><c>subscription.trial_will_end</c></summary>
    public const string SubscriptionTrialWillEnd = "subscription.trial_will_end";
    /// <summary><c>subscription.activated</c></summary>
    public const string SubscriptionActivated = "subscription.activated";
    /// <summary><c>subscription.paused</c></summary>
    public const string SubscriptionPaused = "subscription.paused";
    /// <summary><c>subscription.resumed</c></summary>
    public const string SubscriptionResumed = "subscription.resumed";
    /// <summary><c>subscription.canceled</c> (voluntary).</summary>
    public const string SubscriptionCanceled = "subscription.canceled";
    /// <summary><c>subscription.churned</c> (involuntary — dunning exhausted).</summary>
    public const string SubscriptionChurned = "subscription.churned";

    /// <summary><c>invoice.created</c></summary>
    public const string InvoiceCreated = "invoice.created";
    /// <summary><c>invoice.finalized</c></summary>
    public const string InvoiceFinalized = "invoice.finalized";
    /// <summary><c>invoice.paid</c></summary>
    public const string InvoicePaid = "invoice.paid";
    /// <summary><c>invoice.payment_failed</c> — payload carries <c>reason</c>.</summary>
    public const string InvoicePaymentFailed = "invoice.payment_failed";
    /// <summary><c>invoice.payment_partially_collected</c> — payload carries <c>amountPaid</c>, <c>amountRemaining</c>.</summary>
    public const string InvoicePaymentPartiallyCollected = "invoice.payment_partially_collected";
    /// <summary><c>invoice.payment_recovered</c></summary>
    public const string InvoicePaymentRecovered = "invoice.payment_recovered";
    /// <summary><c>invoice.action_required</c> — payload carries <c>reason</c>, <c>checkoutLink</c>.</summary>
    public const string InvoiceActionRequired = "invoice.action_required";
    /// <summary><c>invoice.voided</c></summary>
    public const string InvoiceVoided = "invoice.voided";

    /// <summary><c>payment_method.attached</c> — payload carries <c>kind</c>, <c>status</c>.</summary>
    public const string PaymentMethodAttached = "payment_method.attached";
    /// <summary><c>payment_method.updated</c> — payload carries <c>subscription</c>.</summary>
    public const string PaymentMethodUpdated = "payment_method.updated";
    /// <summary><c>payment_method.expiring</c> — payload carries <c>reason</c>.</summary>
    public const string PaymentMethodExpiring = "payment_method.expiring";

    /// <summary><c>settlement.created</c></summary>
    public const string SettlementCreated = "settlement.created";
    /// <summary><c>settlement.refunded</c></summary>
    public const string SettlementRefunded = "settlement.refunded";
    /// <summary><c>settlement.payout_created</c></summary>
    public const string SettlementPayoutCreated = "settlement.payout_created";
}
