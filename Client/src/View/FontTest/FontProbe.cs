using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using GodotFileAccess = Godot.FileAccess;
using TianziGe.Sim.Data;

namespace TianziGe.View.FontTest;

/// <summary>
/// T-002 字体探针：用仓库内的 OFL 子集字体渲染 50 个目标字（16 / 32 / 120 px），
/// 做一次原生竖排（TextServer 的 ORIENTATION_VERTICAL），并输出图集页数 / 显存 /
/// 缺字信息，最后把 1920×1080 的画面存成 PNG 供人工验收。
/// </summary>
public partial class FontProbe : Control
{
    private const string FontPath = "res://assets/fonts/NotoSerifSC-Regular-subset.otf";
    private const int VerticalFontSize = 64;

    private FontFile _font = null!;
    private string _chars = string.Empty;
    private bool _captured;
    private bool _failed;
    private int _frames;

    public override void _Ready()
    {
        _font = ResourceLoader.Load<FontFile>(FontPath)
            ?? throw new InvalidOperationException($"无法加载字体：{FontPath}");

        string charTableJson = ReadText("res://Data/char-table.json");
        IReadOnlyList<CharEntry> entries = CharTableLoader.LoadChars(charTableJson);
        _chars = string.Concat(entries.Select(entry => entry.Char));

        GD.Print($"FONT_PROBE font={FontPath} chars={_chars.Length}");
        GD.Print($"FONT_PROBE font_name={_font.GetFontName()} style={_font.GetFontStyleName()} fixed_size={_font.FixedSize}");
        int missing = CountMissingChars();
        GD.Print($"FONT_PROBE has_char_missing={missing}");
        if (missing > 0)
        {
            _failed = true;
            GetTree().Quit(1);
            return;
        }

        SetProcess(true);
    }

    public override void _Draw()
    {
        var paper = new Color("f3ede2");
        var ink = new Color("1d1814");
        var faint = new Color("6a6157");

        DrawRect(new Rect2(Vector2.Zero, Size), paper);

        DrawString(_font, new Vector2(40, 32), "16px / 50 字", HorizontalAlignment.Left, -1, 16, faint);
        DrawString(_font, new Vector2(40, 72), _chars, HorizontalAlignment.Left, -1, 16, ink);

        DrawString(_font, new Vector2(40, 120), "32px / 50 字", HorizontalAlignment.Left, -1, 16, faint);
        DrawString(_font, new Vector2(40, 176), _chars, HorizontalAlignment.Left, -1, 32, ink);

        DrawString(_font, new Vector2(40, 228), "120px / 合字大字（10 字 × 5 行）", HorizontalAlignment.Left, -1, 16, faint);
        for (int row = 0; row < 5; row++)
        {
            string line = _chars.Substring(row * 10, 10);
            DrawString(_font, new Vector2(40, 370 + (row * 138)), line, HorizontalAlignment.Left, -1, 120, ink);
        }

        DrawString(_font, new Vector2(1600, 110), "竖排 64px（含标点）", HorizontalAlignment.Left, -1, 16, faint);
        DrawString(
            _font,
            new Vector2(1680, 150),
            "秋，江。明早",
            HorizontalAlignment.Left,
            -1,
            VerticalFontSize,
            ink,
            TextServer.JustificationFlag.None,
            TextServer.Direction.Auto,
            TextServer.Orientation.Vertical);

        DrawString(_font, new Vector2(40, 1040), "T-002 · OFL 子集字体 · 原生 TextServer 竖排", HorizontalAlignment.Left, -1, 16, faint);
    }

    public override void _Process(double delta)
    {
        if (_captured)
        {
            return;
        }

        _frames++;
        if (_frames < 3)
        {
            return;
        }

        _captured = true;
        CaptureAndReport();
        GetTree().Quit(_failed ? 1 : 0);
    }

    private void CaptureAndReport()
    {
        if (DisplayServer.GetName() == "headless")
        {
            GD.Print("FONT_PROBE headless=1 screenshot=skipped（无头模式不截图；截图请用窗口模式）");
            ReportVerticalMetrics();
            ReportAtlas();
            return;
        }

        Image image = GetViewport().GetTexture().GetImage();
        string outputDirectory = Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"), "..", "docs", "screenshots", "font-validation"));
        Directory.CreateDirectory(outputDirectory);

        float scale = image.GetWidth() / GetViewport().GetVisibleRect().Size.X;
        string fullPath = Path.Combine(outputDirectory, "font-probe-16-32-120.png");
        image.SavePng(fullPath);
        GD.Print($"FONT_PROBE screenshot={fullPath} size={image.GetWidth()}x{image.GetHeight()} dpi_scale={scale:F2}");

        SaveCrop(image, scale, new Rect2I(20, 40, 900, 45), 3, Path.Combine(outputDirectory, "font-probe-16px-3x.png"));
        SaveCrop(image, scale, new Rect2I(20, 250, 1260, 690), 1, Path.Combine(outputDirectory, "font-probe-120px.png"));
        SaveCrop(image, scale, new Rect2I(1580, 90, 260, 500), 2, Path.Combine(outputDirectory, "font-probe-vertical-2x.png"));

        ReportVerticalMetrics();
        ReportAtlas();
    }

    private static void SaveCrop(Image source, float scale, Rect2I logicalRect, int zoom, string path)
    {
        var rect = new Rect2I(
            (int)(logicalRect.Position.X * scale),
            (int)(logicalRect.Position.Y * scale),
            (int)(logicalRect.Size.X * scale),
            (int)(logicalRect.Size.Y * scale));
        rect = rect.Intersection(new Rect2I(Vector2I.Zero, source.GetSize()));
        if (rect.Size.X <= 0 || rect.Size.Y <= 0)
        {
            GD.PushWarning($"FONT_PROBE crop empty: {path}");
            return;
        }

        Image crop = source.GetRegion(rect);
        if (zoom > 1)
        {
            crop.Resize(crop.GetWidth() * zoom, crop.GetHeight() * zoom, Image.Interpolation.Nearest);
        }

        crop.SavePng(path);
        GD.Print($"FONT_PROBE screenshot={path} size={crop.GetWidth()}x{crop.GetHeight()}");
    }

    private int CountMissingChars()
    {
        return _chars.Count(character => !_font.HasChar(character));
    }

    private void ReportVerticalMetrics()
    {
        Vector2 horizontal = _font.GetStringSize("秋，江。明早", HorizontalAlignment.Left, -1, VerticalFontSize);
        Vector2 vertical = _font.GetStringSize(
            "秋，江。明早",
            HorizontalAlignment.Left,
            -1,
            VerticalFontSize,
            TextServer.JustificationFlag.None,
            TextServer.Direction.Auto,
            TextServer.Orientation.Vertical);

        GD.Print($"FONT_PROBE vertical_text_size={vertical} horizontal_text_size={horizontal}");
        GD.Print(vertical.Y > vertical.X
            ? "FONT_PROBE vertical_layout=vertical-shaper"
            : "FONT_PROBE vertical_layout=NOT-vertical");
    }

    private void ReportAtlas()
    {
        TextServer textServer = TextServerManager.GetPrimaryInterface();
        int totalPages = 0;
        long totalBytes = 0;

        foreach (Rid fontRid in _font.GetRids())
        {
            foreach (Vector2I cacheSize in textServer.FontGetSizeCacheList(fontRid))
            {
                int pages = (int)textServer.FontGetTextureCount(fontRid, cacheSize);
                if (pages > 4)
                {
                    _failed = true;
                    GD.PushError($"FONT_PROBE atlas pages={pages} 超过 A4 上限 4（size={cacheSize}）");
                }
                long bytes = 0;
                for (int page = 0; page < pages; page++)
                {
                    Image texture = textServer.FontGetTextureImage(fontRid, cacheSize, page);
                    bytes += (long)texture.GetWidth() * texture.GetHeight() * 4;
                }

                totalPages += pages;
                totalBytes += bytes;
                GD.Print($"FONT_PROBE atlas size={cacheSize} pages={pages} bytes={bytes}");
            }
        }

        GD.Print($"FONT_PROBE atlas_total pages={totalPages} bytes={totalBytes} mib={totalBytes / 1024.0 / 1024.0:F2}");
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
