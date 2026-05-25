using ReVitae.Core.Export;
using ReVitae.Core.Import;
using ReVitae.Core.Projects;

namespace ReVitae.Tests.Projects;

public sealed class CvProjectLifecycleEdgeCaseTests
{
	private static CvProjectSaveRequest CreateRequest(string firstName = "Autosave") =>
		new(
			CvExportSourceDataFactory.Create(
				new PersonalInformationImport { FirstName = firstName, LastName = "Test" },
				[], [], [], [], [], [], [], null),
			CvProjectSettings.CreateDefault(CvExportTemplateId.CleanTopHeader));

	[Fact]
	public void MarkDirty_TryWriteAutosave_WritesRecoveryWhenDebounceElapsed()
	{
		var clock = new FakeClock(DateTimeOffset.UtcNow);
		var store = new InMemoryProjectAutosaveStore(clock);
		var lifecycle = new CvProjectLifecycleService(clock, store, autosaveIntervalSeconds: 0, debounceInterval: TimeSpan.Zero);
		lifecycle.MarkDirty();

		var result = lifecycle.TryWriteAutosaveRecovery(CreateRequest(), hasFormData: true);

		Assert.Equal(AutosaveWriteStatus.Written, result.Status);
		Assert.True(store.RecoveryExists());
		Assert.Equal("Autosave", store.LoadRecovery().Import!.Personal.FirstName);
	}

	[Fact]
	public void TryWriteAutosaveRecovery_SkipsDebounceWhenLastAutosaveRecent()
	{
		var clock = new FakeClock(DateTimeOffset.UtcNow);
		var store = new InMemoryProjectAutosaveStore(clock);
		var lifecycle = new CvProjectLifecycleService(clock, store, autosaveIntervalSeconds: 60, debounceInterval: TimeSpan.FromSeconds(5));
		lifecycle.MarkDirty();

		Assert.Equal(AutosaveWriteStatus.Written, lifecycle.TryWriteAutosaveRecovery(CreateRequest("First"), hasFormData: true).Status);
		Assert.Equal(AutosaveWriteStatus.SkippedDebounce, lifecycle.TryWriteAutosaveRecovery(CreateRequest("Second"), hasFormData: true).Status);
	}

	[Fact]
	public void RapidEdits_OnlyLastStatePersistedAfterDebounce()
	{
		var clock = new FakeClock(DateTimeOffset.UtcNow);
		var store = new InMemoryProjectAutosaveStore(clock);
		var lifecycle = new CvProjectLifecycleService(clock, store, autosaveIntervalSeconds: 0, debounceInterval: TimeSpan.FromSeconds(2));
		lifecycle.MarkDirty();
		lifecycle.TryWriteAutosaveRecovery(CreateRequest("Edit1"), hasFormData: true);
		lifecycle.MarkDirty();
		lifecycle.TryWriteAutosaveRecovery(CreateRequest("Edit2"), hasFormData: true);
		clock.Advance(TimeSpan.FromSeconds(3));

		var result = lifecycle.TryWriteAutosaveRecovery(CreateRequest("Edit3"), hasFormData: true);

		Assert.Equal(AutosaveWriteStatus.Written, result.Status);
		Assert.Equal("Edit3", store.LoadRecovery().Import!.Personal.FirstName);
	}

	[Fact]
	public void ClockJump_AllowsImmediateAutosaveAfterSleepWake()
	{
		var clock = new FakeClock(DateTimeOffset.UtcNow);
		var store = new InMemoryProjectAutosaveStore(clock);
		var lifecycle = new CvProjectLifecycleService(clock, store, autosaveIntervalSeconds: 60, debounceInterval: TimeSpan.FromSeconds(30));
		lifecycle.MarkDirty();
		lifecycle.TryWriteAutosaveRecovery(CreateRequest("BeforeSleep"), hasFormData: true);
		lifecycle.MarkDirty();
		clock.Advance(TimeSpan.FromHours(8));

		var result = lifecycle.TryWriteAutosaveRecovery(CreateRequest("AfterWake"), hasFormData: true);

		Assert.Equal(AutosaveWriteStatus.Written, result.Status);
		Assert.Equal("AfterWake", store.LoadRecovery().Import!.Personal.FirstName);
	}

	[Fact]
	public void OnManualSaveSucceeded_ClearsDirtyAndDeletesRecovery()
	{
		var clock = new FakeClock(DateTimeOffset.UtcNow);
		var store = new InMemoryProjectAutosaveStore(clock);
		var lifecycle = new CvProjectLifecycleService(clock, store, autosaveIntervalSeconds: 0, debounceInterval: TimeSpan.Zero);
		lifecycle.MarkDirty();
		lifecycle.TryWriteAutosaveRecovery(CreateRequest(), hasFormData: true);

		lifecycle.OnManualSaveSucceeded();

		Assert.False(lifecycle.IsDirty);
		Assert.False(store.RecoveryExists());
	}

	[Fact]
	public void TryWriteAutosaveRecovery_SkipsWhenNotDirty()
	{
		var lifecycle = new CvProjectLifecycleService(new FakeClock(), new InMemoryProjectAutosaveStore());

		var result = lifecycle.TryWriteAutosaveRecovery(CreateRequest(), hasFormData: true);

		Assert.Equal(AutosaveWriteStatus.SkippedNotDirty, result.Status);
	}

	[Fact]
	public void TryWriteAutosaveRecovery_SkipsWhenNoFormData()
	{
		var lifecycle = new CvProjectLifecycleService(new FakeClock(), new InMemoryProjectAutosaveStore());
		lifecycle.MarkDirty();

		var result = lifecycle.TryWriteAutosaveRecovery(CreateRequest(), hasFormData: false);

		Assert.Equal(AutosaveWriteStatus.SkippedNoFormData, result.Status);
	}

	[Fact]
	public void DiskFullOnAutosave_RetainsDirtyFlagAndReturnsFailed()
	{
		var store = new InMemoryProjectAutosaveStore { ThrowOnWrite = true };
		var lifecycle = new CvProjectLifecycleService(new FakeClock(), store, autosaveIntervalSeconds: 0, debounceInterval: TimeSpan.Zero);
		lifecycle.MarkDirty();

		var result = lifecycle.TryWriteAutosaveRecovery(CreateRequest(), hasFormData: true);

		Assert.Equal(AutosaveWriteStatus.Failed, result.Status);
		Assert.NotNull(result.Error);
		Assert.True(lifecycle.IsDirty);
		Assert.False(store.RecoveryExists());
	}

	[Fact]
	public void DisposeMidAutosaveSimulation_FailedWriteLeavesRecoveryAbsent()
	{
		var store = new ThrowingMidWriteAutosaveStore();
		var lifecycle = new CvProjectLifecycleService(new FakeClock(), store, autosaveIntervalSeconds: 0, debounceInterval: TimeSpan.Zero);
		lifecycle.MarkDirty();

		var result = lifecycle.TryWriteAutosaveRecovery(CreateRequest(), hasFormData: true);

		Assert.Equal(AutosaveWriteStatus.Failed, result.Status);
		Assert.False(store.RecoveryExists());
		Assert.True(lifecycle.IsDirty);
	}

	[Fact]
	public void OnProjectLoaded_ClearsDirtyAndRecovery()
	{
		var store = new InMemoryProjectAutosaveStore();
		var lifecycle = new CvProjectLifecycleService(new FakeClock(), store, autosaveIntervalSeconds: 0, debounceInterval: TimeSpan.Zero);
		lifecycle.MarkDirty();
		lifecycle.TryWriteAutosaveRecovery(CreateRequest(), hasFormData: true);

		lifecycle.OnProjectLoaded(fromRecovery: false);

		Assert.False(lifecycle.IsDirty);
		Assert.False(store.RecoveryExists());
	}

	[Fact]
	public void HasUnsavedChanges_TracksDirtyState()
	{
		var lifecycle = new CvProjectLifecycleService(new FakeClock(), new InMemoryProjectAutosaveStore());

		Assert.False(lifecycle.HasUnsavedChanges());
		lifecycle.MarkDirty();
		Assert.True(lifecycle.HasUnsavedChanges());
		lifecycle.ClearDirty();
		Assert.False(lifecycle.HasUnsavedChanges());
	}

	[Fact]
	public void SetDirty_OverridesDirtyFlag()
	{
		var lifecycle = new CvProjectLifecycleService(new FakeClock(), new InMemoryProjectAutosaveStore());
		lifecycle.MarkDirty();
		lifecycle.SetDirty(false);
		Assert.False(lifecycle.IsDirty);
	}

	private sealed class FakeClock(DateTimeOffset? initial = null) : IClock
	{
		public DateTimeOffset UtcNow { get; private set; } = initial ?? DateTimeOffset.UtcNow;

		public void Advance(TimeSpan delta) => UtcNow = UtcNow.Add(delta);
	}

	private sealed class InMemoryProjectAutosaveStore : IProjectAutosaveStore
	{
		private readonly IClock? _clock;
		private CvProjectSaveRequest? _recovery;
		private DateTimeOffset? _lastWriteUtc;

		public InMemoryProjectAutosaveStore(IClock? clock = null) => _clock = clock;

		public bool ThrowOnWrite { get; set; }

		public void WriteRecovery(CvProjectSaveRequest request)
		{
			if (ThrowOnWrite)
			{
				throw new IOException("Simulated disk full.");
			}

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

	private sealed class ThrowingMidWriteAutosaveStore : IProjectAutosaveStore
	{
		public void WriteRecovery(CvProjectSaveRequest request) =>
			throw new InvalidOperationException("Simulated dispose mid-autosave.");

		public void DeleteRecovery()
		{
		}

		public bool RecoveryExists() => false;

		public CvProjectLoadResult LoadRecovery() =>
			new(false, null, null, "missing");

		public DateTimeOffset? GetRecoveryLastWriteUtc() => null;
	}
}
