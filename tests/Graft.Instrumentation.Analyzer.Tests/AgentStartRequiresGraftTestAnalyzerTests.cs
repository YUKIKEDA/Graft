using Graft.Instrumentation.Analyzer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace Graft.Instrumentation.Analyzer.Tests;

public sealed class AgentStartRequiresGraftTestAnalyzerTests
{
    /// <summary>
    /// Agent.Start without GRAFT_TEST reports GRAFT001.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Compilation does not define GRAFT_TEST
    /// - Metadata reference to Graft.Instrumentation (built with GRAFT_TEST so Start exists)
    ///
    /// Steps:
    /// - Analyze a source that invokes Agent.Start()
    ///
    /// Expected:
    /// - One GRAFT001 diagnostic on the invocation
    /// </remarks>
    [Fact]
    public async Task AgentStart_WithoutGraftTest_ReportsGraft001()
    {
        const string testCode = """
            using Graft.Instrumentation;

            class C
            {
                void M()
                {
                    {|GRAFT001:Agent.Start()|};
                }
            }
            """;

        await RunAsync(testCode, defineGraftTest: false);
    }

    /// <summary>
    /// Agent.Start with GRAFT_TEST defined does not report GRAFT001.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Compilation defines GRAFT_TEST
    /// - Metadata reference to Graft.Instrumentation
    ///
    /// Steps:
    /// - Analyze a source that invokes Agent.Start()
    ///
    /// Expected:
    /// - No diagnostics
    /// </remarks>
    [Fact]
    public async Task AgentStart_WithGraftTest_NoDiagnostic()
    {
        const string testCode = """
            using Graft.Instrumentation;

            class C
            {
                void M()
                {
                    Agent.Start();
                }
            }
            """;

        await RunAsync(testCode, defineGraftTest: true);
    }

    /// <summary>
    /// Unrelated Start methods are ignored.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Compilation does not define GRAFT_TEST
    /// - Local type also named Agent with Start()
    ///
    /// Steps:
    /// - Analyze invocation of the local Agent.Start
    ///
    /// Expected:
    /// - No GRAFT001 (only Graft.Instrumentation.Agent.Start is in scope for the rule)
    /// </remarks>
    [Fact]
    public async Task OtherAgentStart_WithoutGraftTest_NoDiagnostic()
    {
        const string testCode = """
            class Agent
            {
                public static void Start() { }
            }

            class C
            {
                void M()
                {
                    Agent.Start();
                }
            }
            """;

        await RunAsync(testCode, defineGraftTest: false, referenceInstrumentation: false);
    }

    private static async Task RunAsync(string testCode, bool defineGraftTest, bool referenceInstrumentation = true)
    {
        var test = new CSharpAnalyzerTest<AgentStartRequiresGraftTestAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        };

        if (referenceInstrumentation)
        {
            test.TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(typeof(Agent).Assembly.Location));
        }

        if (defineGraftTest)
        {
            test.SolutionTransforms.Add(
                static (solution, projectId) =>
                {
                    var project = solution.GetProject(projectId)!;
                    var options = (CSharpParseOptions)project.ParseOptions!;
                    return solution.WithProjectParseOptions(projectId, options.WithPreprocessorSymbols("GRAFT_TEST"));
                }
            );
        }

        await test.RunAsync();
    }
}
