# TidyDock Release Checklist

## Before Build

- 产品边界未变化，或已记录到文档。
- 版本号已更新。
- `docs/13-changelog.md` 已更新。
- `README.md` 和 `docs/12-user-guide.md` 已更新。
- 新设置有默认值和规范化逻辑。
- 备份、恢复、卸载保留配置策略已确认。

## WinForms v0.2 Build

```powershell
dotnet build .\src\TidyDock.WinForms\TidyDock.WinForms.csproj -c Release -warnaserror
.\src\TidyDock.WinForms\scripts\Build-Installer.ps1
```

验收：

- 0 warning，0 error。
- `src\TidyDock.WinForms\dist\TidyDockWinFormsSetup.exe` 存在。
- `src\TidyDock.WinForms\dist\TidyDock-winforms-portable.zip` 存在。
- portable 包为 self-contained，不要求用户额外安装 .NET Desktop Runtime。

## Verification

```powershell
.\src\TidyDock.WinForms\scripts\Test-Portable.ps1
```

手动安装启动：

```powershell
Get-Process TidyDock,TidyDock.WinForms -ErrorAction SilentlyContinue | Stop-Process -Force
.\src\TidyDock.WinForms\dist\TidyDock-winforms-portable\Install-CurrentUser.ps1 -Launch
```

## Manual Smoke Test

- 启动 Dock。
- 确认不出现在 Alt+Tab。
- 添加并打开应用、文件、文件夹、URL、此电脑和分隔符。
- 右键单项菜单功能正常。
- 文件夹 Flyout 打开、返回、滚动、关闭正常。
- 设置页草稿、预览、保存、取消正常。
- 数据导出、导入、恢复最近备份正常。
- 复制诊断信息正常且不包含 Dock 项真实目标路径。
- 托盘显示/隐藏、设置、退出正常。
- 开机启动设置正常。

## Performance Check

记录：

- 5 秒 idle CPU delta。
- Private memory。
- Working set。
- Handle count。
- 打开设置页后内存。
- 打开并关闭文件夹 Flyout 后内存。

参考命令：

```powershell
$p1 = Get-Process TidyDock -ErrorAction SilentlyContinue | Select-Object -First 1
Start-Sleep -Seconds 5
$p2 = Get-Process -Id $p1.Id
[pscustomobject]@{
  CpuDeltaSeconds = [math]::Round(($p2.CPU - $p1.CPU), 4)
  WorkingSetMB = [math]::Round($p2.WorkingSet64 / 1MB, 2)
  PrivateMemoryMB = [math]::Round($p2.PrivateMemorySize64 / 1MB, 2)
  Handles = $p2.HandleCount
} | Format-List
```

## Install And Uninstall

- 当前用户安装不需要管理员权限。
- 开始菜单存在 `TidyDock.lnk`。
- 默认卸载保留 `%APPDATA%\TidyDock\winforms`。
- `卸载并清除配置 TidyDock` 会显式删除用户配置。
- 重装后能读取保留的配置。

## Release Artifacts

- `TidyDockWinFormsSetup.exe`
- `TidyDock-winforms-portable.zip`
- Release notes。
- Known issues。

## Rollback

- 保留上一版 installer 和 portable 包。
- 默认卸载不删除用户配置。
- 如新版本配置异常，可用最近自动备份或导出的 zip 备份恢复。
