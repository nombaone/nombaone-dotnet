using System;
using System.Text;
using System.Threading.Tasks;
using NombaOne.Webhooks;

namespace NombaOne.Examples;

/// <summary>
/// How to verify and handle a webhook delivery. A remote sandbox can't reach a
/// listener on your machine, so this demonstrates the exact verification your
/// HTTP handler runs — feeding <see cref="WebhookVerifier"/> a locally-signed
/// payload (the same code path a real delivery takes). Needs no API key.
/// </summary>
internal static class WebhookReceiver
{
    internal static Task RunAsync()
    {
        // In production this is the endpoint's signing secret, shown once at
        // creation (nombaone.WebhookEndpoints.CreateAsync(...).SigningSecret).
        const string signingSecret = "whsec_example_0123456789abcdef";

        // This is what NombaOne POSTs to your endpoint (raw bytes).
        var rawBody = Encoding.UTF8.GetBytes(
            "{\"id\":\"nbo000000000001whd\",\"type\":\"invoice.paid\"," +
            "\"event\":{\"id\":\"nbo000000000001evt\",\"type\":\"invoice.paid\",\"createdAt\":\"2026-07-06T10:00:00.000Z\"}," +
            "\"data\":{\"reference\":\"nbo000000000001inv\"}}");

        // NombaOne sets this header; here we generate the same thing to demo the round-trip.
        var signatureHeader = WebhookVerifier.GenerateTestHeader(rawBody, signingSecret);
        Console.WriteLine($"X-Nombaone-Signature: {signatureHeader}");

        // --- exactly what your handler does ---
        var evt = WebhookVerifier.ConstructEvent(rawBody, signatureHeader, signingSecret);

        if (AlreadyProcessed(evt.Event.Id))
        {
            Console.WriteLine("duplicate — already processed, ack 200");
            return Task.CompletedTask;
        }

        switch (evt.Type)
        {
            case WebhookEventTypes.InvoicePaid:
                Console.WriteLine($"invoice.paid -> unlock access for {evt.Data.Reference}");
                break;
            case WebhookEventTypes.InvoiceActionRequired:
                Console.WriteLine($"action required -> email {evt.Data.CheckoutLink}");
                break;
            case WebhookEventTypes.InvoicePaymentFailed:
                Console.WriteLine($"payment failed -> {evt.Data.Reason}");
                break;
            default:
                Console.WriteLine($"unhandled event: {evt.Type}");
                break;
        }

        Console.WriteLine($"verified delivery {evt.Id}; dedupe key (event id) = {evt.Event.Id}");
        return Task.CompletedTask;
    }

    // Your real dedupe store keyed on the event id (delivery is at-least-once).
    private static bool AlreadyProcessed(string eventId)
    {
        _ = eventId;
        return false;
    }
}
