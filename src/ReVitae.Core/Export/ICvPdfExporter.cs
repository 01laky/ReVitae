namespace ReVitae.Core.Export;

using System.IO;

public interface ICvPdfExporter
{
    byte[] Export(CvExportDocument document);

    void Export(CvExportDocument document, Stream destination);
}
