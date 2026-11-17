#!/usr/bin/env python3
"""T-002 · 可复现的字体子集生成脚本。

输入：Noto Serif CJK SC（Google 品牌名；与 Adobe「思源宋体 Source Han Serif SC」
同一 OFL 字体）的 SubsetOTF 版本。
输出：Client/assets/fonts/NotoSerifSC-Regular-subset.otf

用法：
  python3 -m pip install --user fonttools
  python3 Tools/fonts/subset-font.py path/to/NotoSerifSC-Regular.otf

字符集 = char-table.json 的 50 个字 + UI 固定字（数字 / 拉丁 / 标点 / 菜单常用字）。
保留全部 OpenType layout features，尤其是竖排需要的 vert / vrt2。
"""

from __future__ import annotations

import argparse
import json
import subprocess
import sys
import tempfile
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[2]
CHAR_TABLE = REPO_ROOT / "Client" / "Data" / "char-table.json"
OUTPUT = REPO_ROOT / "Client" / "assets" / "fonts" / "NotoSerifSC-Regular-subset.otf"

# UI 固定字：数字、ASCII 字母、CJK 标点、菜单文案常用字。
UI_CHARS = (
    "0123456789"
    "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz"
    "，。、；：？！“”‘’（）《》〈〉【】「」『』—…·"
    "田字格开始游戏设置退出音量继续新存档读取关闭确定取消是否返回主菜单暂停音效开启"
    # FontProbe 截图里的说明文字（保证验证图本身不出现 fallback 方块）
    "16px / 50 字32px120px合字大字（10 字 × 5 行）竖排 64px（含标点）"
    "T-002 · OFL 子集字体 · 原生 TextServer 竖排"
)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source_font", type=Path, help="NotoSerifSC-Regular.otf 路径")
    parser.add_argument("--output", type=Path, default=OUTPUT)
    args = parser.parse_args()

    if not args.source_font.is_file():
        parser.error(f"找不到源字体：{args.source_font}")

    data = json.loads(CHAR_TABLE.read_text(encoding="utf-8"))
    chars = "".join(dict.fromkeys(entry["char"] for entry in data["chars"]))
    text = chars + "".join(dict.fromkeys(UI_CHARS))

    with tempfile.NamedTemporaryFile("w", encoding="utf-8", suffix=".txt", delete=False) as handle:
        handle.write(text)
        text_file = Path(handle.name)

    try:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        command = [
            sys.executable,
            "-m",
            "fontTools.subset",
            str(args.source_font),
            f"--text-file={text_file}",
            f"--output-file={args.output}",
            "--layout-features=*",
            "--name-IDs=*",
            "--name-legacy",
            "--name-languages=*",
            "--glyph-names",
            "--symbol-cmap",
            "--legacy-cmap",
            "--notdef-glyph",
            "--notdef-outline",
            "--recommended-glyphs",
        ]
        subprocess.run(command, check=True)
    finally:
        text_file.unlink(missing_ok=True)

    print(f"subset OK: {args.output} ({args.output.stat().st_size} bytes, {len(text)} chars)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
