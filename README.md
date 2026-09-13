# 果冻键显 JellyKeys

JellyKeys 是面向 Windows 直播和录屏的键盘按键可视化工具。真实键盘按下时，透明覆盖层上的键帽会下沉并回弹；键盘位置、缩放、配色和文字贴图都可以调整。项目使用 Godot 4.7.1 .NET 开发。

| 机甲晨光 · 84 键 | 橙蓝实验室 · 87 键 |
| :---: | :---: |
| ![机甲晨光主题](docs/images/mecha-dawn.png) | ![橙蓝实验室主题](docs/images/portal-lab.png) |
| 霓虹竞技场 · 87 键 | 虹彩银翼 · 61 键 |
| ![霓虹竞技场主题](docs/images/neon-arena.png) | ![虹彩银翼主题](docs/images/spectrum-silver.png) |

## 下载与运行

从项目的 Releases 下载 Windows x64 安装包或绿色版。绿色版解压后运行 `JellyKeys.exe`，并保持 `data_JellyKeyboardOverlay_windows_x86_64` 文件夹与 EXE 放在一起。绿色版无需安装，也不要求单独安装 .NET 运行时。

首次启动默认锁定键盘位置：屏幕上只显示键盘，鼠标点击可以穿透覆盖层。鼠标移到键盘上方时，键盘会平滑淡化，避免挡住下方内容。

## 如何解锁和调整键盘

1. 按 `Ctrl + Shift + F10` 唤醒 Bar 条；也可以右键系统托盘中的 JellyKeys 图标，选择“打开 Bar 条”。
2. 在 Bar 条上点击**锁图标**，让它处于未选中状态，再按 **OK**。锁图标只是选择退出 Bar 条后的状态，按 OK 才会保存并生效。
3. 解锁后拖动键盘底座调整位置；在底座或 Bar 条上滚动鼠标滚轮调整大小，范围为 25%–150%。
4. 调整完成后再次唤醒 Bar 条，选中锁图标并按 **OK**，恢复鼠标穿透的锁定状态。

Bar 条还可以切换主题和打开设置。设置中可编辑键帽、底板与文字贴图，管理主题，以及选择实体键盘布局。实体布局可以跟随主题、自动识别或手动选择；自动识别不准确时手动指定即可。托盘菜单中的“自启动”由用户自行开启或关闭。

## 主题与贴图

主题导出为单个 `.jellytheme` 文件，内部包含 `theme.json` 和 `text.png`。导入时会检查文件结构、格式版本和贴图尺寸；单独导入自制文字贴图时也会检查尺寸。具体字段、尺寸和限制见 [主题包格式](THEME_PACKAGE_FORMAT.md)。

内置主题随程序提供；新建、导入或修改后保存的主题位于本机 `user://themes/user/`，当前状态位于 `user://state/`。更换主题不会覆盖用户单独选择的实体键盘布局。

## 从源码运行

需要 Windows x64、Godot **4.7.1 .NET** 编辑器和 .NET **8 SDK**。使用 Godot 打开 `project.godot`，编译 C# 项目后运行主场景；也可以在项目目录执行：

```powershell
dotnet restore .\JellyKeyboardOverlay.sln
dotnet build .\JellyKeyboardOverlay.sln -c Release
```

Windows 导出预设位于 `export_presets.cfg`。安装与 Godot 4.7.1 对应的 .NET 导出模板后，可以在编辑器中选择 **Project → Export → Windows Desktop**。发行包中的 C# 程序集经过兼容 Godot 的混淆处理；仓库源码保持可读，方便审阅和修改。

`assets/figma/` 中的 `.svg.import` 是 Godot 的图标导入设置，请与 SVG 一起保留。`builds/` 可放置发行文件，已被 Git 忽略，也不会参与主项目的 C# 编译。

## Windows 安装包

`packaging/windows/JellyKeys.iss` 是 Inno Setup 6 安装脚本。准备完整的 Windows x64 绿色版目录后，用 `ISCC.exe /DPortableDir=<绿色版目录> /O<输出目录> packaging/windows/JellyKeys.iss` 编译。安装默认使用当前用户目录，不要求管理员权限；桌面快捷方式可选。卸载会移除安装文件，用户主题和配置会保留。

## 隐私

程序只读取按键的按下/松开事件以驱动动画，不记录按键序列，也不上传输入内容。配置和用户主题保存在 Godot 的本地用户数据目录。只有用户主动开启托盘“自启动”时，程序才会写入当前 Windows 用户的启动项。

## 参与贡献

欢迎通过 Issue 反馈问题或提出建议。提交代码请先阅读 [贡献指南](CONTRIBUTING.md)：Fork 仓库、创建分支、提交 Pull Request，并说明修改目的和验证方式。界面变化请附截图，保持现有设计语言。

## 开源许可

Copyright (C) 2026 米夏小雨。项目源码、原创图标和仓库中的主题预览图按 [GNU GPL 3.0（仅此版本）](LICENSE) 授权，SPDX 标识为 `GPL-3.0-only`。分发基于本项目的受许可作品或其二进制版本时，须按 GPLv3 的条件向接收者提供对应源码；仅供自己使用的修改无需公开。发布安装包或绿色版时，请同时提供同版本的对应源码包。第三方组件仍遵循各自许可证。

## 项目结构

| 路径 | 内容 |
| --- | --- |
| `assets/` | 原创图标、默认黑白键字贴图和界面矢量图 |
| `docs/images/` | README 中使用的主题预览图 |
| `scenes/` | 键盘覆盖层、Bar 条和设置面板场景 |
| `scripts/input/` | 只读全局键盘输入 |
| `scripts/layout/` | 键盘位置和键型定义 |
| `scripts/overlay/` | 覆盖层窗口、托盘和自启动 |
| `scripts/settings/` | 本地设置 |
| `scripts/ui/` | 键帽渲染、主题编辑与导入导出 |

这个公开源码包不包含编译产物、测试文件或用户数据。
