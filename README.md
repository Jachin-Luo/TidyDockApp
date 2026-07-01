# TidyDock

TidyDock 是一个轻量级 Windows 桌面 Dock，用来手动整理和快速启动常用应用、文件夹、文件、URL 和快捷方式。

它参考了 macOS Dock 的使用体验，但刻意保持更小的产品边界：不扫描桌面、不监控进程、不记录最近文件、不做后台索引，也不进行任何联网行为。

English summary: TidyDock is a lightweight Windows desktop Dock for manually organizing frequently used apps, folders, files, URLs, and shortcuts.

## 项目状态

TidyDock 目前处于早期开发阶段。当前 WPF / .NET Framework 版本已经可用，并支持 portable 包和当前用户安装器，但在稳定版发布前，配置结构、打包方式、UI 细节和内部架构仍可能继续调整。

当前重点：

- 收缩并稳定 MVP 范围
- 优化运行时内存和资源生命周期
- 改善快捷方式导入、配置恢复和发布验证流程
- 把项目结构整理成更适合开源协作的形态

## 适合谁

TidyDock 适合这些用户：

- 想要一个类似 macOS Dock 的 Windows 启动栏
- 希望手动管理常用入口，而不是让工具自动扫描或整理桌面
- 希望 Dock 占用低、行为透明、数据只保存在本地
- 经常使用桌面快捷方式、文件夹入口、开发工具、网页链接的人

它不适合这些场景：

- 替代 Windows 任务栏
- 管理正在运行的窗口
- 自动整理桌面文件
- 展示最近文件或使用历史
- 做文件搜索、后台索引或同步

## 功能概览

- 手动添加 Dock 项：应用、快捷方式、文件夹、文件、URL、分隔符
- 编辑模式：添加、移除、重命名、修改目标、替换图标、拖拽排序
- 支持拖入 `.exe`、`.lnk`、文件和文件夹
- `.lnk` 快捷方式会导入到 TidyDock 本地数据目录，删除原桌面快捷方式后 Dock 项仍可使用
- 支持图标名称显示开关
- 支持 Dock 背景完全透明
- 支持 hover 放大效果
- 支持底部、顶部、左侧、右侧位置
- 支持多显示器选择
- 支持自动隐藏和边缘唤出
- 支持托盘菜单
- 支持开机启动
- 支持中文和英文基础界面文案
- 支持浅色、深色、跟随系统主题
- 文件夹 Stack 面板按需读取目录，不递归扫描
- 本地 JSON 配置，带 `.bak` 备份替换
- 本地图标缓存
- 单实例运行保护
- portable 包和当前用户安装器

## 产品边界

TidyDock 是一个启动器和手动整理入口，不是系统管理工具。

明确不做：

- 不替代 Windows 任务栏
- 不扫描桌面
- 不自动分类或移动用户文件
- 不展示最近文件
- 不展示运行状态小圆点
- 不提供窗口列表
- 不做任务切换
- 不监控进程
- 不后台索引
- 不云同步
- MVP 阶段不联网

这个边界很重要。它让 TidyDock 保持轻、稳、可解释，也避免变成一个复杂的桌面管家。

## 截图

当前预览图：

- [docs/TidyDock_preview.png](docs/TidyDock_preview.png)

## 安装和运行

构建后会生成两个主要产物：

```text
src\TidyDock\dist\TidyDockSetup.exe
src\TidyDock\dist\TidyDock-portable.zip
```

使用方式：

- 想正常安装：运行 `TidyDockSetup.exe`
- 想免安装试用：解压 `TidyDock-portable.zip` 后运行 `TidyDock.exe`

安装器是当前用户安装，不需要管理员权限。默认安装位置类似：

```text
%LOCALAPPDATA%\Programs\TidyDock
```

## 本地数据

TidyDock 的数据保存在本地：

```text
%APPDATA%\TidyDock\
  config\settings.json
  config\settings.json.bak
  cache\icons\
  shortcuts\
  logs\error.log
```

说明：

- `settings.json` 保存 Dock 配置和 Dock 项
- `settings.json.bak` 是配置替换时保留的备份
- `cache\icons` 保存图标缓存
- `shortcuts` 保存导入后的 `.lnk` 快捷方式副本
- `logs\error.log` 保存本地异常日志

移除 Dock 项不会删除用户原始文件。清理图标缓存也不会影响用户文件。

## 开发环境

要求：

- Windows 10 / Windows 11
- .NET Framework MSBuild

常见路径：

```powershell
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe
```

源码工程位于：

```text
src\TidyDock\TidyDock.csproj
```

直接编译 Release：

```powershell
& 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe' `
  '.\src\TidyDock\TidyDock.csproj' `
  /p:Configuration=Release `
  /verbosity:minimal
```

## 构建发布包

生成 portable 包和安装器：

```powershell
powershell -ExecutionPolicy Bypass -File ".\src\TidyDock\scripts\Build-Installer.ps1"
```

生成产物：

```text
src\TidyDock\dist\TidyDockSetup.exe
src\TidyDock\dist\TidyDock-portable.zip
src\TidyDock\dist\TidyDock-portable\
```

构建脚本会停止正在运行的 `TidyDock.exe`，避免 exe 被锁导致构建失败。

## 验证

验证 portable 包结构、zip 内容、图标、版本和常见乱码标记：

```powershell
powershell -ExecutionPolicy Bypass -File ".\src\TidyDock\scripts\Test-Portable.ps1" -SkipLaunch
```

测量运行时内存：

```powershell
powershell -ExecutionPolicy Bypass -File ".\src\TidyDock\scripts\Measure-Memory.ps1"
```

启动、预热、采样、保存 CSV，并停止脚本启动的进程：

```powershell
powershell -ExecutionPolicy Bypass -File ".\src\TidyDock\scripts\Measure-Memory.ps1" `
  -Launch `
  -StopAfter `
  -CsvPath ".\src\TidyDock\dist\memory-baseline.csv"
```

## 仓库结构

```text
.
|-- .github/              Issue 模板、PR 模板、GitHub Actions
|-- docs/                 产品、设计、测试、发布和重构文档
|-- src/
|   `-- TidyDock/         WPF 应用源码、脚本、安装器、资源
|-- CHANGELOG.md          变更记录入口
|-- CODE_OF_CONDUCT.md    行为准则
|-- CONTRIBUTING.md       贡献指南
|-- LICENSE               开源许可证
|-- README.md             项目首页
|-- ROADMAP.md            路线图
|-- SECURITY.md           安全政策
`-- SUPPORT.md            支持说明
```

## 文档入口

- [文档索引](docs/README.md)
- [产品愿景](docs/00-vision.md)
- [PRD](docs/01-prd.md)
- [用户流程](docs/02-user-flows.md)
- [UI 规格](docs/03-ui-spec.md)
- [技术设计](docs/04-tech-design.md)
- [数据模型](docs/05-data-model.md)
- [测试计划](docs/09-test-plan.md)
- [发布检查清单](docs/10-release-checklist.md)
- [性能基线](docs/15-performance-baseline.md)
- [技术决策记录](docs/adr/)
- [应用发布说明](src/TidyDock/RELEASE.md)

## 贡献

欢迎贡献，但请尽量保持改动小而聚焦。

提交 PR 前建议：

- 阅读 [CONTRIBUTING.md](CONTRIBUTING.md)
- 确认改动符合 [产品愿景](docs/00-vision.md)
- 行为变化同步更新文档
- 性能优化附带前后测量数据
- 构建或打包相关改动需要跑 portable 验证

较大的功能建议先开 issue 讨论，避免偏离 TidyDock 的轻量边界。

## 安全和隐私

TidyDock 是本地优先应用。MVP 阶段不应该有联网行为，也不应该收集分析数据。

更多说明：

- [SECURITY.md](SECURITY.md)
- [docs/11-security-privacy.md](docs/11-security-privacy.md)

## 路线图

近期方向：

- 完善配置校验和修复
- 拆分 Dock item、布局、文件夹读取等服务边界
- 持续优化内存和资源生命周期
- 完善安装、卸载、portable 验证
- 做更完整的多显示器测试

更完整路线见 [ROADMAP.md](ROADMAP.md)。

## 许可证

TidyDock 使用 [MIT License](LICENSE)。
