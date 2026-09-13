# 参与贡献

[简体中文](#zh-cn) · [English](#english)

<a id="zh-cn"></a>

感谢你愿意改进 JellyKeys。小修改可以直接提交 Pull Request；较大的功能或界面调整，建议先开 Issue 说明使用场景和预期行为，避免与现有工作重复。

## 反馈问题

请写明 Windows 版本、JellyKeys 版本、复现步骤、预期结果和实际结果。能帮助定位问题的截图或错误日志可以一并附上；提交前请检查并移除个人信息。程序不应记录按键序列，反馈中也请不要附真实输入内容。

## 提交代码

1. Fork 仓库，从最新的主分支创建一个专注于单项改动的分支。
2. 使用 Godot **4.7.1 .NET** 和 .NET **8 SDK** 打开项目。提交前至少运行 `dotnet restore .\JellyKeyboardOverlay.sln`、`dotnet build .\JellyKeyboardOverlay.sln -c Release`，并在 Windows 上实际试用改动涉及的流程。
3. 不要提交 `.godot/`、`.nuget/`、`bin/`、`obj/`、`builds/`、本机配置和用户主题。Godot 资源的 `.uid`、`.import` 文件需要保留。
4. 提交 Pull Request，说明修改了什么、为什么修改、如何验证。界面变化请提供改动前后的截图；兼容性或数据格式变化请说明迁移影响。

## 改动约定

- 保持锁定模式下覆盖层简洁、可穿透；解锁和设置界面的视觉与现有设计语言一致。
- 全局键盘输入只用于驱动视觉反馈，不注入、不拦截、不保存、不传输用户的按键序列。
- 改动键盘布局、主题格式或导入导出时，确认旧主题和用户数据仍可正常使用。
- 提交原创代码和资源，或确认你有权按本项目的 `GPL-3.0-only` 许可证提供贡献。第三方素材请注明来源与许可证，不要直接加入来源不明的图片、字体或代码。

项目采用 [GNU GPL 3.0（仅此版本）](LICENSE)。提交贡献表示你同意将该贡献按此许可证纳入项目；无需签署额外的贡献者协议。

<a id="english"></a>

## English

Thanks for helping improve JellyKeys. Small fixes can go straight to a Pull Request. For a larger feature or UI change, please open an Issue first and describe the use case and expected behavior.

### Report a problem

Include your Windows and JellyKeys versions, steps to reproduce, expected and actual results, and relevant screenshots or error logs. Remove personal information before posting. JellyKeys must not record key sequences; please do not attach real typed content to a report.

### Submit a change

1. Fork the repository and create a focused branch from the latest main branch.
2. Open the project with **Godot 4.7.1 .NET** and the **.NET 8 SDK**. Before submitting, run `dotnet restore ./JellyKeyboardOverlay.sln` and `dotnet build ./JellyKeyboardOverlay.sln -c Release`, then manually try the affected flow on Windows.
3. Do not commit `.godot/`, `.nuget/`, `bin/`, `obj/`, `builds/`, local settings, or user themes. Keep Godot `.uid` and `.import` resource files.
4. Open a Pull Request explaining what changed, why, and how you tested it. Include before-and-after screenshots for UI changes and describe any compatibility or data migration impact.

### Project conventions

- Keep the locked overlay clean and mouse-click-through; match the existing visual language in the Bar and Settings.
- Global keyboard input is for visual feedback only. Never inject, block, store, or transmit users' key sequences.
- When changing layouts, themes, or import/export, check that existing themes and user data still work.
- Contribute original code and assets, or make sure you have the right to provide them under `GPL-3.0-only`. State the source and license of any third-party material.

The project uses [GNU GPL 3.0 only](LICENSE). By submitting a contribution, you agree to include it under that license. No separate contributor agreement is required.
