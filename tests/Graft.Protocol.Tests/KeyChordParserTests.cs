using Graft.Protocol;

namespace Graft.Protocol.Tests;

public sealed class KeyChordParserTests
{
    /// <summary>
    /// Parses single keys and modifier chords into canonical tokens.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - None
    ///
    /// Steps:
    /// - KeyChordParser.Parse for representative chords
    ///
    /// Expected:
    /// - Modifiers and key are canonical (Control/Alt/Shift; A–Z; named keys)
    /// </remarks>
    [Theory]
    [InlineData("Enter", "", "Enter")]
    [InlineData("delete", "", "Delete")]
    [InlineData("F1", "", "F1")]
    [InlineData("f5", "", "F5")]
    [InlineData("NumPad0", "", "NumPad0")]
    [InlineData("NumPadEnter", "", "NumPadEnter")]
    [InlineData("Control+F12", "Control", "F12")]
    [InlineData("Control+A", "Control", "A")]
    [InlineData("control+a", "Control", "A")]
    [InlineData("Shift+Tab", "Shift", "Tab")]
    [InlineData("Control+Shift+ArrowUp", "Control,Shift", "ArrowUp")]
    [InlineData("Alt+3", "Alt", "3")]
    public void Parse_ValidChords_ReturnsCanonical(string input, string modsCsv, string key)
    {
        var chord = KeyChordParser.Parse(input);
        var expectedMods = string.IsNullOrEmpty(modsCsv) ? Array.Empty<string>() : modsCsv.Split(',');
        Assert.Equal(expectedMods, chord.Modifiers);
        Assert.Equal(key, chord.Key);
    }

    /// <summary>
    /// Rejects empty, unknown, multi-key, and modifier-only chords.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - None
    ///
    /// Steps:
    /// - KeyChordParser.Parse on invalid inputs
    ///
    /// Expected:
    /// - ArgumentException
    /// </remarks>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Control")]
    [InlineData("Control+A+Delete")]
    [InlineData("Win+A")]
    [InlineData("Meta+A")]
    [InlineData("Control+")]
    [InlineData("+A")]
    public void Parse_InvalidChords_Throws(string input)
    {
        Assert.Throws<ArgumentException>(() => KeyChordParser.Parse(input));
    }
}
