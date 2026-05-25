using System.Text.Json.Nodes;
using ReVitae.Core.Ai.Import;
using ReVitae.Core.Import;
using ReVitae.Core.Localization;

namespace ReVitae.Tests.Ai.Import;

public sealed class AiImportDiagnosticsLoggerTests
{
    [Fact]
    public void LogBatch_WhenDebugOff_DoesNotWriteAiImportLines()
    {
        var original = Environment.GetEnvironmentVariable("REVITAE_IMPORT_DEBUG");
        try
        {
            Environment.SetEnvironmentVariable("REVITAE_IMPORT_DEBUG", null);
            var logPath = Path.Combine(Path.GetTempPath(), $"revitae-ai-log-{Guid.NewGuid():N}.log");
            Environment.SetEnvironmentVariable("REVITAE_IMPORT_DEBUG_LOG", logPath);
            AiImportDiagnosticsLogger.LogBatch(
                AiImportPhase.Personal,
                1,
                1,
                "compact",
                100,
                0,
                50,
                true,
                false,
                10);
            Assert.False(File.Exists(logPath));
        }
        finally
        {
            Environment.SetEnvironmentVariable("REVITAE_IMPORT_DEBUG", original);
        }
    }

    [Fact]
    public void LogBatch_WhenDebugOn_ExcludesEmailPatternFromLoggedSlice()
    {
        var originalDebug = Environment.GetEnvironmentVariable("REVITAE_IMPORT_DEBUG");
        var originalLog = Environment.GetEnvironmentVariable("REVITAE_IMPORT_DEBUG_LOG");
        var logPath = Path.Combine(Path.GetTempPath(), $"revitae-ai-log-{Guid.NewGuid():N}.log");
        try
        {
            Environment.SetEnvironmentVariable("REVITAE_IMPORT_DEBUG", "1");
            Environment.SetEnvironmentVariable("REVITAE_IMPORT_DEBUG_LOG", logPath);
            AiImportDiagnosticsLogger.LogSessionStart("compact", 3, "en");
            AiImportDiagnosticsLogger.LogBatch(
                AiImportPhase.Personal,
                1,
                1,
                "compact",
                "john.doe@example.com".Length,
                0,
                50,
                true,
                false,
                10);
            Assert.True(File.Exists(logPath));
            var content = File.ReadAllText(logPath);
            Assert.DoesNotContain("@example.com", content, StringComparison.Ordinal);
            Assert.Contains("inputChars=", content, StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable("REVITAE_IMPORT_DEBUG", originalDebug);
            Environment.SetEnvironmentVariable("REVITAE_IMPORT_DEBUG_LOG", originalLog);
            if (File.Exists(logPath))
            {
                File.Delete(logPath);
            }
        }
    }
}

public sealed class AiCvImportEdgeCaseTests
{
    [Theory]
    [InlineData(CvImportFormat.ReVitaeJson, 6, false)]
    [InlineData(CvImportFormat.PlainText, 2, true)]
    public void EdgeCase_TriggerMatrix(CvImportFormat format, int sections, bool expectOffer)
    {
        var flags = Enum.GetValues<CvImportSectionId>()
            .Select((id, index) => (id, populated: index < sections))
            .ToDictionary(pair => pair.id, pair => pair.populated);
        var result = new CvImportResult { Success = true, SectionHasData = flags };
        var attempt = AiImportTestHelpers.CreateAttempt(result, new string('x', 500), format);
        Assert.Equal(expectOffer, AiCvImportTriggerEvaluator.ShouldOfferAi(attempt));
    }

    [Fact]
    public void EdgeCase_Gemma2Compact_MaxInput1200AndCombinedSkillsLanguages()
    {
        var profile = AiImportBatchPlanResolver.Resolve(AiImportTestHelpers.LocalSettings("gemma2-2b"));
        Assert.Equal(1200, profile.MaxInputChars);
        Assert.True(profile.CombineSkillsAndLanguages);
    }

    [Fact]
    public void EdgeCase_UnknownOnlineModel_UsesSmallProfile()
    {
        var profile = AiImportOnlineModelProfileMap.Resolve("totally-unknown-model");
        Assert.Equal(2400, profile.MaxInputChars);
    }

    [Fact]
    public void EdgeCase_ParserInvalidJsonTwice_IncrementsFailedBatches()
    {
        var first = AiCvImportResponseParser.TryParse("bad", AiImportPhase.Personal);
        var second = AiCvImportResponseParser.TryParse("still bad", AiImportPhase.Personal);
        Assert.False(first.Success);
        Assert.False(second.Success);
        Assert.True(first.ShouldRetry);
    }

    [Fact]
    public void EdgeCase_MergerFillEmpty_WorkAppendDeduped()
    {
        var baseline = new CvImportResult
        {
            Success = true,
            WorkExperienceEntries = [new ReVitae.Core.Cv.WorkExperience.WorkExperienceEntry("w1") { Company = "Acme", StartYear = 2020, StartMonth = 1 }],
        };
        var ai = new CvImportResult
        {
            Success = true,
            WorkExperienceEntries =
            [
                new ReVitae.Core.Cv.WorkExperience.WorkExperienceEntry("w1") { Company = "Acme", StartYear = 2020, StartMonth = 1 },
                new ReVitae.Core.Cv.WorkExperience.WorkExperienceEntry("w2") { Company = "Globex", StartYear = 2018, StartMonth = 1 },
                new ReVitae.Core.Cv.WorkExperience.WorkExperienceEntry("w3") { Company = "Initech", StartYear = 2015, StartMonth = 1 },
            ],
            Warnings = [],
        };
        var merged = AiCvImportResultMerger.MergeForApply(ai, baseline, AiCvImportMergeMode.FillEmptyOnly, null);
        Assert.Equal(3, merged.WorkExperienceEntries.Count);
    }

    [Fact]
    public void EdgeCase_LocaleSkSource_PreservesDiacriticsInMerger()
    {
        var accumulated = AiCvImportResultMerger.CreateEmptyDocument();
        AiCvImportResultMerger.MergeFragment(
            accumulated,
            AiImportTestHelpers.PersonalJson("Ján", "Horváth", "jan@example.sk"));
        var result = AiCvImportResultMerger.BuildFinalResult(accumulated, null, AiCvImportMergeMode.ReplaceAll, [], 0, null);
        Assert.Equal("Ján", result.Personal.FirstName);
        Assert.Equal("Horváth", result.Personal.LastName);
    }

    [Fact]
    public void EdgeCase_LocaleEnSource_NoForcedTranslation()
    {
        var accumulated = AiCvImportResultMerger.CreateEmptyDocument();
        AiCvImportResultMerger.MergeFragment(
            accumulated,
            AiImportTestHelpers.PersonalJson("Maria", "Nováková", "maria@example.com"));
        var result = AiCvImportResultMerger.BuildFinalResult(accumulated, null, AiCvImportMergeMode.ReplaceAll, [], 0, null);
        Assert.Equal("Nováková", result.Personal.LastName);
    }

    [Fact]
    public void EdgeCase_PhotoPathEmptyAfterTextImport()
    {
        var accumulated = AiImportTestHelpers.PersonalJson();
        var result = AiCvImportResultMerger.BuildFinalResult(accumulated, null, AiCvImportMergeMode.ReplaceAll, [], 0, null);
        Assert.True(string.IsNullOrWhiteSpace(result.Personal.ProfilePhotoPath));
    }

    [Fact]
    public void EdgeCase_ReviewSummaryBeforeZeroAfterFour_IsImproved()
    {
        var before = new CvImportResult { Success = true, WorkExperienceEntries = [] };
        var after = new CvImportResult
        {
            Success = true,
            WorkExperienceEntries = Enumerable.Range(1, 4)
                .Select(i => new ReVitae.Core.Cv.WorkExperience.WorkExperienceEntry($"w{i}") { Company = $"C{i}" })
                .ToList(),
        };
        var summary = AiCvImportReviewSummaryBuilder.Build(before, after);
        Assert.True(summary.Rows.First(r => r.SectionId == CvImportSectionId.WorkExperience).IsImproved);
    }

    [Fact]
    public void EdgeCase_CoordinatorMissingFile_ReturnsNull()
    {
        var attempt = CvTextImportCoordinator.TryImport(Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.pdf"));
        Assert.Null(attempt);
    }

    [Fact]
    public void EdgeCase_CoordinatorTextRouteFail_StillReturnsNormalizedTextWhenExtracted()
    {
        var temp = Path.Combine(Path.GetTempPath(), $"revitae-ai-{Guid.NewGuid():N}.txt");
        File.WriteAllText(temp, SampleCvText.GarbledCv(500));
        try
        {
            var attempt = CvTextImportCoordinator.TryImport(temp);
            Assert.NotNull(attempt);
            Assert.True(attempt!.NonWhitespaceCharCount >= AiImportLimits.MinSourceCharsForAi);
            Assert.NotNull(attempt.NormalizedText);
        }
        finally
        {
            File.Delete(temp);
        }
    }

    [Fact]
    public void EdgeCase_JsonMapEndMonthZero_HandledByMapper()
    {
        var obj = new JsonObject
        {
            ["workExperience"] = new JsonArray
            {
                new JsonObject
                {
                    ["company"] = "Acme",
                    ["jobTitle"] = "Dev",
                    ["startYear"] = 2020,
                    ["startMonth"] = 1,
                    ["endMonth"] = 0,
                    ["endYear"] = 0,
                },
            },
        };
        var parsed = AiCvImportResponseParser.TryParse(obj.ToJsonString(), AiImportPhase.Work);
        Assert.True(parsed.Success);
        var mapped = AiCvImportResponseParser.MapAccumulated(
            AiCvImportResultMerger.CreateEmptyDocument().AlsoMerge(parsed.Fragment!),
            []);
        Assert.Single(mapped.WorkExperienceEntries);
    }

    [Fact]
    public void EdgeCase_FullMergedDocument_MapsThroughReVitaeJsonMapper()
    {
        var accumulated = AiCvImportResultMerger.CreateEmptyDocument();
        AiCvImportResultMerger.MergeFragment(accumulated, AiImportTestHelpers.PersonalJson());
        AiCvImportResultMerger.MergeFragment(accumulated, AiImportTestHelpers.WorkJson(("Acme", 2020), ("Globex", 2018)));
        var result = AiCvImportResponseParser.MapAccumulated(accumulated, []);
        Assert.True(result.Success);
        Assert.Equal(2, result.WorkExperienceEntries.Count);
    }

    [Fact]
    public void EdgeCase_EnhancePartialDeterministic_IncreasesSectionCount()
    {
        var baseline = AiImportTestHelpers.ThinSuccess(new string('x', 300), 2);
        var accumulated = AiCvImportResultMerger.CreateEmptyDocument();
        AiCvImportResultMerger.MergeFragment(accumulated, AiImportTestHelpers.PersonalJson());
        AiCvImportResultMerger.MergeFragment(accumulated, AiImportTestHelpers.WorkJson(("Acme", 2020), ("Globex", 2018)));
        AiCvImportResultMerger.MergeFragment(
            accumulated,
            new JsonObject { ["education"] = new JsonArray { new JsonObject { ["institution"] = "MIT", ["startYear"] = 2012 } } });
        var ai = AiCvImportResultMerger.BuildFinalResult(accumulated, null, AiCvImportMergeMode.ReplaceAll, [], 0, null);
        var merged = AiCvImportResultMerger.MergeForApply(ai, baseline, AiCvImportMergeMode.FillEmptyOnly, null);
        Assert.True(
            CvImportSectionMetrics.CountPopulatedSections(merged.SectionHasData) >
            CvImportSectionMetrics.CountPopulatedSections(baseline.SectionHasData));
    }

    [Fact]
    public void EdgeCase_StructuredJsonResumeSuccess_NoAiTrigger()
    {
        var flags = Enum.GetValues<CvImportSectionId>().Take(6).ToDictionary(id => id, _ => true);
        foreach (var id in Enum.GetValues<CvImportSectionId>().Skip(6))
        {
            flags[id] = false;
        }

        var result = new CvImportResult { Success = true, SectionHasData = flags };
        var attempt = AiImportTestHelpers.CreateAttempt(result, "{}", CvImportFormat.JsonResume);
        Assert.False(AiCvImportTriggerEvaluator.ShouldOfferAi(attempt));
    }

    [Fact]
    public void EdgeCase_CarryForwardSummary_TruncatesToProfileMax()
    {
        var accumulated = AiCvImportResultMerger.CreateEmptyDocument();
        for (var i = 0; i < 20; i++)
        {
            AiCvImportResultMerger.MergeFragment(accumulated, AiImportTestHelpers.WorkJson(($"Company{i}", 2000 + i)));
        }

        var summary = AiImportCarryForwardSummaryBuilder.Build(accumulated, 280);
        Assert.True(summary.Length <= 280);
    }

    [Fact]
    public void EdgeCase_IsEnabledReflectsEnvironmentVariable()
    {
        var original = Environment.GetEnvironmentVariable("REVITAE_IMPORT_DEBUG");
        try
        {
            Environment.SetEnvironmentVariable("REVITAE_IMPORT_DEBUG", "1");
            Assert.True(AiImportDiagnosticsLogger.IsEnabled);
        }
        finally
        {
            Environment.SetEnvironmentVariable("REVITAE_IMPORT_DEBUG", original);
        }
    }
}

internal static class JsonObjectMergeExtensions
{
    public static JsonObject AlsoMerge(this JsonObject target, JsonObject fragment)
    {
        AiCvImportResultMerger.MergeFragment(target, fragment);
        return target;
    }
}
