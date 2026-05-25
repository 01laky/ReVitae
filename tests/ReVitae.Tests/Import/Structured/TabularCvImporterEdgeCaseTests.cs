using ReVitae.Core.Import;
using ReVitae.Core.Import.Structured;
using ReVitae.Core.Localization;

namespace ReVitae.Tests.Import.Structured;

public sealed class TabularCvImporterEdgeCaseTests
{
	[Fact]
	public void Map_ReturnsNoStructuredDataWhenFewerThanTwoLines()
	{
		var result = TabularCvMapper.Map("email\n", tabDelimited: false);

		Assert.False(result.Success);
		Assert.Equal(TranslationKeys.ImportErrorNoStructuredData, result.ErrorMessageKey);
	}

	[Fact]
	public void Map_ReturnsNoStructuredDataWhenHeaderOnly()
	{
		var csv = """
            email,phone
            """;

		var result = TabularCvMapper.Map(csv, tabDelimited: false);

		Assert.False(result.Success);
		Assert.Equal(TranslationKeys.ImportErrorNoStructuredData, result.ErrorMessageKey);
	}

	[Fact]
	public void Map_MapsFriendlyHeadersIntoPersonalInformation()
	{
		var csv = """
            Full Name,Email,City
            Jane Doe,jane@example.com,Bratislava
            """;

		var result = TabularCvMapper.Map(csv, tabDelimited: false);

		Assert.True(result.Success);
		Assert.Equal("Jane", result.Personal.FirstName);
		Assert.Equal("Doe", result.Personal.LastName);
		Assert.Equal("jane@example.com", result.Personal.Email);
		Assert.Equal("Bratislava", result.Personal.Location);
	}

	[Fact]
	public void Map_WarnsWhenMultipleRowsExist_BeyondFirstDataRow()
	{
		var csv = """
            email
            first@example.com
            second@example.com
            """;

		var result = TabularCvMapper.Map(csv, tabDelimited: false);

		Assert.True(result.Success);
		Assert.Contains(result.Warnings, warning =>
			warning.MessageKey == TranslationKeys.ImportWarningTabularMultipleRowsIgnored);
	}

	[Fact]
	public void ImportViaFacade_TsvUsesTabDelimiter()
	{
		using var dir = new TempImportDirectory();
		var tsv = "email\tphone\njane@example.com\t+421900000000\n";
		var path = dir.FilePath("row.tsv", tsv);

		var result = CvDocumentImporter.Import(path);

		Assert.True(result.Success);
		Assert.Equal("jane@example.com", result.Personal.Email);
		Assert.Equal("+421900000000", result.Personal.Phone);
	}

	[Fact]
	public void ImportViaFacade_CsvRoutesThroughTabularMapper()
	{
		using var dir = new TempImportDirectory();
		var csv = """
            Email,"Professional Title"
            jane@example.com,"Staff Engineer"
            """;
		var path = dir.FilePath("tabular.csv", csv);

		var result = CvDocumentImporter.Import(path);

		Assert.True(result.Success);
		Assert.Equal("Staff Engineer", result.Personal.ProfessionalTitle);
	}
}
