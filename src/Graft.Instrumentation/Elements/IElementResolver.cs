namespace Graft.Instrumentation.Elements;

#if GRAFT_TEST

/// <summary>
/// Framework-specific live element resolution used by invoke / setValue.
/// </summary>
public interface IElementResolver
{
    /// <summary>
    /// Resolves a single element, marshaling to the UI thread as required.
    /// </summary>
    /// <param name="selector">Selector (automationId required; runtimeId optional).</param>
    /// <returns>The matched live element.</returns>
    /// <exception cref="ElementResolveException">
    /// Thrown when the selector is invalid, no element matches, or multiple elements match.
    /// </exception>
    ResolvedElement Resolve(ElementSelector selector);
}

#endif
