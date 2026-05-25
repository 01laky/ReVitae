using ReVitae.Core.Ai.Ollama;

namespace ReVitae.Tests.Ai.Ollama;

public sealed class OllamaServeSupervisorEdgeCaseTests
{
	[Fact]
	public void DefaultOllamaServeSupervisor_Instance_IsSingleton()
	{
		Assert.Same(DefaultOllamaServeSupervisor.Instance, DefaultOllamaServeSupervisor.Instance);
	}

	[Fact]
	public void TryStartManagedServe_ReturnsConsistentResultOnRepeatCalls()
	{
		var supervisor = DefaultOllamaServeSupervisor.Instance;
		var first = supervisor.TryStartManagedServe();
		var second = supervisor.TryStartManagedServe();

		Assert.Equal(first, second);
	}

	[Fact]
	public void TryStartManagedServe_IsIdempotentWhenManagedBinaryPresent()
	{
		if (!OllamaPaths.IsManagedInstallPresent())
		{
			return;
		}

		var supervisor = DefaultOllamaServeSupervisor.Instance;
		var first = supervisor.TryStartManagedServe();
		if (!first)
		{
			return;
		}

		Assert.True(supervisor.TryStartManagedServe());
	}

	[Fact]
	public void FakeSupervisor_StartStop_IsIdempotent()
	{
		var fake = new FakeOllamaServeSupervisor();

		Assert.True(fake.TryStartManagedServe());
		Assert.True(fake.TryStartManagedServe());
		Assert.Equal(2, fake.StartCount);
	}

	[Fact]
	public void FakeSupervisor_Reset_AllowsRestart()
	{
		var fake = new FakeOllamaServeSupervisor();
		Assert.True(fake.TryStartManagedServe());
		fake.Reset();

		Assert.True(fake.TryStartManagedServe());
		Assert.Equal(1, fake.StartCount);
	}

	[Fact]
	public void FakeSupervisor_CanSimulateFailure()
	{
		var fake = new FakeOllamaServeSupervisor { ShouldSucceed = false };

		Assert.False(fake.TryStartManagedServe());
		Assert.Equal(1, fake.StartCount);
	}

	private sealed class FakeOllamaServeSupervisor : IOllamaServeSupervisor
	{
		public bool ShouldSucceed { get; set; } = true;

		public int StartCount { get; private set; }

		public bool TryStartManagedServe()
		{
			StartCount++;
			return ShouldSucceed;
		}

		public void Reset() => StartCount = 0;
	}
}
