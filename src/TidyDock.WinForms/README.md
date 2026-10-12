# TidyDock WinForms

这是 TidyDock v0.2 的 Windows-only 重写版本，重点是降低常驻资源占用，并保留 Dock 的核心体验。

旧 WPF 项目仍保留在 `src/TidyDock`，作为 v0.1 legacy 实现和功能参考。

## 目标

- 让 TidyDock 保持为一个小而清晰的手动 Dock，而不是任务栏替代品。
- 常驻 Dock 使用 WinForms + Win32 interop，避免 WPF 控件树带来的额外开销。
- v0.2 配置独立保存，不读取或覆盖 v0.1 WPF 配置。
- 打包方式保持本地优先，portable / installer 使用 self-contained 单文件，优先支持当前用户安装。

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
- 透明玻璃风格 Dock 底座和悬停放大，可开启增强底座对比度。
- Dock 与文件夹浮窗使用传统磨砂玻璃材质：系统背景模糊、浅深色半透明底色、细亮边。Windows 10 1803 及以上尝试启用合成器效果；关闭系统透明效果、启用高对比度或合成不可用时使用实色底板。
- 设置页保留实色表单和中性配色，避免原生控件在透明合成下缺字。磨砂背景不依赖桌面截图或持续刷新计时器。
- 支持应用、文件、文件夹、网址、分隔符和设置项。
- 空 Dock 会显示首次使用入口：添加应用/文件、添加文件夹、打开设置。
- 支持拖拽文件/文件夹到 Dock。
- 支持拖拽 Dock 项排序。
- 右键菜单支持单项打开、重命名、更换图标、切换文件夹打开方式、管理分组和移除确认，不会删除用户原始文件。
- 失效目标会显示警示标记，并支持重新定位目标。
- 点击文件夹项会打开轻量文件夹面板，支持手动分组、搜索和筛选，而不是把 TidyDock 变成任务栏或文件索引器。
- 支持中文右键菜单和托盘菜单。
- 设置窗口使用草稿流程，提供 `预览到 Dock` 和 `保存并关闭`。
- 设置窗口包含显示器选择、文件夹面板展示数量、高度、隐藏文件和复制诊断信息。
- 设置按分类标签切换，Dock 项支持搜索、上下移动和撤销移除；列表高度随窗口调整，操作栏保持可见。
- 自绘按钮支持 Enter / Space，开关支持 Space，滑条支持方向键、Home / End 和 PageUp / PageDown。
- Dock 项列表支持 F2 重命名、Delete 移除、Alt + 上下键排序、Ctrl + Z 撤销移除；Ctrl + F 搜索，Ctrl + Tab 切换设置分类。
- 支持自动隐藏热区。
- 支持当前用户开机启动设置。
- 支持 portable zip 和当前用户安装器。

## 产品边界

TidyDock 是本地优先、手动管理的轻量启动 Dock。v0.2 不监控进程、不显示运行状态、不做窗口切换、不接管任务栏，也不递归扫描或索引文件夹内容。
