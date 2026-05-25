using System.Text.RegularExpressions;
using ReVitae.Core.Cv;
using ReVitae.Core.Cv.AdditionalInformation;
using ReVitae.Core.Cv.Certificates;
using ReVitae.Core.Cv.Education;
using ReVitae.Core.Cv.Languages;
using ReVitae.Core.Cv.Links;
using ReVitae.Core.Cv.Projects;
using ReVitae.Core.Cv.Skills;
using ReVitae.Core.Cv.WorkExperience;
using ReVitae.Core.Import.Patterns;
using ReVitae.Core.Import.Pdf;
using ReVitae.Core.Localization;

namespace ReVitae.Core.Import.Extraction;

internal static partial class ImportFieldExtractionCore
{
	internal static string BuildAdditionalInformation(CvSegmentationResult segmentation, ImportSectionExtractionContext context)
	{
		if (segmentation.SectionBodies.TryGetValue(CvImportSectionId.AdditionalInformation, out var body))
		{
			if (!string.IsNullOrWhiteSpace(body))
			{
				context.AddConfidence(Cv.AdditionalInformation.AdditionalInformationFieldKeys.Content, CvImportConfidence.Medium);
			}

			return NormalizeImportedBoundedText(body.Trim(), AdditionalInformationSchema.ContentMaxLength);
		}

		return string.Empty;
	}

}
