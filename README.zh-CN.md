# EjectLens

找出哪个进程阻止了 Windows 安全弹出你的 U 盘或移动硬盘。

Windows 在无法弹出 USB 设备时给出的提示通常很模糊。真正有用的信息 — 哪个进程阻止了弹出 — 藏在事件查看器里，多数用户不会去翻。EjectLens 把这些信息提取出来，集中展示。

## 功能

- 列出可移动磁盘，显示卷标、文件系统、容量。
- 从系统事件日志中读取 Microsoft-Windows-Kernel-PnP 事件 ID 225。
- 提取进程名、PID、命令行、受影响设备硬件 ID。
- 按匹配状态分组着色：**已匹配**、**可能相关**、**未匹配**。
- 显示阻止进程是否仍在运行。
- 扫描指定文件夹中的文件占用（Restart Manager，上限 300 个文件）。
- 通过 Windows Configuration Manager API 请求安全移除所选设备。
- 应用设置对话框：语言、主题、窗口大小、行为选项。
- 导出纯文本诊断报告（复制到剪贴板或保存为 .txt 文件）。
- 一键打开任务管理器。

## 安全设计

EjectLens 默认只读，不会主动修改系统。它不会：

- 自动结束进程
- 强制弹出设备
- 修改注册表
- 安装服务或驱动
- 开机自启
- 联网
- 收集或发送任何数据

**弹出按钮**只是调用 Windows 官方 API（CM_Request_Device_Eject）请求安全移除，不会强制卸载设备，也不会结束进程。如果 Windows 拒绝了弹出请求，EjectLens 会显示拒绝原因。

## 环境要求

- Windows 10 或 Windows 11
- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

## 下载

可从 [GitHub Releases](https://github.com/weinianxue/EjectLens/releases) 下载发布包。

**注意：** GitHub 页面上的 "Code → Download ZIP" 按钮只会下载源码压缩包，不包含可执行文件。请到 Releases 页面下载 `EjectLens-*-win-x64-portable.zip`，解压后运行 `EjectLens.exe`。

## 从源码构建

```powershell
git clone https://github.com/weinianxue/EjectLens.git
cd EjectLens
dotnet restore
dotnet build EjectLens.sln -c Release
dotnet test EjectLens.sln -c Release
```

创建自包含单文件可执行文件：

```powershell
dotnet publish src/EjectLens/EjectLens.csproj -c Release -r win-x64 `
  --self-contained true -p:PublishSingleFile=true `
  -p:EnableCompressionInSingleFile=true -o publish/EjectLens
```

## 使用说明

1. 启动 EjectLens。
2. 从下拉菜单选择要诊断的移动磁盘。
3. 选择时间范围，点击 **Scan Events**。
4. 查看事件列表：绿色为已匹配，黄色为可能相关，灰色为未匹配。
5. 点击某行查看详情，包括对常见系统进程的提示信息。
6. 点击 **Eject Selected Drive** 请求 Windows 安全弹出设备。
7. 可选：选择一个文件夹扫描文件占用情况。
8. 使用 **Copy Report** 或 **Save Report** 导出诊断结果。

## 设置

点击 **Settings** 可配置：

- **General**：界面语言（English、简体中文 或跟随系统）。
- **Appearance**：主题（Light、Dark 或跟随系统）、窗口大小和位置记忆。
- **Behavior**：弹出前确认、弹出后刷新盘符列表、记住上次选择、默认时间范围。

设置保存在 `%APPDATA%\EjectLens\settings.json`，不写注册表。

## 示例报告

脱敏后的诊断报告示例见 [docs/sample-report.txt](docs/sample-report.txt)。

## 截图

截图将在 Windows 上捕获真实运行效果后添加。详见 [docs/screenshots/README.md](docs/screenshots/README.md)。

## 注意事项

- 如果 Windows 没有记录事件 ID 225，本工具无法给出明确结果。这与 Windows 版本、设备驱动有关。
- 读取系统事件日志在某些系统上需要管理员权限。
- 文件占用扫描仅检查文件夹中前 300 个文件，避免卡死。
- 卷匹配采用启发式比较，结果不一定完全精确。
- 安全弹出取决于 Windows 和设备驱动行为。部分设备可能无法可靠地进行盘符到设备的映射。
- 某些弹出操作可能需要管理员权限。

## 隐私

EjectLens 完全在本地运行，不会收集、存储或上传任何数据。报告中会自动将用户目录替换为 `%USERPROFILE%` 等环境变量标记。

## 路线图

- 支持事件 ID 226（设备移除超时）分析。
- 超越 Restart Manager 的文件级句柄枚举。
- 更多语言支持。

## 许可证

MIT。详见 [LICENSE](LICENSE)。
