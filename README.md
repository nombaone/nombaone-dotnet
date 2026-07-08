# NombaOne .NET SDK

The official .NET SDK for the [Nomba One](https://nombaone.xyz) subscription-billing API — recurring billing for Nigeria over card, direct debit, and bank transfer, with dunning that recovers and a ledger that never loses a kobo.

```bash
dotnet add package NombaOne
```

Targets `net8.0` and `netstandard2.0` (runs on .NET 8+, .NET Framework 4.6.2+, and Mono/Unity). Depends only on `System.Text.Json`. Server-side only — an API key is a secret and must never ship in a client app.

> **Status:** under active development. The client surface below is the target API; see the [CHANGELOG](CHANGELOG.md) for what has shipped.

## Quickstart

Grab a sandbox key (`nbo_sandbox_…`) from the [dashboard](https://console.nombaone.xyz), set it as `NOMBAONE_API_KEY`, and you are three objects away from a live subscription:

```csharp
using NombaOne;

var nombaone = new Nombaone(); // reads NOMBAONE_API_KEY

var plan = await nombaone.Plans.CreateAsync(new PlanCreateParams { Name = "Pro" });
var price = await nombaone.Plans.Prices.CreateAsync(plan.Id, new PriceCreateParams
{
    UnitAmountInKobo = 250_000, // ₦2,500.00 per month
    Interval = "month",
});
var customer = await nombaone.Customers.CreateAsync(new CustomerCreateParams
{
    Email = "ada@example.com",
    Name = "Ada Lovelace",
});

// Sandbox: mint a deterministic test card, then subscribe.
var method = await nombaone.Sandbox.CreatePaymentMethodAsync(
    new SandboxPaymentMethodParams { CustomerId = customer.Id });
var subscription = await nombaone.Subscriptions.CreateAsync(new SubscriptionCreateParams
{
    CustomerId = customer.Id,
    PriceId = price.Id,
    PaymentMethodId = method.Id,
});

Console.WriteLine(subscription.Status); // "active"
```

The client derives the host from your key prefix — `nbo_sandbox_…` talks to `https://sandbox.api.nombaone.xyz`, `nbo_live_…` to `https://api.nombaone.xyz`.

## Sandbox first

The sandbox runs the real billing engine. `nombaone.Sandbox.*` gives you the levers to make a month happen in a second, and every method throws locally (before any network call) if used with a live key:

```csharp
await nombaone.Sandbox.CreatePaymentMethodAsync(new SandboxPaymentMethodParams
{
    CustomerId = customer.Id,
    Behavior = "decline_insufficient_funds", // rehearse thin-balance dunning
});

var cycle = await nombaone.Sandbox.AdvanceCycleAsync(subscription.Id); // the test clock
Console.WriteLine(cycle.Outcome); // "paid" | "past_due" | …

await nombaone.Sandbox.SimulateWebhookAsync(
    new SandboxSimulateWebhookParams { Type = "invoice.payment_failed" });
```

## Money is integer kobo

Every amount in the API is a `long` count of **kobo**: `₦1.00 = 100`. `250_000` is ₦2,500 — not ₦250,000. No decimals, no floats; `Currency` is always `"NGN"`. Every money field is suffixed `InKobo`.

## Pagination

Cursor-based. List one page, or auto-page every item across every page:

```csharp
// One page.
var page = await nombaone.Invoices.ListAsync(new InvoiceListParams { Status = "open", Limit = 50 });
foreach (var invoice in page.Data) { /* … */ }
if (page.HasNextPage) { var next = await page.NextPageAsync(); }

// Or let the SDK thread the cursors for you.
await foreach (var invoice in nombaone.Invoices.ListAutoPagingAsync(
    new InvoiceListParams { Status = "open" }))
{
    // every item across every page
}
```

## Errors are a feature

Failures throw a typed `NombaoneApiException` carrying everything the API said — the stable `Code` to branch on, a `Hint` telling you exactly what to do next, a `DocUrl` into the error reference, per-field details on validation failures, and the `RequestId` to quote to support:

```csharp
try
{
    await nombaone.Subscriptions.CreateAsync(subscriptionParams);
}
catch (ValidationException ex)      { Console.WriteLine(ex.Fields);     } // per-field messages
catch (RateLimitException ex)       { Console.WriteLine(ex.RetryAfter); } // seconds
catch (NotFoundException ex)        { Console.WriteLine(ex.Code);       } // "CUSTOMER_NOT_FOUND"
```

| Status | Exception | Notes |
| ------ | --------- | ----- |
| 400 | `BadRequestException` | malformed request |
| 401 | `AuthenticationException` | missing/invalid/wrong-environment key |
| 403 | `PermissionDeniedException` | missing scope, foreign resource |
| 404 | `NotFoundException` | wrong id or wrong environment |
| 409 | `ConflictException` | state conflicts, idempotency reuse |
| 422 | `ValidationException` | `ex.Fields` has the per-field messages |
| 429 | `RateLimitException` | `RetryAfter`, `Limit`, `Remaining` |
| 5xx | `ServerException` | safe to retry (the SDK already did) |
| — | `NombaoneConnectionException` / `NombaoneTimeoutException` | transport-level |

## Idempotency & retries

The SDK auto-generates an `Idempotency-Key` for every POST and **reuses it across its automatic retries** (network failures, timeouts, 408/429/5xx — 2 retries by default, honoring `Retry-After`), so a blip can never double-charge. Pass your own key when the operation must stay idempotent across _process_ restarts:

```csharp
await nombaone.Settlements.CreatePayoutAsync(
    new PayoutCreateParams { AmountInKobo = 5_000_000, BankCode = "058", AccountNumber = "0123456789" },
    new RequestOptions { IdempotencyKey = $"payout-{myPayout.Id}" }); // ⚠ doubles as the payout's durable merchantTxRef
```

Every method takes an optional `RequestOptions` (idempotency key, per-call `Timeout`, `MaxRetries`, extra `Headers`) and a `CancellationToken`. Every returned object exposes `.RequestId` and `.RawResponse`.

## Webhooks

Verify before you parse, and dedupe on the event id — delivery is at-least-once, never exactly-once. The verifier needs only the signing secret, not an API key:

```csharp
using NombaOne.Webhooks;

// In your handler — feed it the RAW request body, never a re-serialized object.
var evt = WebhookVerifier.ConstructEvent(rawBody, signatureHeader, signingSecret);

if (AlreadyProcessed(evt.Event.Id)) return; // at-least-once ⇒ dedupe

switch (evt.Type)
{
    case "invoice.paid":            Unlock(evt.Data.Reference);         break;
    case "invoice.action_required": Send(evt.Data.CheckoutLink);        break;
    case "invoice.payment_failed":  Note(evt.Data.Reason);              break;
}
```

`ConstructEvent` checks the `X-Nombaone-Signature` (`t=<unix>,v1=<hex>`, HMAC-SHA256 over `"{t}.{body}"`) in constant time, rejects stale timestamps (300s tolerance, configurable), and returns the parsed event. Manage endpoints via `nombaone.WebhookEndpoints` — create/rotate return the signing secret **exactly once**.

## The full surface

`Customers` (+credit, discount) · `Plans` (+nested `Prices`) · `Prices` · `Subscriptions` (pause/resume/cancel/resubscribe/change, `Schedule`, `Dunning`, upcoming invoice, events) · `Invoices` · `Coupons` · `PaymentMethods` (hosted-checkout cards, virtual accounts) · `Mandates` (NIBSS direct debit) · `Settlements` (escrow, refunds, payouts) · `WebhookEndpoints` (+deliveries, replay) · `Events` (+catalog) · `Organization` (+billing policy) · `Metrics` · `Sandbox` — every operation in the [API reference](https://docs.nombaone.xyz), 1:1.

Worth knowing:

- **Mandates are asynchronous.** They start `consent_pending` and activate when the customer's bank confirms — listen for `payment_method.updated`, don't poll, don't charge early.
- **Bank transfer is a push rail.** `PaymentMethods.CreateVirtualAccountAsync` issues a NUBAN; collection completes when the transfer arrives and reconciles.
- **`past_due` is not canceled.** Read `Subscriptions.Dunning.RetrieveAsync()` and honor `GraceAccessUntil` before cutting anyone off.

## Configuration

```csharp
var nombaone = new Nombaone(new NombaoneOptions
{
    ApiKey = "nbo_sandbox_…", // default: NOMBAONE_API_KEY
    BaseUrl = null,           // override the derived host
    Timeout = TimeSpan.FromSeconds(30),
    MaxRetries = 2,
    HttpClient = null,        // bring your own HttpClient (tests, proxies)
});
```

## Requirements & versioning

.NET 8+ or any `netstandard2.0`-compatible runtime (.NET Framework 4.6.2+). Semantic versioning; the API itself is versioned at `/v1` and additive changes never break you. MIT licensed.
