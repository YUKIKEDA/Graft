namespace Graft.Instrumentation.Wpf;

/// <summary>
/// Marker for the WPF adapter assembly.
/// </summary>
/// <remarks>
/// Agent types and Harmony patches are compiled only when this project is built with
/// <c>GraftTest=true</c>. A normal Debug or Release reference does not contain them.
/// </remarks>
internal static class WpfInstrumentationAssembly { }
