# JellyKeys 主题包格式

主题导出为单个 `.jellytheme` 文件。它是标准 ZIP，根目录固定包含两个文件：

| 文件 | 内容 |
| --- | --- |
| `theme.json` | 主题名称、键盘类型、配色和每键设置 |
| `text.png` | 键盘文字贴图，尺寸固定为 2356 × 708 |

`theme.json` 使用 `"schema": "jelly-keyboard-package/v1"`，并用 `"texture_file": "text.png"` 指向贴图。PNG 不再以 Base64 写入 JSON。包内不接受其他文件；导入时应用直接在内存中解包、校验并保存到主题库，不会在用户目录留下解压文件。

数据结构示例（颜色值使用 HTML 十六进制格式）：

```json
{
  "schema": "jelly-keyboard-package/v1",
  "id": "theme-id",
  "name": "我的主题",
  "keyboard_type": 108,
  "palette": {
    "id": "palette-id",
    "name": "我的配色",
    "page": "#3455e9",
    "deck": "#243fc4",
    "key_top": "#d9f871",
    "key_edge": "#aabd51",
    "key_ink": "#1f2937",
    "input_ink": "#f7f4ff"
  },
  "texture_file": "text.png",
  "keys": {}
}
```

`keys` 是可选的每键设置表，以键位 ID 为键，值可包含 `label`、`font_size`、`top_color`、`ink_color`、`has_top_color`、`has_ink_color`。应用生成的文件还可能包含 `text_label_fallback_ids`，用于标记由 JSON 文字而非 PNG 显示的键位。

有自定义贴图时，导出保留这张 PNG 的原始字节。没有自定义贴图时，应用按该主题每个键的字色，从内置黑白键字合成一张 PNG。若 JSON 有自定义键名字样，`text_label_fallback_ids` 会让这些键继续用 JSON 文字显示。

导入要求两个文件齐全、格式版本受支持，且 PNG 可读取、尺寸与当前文字模板一致。编辑模板后单独导回的 PNG 也执行相同检查；校验失败不会覆盖当前贴图。旧版 `jelly-keyboard-theme/v1` 的 `.json` 主题仍可导入。文字贴图模板的导出格式和文字内容不受主题包格式影响。

设备键盘布局的“自动识别 / 手动选择”是本机设置，不写入主题包；`keyboard_type` 仍表示主题自带的布局。

当前导入上限为 JSON 2 MiB、PNG 24 MiB、整个主题包 32 MiB。`text.png` 建议保留透明背景，避免遮住键帽颜色。
