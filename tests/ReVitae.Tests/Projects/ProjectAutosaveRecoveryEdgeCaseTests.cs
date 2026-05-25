using ReVitae.Core.Export;
using ReVitae.Core.Import;
using ReVitae.Core.Projects;

namespace ReVitae.Tests.Projects;

public sealed class ProjectAutosaveRecoveryEdgeCaseTests
{
	private static CvProjectSaveRequest CreateRequest(string firstName) =>
		new(
			CvExportSourceDataFactory.Create(
				new PersonalInformationImport { FirstName = firstName, LastName = "Recovery" },
				[], [], [], [], [], [], [], null),
			CvProjectSettings.CreateDefault(CvExportTemplateId.CleanTopHeader));

	[Fact]
	public void EvaluateRecovery_NoneAvailableWhenStoreEmpty()
	{
		var lifecycle = CreateLifecycle(new InMemoryRecoveryStore());

		var evaluation = lifecycle.EvaluateRecovery(primaryProjectLastWriteUtc: DateTimeOffset.UtcNow);

		Assert.Equal(RecoveryRestoreRecommendation.NoneAvailable, evaluation.Recommendation);
	}

	[Fact]
	public void EvaluateRecovery_DiscardCorruptRecoveryWhenLoadFails()
	{
		var store = new InMemoryRecoveryStore();
		store.WriteRecovery(CreateRequest("Recovery"));
		store.Corrupt = true;
		var lifecycle = CreateLifecycle(store);

		var evaluation = lifecycle.EvaluateRecovery(primaryProjectLastWriteUtc: null);

		Assert.Equal(RecoveryRestoreRecommendation.DiscardCorruptRecovery, evaluation.Recommendation);
		Assert.False(evaluation.RecoveryLoadResult!.Success);
	}

	[Fact]
	public void EvaluateRecovery_PreferPrimaryWhenPrimaryIsNewer()
	{
		var clock = new SteppedClock(DateTimeOffset.UtcNow);
		var store = new InMemoryRecoveryStore(clock);
		store.WriteRecovery(CreateRequest("Recovery"));
		clock.Advance(TimeSpan.FromMinutes(5));
		var primaryWrite = clock.UtcNow;

		var lifecycle = CreateLifecycle(store);
		var evaluation = lifecycle.EvaluateRecovery(primaryWrite);

		Assert.Equal(RecoveryRestoreRecommendation.PreferPrimaryProject, evaluation.Recommendation);
		Assert.True(evaluation.RecoveryLoadResult!.Success);
	}

	[Fact]
	public void EvaluateRecovery_RestoreRecoveryWhenNewerThanPrimary()
	{
		var clock = new SteppedClock(DateTimeOffset.UtcNow.AddHours(-1));
		var primaryWrite = clock.UtcNow;
		clock.Advance(TimeSpan.FromHours(2));
		var store = new InMemoryRecoveryStore(clock);
		store.WriteRecovery(CreateRequest("NewerRecovery"));

		var lifecycle = CreateLifecycle(store);
		var evaluation = lifecycle.EvaluateRecovery(primaryWrite);

		Assert.Equal(RecoveryRestoreRecommendation.RestoreRecovery, evaluation.Recommendation);
		Assert.Equal("NewerRecovery", evaluation.RecoveryLoadResult!.Import!.Personal.FirstName);
	}

	[Fact]
	public void OnProjectLoaded_DeletesRecoveryAfterRestore()
	{
		var store = new InMemoryRecoveryStore();
		store.WriteRecovery(CreateRequest("Restored"));
		var lifecycle = CreateLifecycle(store);

		lifecycle.OnProjectLoaded(fromRecovery: true);

		Assert.False(store.RecoveryExists());
	}

	[Fact]
	public void DiscardRecovery_RemovesRecoveryFile()
	{
		var store = new InMemoryRecoveryStore();
		store.WriteRecovery(CreateRequest("Discard"));
		var lifecycle = CreateLifecycle(store);

		lifecycle.DiscardRecovery();

		Assert.False(store.RecoveryExists());
	}

	[Fact]
	public void EvaluateRecovery_RestoreWhenPrimaryTimestampMissing()
	{
		var store = new InMemoryRecoveryStore();
		store.WriteRecovery(CreateRequest("OnlyRecovery"));
		var lifecycle = CreateLifecycle(store);

		var evaluation = lifecycle.EvaluateRecovery(primaryProjectLastWriteUtc: null);

		Assert.Equal(RecoveryRestoreRecommendation.RestoreRecovery, evaluation.Recommendation);
	}

	[Fact]
	public void RecoveryExists_ReflectsStoreState()
	{
		var store = new InMemoryRecoveryStore();
		var lifecycle = CreateLifecycle(store);

		Assert.False(lifecycle.RecoveryExists());
		store.WriteRecovery(CreateRequest("Exists"));
		Assert.True(lifecycle.RecoveryExists());
	}

	private static CvProjectLifecycleService CreateLifecycle(IProjectAutosaveStore store) =>
		new(new SteppedClock(), store, autosaveIntervalSeconds: 0, debounceInterval: TimeSpan.Zero);

	private sealed class SteppedClock(DateTimeOffset? initial = null) : IClock
	{
		public DateTimeOffset UtcNow { get; private set; } = initial ?? DateTimeOffset.UtcNow;

		public void Advance(TimeSpan delta) => UtcNow = UtcNow.Add(delta);
	}

	private sealed class InMemoryRecoveryStore : IProjectAutosaveStore
	{
		private readonly SteppedClock? _clock;
		private CvProjectSaveRequest? _recovery;
		private DateTimeOffset? _lastWriteUtc;

		public InMemoryRecoveryStore(SteppedClock? clock = null) => _clock = clock;

		public bool Corrupt { get; set; }

		public void WriteRecovery(CvProjectSaveRequest request)
		{
			_recovery = request;
			_lastWriteUtc = _clock?.UtcNow ?? DateTimeOffset.UtcNow;
		}

		public void DeleteRecovery() => _recovery = null;

		public bool RecoveryExists() => _recovery is not null;

		public CvProjectLoadResult LoadRecovery()
		{
			if (_recovery is null)
			{
				return new CvProjectLoadResult(false, null, null, "missing");
			}

			if (Corrupt)
			{
				return new CvProjectLoadResult(false, null, null, "corrupt");
			}

			return new CvProjectLoadResult(
				true,
				new CvImportResult
				{
					Success = true,
					Personal = _recovery.Source.Personal,
				},
				_recovery.Settings,
				null);
		}

		public DateTimeOffset? GetRecoveryLastWriteUtc() => _lastWriteUtc;
	}
}
