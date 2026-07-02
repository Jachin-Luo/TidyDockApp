# TidyDock WinForms

这是 TidyDock v0.2 的 Windows-only 重写版本，重点是降低常驻资源占用，并保留 Dock 的核心体验。

旧 WPF 项目仍保留在 `src/TidyDock`，作为 v0.1 legacy 实现和功能参考。

## 目标

- 让 TidyDock 保持为一个小而清晰的手动 Dock，而不是任务栏替代品。
- 常驻 Dock 使用 WinForms + Win32 interop，避免 WPF 控件树带来的额外开销。
- v0.2 配置独立保存，不读取或覆盖 v0.1 WPF 配置。
- 打包方式保持本地、轻量，并优先支持当前用户安装。

## 本地数据

```text
%APPDATA%\TidyDock\winforms\
  settings.json
  settings.json.bak
  cache\
  shortcuts\
  logs\tidydock.log
```

v0.2 配置不会迁移或覆盖 WPF v0.1 配置。

## 构建

```powershell
powershell -ExecutionPolicy Bypass -File '.\src\TidyDock.WinForms\scripts\Build-Installer.ps1'
```

输出：

```text
src\TidyDock.WinForms\dist\TidyDockWinFormsSetup.exe
src\TidyDock.WinForms\dist\TidyDock-winforms-portable.zip
src\TidyDock.WinForms\dist\TidyDock-winforms-portable\
```

## 验证

```powershell
powershell -ExecutionPolicy Bypass -File '.\src\TidyDock.WinForms\scripts\Test-Portable.ps1'
```

## 当前 MVP

- 无边框自绘 Dock 窗口。
- 透明玻璃风格 Dock 底座和悬停放大。
- 支持应用、文件、文件夹、网址、分隔符和设置项。
- 支持拖拽文件/文件夹到 Dock。
- 支持拖拽 Dock 项排序。
- 点击文件夹项会打开轻量文件夹面板，而不是把 TidyDock 变成任务栏或文件索引器。
- 支持中文右键菜单和托盘菜单。
- 设置窗口提供清晰的深色/浅色主题，并使用半透明背景。
- 设置窗口包含文件夹面板展示数量、高度和隐藏文件选项。
- 支持自动隐藏热区。
- 支持当前用户开机启动设置。
- 支持 portable zip 和当前用户安装器。

## 产品边界

TidyDock 是本地优先、手动管理的轻量启动 Dock。v0.2 不监控进程、不显示运行状态、不做窗口切换、不接管任务栏，也不递归扫描或索引文件夹内容。
