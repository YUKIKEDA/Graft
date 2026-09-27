using System.Reflection;
using Graft.Instrumentation.Pipe;
using Graft.Protocol;

namespace Graft.Instrumentation.Tests;

public sealed class DispatchTableTests
{
    /// <summary>
    /// Every wire method name has a dispatch entry.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - ProtocolMethods exposes public string constants
    /// - AgentPipeServer builds its dispatch table at type initialization
    ///
    /// Steps:
    /// - Read both sets
    ///
    /// Expected:
    /// - The two sets contain the same method names
    /// </remarks>
    [Fact]
    public void EveryProtocolMethod_HasDispatchEntry()
    {
        var methods = ConstStrings(typeof(ProtocolMethods));
        var dispatched = AgentPipeServer.DispatchedMethods;

        Assert.Empty(methods.Except(dispatched, StringComparer.Ordinal));
        Assert.Empty(dispatched.Except(methods, StringComparer.Ordinal));
    }

    private static HashSet<string> ConstStrings(Type type) =>
        type.GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToHashSet(StringComparer.Ordinal);
}
