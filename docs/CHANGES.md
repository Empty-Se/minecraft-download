# 相对 PCL 2.99.0.0 的改动

本项目是第三方修改版；原始下载引擎并未重写。

- `FormMain.xaml.vb`：默认进入目录管理，隐藏无关主导航，加入下载目录确认和明确的返回逻辑。
- `Pages/PageSelectLeft.xaml(.vb)`、`PageSelectRight.xaml(.vb)`：管理多个 Minecraft 根目录，保存目录选择，展示已安装版本并确认下载位置。
- `Pages/PageDownload/PageDownloadLeft.xaml(.vb)`：仅保留游戏版本下载导航，增加“返回目录”。
- `Pages/PageDownload/PageDownloadInstall.xaml(.vb)`：增加版本搜索、当前目录、下载源选择和关于入口，修复首次展开空白及过滤后的分类问题。
- `Modules/Minecraft/ModLaunch.vb`：拦截游戏启动主入口。
- `Application.xaml.vb`：独立应用单例标识。
- `My Project/AssemblyInfo.vb` 及图标：下载版应用信息和图标。
- `Modules/ModSecret.vb`：独立配置名；客户端 ID 和 API Key 无默认值，从本机环境变量读取。
- 移除上游 GitHub 工作流与问题模板，避免将上游的发布、反馈流程误用在此衍生项目中。
- MeloongCore 以源码目录随附，移除不再使用的子模块配置；保留其许可证。

已在本机验证 Release 构建、目录与下载页往返、搜索及部分原版安装流程。
没有声称全部 Minecraft 版本、加载器组合或 CurseForge 集成均已验证。

源码中仍有上游未在本下载版启用的模块；保留它们用于当前工程依赖，不表示相关界面功能已提供。
