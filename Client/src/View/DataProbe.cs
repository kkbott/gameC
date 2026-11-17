using System;
using System.IO;
using Godot;
using GodotFileAccess = Godot.FileAccess;
using TianziGe.Sim.Data;

namespace TianziGe.View;

/// <summary>
/// T-003 的 View 侧最小加载入口：从 <c>res://Data</c> 读 JSON，交给 Sim 解析。
/// 用于验证开发运行与导出包内都能读到同一份数据。
/// </summary>
public static class DataProbe
{
    public static int Run()
    {
        try
        {
            string charTableJson = ReadText("res://Data/char-table.json");
            string componentJson = ReadText("res://Data/component-registry.json");
            CharTable table = CharTableLoader.Load(charTableJson, componentJson);

            GD.Print($"TianziGe data probe OK: chars={table.Chars.Count}, components={table.Components.Count}");
            return 0;
        }
        catch (Exception exception)
        {
            GD.PushError($"TianziGe data probe FAILED: {exception.Message}");
            return 1;
        }
    }

    private static string ReadText(string path)
    {
        if (!GodotFileAccess.FileExists(path))
        {
            throw new FileNotFoundException($"数据文件不存在：{path}");
        }

        using GodotFileAccess file = GodotFileAccess.Open(path, GodotFileAccess.ModeFlags.Read)
            ?? throw new IOException($"无法打开 {path}：{GodotFileAccess.GetOpenError()}");
        return file.GetAsText();
    }
}
