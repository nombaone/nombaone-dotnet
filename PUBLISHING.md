# Publishing `NombaOne` to NuGet

For the product owner. You do **not** need to know C#. Publishing is automatic:
CI ships a new version whenever the version number in the code changes and you
merge to `main`. You never build or upload from your laptop.

---

## The release ritual (every release)

1. Open `src/NombaOne/NombaOne.csproj`.
2. Change the one version line:

   ```xml
   <Version>0.1.0</Version>   <!-- bump to 0.1.1, 0.2.0, … -->
   ```

3. Also add a short entry under `## [Unreleased]` in `CHANGELOG.md` (optional but nice).
4. Merge that change to `main` (open a PR, approve, merge).

That's it. CI runs the tests, packs the package, and publishes it to NuGet — but
**only if that version number is new**. If you merge without changing `<Version>`,
nothing publishes (it's a safe no-op). Follow [semver](https://semver.org):
patch for fixes, minor for new features, major for breaking changes.

---

## One-time setup (do this once, before the first release)

1. **Create a NuGet.org account** at <https://www.nuget.org> and turn on 2FA.
2. **Make sure the package name is free.** Visit
   <https://www.nuget.org/packages/NombaOne> — it should say "not found". (Once
   you publish the first version, the name is yours.)
3. **Push this repository** to `https://github.com/nombaone/nombaone-dotnet`.
4. **Give CI permission to publish via Trusted Publishing (OIDC — no secret to
   store).** The release workflow is already wired for this. Two small steps:
   - On NuGet.org → your account → *Trusted Publishing* → add a policy for
     package `NombaOne`, pointing at repository `nombaone/nombaone-dotnet`,
     workflow `release.yml`, branch `main`. (For the very first publish, NuGet
     lets you pre-register the package name here so the policy can exist before
     the package does.)
   - In the GitHub repo → *Settings → Secrets and variables → Actions →
     Variables* → add a **variable** (not a secret) named `NUGET_USER` set to
     your nuget.org username. The workflow feeds it to the OIDC login action; no
     API key is ever stored. Until both are configured, the release workflow
     **fails red** (a release that can't publish should never look green) — it
     turns green only once the package is actually published.

---

## After the first publish — the clean-room check (5 minutes)

Confirm the *published* package works from scratch (not your working copy):

```bash
mkdir /tmp/nombaone-check && cd /tmp/nombaone-check
dotnet new console
dotnet add package NombaOne          # pulls it from NuGet.org
# put a sandbox key in the environment, then:
NOMBAONE_API_KEY=nbo_sandbox_… dotnet run
```

with a `Program.cs` of:

```csharp
using NombaOne;
using var nombaone = new Nombaone();
var customer = await nombaone.Customers.CreateAsync(new CustomerCreateParams
{
    Email = "check@example.com",
    Name = "Clean Room",
});
Console.WriteLine($"created {customer.Id} in {nombaone.Mode}");
```

If it prints `created nbo…cus in sandbox`, the release is good.

---

## Trusting a release without an engineer

Run the full-surface live verification and read the verdict — it calls **every**
SDK method against the real sandbox and reports any mis-parsed response:

```bash
NOMBAONE_API_KEY=nbo_sandbox_… dotnet run --project examples/NombaOne.Examples -- verify
```

The last line reads, e.g.:

```
87 methods | ok 81 | expected-errors 6 | DEFECTS 0
```

`DEFECTS 0` means every method was exercised against the real API and nothing
mis-parsed. `expected-errors` are endpoints that correctly returned a typed error
for the sandbox's state (e.g. a settlement subaccount that isn't configured). If
you ever see `DEFECTS` above 0, do not release — hand it back to the engineer.
