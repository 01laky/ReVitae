using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace ReVitae.Core.Export.Pdf;

internal static class CvPdfRenderHelper
{
	internal static byte[] Generate(CvExportDocument document, Action<IDocumentContainer> compose) =>
		Document.Create(compose)
			.WithMetadata(CvPdfExportMetadata.ForTemplate(document.TemplateId))
			.GeneratePdf();

	/// <summary>
	/// Renders a single A4 page document with the shared scaffold (047 T6): create the document
	/// with template metadata, add one A4 page configured by
	/// <see cref="CvPdfLayoutHelpers.ConfigureA4Page"/>, then run the caller's page composition.
	/// Replaces the <c>Generate → container.Page → ConfigureA4Page</c> boilerplate repeated by
	/// every template.
	/// </summary>
	internal static byte[] RenderPage(
		CvExportDocument document,
		Action<PageDescriptor> composePage,
		string backgroundColor = "#FFFFFF") =>
		Generate(document, container =>
			container.Page(page =>
			{
				CvPdfLayoutHelpers.ConfigureA4Page(page, backgroundColor);
				composePage(page);
			}));
}
