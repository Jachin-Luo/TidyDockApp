# TidyDock.WinForms 代码审查记录

审查日期：2026-07-05

审查范围：仅 `src/TidyDock.WinForms`，不覆盖 legacy WPF 项目 `src/TidyDock`。

## 结论

`TidyDock.WinForms` 当前可正常编译，入口、配置目录、安装/卸载脚本整体清晰，没有发现会直接导致构建失败的问题。

主要风险集中在发布包运行时依赖、长期常驻资源增长、大目录读取性能、备份导入校验这四类。

## 验证

```powershell
dotnet build src\TidyDock.WinForms\TidyDock.WinForms.csproj
dotnet build src\TidyDock.WinForms\TidyDock.WinForms.csproj -c Release
```

结果：Debug 与 Release 均通过，0 警告，0 错误。

当前未发现独立测试项目，仅有 `src\TidyDock.WinForms\scripts\Test-Portable.ps1` 作为 portable 包冒烟检查。

## 发现

| 优先级 | 位置 | 现象 | 原因 | 推荐解决方案 |
|---|---|---|---|---|
| P1 | `scripts/Build-Portable.ps1:24`，`installer/TidyDockWinFormsSetup.cs:31` | 安装器显示安装成功后，干净机器上可能无法启动应用。 | portable 发布使用 `--self-contained false`，安装器只释放 `TidyDock.exe` 等文件，没有检测或安装 .NET 8 Windows Desktop Runtime。 | 改为自包含发布，或在安装器启动前检测 runtime 并给出明确提示。 |
| P2 | `Services/IconService.cs:13`，`UI/FolderFlyoutForm.cs:438` | 长时间浏览文件夹面板后，常驻进程内存和 GDI 资源可能持续增长。 | 图标缓存以路径和尺寸为 key 无限增长，只在清缓存或退出时释放。文件夹面板会为浏览过的每个条目取图标。 | 给图标缓存加容量/LRU，或为文件夹面板使用短生命周期缓存。 |
| P2 | `Services/FolderService.cs:19` | 大目录会造成读取慢，分组管理窗口可能卡顿。 | `MaxItems` 在 `ToList()` 后才生效，代码会先枚举、建模、过滤、排序全部条目。 | 读取时先限制到 `MaxItems + 1`，或改为分页/可取消加载。 |
| P3 | `UI/SettingsForm.cs:619`，`Services/ConfigService.cs:77` | 异常备份中的极端配置值可进入预览流程。 | 备份导入走 `Deserialize` + `CopyInto`，没有复用配置规范化逻辑。 | 导入后立即执行规范化/校验，再允许预览或保存。 |

## 补充判断

安装脚本没有发现明显的误删目录风险。`Uninstall-CurrentUser.ps1` 对用户数据删除有 `APPDATA` 边界检查。

中文乱码不是源码损坏，是 Windows PowerShell 默认编码读取 UTF-8 文件时的显示问题；用 UTF-8 读取后文本正常。

## 推荐处理顺序

1. 先处理发布包运行时依赖，这是最容易影响新用户启动成功率的问题。
2. 再处理图标缓存，避免常驻程序使用越久资源越高。
3. 然后优化大目录读取，减少文件夹面板和分组管理的卡顿。
4. 最后补备份导入校验，并为配置规范化加最小测试覆盖。

## 处理记录

日期：2026-07-05

| 优先级 | 项目 | 状态 | 处理 |
|---|---|---|---|
| P1 | 发布包运行时依赖 | 已处理 | `Build-Portable.ps1` 改为 self-contained 单文件发布，portable 和 installer 不要求额外安装 .NET Desktop Runtime。 |
| P2 | 图标缓存无限增长 | 已处理 | `IconService` 增加 256 项 LRU 内存缓存上限，淘汰时释放 `Image`。 |
| P2 | 大目录读取慢 | 已处理 | `FolderService` 达到 `MaxItems + 1` 个可显示项后停止枚举，避免先建模整个大目录。 |
| P3 | 备份导入缺少规范化 | 已处理 | `DataBackupService.ReadConfigFromBackup()` 导入后复用 `ConfigService.Normalize()`。 |
| P0 补充 | 配置损坏恢复 | 已处理 | `ConfigService.Load()` 失败时保留 broken 文件，优先尝试最近自动备份，失败后才写默认配置。 |
