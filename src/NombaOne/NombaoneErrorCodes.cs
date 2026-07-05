namespace NombaOne;

/// <summary>
/// The stable, machine-readable error codes the NombaOne API can emit on the
/// wire, vendored from the platform's <c>PUBLIC_ERROR_CODES</c> set (72 codes).
/// Branch on <see cref="NombaoneApiException.Code"/> against these constants —
/// the code never changes for a given failure, unlike the human message.
/// </summary>
/// <remarks>
/// The set is <b>open</b>: a code the API adds tomorrow still parses into a
/// <see cref="NombaoneApiException"/> today — it simply will not match one of
/// these constants. Compare with <c>==</c>; never assume this list is closed.
/// </remarks>
public static class NombaoneErrorCodes
{
    // ---- Generic request errors ----

    /// <summary>The request could not be understood (malformed 400).</summary>
    public const string ClientInvalidRequest = "CLIENT_INVALID_REQUEST";
    /// <summary>One or more fields are invalid; see <see cref="NombaoneApiException.Fields"/> (422).</summary>
    public const string ClientValidationFailed = "CLIENT_VALIDATION_FAILED";
    /// <summary>A valid key that is not allowed to perform this action (403).</summary>
    public const string ClientForbidden = "CLIENT_FORBIDDEN";
    /// <summary>No route matches the requested method and path (404).</summary>
    public const string ClientRouteNotFound = "CLIENT_ROUTE_NOT_FOUND";
    /// <summary>No resource exists at that id in this environment (404).</summary>
    public const string ClientResourceNotFound = "CLIENT_RESOURCE_NOT_FOUND";
    /// <summary>The request conflicts with current state (409).</summary>
    public const string ClientConflict = "CLIENT_CONFLICT";
    /// <summary>The pagination cursor is malformed or expired.</summary>
    public const string InvalidCursor = "INVALID_CURSOR";

    // ---- API-key authentication ----

    /// <summary>No API key was supplied.</summary>
    public const string ApiKeyMissing = "API_KEY_MISSING";
    /// <summary>The API key is missing, invalid, or revoked.</summary>
    public const string ApiKeyInvalid = "API_KEY_INVALID";
    /// <summary>The key lacks the scope this endpoint requires.</summary>
    public const string ApiKeyScopeForbidden = "API_KEY_SCOPE_FORBIDDEN";
    /// <summary>The key's environment does not match the resource's environment.</summary>
    public const string ApiKeyEnvironmentMismatch = "API_KEY_ENVIRONMENT_MISMATCH";
    /// <summary>The key was used against a host it is not permitted for.</summary>
    public const string ApiKeyHostMismatch = "API_KEY_HOST_MISMATCH";

    // ---- Idempotency ----

    /// <summary>A required <c>Idempotency-Key</c> header was not supplied.</summary>
    public const string IdempotencyKeyMissing = "IDEMPOTENCY_KEY_MISSING";
    /// <summary>The same idempotency key was reused with a different request body.</summary>
    public const string IdempotencyKeyReused = "IDEMPOTENCY_KEY_REUSED";
    /// <summary>An earlier request with this key is still in flight (retryable).</summary>
    public const string IdempotencyInProgress = "IDEMPOTENCY_IN_PROGRESS";

    // ---- Rate limiting / platform ----

    /// <summary>Too many requests; slow down (429).</summary>
    public const string RateLimitExceeded = "RATE_LIMIT_EXCEEDED";
    /// <summary>The organization's request quota has been exhausted.</summary>
    public const string QuotaExceeded = "QUOTA_EXCEEDED";
    /// <summary>The platform is in maintenance; try again shortly.</summary>
    public const string PlatformMaintenance = "PLATFORM_MAINTENANCE";

    // ---- Webhooks ----

    /// <summary>A webhook signature failed verification.</summary>
    public const string WebhookSignatureInvalid = "WEBHOOK_SIGNATURE_INVALID";

    // ---- Customers ----

    /// <summary>No customer exists with that id.</summary>
    public const string CustomerNotFound = "CUSTOMER_NOT_FOUND";
    /// <summary>A customer with that email already exists in this environment.</summary>
    public const string CustomerEmailTaken = "CUSTOMER_EMAIL_TAKEN";

    // ---- Plans &amp; prices ----

    /// <summary>No plan exists with that id.</summary>
    public const string PlanNotFound = "PLAN_NOT_FOUND";
    /// <summary>A plan with that name already exists.</summary>
    public const string PlanNameTaken = "PLAN_NAME_TAKEN";
    /// <summary>The plan is already archived.</summary>
    public const string PlanAlreadyArchived = "PLAN_ALREADY_ARCHIVED";
    /// <summary>The plan still has active subscribers and cannot be archived.</summary>
    public const string PlanHasActiveSubscribers = "PLAN_HAS_ACTIVE_SUBSCRIBERS";
    /// <summary>No price exists with that id.</summary>
    public const string PriceNotFound = "PRICE_NOT_FOUND";
    /// <summary>The price does not belong to the referenced plan.</summary>
    public const string PricePlanMismatch = "PRICE_PLAN_MISMATCH";
    /// <summary>The price is already inactive.</summary>
    public const string PriceAlreadyInactive = "PRICE_ALREADY_INACTIVE";
    /// <summary>Tiered pricing is not supported for this operation.</summary>
    public const string PriceTieredNotSupported = "PRICE_TIERED_NOT_SUPPORTED";

    // ---- Payment methods &amp; mandates ----

    /// <summary>No payment method exists with that id.</summary>
    public const string PaymentMethodNotFound = "PAYMENT_METHOD_NOT_FOUND";
    /// <summary>The payment method is not active (setup pending, removed, or expired).</summary>
    public const string PaymentMethodNotActive = "PAYMENT_METHOD_NOT_ACTIVE";
    /// <summary>The payment method is the wrong kind for this operation.</summary>
    public const string PaymentMethodKindMismatch = "PAYMENT_METHOD_KIND_MISMATCH";
    /// <summary>The mandate is not active.</summary>
    public const string MandateNotActive = "MANDATE_NOT_ACTIVE";
    /// <summary>The charge exceeds the mandate's per-debit ceiling.</summary>
    public const string MandateMaxAmountExceeded = "MANDATE_MAX_AMOUNT_EXCEEDED";
    /// <summary>The mandate is still awaiting the customer's bank consent.</summary>
    public const string MandateConsentPending = "MANDATE_CONSENT_PENDING";

    // ---- Subscriptions &amp; invoices ----

    /// <summary>No subscription exists with that id.</summary>
    public const string SubscriptionNotFound = "SUBSCRIPTION_NOT_FOUND";
    /// <summary>The requested state transition is not legal from the current state.</summary>
    public const string SubscriptionIllegalTransition = "SUBSCRIPTION_ILLEGAL_TRANSITION";
    /// <summary>The subscription changed concurrently; retry with the latest version.</summary>
    public const string SubscriptionVersionConflict = "SUBSCRIPTION_VERSION_CONFLICT";
    /// <summary>The subscription must be in a terminal state for this operation.</summary>
    public const string SubscriptionNotTerminal = "SUBSCRIPTION_NOT_TERMINAL";
    /// <summary>The subscription needs an active payment method to proceed.</summary>
    public const string SubscriptionPaymentMethodRequired = "SUBSCRIPTION_PAYMENT_METHOD_REQUIRED";
    /// <summary>No invoice exists with that id.</summary>
    public const string InvoiceNotFound = "INVOICE_NOT_FOUND";
    /// <summary>The invoice is already finalized.</summary>
    public const string InvoiceAlreadyFinalized = "INVOICE_ALREADY_FINALIZED";
    /// <summary>The invoice is already paid.</summary>
    public const string InvoiceAlreadyPaid = "INVOICE_ALREADY_PAID";
    /// <summary>The invoice is not in a voidable state.</summary>
    public const string InvoiceNotVoidable = "INVOICE_NOT_VOIDABLE";

    // ---- Schedules &amp; proration ----

    /// <summary>No subscription schedule exists for that subscription.</summary>
    public const string SubscriptionScheduleNotFound = "SUBSCRIPTION_SCHEDULE_NOT_FOUND";
    /// <summary>A schedule already exists or conflicts with the requested change.</summary>
    public const string SubscriptionScheduleConflict = "SUBSCRIPTION_SCHEDULE_CONFLICT";
    /// <summary>The schedule's effective-at value is invalid.</summary>
    public const string SubscriptionScheduleInvalidEffectiveAt = "SUBSCRIPTION_SCHEDULE_INVALID_EFFECTIVE_AT";
    /// <summary>Proration does not apply to this change.</summary>
    public const string ProrationNotApplicable = "PRORATION_NOT_APPLICABLE";
    /// <summary>Switching billing interval mid-cycle is not supported; use a schedule.</summary>
    public const string ProrationIntervalSwitchUnsupported = "PRORATION_INTERVAL_SWITCH_UNSUPPORTED";

    // ---- Coupons, discounts &amp; credits ----

    /// <summary>No coupon exists with that id or code.</summary>
    public const string CouponNotFound = "COUPON_NOT_FOUND";
    /// <summary>The coupon has expired.</summary>
    public const string CouponExpired = "COUPON_EXPIRED";
    /// <summary>The coupon has reached its maximum redemptions.</summary>
    public const string CouponMaxRedemptionsReached = "COUPON_MAX_REDEMPTIONS_REACHED";
    /// <summary>The coupon definition is invalid.</summary>
    public const string CouponInvalidDefinition = "COUPON_INVALID_DEFINITION";
    /// <summary>A discount from this coupon is already applied.</summary>
    public const string CouponAlreadyApplied = "COUPON_ALREADY_APPLIED";
    /// <summary>No active discount exists to remove.</summary>
    public const string DiscountNotFound = "DISCOUNT_NOT_FOUND";
    /// <summary>No credit grant exists with that id.</summary>
    public const string CreditGrantNotFound = "CREDIT_GRANT_NOT_FOUND";
    /// <summary>The credit grant is already voided.</summary>
    public const string CreditGrantAlreadyVoided = "CREDIT_GRANT_ALREADY_VOIDED";
    /// <summary>The customer's credit balance is insufficient.</summary>
    public const string CreditInsufficientBalance = "CREDIT_INSUFFICIENT_BALANCE";
    /// <summary>The credit amount is invalid (must be a positive integer of kobo).</summary>
    public const string CreditInvalidAmount = "CREDIT_INVALID_AMOUNT";

    // ---- Dunning ----

    /// <summary>There is no open invoice to run dunning against.</summary>
    public const string DunningNoOpenInvoice = "DUNNING_NO_OPEN_INVOICE";
    /// <summary>No dunning attempt exists with that id.</summary>
    public const string DunningAttemptNotFound = "DUNNING_ATTEMPT_NOT_FOUND";
    /// <summary>Recovery is blocked pending a card update.</summary>
    public const string DunningCardUpdateRequired = "DUNNING_CARD_UPDATE_REQUIRED";
    /// <summary>The dunning run is already in a terminal state.</summary>
    public const string DunningAlreadyTerminal = "DUNNING_ALREADY_TERMINAL";

    // ---- Settlement, refunds &amp; payouts ----

    /// <summary>No settlement exists with that id.</summary>
    public const string SettlementNotFound = "SETTLEMENT_NOT_FOUND";
    /// <summary>The organization's settlement subaccount is not configured.</summary>
    public const string SettlementSubaccountNotFound = "SETTLEMENT_SUBACCOUNT_NOT_FOUND";
    /// <summary>The settlement has already been refunded.</summary>
    public const string RefundAlreadyRefunded = "REFUND_ALREADY_REFUNDED";
    /// <summary>The refund amount exceeds the settlement's net-to-tenant.</summary>
    public const string RefundAmountExceedsNet = "REFUND_AMOUNT_EXCEEDS_NET";
    /// <summary>Funds are still under the escrow lock.</summary>
    public const string EscrowLocked = "ESCROW_LOCKED";
    /// <summary>The payout amount exceeds the available (withdrawable) balance.</summary>
    public const string PayoutExceedsAvailable = "PAYOUT_EXCEEDS_AVAILABLE";

    // ---- Example scaffold ----

    /// <summary>No example resource exists with that id (reference scaffold).</summary>
    public const string ExampleNotFound = "EXAMPLE_NOT_FOUND";

    // ---- System fallbacks ----

    /// <summary>An unexpected error occurred on NombaOne's side.</summary>
    public const string SystemInternalError = "SYSTEM_INTERNAL_ERROR";
    /// <summary>An upstream dependency failed (safe to retry).</summary>
    public const string SystemUpstreamError = "SYSTEM_UPSTREAM_ERROR";
}
