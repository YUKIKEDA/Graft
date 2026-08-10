namespace Graft.Protocol;

/// <summary>
/// One keyboard chord: zero or more modifiers plus a single key (wire / DSL unit).
/// </summary>
/// <param name="Modifiers">Canonical modifiers in parse order (<c>Control</c>, <c>Alt</c>, <c>Shift</c>).</param>
/// <param name="Key">Canonical key token (e.g. <c>A</c>, <c>Enter</c>, <c>ArrowUp</c>).</param>
public sealed record KeyChord(IReadOnlyList<string> Modifiers, string Key);
