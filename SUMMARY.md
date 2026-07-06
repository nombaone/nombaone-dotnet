# Build summary — NombaOne .NET SDK v0.1.0

Official .NET SDK for the NombaOne subscription-billing API. NuGet package
`NombaOne`, multi-targeted `netstandard2.0` + `net8.0`, single dependency on
`System.Text.Json`.

## What shipped

- Full **78-operation** surface across **15 namespaces** plus a standalone
  `NombaOne.Webhooks.WebhookVerifier` (usable without an API key).
- Transport with the money-safety invariants: `/v1` applied once, idempotency
  key computed once **before** the retry loop, retries on transport/timeout/
  408/429/5xx and 409-only-when-`IDEMPOTENCY_IN_PROGRESS`, caller cancellation
  never retried (propagates `OperationCanceledException`), full-jitter backoff
  honoring `Retry-After`, per-attempt timeout enforced via cancellation.
- Typed exception hierarchy, open `NombaoneErrorCodes` (72 public codes), cursor
  pagination with an `IAsyncEnumerable` auto-pager, integer-kobo money (`long`),
  `DateTimeOffset` timestamps, three-state `Optional<T>` for clear-to-null fields,
  and the sandbox live-key local guard.
- Webhook helper implementing the **documented** `t=<unix>,v1=<hex>` scheme;
  golden vector passes **byte-for-byte**.

## Verification performed

- **97 unit + conformance tests** green on `net8.0`; build clean on **both**
  target frameworks with **0 warnings** (warnings-as-errors) and `dotnet format`
  clean.
- **Same-idempotency-key-across-retries** unit test passes.
- **Webhook golden vector + full rejection matrix** pass (tamper, wrong secret,
  stale/future timestamp, multi-`v1` rotation, missing header/secret, malformed,
  non-JSON).
- **Bidirectional OpenAPI conformance** (all 78 non-excluded spec ops covered, no
  SDK call outside the spec); the **deliberate-break drill** was performed
  (breaking one path turned the suite red naming the route, then reverted).
- **Live integration against the deployed sandbox**
  (`https://sandbox.api.nombaone.xyz`) — 6 gated tests green: full lifecycle to
  an **active** subscription, `advance-cycle` → real invoice, upcoming-invoice +
  dunning reads, typed `CUSTOMER_NOT_FOUND` (`code`/`hint`/`docUrl`/`requestId`),
  idempotency replay returning the identical resource, real cursor pagination,
  and clean cancellation.
- **Five runnable examples** (quickstart, pagination, lifecycle, webhook,
  dunning) **executed for real** against the sandbox.
- Package **packed and consumed from an external scratch project** (local package
  source) with a real API call — created a customer and read its `requestId`.

## New backend quirks discovered — report to the operator

1. **`API_KEY_HOST_MISMATCH` is a public error code that the reference SDKs
   miss.** The served `PUBLIC_ERROR_CODES` set (`packages/errors/src/codes.ts:272`)
   includes `API_KEY_HOST_MISMATCH`, but the SDK-WORKFLOW Appendix A list and the
   Node and Go SDKs' vendored code enums omit it (they list only
   `API_KEY_MISSING/INVALID/SCOPE_FORBIDDEN/ENVIRONMENT_MISMATCH`). The .NET SDK
   vendors the full 72 including `API_KEY_HOST_MISMATCH`; the Node/Go lists should
   be reconciled.
2. **Dunning state is populated asynchronously.** Immediately after a failing
   cycle (`sandbox.advance-cycle` with a `decline_insufficient_funds` card), the
   subscription is `past_due` and its invoice `open`, but
   `GET /v1/subscriptions/{id}/dunning` returns `status: none`, `attemptsUsed: 0`,
   `graceAccessUntil: null`, `nextAttemptAt: null`. The dunning scheduler
   populates state on the first *scheduled* retry, not synchronously with the
   failed charge. The SDK surfaces all of this correctly; noted so docs/examples
   don't imply the funnel is readable the instant a charge fails.

## Brief quirks (§10) confirmed on the wire

- Creates return **201** (customer create returned 201); any 2xx is treated as
  success.
- `mode` is **`sandbox`** on the wire (spec says `test`) — every created resource
  reported `mode: sandbox`.
- Filter-name inconsistencies are wire law — `customerRef` (payment-methods) vs
  `customerId` (subscriptions/invoices) vs `planRef` (prices) all mirrored.
- `cancel` with `mode: at_period_end` returns `status: active` with
  `cancelAtPeriodEnd: true` (access retained until the period closes).
- Every response carries `X-Request-Id`; error envelopes carry
  `code`/`hint`/`docUrl`/`requestId` (verified via the typed not-found test).

## Not done (out of SDK scope / needs operator)

- Publish to NuGet + push to a Git remote and tag `v0.1.0` (needs the NuGet API
  key and repo access; the tag-driven release workflow is ready at
  `.github/workflows/release.yml`).
- Webhook **round-trip** against a live remote delivery is not exercised — a
  deployed sandbox cannot reach a listener on `127.0.0.1`; the byte-for-byte
  golden vector proves the documented-scheme implementation. Run it against a
  local API to exercise live delivery.
- Mandate creation to `consent_pending` was **not** exercised in the integration
  suite: per the Go build, `POST /v1/mandates` currently 504s on the deployed
  sandbox (NIBSS upstream unavailable), and a retried 504 is slow. The request
  shape and route are conformance-verified; pass `MaxRetries = 0` to fail fast
  against that endpoint.
