using ReVitae.Core.Import;

namespace ReVitae.Tests.Import;

public sealed class CvImportSessionOptionsEdgeCaseTests
{
	[Fact]
	public void Begin_NestedScopes_RestoresPreviousOnDispose()
	{
		Assert.False(CvImportSessionOptions.Session.ForceOcr);
		Assert.Null(CvImportSessionOptions.Session.UiLanguageCode);

		using (CvImportSessionOptions.Begin(new CvImportSessionOptions(ForceOcr: true, UiLanguageCode: "sk")))
		{
			Assert.True(CvImportSessionOptions.Session.ForceOcr);
			Assert.Equal("sk", CvImportSessionOptions.Session.UiLanguageCode);

			using (CvImportSessionOptions.Begin(new CvImportSessionOptions(ForceOcr: false, UiLanguageCode: "en")))
			{
				Assert.False(CvImportSessionOptions.Session.ForceOcr);
				Assert.Equal("en", CvImportSessionOptions.Session.UiLanguageCode);
			}

			Assert.True(CvImportSessionOptions.Session.ForceOcr);
			Assert.Equal("sk", CvImportSessionOptions.Session.UiLanguageCode);
		}

		Assert.False(CvImportSessionOptions.Session.ForceOcr);
		Assert.Null(CvImportSessionOptions.Session.UiLanguageCode);
	}
}
