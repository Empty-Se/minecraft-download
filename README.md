# Minecraft Download

项目全称：**Plain Craft Launcher (PCL) Minecraft Download（第三方下载版）**。

原作者：**龙腾猫跃** · [赞助原作者](https://meloong.com/afd/a/LTCat)

这是第三方基于 [Meloong-Git/PCL](https://github.com/Meloong-Git/PCL) **2.99.0.0** 修改的衍生工具。
下载引擎、界面控件及大量基础代码来自该项目，作者归属与上游许可保留。

本工具专注 Minecraft: Java Edition 的**版本下载、组件安装和多个游戏目录管理**。
正常界面不提供账号登录或游戏启动。用于 CurseForge API 申请的项目目标是为这个实际工具增加模组与整合包资源检索；
当前下载界面尚未提供完整的独立 CurseForge 资源管理入口。

## 功能

- 浏览和搜索正式版、快照/预发布、远古版及源清单收录的愚人节版本。
- 提供 Minecraft 本体、资源、依赖库下载、校验和兼容组件安装流程。
- 按 Minecraft 版本选择 Forge、Fabric、NeoForge、OptiFine 等可用组件。
- 先确认 `.minecraft` 根目录，再下载安装；一个目录可以包含多个版本。
- 查看已安装版本、切换游戏目录、查看下载任务及返回目录管理。
- 下载源沿用上游的官方/BMCLAPI 选择机制。

可用版本与组件以各服务实际提供的列表及兼容性为准。没有逐一验证全部历史版本与组件组合。

## 使用与构建

需要 Windows、.NET Framework 4.8，以及支持 C# 13 的 Visual Studio / MSBuild。

1. 克隆或下载本仓库。此次使用的 MeloongCore 源码已随仓库保留，不需要另外初始化子模块。
2. 用 Visual Studio 打开 `Plain Craft Launcher 2.sln`，恢复 NuGet 依赖。
3. 选择 Release 构建。完整解决方案会先编译 MeloongCore 和 PCLCS，再编译 VB.NET WPF 主程序。
4. 运行后选择 Minecraft 根目录，点击“在此目录下载新版本”。

Forge/NeoForge 的部分安装流程需要合适的 Java。安装完成后，用你自己的启动器添加同一个游戏目录来运行游戏。

## 凭据与隐私

本仓库不包含开发者的 Microsoft Client ID、CurseForge API Key、登录令牌、账号缓存、游戏文件或运行日志。
不要将自己的凭据写进源码、README、截图、提交或 Issue。

`ModSecret.vb` 只从以下**本机环境变量**读取可选配置，未配置时为空：

| 环境变量 | 用途 |
| --- | --- |
| `PCL_OAUTH_CLIENT_ID` | 上游保留的微软登录代码使用；本下载版界面不开放登录，无需配置 |
| `PCL_CURSEFORGE_API_KEY` | 访问 CurseForge 的资源接口；由开发者自行申请 |

下载 MC 本体及 Forge/Fabric/NeoForge 加载器不依赖 CurseForge Key。
可选的 OptiFabric 以及保留的 CurseForge 社区资源代码会使用它；Fabric API 查询使用 Modrinth。
环境变量不会被本仓库自动上传，也不会作为常量编译进程序。不要在日志或错误报告中粘贴它们。

## 与上游的差异

参见 [改动说明](docs/CHANGES.md)。大部分引擎仍使用上游实现，保留必要的登录/启动源码以兼容工程依赖，
但这些入口在本下载版中不开放，游戏启动主入口已拦截。

## 来源与许可

- PCL 原项目及其许可：[Meloong-Git/PCL](https://github.com/Meloong-Git/PCL)、[LICENCE](LICENCE)。
- MeloongCore 原项目：[Meloong-Git/MeloongCore](https://github.com/Meloong-Git/MeloongCore)，随附 [许可证](MeloongCore/LICENSE.txt)。
- 原项目说明保留在 [UPSTREAM_README.md](docs/UPSTREAM_README.md)。
- 资源目录保留构建所需的上游第三方依赖；其作者与权利归属未改变。

本仓库保留上游许可及合理使用指南，没有将 PCL 改为 MIT 或声明全部内容属于本项目作者。
仓库名 `minecraft-download` 和页面主标题用来描述工具用途；项目全称保留上游指南要求的名称前缀和第三方修改说明。
