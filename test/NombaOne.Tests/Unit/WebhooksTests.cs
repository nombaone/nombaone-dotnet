using System;
using NombaOne;
using NombaOne.Webhooks;

namespace NombaOne.Tests.Unit;

public class WebhooksTests
{
    // Golden vector — must pass byte-for-byte across every NombaOne SDK.
    private const string GoldenSecret = "nbo_whsec_golden_0123456789abcdef0123456789abcdef";
    private const string GoldenPayload =
        "{\"id\":\"nbo000000000001whd\",\"type\":\"invoice.paid\",\"event\":{\"id\":\"nbo000000000001evt\",\"type\":\"invoice.paid\",\"createdAt\":\"2026-07-04T10:00:00.000Z\"},\"data\":{\"reference\":\"nbo000000000001inv\"}}";
    private const string GoldenHeader =
        "t=1751600000,v1=ba56a072beccddbc014a3f72ef1b4a30e2008b61dcbcca4ae2f16c7e4427b374";

    // The golden `t` is fixed in the past, so verify with an effectively infinite tolerance.
    private static readonly TimeSpan Huge = TimeSpan.FromDays(100_000);

    [Fact]
    public void Golden_vector_verifies_and_parses()
    {
        var evt = WebhookVerifier.ConstructEvent(GoldenPayload, GoldenHeader, GoldenSecret, Huge);

        Assert.Equal("nbo000000000001whd", evt.Id);
        Assert.Equal("invoice.paid", evt.Type);
        Assert.Equal("nbo000000000001evt", evt.Event.Id);
        Assert.Equal("invoice.paid", evt.Event.Type);
        Assert.Equal("nbo000000000001inv", evt.Data.Reference);
    }

    [Fact]
    public void GenerateTestHeader_round_trips()
    {
        var header = WebhookVerifier.GenerateTestHeader(GoldenPayload, GoldenSecret);
        var evt = WebhookVerifier.ConstructEvent(GoldenPayload, header, GoldenSecret);
        Assert.Equal("invoice.paid", evt.Type);
    }

    [Fact]
    public void Tampered_payload_is_rejected()
    {
        Assert.Throws<WebhookVerificationException>(() =>
            WebhookVerifier.ConstructEvent(GoldenPayload + " ", GoldenHeader, GoldenSecret, Huge));
    }

    [Fact]
    public void Wrong_secret_is_rejected()
    {
        Assert.Throws<WebhookVerificationException>(() =>
            WebhookVerifier.ConstructEvent(GoldenPayload, GoldenHeader, "nbo_whsec_wrong", Huge));
    }

    [Fact]
    public void Stale_timestamp_is_rejected_at_default_tolerance()
    {
        var stale = DateTimeOffset.UtcNow.AddSeconds(-301);
        var header = WebhookVerifier.GenerateTestHeader(GoldenPayload, GoldenSecret, stale);
        Assert.Throws<WebhookVerificationException>(() =>
            WebhookVerifier.ConstructEvent(GoldenPayload, header, GoldenSecret));
    }

    [Fact]
    public void Future_timestamp_is_rejected_symmetrically()
    {
        var future = DateTimeOffset.UtcNow.AddSeconds(301);
        var header = WebhookVerifier.GenerateTestHeader(GoldenPayload, GoldenSecret, future);
        Assert.Throws<WebhookVerificationException>(() =>
            WebhookVerifier.ConstructEvent(GoldenPayload, header, GoldenSecret));
    }

    [Fact]
    public void Multiple_v1_where_only_the_second_matches_is_accepted()
    {
        var now = DateTimeOffset.UtcNow;
        var valid = WebhookVerifier.GenerateTestHeader(GoldenPayload, GoldenSecret, now);
        var parts = valid.Split(new[] { ',' }, 2); // ["t=<sec>", "v1=<correct>"]
        var rotated = $"{parts[0]},v1=deadbeefdeadbeef,{parts[1]}";

        var evt = WebhookVerifier.ConstructEvent(GoldenPayload, rotated, GoldenSecret);
        Assert.Equal("invoice.paid", evt.Type);
    }

    [Fact]
    public void Missing_header_gives_a_distinct_error()
    {
        var ex = Assert.Throws<WebhookVerificationException>(() =>
            WebhookVerifier.ConstructEvent(GoldenPayload, string.Empty, GoldenSecret));
        Assert.Contains("Missing X-Nombaone-Signature", ex.Message);
    }

    [Fact]
    public void Missing_secret_gives_a_distinct_error()
    {
        var ex = Assert.Throws<WebhookVerificationException>(() =>
            WebhookVerifier.ConstructEvent(GoldenPayload, GoldenHeader, string.Empty));
        Assert.Contains("Missing signing secret", ex.Message);
    }

    [Fact]
    public void Malformed_header_gives_a_distinct_error()
    {
        var ex = Assert.Throws<WebhookVerificationException>(() =>
            WebhookVerifier.ConstructEvent(GoldenPayload, "not-a-signature", GoldenSecret));
        Assert.Contains("Malformed", ex.Message);
    }

    [Fact]
    public void Non_json_body_after_valid_signature_is_rejected()
    {
        const string body = "this is not json";
        var header = WebhookVerifier.GenerateTestHeader(body, GoldenSecret);
        var ex = Assert.Throws<WebhookVerificationException>(() =>
            WebhookVerifier.ConstructEvent(body, header, GoldenSecret));
        Assert.Contains("not valid JSON", ex.Message);
    }

    [Fact]
    public void Flat_delivery_body_synthesizes_the_event_ref()
    {
        const string flat = "{\"id\":\"nbo000000000009evt\",\"type\":\"invoice.paid\",\"createdAt\":\"2026-07-04T10:00:00.000Z\",\"data\":{\"reference\":\"nbo1inv\"}}";
        var header = WebhookVerifier.GenerateTestHeader(flat, GoldenSecret);

        var evt = WebhookVerifier.ConstructEvent(flat, header, GoldenSecret);

        Assert.Equal("nbo000000000009evt", evt.Event.Id); // synthesized from top-level id
        Assert.Equal("invoice.paid", evt.Event.Type);
        Assert.Equal("nbo1inv", evt.Data.Reference);
    }

    [Fact]
    public void Typed_payload_fields_are_exposed()
    {
        const string body = "{\"id\":\"whd\",\"type\":\"invoice.payment_partially_collected\"," +
            "\"event\":{\"id\":\"evt\",\"type\":\"invoice.payment_partially_collected\",\"createdAt\":\"2026-07-04T10:00:00.000Z\"}," +
            "\"data\":{\"reference\":\"nbo1inv\",\"amountPaid\":150000,\"amountRemaining\":100000}}";
        var header = WebhookVerifier.GenerateTestHeader(body, GoldenSecret);

        var evt = WebhookVerifier.ConstructEvent(body, header, GoldenSecret);

        Assert.Equal(WebhookEventTypes.InvoicePaymentPartiallyCollected, evt.Type);
        Assert.Equal(150_000, evt.Data.AmountPaid);
        Assert.Equal(100_000, evt.Data.AmountRemaining);
        Assert.Equal("nbo1inv", evt.Data.Raw.GetProperty("reference").GetString());
    }

    [Fact]
    public void VerifySignature_only_succeeds_on_the_golden_vector()
    {
        // Does not throw.
        WebhookVerifier.VerifySignature(GoldenPayload, GoldenHeader, GoldenSecret, Huge);
    }
}
