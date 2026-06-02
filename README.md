# 📖 刻简

> 刻简（Kèjiǎn）—— 刻在竹简上，雕琢时光。  
> Windows 10+ 单机日记本，不需安装，点击即用，响应极速。

---

## ✨ 功能特性

| 功能 | 说明 |
|------|------|
| **Markdown 编辑** | 完整支持 GFM 语法：标题、列表、表格、代码块、任务列表等 |
| **实时预览** | WebView2 引擎，类 GitHub 风格渲染，暗黑模式自动适配 |
| **日期导航** | 前一天/后一天/今天/日历选择，← → 快捷键切换 |
| **自动保存** | 输入停顿 3 秒自动保存，无需手动点保存 |
| **标签系统** | 给日记打标签，按标签筛选，右键管理 |
| **全文搜索** | 搜索标题和正文，按匹配度排序 |
| **导出** | 导出为 HTML / TXT / Markdown |
| **备份** | 一键备份全部数据到指定目录 |
| **暗黑模式** | 跟随系统主题或手动切换 |
| **单文件发布** | Costura.Fody 将所有依赖嵌入 exe，一个文件搞定 |
| **快捷键** | Ctrl+S 保存 · Ctrl+B 加粗 · Ctrl+I 斜体 · Ctrl+K 链接 · →← 切换日期 |

## 🚀 使用方式

### 方式一：下载 Release（推荐）

从 Releases 页面下载 `KeJian.exe`，双击运行即可。  
所有数据保存在 exe 同目录下的 `data/` 文件夹中。

### 方式二：自行构建

**前置要求：**
- Windows 10 / 11
- .NET Framework 4.8 SDK（Win10 自带运行时，构建需要 SDK）
- NuGet 包管理器

**构建步骤：**

```bash
# 方式 A - 使用 dotnet CLI（推荐）
dotnet restore
dotnet build -c Release
dotnet publish -c Release -o publish

# 方式 B - 双击 build.bat
```

## 🗂️ 项目结构

```
KeJian/
├── Program.cs                # 入口（单实例检测）
├── KeJian.csproj             # 项目文件（.NET Framework 4.8）
├── FodyWeavers.xml           # Costura 嵌入配置
├── app.manifest              # 高 DPI / Win10 主题声明
├── build.bat                 # Windows 构建脚本
├── Forms/
│   ├── MainForm.cs           # 主界面（工具栏 + 编辑器 + 预览 + 标签 + 搜索）
│   └── SettingsForm.cs       # 设置窗体
├── Core/
│   ├── DiaryEntry.cs         # 日记数据模型
│   ├── DiaryStorage.cs       # JSON 本地持久化引擎（带 LRU 缓存）
│   └── SearchEngine.cs       # 全文搜索引擎（标题+内容+标签）
├── Markdown/
│   └── MarkdownRenderer.cs   # Markdig → HTML 渲染器
└── Resources/
    └── markdown.css          # GitHub 风格预览样式（含暗黑模式适配）
```

## 🛠️ 技术栈

- **语言:** C# 10
- **框架:** .NET Framework 4.8 (WinForms)
- **Markdown:** Markdig 0.37（完整 GFM 支持）
- **预览:** WebView2 (Edge Chromium)
- **存储:** JSON 文件（每篇日记独立文件，LRU 缓存加速）
- **打包:** Costura.Fody（单文件发布）

## 📝 数据格式

日记存储在 `data/` 目录下，按 `年/月/日.json` 组织：

```
data/
├── 2026/
│   ├── 06/
│   │   ├── 2026-06-01.json
│   │   └── 2026-06-02.json
│   └── ...
└── ...
```

每篇日记是独立 JSON 文件，可直接用文本编辑器查看/修改，方便手工备份。

## ⌨️ 快捷键一览

| 快捷键 | 功能 |
|--------|------|
| `Ctrl+S` | 保存当前日记 |
| `Ctrl+B` | 加粗选中文字 |
| `Ctrl+I` | 斜体选中文字 |
| `Ctrl+K` | 插入链接 |
| `←` / `→` | 前一天 / 后一天 |
| `Tab` | 输入 4 个空格 |
| `Ctrl+D` | 回到今天 |
| `Ctrl+Shift+P` | 切换编辑/预览/分栏 |

## 📄 许可证

MIT License
