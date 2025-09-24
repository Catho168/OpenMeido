# OpenMeido

![.NET 版本](https://img.shields.io/badge/.NET-8-512bd4?style=flat-square)
![GitHub Repo size](https://img.shields.io/github/repo-size/Catho168/OpenMeido?style=flat-square&color=3cb371)

LLM驱动的桌面电子女仆，基于WPF开发，灵感来自作品《樱花庄的宠物女孩》中的角色女仆酱(Meido-chan)。

![妹抖酱待机ing...](assets/readme/standby.png)

## 目录

- [特性](#特性)
- [TODO](#todo)
- [技术栈](#技术栈)
- [开始使用](#开始使用)
- [开发与贡献](#开发与贡献)
    - [开发环境准备](#开发环境准备)
- [感谢](#感谢)
- [开源协议](#开源协议)

## 特性

- **全局快捷键唤醒**: 随时随地按下 `Alt + R` 组合键呼出妹抖酱。

- **智能径向菜单**: 快捷浮动按钮，高效完成任务

- **双模式聊天系统**: 
  - 点击妹抖酱，唤出迷你聊天框，快速获取信息
  - 点击"窗口聊天"浮动按钮，进行深入交流

- **智能分句回复**: 妹抖酱可自动分句，拟人化地分多个气泡回复问题

- **兼容OpenAI格式**: 支持自定义API链接，兼容OpenAI标准接口。

- **完整系统提示词**: 自带全面的系统提示词，也支持用户自定义

- **完整MCP支持**: 基于Model Context Protocol标准的工具集成

## TODO

- 桌宠相关功能加入

- 定制的MCP服务器（届时将以独立仓库发布）

- 多句对话处理优化，支持用户短时间内多句输入，统一交妹抖酱处理。

- 支持自定义用户昵称。


## 开始使用

这个项目还处于快速迭代的起步阶段，暂无打包发布版本。如需使用，请按照下文的[开发环境准备](#开发环境准备)自行构建，不要用于生产环境。

## 开发与贡献

欢迎您向 OpenMeido 做出贡献，您可以为本项目做出包括但不限于反馈 Bug、提出功能请求、贡献代码等贡献。

### 开发环境准备

在开始部署OpenMeido之前，您需要确保本地开发环境满足以下要求：

- **操作系统**： Windows 10 版本1803或更高版本，x86_64架构。

- **.NET SDK**： OpenMeido基于.NET 8.0构建。

- **集成开发环境 (IDE)**： 推荐使用 Visual Studio 2022，并确保安装了 [ .NET 桌面开发 ] 工作负载。

- **版本控制**： 安装 Git，用于克隆项目源代码。

- **命令行工具**： 安装 Powershell Core，用于执行命令行操作。

在本代码仓库提交时，请尽量遵守[约定式提交规范](https://www.conventionalcommits.org/zh-hans/v1.0.0/)。

## 感谢

本项目使用了 [FastMenu](https://github.com/FZZoooh/FastMenu) 的部分代码。

妹抖酱形象归版权方所有，如有侵权请联系删除。


## 开源协议

本项目基于 [GNU General Public License v3.0](https://github.com/Catho168/OpenMeido/blob/main/LICENSE) 获得许可。


<div align="center">

如果这个项目对您有帮助，欢迎点亮 Star ⭐！

</div>
