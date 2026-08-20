using AutoVpn.Configuration;
using Xunit;

namespace AutoVpn.Tests;

public sealed class ProfileTests
{
    [Fact]
    public void ProfileEditorUsesTypedProperties()
    {
        var original = new Profile { Server = "old" };

        Assert.True(ProfileEditor.TryParse("server", out ProfileProperty property));
        Assert.Equal("old", ProfileEditor.Get(original, property));
        Assert.Equal("new", ProfileEditor.Set(original, property, "new").Server);
        Assert.Equal("old", original.Server);
    }

    [Fact]
    public void ProfileValidatorRejectsMissingValues()
    {
        Assert.Throws<InvalidDataException>(() => ProfileValidator.Validate(new Profile(), false));
    }
}
