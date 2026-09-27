using System.Reflection;
using Graft.Instrumentation;

namespace Graft.Instrumentation.Surface.Tests;

/// <summary>
/// Guards the compile-time removal of the agent from non-GraftTest builds.
/// </summary>
public sealed class NonGraftTestSurfaceTests
{
    /// <summary>
    /// A Debug build of Graft.Instrumentation does not expose the agent entry points.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - This test project references Graft.Instrumentation without GraftTest=true
    ///
    /// Steps:
    /// - Reflect Agent.Start and the pipe server / input injector types
    ///
    /// Expected:
    /// - Start is absent
    /// - AgentPipeServer and InputInjector are absent
    /// </remarks>
    [Fact]
    public void Instrumentation_WithoutGraftTest_OmitsAgentSurface()
    {
        var assembly = typeof(Agent).Assembly;

        Assert.Null(typeof(Agent).GetMethod("Start"));
        Assert.Null(typeof(Agent).GetMethod("Stop"));
        Assert.Null(assembly.GetType("Graft.Instrumentation.Pipe.AgentPipeServer"));
        Assert.Null(assembly.GetType("Graft.Instrumentation.Input.InputInjector"));
    }

    /// <summary>
    /// A Debug build of Graft.Instrumentation.Wpf does not contain WpfGraft or Harmony patches.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - This test project references Graft.Instrumentation.Wpf without GraftTest=true
    ///
    /// Steps:
    /// - Load the WPF adapter assembly and inspect types and references
    ///
    /// Expected:
    /// - WpfGraft and MessageBoxPatch are absent
    /// - The assembly does not reference 0Harmony
    /// </remarks>
    [Fact]
    public void Wpf_WithoutGraftTest_OmitsPatches()
    {
        var assembly = Assembly.Load("Graft.Instrumentation.Wpf");

        Assert.Null(assembly.GetType("Graft.Instrumentation.Wpf.WpfGraft"));
        Assert.Null(assembly.GetType("Graft.Instrumentation.Wpf.Dialogs.MessageBoxPatch"));
        Assert.DoesNotContain(assembly.GetReferencedAssemblies(), name => name.Name == "0Harmony");
    }
}
