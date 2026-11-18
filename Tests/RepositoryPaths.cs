namespace TianziGe.Sim.Tests;

/// <summary>
/// 定位仓库根，供需要直接读源文件 / 真实数据的测试使用。
/// 从测试程序集的输出目录向上找 <c>TianziGe.Tools.sln</c>。
/// </summary>
internal static class RepositoryPaths
{
    public static string FindRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "TianziGe.Tools.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("找不到仓库根（TianziGe.Tools.sln）。");
    }
}
