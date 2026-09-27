using System.Reflection;
using Graft.Core.Diagnostics;
using Graft.Core.Scenario;
using Graft.Protocol;

namespace Graft.Core.Tests;

public sealed class ActionNameTests
{
    /// <summary>
    /// Every wire method name is also a failure-step name.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - ProtocolMethods and FailureSteps expose public string constants
    ///
    /// Steps:
    /// - Read both constant sets
    ///
    /// Expected:
    /// - Each ProtocolMethods value appears in FailureSteps
    /// </remarks>
    [Fact]
    public void EveryProtocolMethod_HasMatchingFailureStep()
    {
        var methods = ConstStrings(typeof(ProtocolMethods));
        var steps = ConstStrings(typeof(FailureSteps));

        Assert.Empty(methods.Except(steps, StringComparer.Ordinal));
    }

    /// <summary>
    /// Scenario action names alias failure steps, except launch.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - ScenarioActions and FailureSteps expose public string constants
    ///
    /// Steps:
    /// - Compare the two sets
    ///
    /// Expected:
    /// - launch is a Scenario action and not a failure step
    /// - every other Scenario action is a failure step
    /// </remarks>
    [Fact]
    public void ScenarioActions_AliasFailureSteps_ExceptLaunch()
    {
        var steps = ConstStrings(typeof(FailureSteps));
        var actions = ConstStrings(typeof(ScenarioActions));

        Assert.Contains(ScenarioActions.Launch, actions);
        Assert.DoesNotContain(ScenarioActions.Launch, steps);
        Assert.Empty(actions.Where(action => action != ScenarioActions.Launch).Except(steps, StringComparer.Ordinal));
    }

    private static HashSet<string> ConstStrings(Type type) =>
        type.GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToHashSet(StringComparer.Ordinal);
}
