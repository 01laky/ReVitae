using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using ReVitae.Core.Cv.ProfilePhoto;

namespace ReVitae.Core.Export;

internal static class CvDocxPhotoInserter
{
    private const long PhotoSizeEmus = 914400L;

    public static void TryAppendPhoto(MainDocumentPart mainPart, Body body, string? photoPath)
    {
        var bytes = ProfilePhotoBytes.TryRead(photoPath);
        if (bytes is null || bytes.Length == 0)
        {
            return;
        }

        var extension = Path.GetExtension(photoPath ?? string.Empty).ToLowerInvariant();
        var imagePartType = extension == ".png" ? ImagePartType.Png : ImagePartType.Jpeg;
        var imagePart = mainPart.AddImagePart(imagePartType);
        using (var stream = new MemoryStream(bytes))
        {
            imagePart.FeedData(stream);
        }

        var relationshipId = mainPart.GetIdOfPart(imagePart);
        body.Append(new Paragraph(new Run(CreateDrawing(relationshipId, PhotoSizeEmus, PhotoSizeEmus))));
    }

    private static Drawing CreateDrawing(string relationshipId, long widthEmus, long heightEmus)
    {
        return new Drawing(
            new DW.Inline(
                new DW.Extent { Cx = widthEmus, Cy = heightEmus },
                new DW.EffectExtent
                {
                    LeftEdge = 0L,
                    TopEdge = 0L,
                    RightEdge = 0L,
                    BottomEdge = 0L
                },
                new DW.DocProperties { Id = 1U, Name = "Profile photo" },
                new DW.NonVisualGraphicFrameDrawingProperties(new A.GraphicFrameLocks { NoChangeAspect = true }),
                new A.Graphic(
                    new A.GraphicData(
                        new PIC.Picture(
                            new PIC.NonVisualPictureProperties(
                                new PIC.NonVisualDrawingProperties { Id = 0U, Name = "Profile photo" },
                                new PIC.NonVisualPictureDrawingProperties()),
                            new PIC.BlipFill(
                                new A.Blip { Embed = relationshipId },
                                new A.Stretch(new A.FillRectangle())),
                            new PIC.ShapeProperties(
                                new A.Transform2D(
                                    new A.Offset { X = 0L, Y = 0L },
                                    new A.Extents { Cx = widthEmus, Cy = heightEmus }),
                                new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle })))
                    { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }))
            {
                DistanceFromTop = 0U,
                DistanceFromBottom = 0U,
                DistanceFromLeft = 0U,
                DistanceFromRight = 0U
            });
    }
}
