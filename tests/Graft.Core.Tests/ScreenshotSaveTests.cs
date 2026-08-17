namespace Graft.Core.Tests;

public sealed class ScreenshotSaveTests
{
    /// <summary>
    /// SaveAsync writes PNG bytes to the destination path (creating directories).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A Screenshot instance with known bytes
    ///
    /// Steps:
    /// - SaveAsync to a nested temp path
    ///
    /// Expected:
    /// - File exists with the same bytes
    /// </remarks>
    [Fact]
    public async Task SaveAsync_WritesBytesAndCreatesDirectory()
    {
        var png = new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G', 0, 1, 2, 3 };
        var shot = new Screenshot("png", 10, 20, png);
        var path = Path.Combine(Path.GetTempPath(), "graft-screenshot-tests", Guid.NewGuid().ToString("N"), "shot.png");

        try
        {
            await shot.SaveAsync(path);
            Assert.True(File.Exists(path));
            Assert.Equal(png, await File.ReadAllBytesAsync(path));
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            var dir = Path.GetDirectoryName(path);
            if (dir is not null && Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
    }
}
