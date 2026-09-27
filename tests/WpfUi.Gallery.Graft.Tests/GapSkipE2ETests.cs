namespace WpfUi.Gallery.Graft.Tests;

/// <summary>
/// Explicit Skip ledger for product non-goals (competitive-gap / task SoT).
/// </summary>
public sealed class GapSkipE2ETests
{
    /// <summary>Desktop-wide screenshot is非目標.</summary>
    /// <remarks>
    /// Preconditions: N/A
    /// Steps: Skip
    /// Expected: Skipped
    /// </remarks>
    [Fact(Skip = "P04 desktop-wide screenshot is非目標")]
    public void DesktopScreenshot_Skipped() { }

    /// <summary>Video / trace recording is非目標.</summary>
    /// <remarks>
    /// Preconditions: N/A
    /// Steps: Skip
    /// Expected: Skipped
    /// </remarks>
    [Fact(Skip = "P05 video/trace recording is非目標")]
    public void VideoTrace_Skipped() { }

    /// <summary>Fuzzy selector matching is非目標.</summary>
    /// <remarks>
    /// Preconditions: N/A
    /// Steps: Skip
    /// Expected: Skipped
    /// </remarks>
    [Fact(Skip = "F07 fuzzy/edit-distance selector is非目標")]
    public void FuzzySelector_Skipped() { }

    /// <summary>Image expect/diff is任意未実装.</summary>
    /// <remarks>
    /// Preconditions: N/A
    /// Steps: Skip
    /// Expected: Skipped
    /// </remarks>
    [Fact(Skip = "P03 image expect/diff is任意/未実装")]
    public void ImageDiff_Skipped() { }

    /// <summary>Win key chords excluded.</summary>
    /// <remarks>
    /// Preconditions: N/A
    /// Steps: Skip
    /// Expected: Skipped
    /// </remarks>
    [Fact(Skip = "K04 Win key excluded from Graft key coverage")]
    public void WinKey_Skipped() { }

    /// <summary>Toast-specific API is任意未実装.</summary>
    /// <remarks>
    /// Preconditions: N/A
    /// Steps: Skip
    /// Expected: Skipped
    /// </remarks>
    [Fact(Skip = "W12 toast/temporary notification API is任意/未実装")]
    public void ToastApi_Skipped() { }
}
