namespace WpfUi.Gallery.Graft.Tests;

/// <summary>
/// Explicit Skip ledger for product non-goals (competitive-gap / task SoT).
/// </summary>
public sealed class GapSkipE2ETests
{
    /// <summary>Desktop-wide screenshot is out of scope.</summary>
    /// <remarks>
    /// Preconditions: N/A
    /// Steps: Skip
    /// Expected: Skipped
    /// </remarks>
    [Fact(Skip = "P04 desktop-wide screenshot is out of scope")]
    public void DesktopScreenshot_Skipped() { }

    /// <summary>Video / trace recording is out of scope.</summary>
    /// <remarks>
    /// Preconditions: N/A
    /// Steps: Skip
    /// Expected: Skipped
    /// </remarks>
    [Fact(Skip = "P05 video/trace recording is out of scope")]
    public void VideoTrace_Skipped() { }

    /// <summary>Fuzzy selector matching is out of scope.</summary>
    /// <remarks>
    /// Preconditions: N/A
    /// Steps: Skip
    /// Expected: Skipped
    /// </remarks>
    [Fact(Skip = "F07 fuzzy/edit-distance selector is out of scope")]
    public void FuzzySelector_Skipped() { }

    /// <summary>Image expect/diff is optional and not implemented.</summary>
    /// <remarks>
    /// Preconditions: N/A
    /// Steps: Skip
    /// Expected: Skipped
    /// </remarks>
    [Fact(Skip = "P03 image expect/diff is optional and not implemented")]
    public void ImageDiff_Skipped() { }

    /// <summary>Win key chords excluded.</summary>
    /// <remarks>
    /// Preconditions: N/A
    /// Steps: Skip
    /// Expected: Skipped
    /// </remarks>
    [Fact(Skip = "K04 Win key excluded from Graft key coverage")]
    public void WinKey_Skipped() { }

    /// <summary>Toast-specific API is optional and not implemented.</summary>
    /// <remarks>
    /// Preconditions: N/A
    /// Steps: Skip
    /// Expected: Skipped
    /// </remarks>
    [Fact(Skip = "W12 toast/temporary notification API is optional and not implemented")]
    public void ToastApi_Skipped() { }
}
