# TidyDock Test Plan

## Build Tests

```powershell
dotnet build .\src\TidyDock.WinForms\TidyDock.WinForms.csproj -c Release -warnaserror
.\src\TidyDock.WinForms\scripts\Build-Installer.ps1
.\src\TidyDock.WinForms\scripts\Test-Portable.ps1
```

验收：

- Release 构建通过，0 warning，0 error。
- portable zip 生成。
- 当前用户安装包生成。
- portable 冒烟启动通过。
- portable 为 self-contained 发布，干净机器不要求额外安装 .NET Desktop Runtime。

## Functional Smoke Tests

- 启动应用。
- 单实例保护生效。
- 空 Dock 显示首次使用入口。
- 添加应用、文件、文件夹、URL、此电脑、分隔符。
- 打开各类 Dock 项。
- 右键单项菜单：打开、重命名、更换图标、恢复默认图标、移除。
- 失效目标显示警示，并可重新定位。
- 移除 Dock 项前有确认，且不删除原文件。
- 拖拽排序默认关闭；打开 `允许拖拽排序` 后可排序。
- 托盘菜单只提供显示/隐藏、设置、退出。

## Settings Tests

- 打开设置页未修改时显示无未保存修改。
- 修改滑条、开关、Dock 项表格后只进入草稿，不立即保存。
- 点击 `预览到 Dock` 后临时应用。
- 点击 `取消` 或关闭确认放弃后恢复打开设置前状态。
- 点击 `保存并关闭` 后重启仍保留。
- 显示器选择、位置、透明度、增强底座对比度、自动隐藏、置顶、托盘图标均按草稿流程生效。
- `跟随 Windows 启动` 即时生效。

## Folder Flyout Tests

- 打开普通文件夹、空文件夹、大文件夹、无权限文件夹、不存在文件夹。
- 进入子文件夹、返回、在资源管理器中打开。
- Header 显示项目数和短路径，按钮 tooltip 正常。
- 文件名超长时省略号显示，悬浮显示完整名称。
- 快速滚动不越界。
- 快速切换文件夹不显示过期读取结果。
- 打开/关闭动画结束后不保留空闲 Timer。

## Folder Group Tests

- 为一个文件夹创建多个分组。
- Flyout 按分组纵向展示。
- 全部分组后不显示 `未分组`。
- 分组管理支持搜索、只看未分组、只看当前分组。
- 多选加入或移出分组后列表不跳顶，并自动清空勾选。
- 点击取消不写回配置；点击确定后重启仍保留。

## Config And Recovery Tests

- Fresh config 自动创建。
- 缺失字段会补默认值。
- 极端配置值会被 clamp 到合法范围。
- `.lnk` 和 `.url` 会导入本地数据目录。
- 主配置 JSON 损坏时复制为 `settings.broken-时间戳.json`。
- 主配置损坏时优先尝试最近自动备份。
- 备份恢复失败时回到默认配置，并写入日志。
- 备份导入会先进入设置草稿，仍需预览或保存。
- 备份导入的配置会先规范化，再允许预览。

## Diagnostics Tests

- 设置页 `复制诊断信息` 可复制版本、启动耗时、OS、.NET、配置路径、日志路径、内存和句柄摘要。
- 诊断摘要不包含 Dock 项真实目标路径。
- 异常仍写入本地日志。

## Performance Tests

- 记录冷启动后 5 秒 idle private memory、working set、handle count、CPU delta。
- 打开设置页后再次记录。
- 打开并关闭 Folder Flyout 后再次记录。
- 包含 50 个 Dock 项的配置下记录。
- 10 分钟 idle 记录资源是否持续增长。
- 重点观察图标缓存 LRU 是否避免长期浏览后内存持续上升。

## Install Tests

- 当前用户安装。
- 开始菜单可搜索并启动 `TidyDock`。
- 默认卸载保留 `%APPDATA%\TidyDock\winforms`。
- `Uninstall-CurrentUser.ps1 -RemoveUserData` 清除用户配置。
- 不需要管理员权限。
