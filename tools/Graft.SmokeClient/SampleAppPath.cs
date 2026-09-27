using Graft.Core;
using Graft.Protocol;

namespace Graft.SmokeClient;

internal static class SampleAppPath
{
    public static string ResolveDefault()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "tests", "sample-apps", "SampleWpfApp", "SampleWpfApp.csproj");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new GraftException(GraftErrorCodes.ActionFailed, "Could not locate SampleWpfApp.csproj. Pass --app <path>.");
    }
}
