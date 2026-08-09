using Microsoft.CodeAnalysis;

namespace Graft.Instrumentation.Analyzer.Diagnostics;

/// <summary>
/// Shared <see cref="DiagnosticDescriptor"/> instances for Graft analyzers.
/// </summary>
internal static class GraftDescriptors
{
    /// <summary>
    /// Error when <c>Agent.Start</c> is referenced without <c>GRAFT_TEST</c>.
    /// </summary>
    public static readonly DiagnosticDescriptor Graft001AgentStartRequiresGraftTest = new(
        id: GraftDiagnosticIds.Graft001,
        title: "Agent.Start requires GRAFT_TEST",
        messageFormat: "Agent.Start requires the GRAFT_TEST preprocessor symbol. Enable GraftTest=true (or define GRAFT_TEST) for builds that call Start.",
        category: "Graft.Safety",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Calling Agent.Start outside a GRAFT_TEST compilation is not allowed. The WPF/Avalonia packages ship this analyzer so accidental production enablement fails at build time."
    );
}
