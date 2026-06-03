# EjectLens

一款 Windows 桌面诊断工具，用于排查 USB 移动硬盘或 U 盘安全弹出时提示"设备正在使用中，无法弹出"的问题。

## 它能做什么

EjectLens 读取 Windows 系统事件日志，找出哪些进程阻止了设备弹出，并以清晰的表格展示结果。它还能通过 Windows Restart Manager API 扫描文件夹中被占用的文件。

## 为什么会有这个工具

Windows 在无法弹出 USB 设备时给出的提示通常很模糊。真正有用的信息（哪个进程阻止了弹出）藏在事件查看器里，绝大多数用户不会去翻。EjectLens 把这些信息提取出来，集中展示。

## 功能

- 列出可移动磁盘，显示盘符、卷标、文件系统、容量。
- 从系统事件日志中读取 Microsoft-Windows-Kernel-PnP 事件 ID 225。
- 解析事件内容，提取进程名、PID、命令行、受影响设备。
- 通过卷 GUID 匹配将事件与所选磁盘关联（支持 mountvol 兜底方案）。
- 按匹配状态分组显示：已匹配、可能相关、未匹配。
- 显示阻止进程当前是否仍在运行。
- 扫描指定文件夹中被占用的文件（基于 Restart Manager API，限制 300 个文件）。
- 导出纯文本诊断报告（复制到剪贴板或保存为 .txt 文件）。
- 一键打开任务管理器。

## 安全设计

EjectLens **默认只读，不会主动修改系统**。

- 不会自动结束进程。
- 不会强制弹出设备。
- 不会修改注册表。
- 不会安装服务或驱动。
- 不会开机自启。
- 不会联网。
- 不会收集或发送任何数据。

工具只提供信息，如何处理由你决定。

## 注意事项

- 如果 Windows 没有记录事件 ID 225，则本工具无法给出明确结果。这种情况与 Windows 版本、设备驱动有关。
- 读取系统事件日志在某些系统上需要管理员权限。
- 获取进程命令行通常需要以管理员身份运行。
- 文件占用扫描仅检查文件夹中前 300 个文件，避免卡死。
- 卷匹配采用启发式比较，结果不一定完全精确。

## 环境要求

- Windows 10 或 Windows 11
- .NET 8 Desktop Runtime

## 从源码构建

```powershell
git clone https://github.com/weinianxue/EjectLens.git
cd EjectLens
dotnet restore
dotnet build EjectLens.sln -c Release
```

## 运行

```powershell
dotnet run --project src/EjectLens/EjectLens.csproj
```

或发布为可执行文件：

```powershell
dotnet publish src/EjectLens/EjectLens.csproj -c Release -r win-x64 --self-contained false
```

## 测试

```powershell
dotnet test EjectLens.sln -c Release
```

## 使用说明

1. 启动 EjectLens。
2. 从下拉菜单选择要诊断的移动磁盘。
3. 选择时间范围，点击 **Scan Events**。
4. 查看事件列表：绿色为已匹配事件，黄色为可能相关，灰色为未匹配。
5. 点击某行查看详情，包括对常见系统进程的提示信息。
6. 可选：选择一个文件夹扫描文件占用情况。
7. 使用 **Copy Report** 或 **Save Report** 导出诊断结果。

## 隐私

EjectLens 完全在本地运行，不会收集、存储或上传任何数据。报告中会自动将用户目录路径替换为 `%USERPROFILE%` 等环境变量标记。

## 路线图

- 支持事件 ID 226（设备移除超时）分析。
- 改进驱动级句柄枚举。
- 多语言支持。

## 许可证

MIT。详见 [LICENSE](LICENSE)。
