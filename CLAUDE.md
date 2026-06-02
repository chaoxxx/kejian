# KeJian (刻简) - 开发规范

## 项目信息
- .NET Framework 4.8 WinForms 桌面应用（C#）
- 极速单机日记本，支持 Markdown 语法与实时预览
- CI: GitHub Actions (`.github/workflows/build.yml`)
- 构建产物包含 Costura 嵌入的 DLL (WebView2Loader 已内嵌)

## Git 工作流
- 主开发分支: `dev`
- 修复分支命名: `fix/<描述>` 或 `feat/<功能名>`
- 所有代码变更必须从 `dev` 拉出功能/修复分支
- 完成后提交 PR 合并回 `dev`
- PR 标题使用 Conventional Commits 格式: `fix: 描述` / `feat: 描述`

## 开发工具
- 所有代码编写/修改使用 Claude Code CLI
- 不直接手写代码文件

## 代码规范
- WinForms 事件命名: `ControlName_Event` (例如 `MainForm_Load`)
- 使用常量代替魔法数字
- 中文注释说明业务逻辑
