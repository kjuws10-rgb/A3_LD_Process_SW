using System.IO;
using System.Text;

namespace MofCoordinateDemo.Automation1;

public static class AeroScriptLocalFileStore
{
    public static string ResolvePath(string? configuredPath, string baseDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);
        var path = string.IsNullOrWhiteSpace(configuredPath)
            ? "mof_generated.ascript"
            : configuredPath.Trim();

        if (!path.EndsWith(".ascript", StringComparison.OrdinalIgnoreCase))
        {
            path += ".ascript";
        }

        return Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(baseDirectory, path));
    }

    public static string Save(string? configuredPath, string scriptText, string baseDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scriptText);
        var fullPath = ResolvePath(configuredPath, baseDirectory);
        var directory = Path.GetDirectoryName(fullPath)
                        ?? throw new InvalidOperationException("Local Script 저장 폴더를 계산할 수 없습니다.");
        Directory.CreateDirectory(directory);
        File.WriteAllText(fullPath, scriptText, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        return fullPath;
    }
}
