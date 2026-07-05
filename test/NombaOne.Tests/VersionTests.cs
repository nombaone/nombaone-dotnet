using NombaOne.Internal;

namespace NombaOne.Tests;

public class VersionTests
{
    [Fact]
    public void UserAgent_uses_the_sdk_version()
    {
        Assert.Equal("nombaone-dotnet/0.1.0", NombaoneVersion.UserAgent);
    }
}
