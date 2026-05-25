using ReVitae.Core.Cv.ProfilePhoto;

namespace ReVitae.Tests;

public sealed class ProfilePhotoInitialsTests
{
	[Fact]
	public void Derive_FirstAndLastName_ReturnsTwoInitials()
	{
		Assert.Equal("JD", ProfilePhotoInitials.Derive("John", "Doe"));
	}

	[Fact]
	public void Derive_SingleName_ReturnsUpToTwoGraphemes()
	{
		Assert.Equal("MA", ProfilePhotoInitials.Derive("Madonna", null));
	}

	[Fact]
	public void Derive_EmptyNames_ReturnsEmpty()
	{
		Assert.Equal(string.Empty, ProfilePhotoInitials.Derive(string.Empty, string.Empty));
		Assert.Equal(string.Empty, ProfilePhotoInitials.Derive(null, null));
	}

	[Fact]
	public void Derive_NonLatinNames_UsesFirstGraphemeSafely()
	{
		Assert.Equal("北山", ProfilePhotoInitials.Derive("北京", "山田"));
		Assert.Equal("北京", ProfilePhotoInitials.Derive("北京", string.Empty));
	}

	[Theory]
	[InlineData("john", "doe", "JD")]
	[InlineData("  Jane ", " Smith ", "JS")]
	public void Derive_TrimsWhitespace(string first, string last, string expected)
	{
		Assert.Equal(expected, ProfilePhotoInitials.Derive(first, last));
	}
}
