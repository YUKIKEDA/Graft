using System.Collections.Immutable;
using Graft.Instrumentation.Analyzer.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Graft.Instrumentation.Analyzer;

/// <summary>
/// Reports <c>GRAFT001</c> when <c>Agent.Start</c> is invoked without <c>GRAFT_TEST</c>.
/// </summary>
/// <remarks>
/// Judgment uses only the compilation preprocessor symbol <c>GRAFT_TEST</c>
/// (see project.md Q35). Configuration names and syntactic <c>#if</c> heuristics are not used.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AgentStartRequiresGraftTestAnalyzer : DiagnosticAnalyzer
{
    private const string AgentMetadataName = "Graft.Instrumentation.Agent";
    private const string StartMethodName = "Start";
    private const string GraftTestSymbol = "GRAFT_TEST";

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(GraftDescriptors.Graft001AgentStartRequiresGraftTest);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static compilationStartContext =>
        {
            if (DefinesGraftTest(compilationStartContext.Compilation))
            {
                return;
            }

            var agentType = compilationStartContext.Compilation.GetTypeByMetadataName(AgentMetadataName);
            if (agentType is null)
            {
                return;
            }

            compilationStartContext.RegisterSyntaxNodeAction(
                analysisContext => AnalyzeInvocation(analysisContext, agentType),
                SyntaxKind.InvocationExpression
            );
        });
    }

    private static bool DefinesGraftTest(Compilation compilation)
    {
        foreach (var tree in compilation.SyntaxTrees)
        {
            if (tree.Options is not CSharpParseOptions parseOptions)
            {
                continue;
            }

            foreach (var symbol in parseOptions.PreprocessorSymbolNames)
            {
                if (symbol == GraftTestSymbol)
                {
                    return true;
                }
            }

            // Preprocessor symbols are project-wide; one C# tree is enough.
            return false;
        }

        return false;
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context, INamedTypeSymbol agentType)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken);
        if (GetTargetMethod(symbolInfo) is not { } method || method.Name != StartMethodName || method.ContainingType is null)
        {
            return;
        }

        if (!SymbolEqualityComparer.Default.Equals(method.ContainingType.OriginalDefinition, agentType))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(GraftDescriptors.Graft001AgentStartRequiresGraftTest, invocation.GetLocation()));
    }

    private static IMethodSymbol? GetTargetMethod(SymbolInfo symbolInfo)
    {
        if (symbolInfo.Symbol is IMethodSymbol method)
        {
            return method;
        }

        foreach (var candidate in symbolInfo.CandidateSymbols)
        {
            if (candidate is IMethodSymbol candidateMethod)
            {
                return candidateMethod;
            }
        }

        return null;
    }
}
