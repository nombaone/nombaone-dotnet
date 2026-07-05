using System;
using NombaOne;

namespace NombaOne.Tests.Unit;

public class ClientTests
{
    private const string EnvVar = "NOMBAONE_API_KEY";

    private static void WithEnv(string? value, Action body)
    {
        var original = Environment.GetEnvironmentVariable(EnvVar);
        try
        {
            Environment.SetEnvironmentVariable(EnvVar, value);
            body();
        }
        finally
        {
            Environment.SetEnvironmentVariable(EnvVar, original);
        }
    }

    [Fact]
    public void Sandbox_key_derives_sandbox_mode_and_host()
    {
        using var client = new Nombaone("nbo_sandbox_abc");
        Assert.Equal("sandbox", client.Mode);
        Assert.Equal(Nombaone.SandboxBaseUrl, client.BaseUrl);
    }

    [Fact]
    public void Live_key_derives_live_mode_and_host()
    {
        using var client = new Nombaone("nbo_live_abc");
        Assert.Equal("live", client.Mode);
        Assert.Equal(Nombaone.LiveBaseUrl, client.BaseUrl);
    }

    [Fact]
    public void Missing_key_throws_with_actionable_message()
    {
        WithEnv(null, () =>
        {
            var ex = Assert.Throws<NombaoneException>(() => new Nombaone(new NombaoneOptions()));
            Assert.Contains("NOMBAONE_API_KEY", ex.Message);
        });
    }

    [Fact]
    public void Unrecognized_key_without_base_url_throws()
    {
        var ex = Assert.Throws<NombaoneException>(() => new Nombaone("totally-not-a-nomba-key"));
        Assert.Contains("nbo_sandbox_", ex.Message);
    }

    [Fact]
    public void Unrecognized_key_with_base_url_is_allowed_and_defaults_to_sandbox()
    {
        using var client = new Nombaone(new NombaoneOptions
        {
            ApiKey = "custom-key",
            BaseUrl = "https://custom.example.com/",
        });
        Assert.Equal("sandbox", client.Mode);
        Assert.Equal("https://custom.example.com", client.BaseUrl); // trailing slash trimmed
    }

    [Fact]
    public void Explicit_base_url_overrides_the_derived_host()
    {
        using var client = new Nombaone(new NombaoneOptions
        {
            ApiKey = "nbo_live_abc",
            BaseUrl = "https://proxy.internal/",
        });
        Assert.Equal("live", client.Mode);
        Assert.Equal("https://proxy.internal", client.BaseUrl);
    }

    [Fact]
    public void Reads_key_from_environment_when_no_argument()
    {
        WithEnv("nbo_sandbox_fromenv", () =>
        {
            using var client = new Nombaone();
            Assert.Equal("sandbox", client.Mode);
        });
    }
}
