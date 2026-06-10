using System.Reflection;

namespace ReVitae.Tests;

public sealed class TestCountBaselineTests
{
	/// <summary>
	/// Documented minimum test count for the drift guard (raised in prompt 045).
	/// Update when adding suites; must stay in sync with README badge.
	/// </summary>
	public const int MinimumTestCount = 2152;

	[Fact]
	public void MinimumTestCount_MeetsPrompt044Baseline()
	{
		Assert.True(MinimumTestCount >= 1685);
	}

	[Fact]
	public void Assembly_DiscoveredTestMethods_MeetMinimumBaseline()
	{
		var testMethods = typeof(TestCountBaselineTests).Assembly
			.GetTypes()
			.Where(type => type.GetCustomAttributes(typeof(CollectionDefinitionAttribute), inherit: false).Length == 0)
			.SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
			.Where(method =>
				method.GetCustomAttributes(typeof(FactAttribute), inherit: false).Length > 0
				|| method.GetCustomAttributes(typeof(TheoryAttribute), inherit: false).Length > 0)
			.ToArray();

		Assert.True(testMethods.Length >= 200, $"Expected at least 200 test methods, found {testMethods.Length}.");
	}
}
