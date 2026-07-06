# Changelog

All notable changes to the NombaOne .NET SDK are documented here. The format is
based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this
project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.1.0] - 2026-07-06

Initial release — the complete NombaOne API surface for .NET.

### Added

- Multi-targeted client (`netstandard2.0`, `net8.0`) with a single dependency
  on `System.Text.Json`; runs on .NET 8+, .NET Framework 4.6.2+, and Mono/Unity.
- The full resource surface — 15 namespaces, 78 operations: `Customers`,
  `Plans` (+`Prices`), `Prices`, `Subscriptions` (+`Schedule`, +`Dunning`),
  `Invoices`, `Coupons`, `PaymentMethods`, `Mandates`, `Settlements`,
  `WebhookEndpoints` (+`Deliveries`), `Events`, `Organization` (+`Billing`),
  `Metrics`, and `Sandbox` — proven 1:1 against the OpenAPI spec by a
  bidirectional conformance suite.
- Money-safety transport: the `Idempotency-Key` is computed once before the
  retry loop; retries cover transport failures, timeouts, 408/429/5xx, and
  409-`IDEMPOTENCY_IN_PROGRESS`; caller cancellation is never retried; full-jitter
  backoff honors `Retry-After`; per-attempt timeout via cancellation.
- Typed exception hierarchy (`NombaoneApiException` and per-status subclasses)
  carrying `Code`, `Hint`, `DocUrl`, `Fields`, and `RequestId`; 72 public
  error-code constants (`NombaoneErrorCodes`).
- Cursor pagination: one-page reads, manual `NextPageAsync`, and
  `IAsyncEnumerable` auto-paging that preserves the original filters.
- Webhook verification helper (`WebhookVerifier`, no API key required):
  HMAC-SHA256 over `"{t}.{rawBody}"`, configurable timestamp tolerance,
  constant-time comparison, multi-`v1` secret rotation, and a typed, open event
  catalog.
- Integer-kobo money (`long`, `…InKobo`), `DateTimeOffset` timestamps, and a
  three-state `Optional<T>` for fields cleared with an explicit JSON `null`.
