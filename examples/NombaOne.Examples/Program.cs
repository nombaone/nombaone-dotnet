using System;
using System.Threading.Tasks;
using NombaOne.Examples;

// Run one example: `dotnet run --project examples/NombaOne.Examples -- <name>`
// where <name> is quickstart | pagination | lifecycle | webhook | dunning.
// Set NOMBAONE_API_KEY (a nbo_sandbox_… key) first.

var which = args.Length > 0 ? args[0] : "quickstart";

Func<Task> run = which switch
{
    "quickstart" => Quickstart.RunAsync,
    "pagination" => Pagination.RunAsync,
    "lifecycle" => Lifecycle.RunAsync,
    "webhook" => WebhookReceiver.RunAsync,
    "dunning" => SandboxDunning.RunAsync,
    "verify" => Verify.RunAsync,
    _ => throw new ArgumentException($"Unknown example '{which}'. Use quickstart | pagination | lifecycle | webhook | dunning."),
};

await run();
