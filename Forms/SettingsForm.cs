using System;
using System.Drawing;
using System.Windows.Forms;

namespace KeJian.Forms
{
    /// <summary>
    /// 设置窗体 - 主题切换、字体设置等
    /// </summary>
    public class SettingsForm : Form
    {
        private CheckBox _chkDarkMode;
        private Button _btnSave;
        private Button _btnCancel;
        private Label _lblAbout;

        /// <summary>暗黑模式变更事件</summary>
        public event EventHandler<bool> DarkModeChanged;

        public SettingsForm(bool currentDarkMode)
        {
            InitializeComponent(currentDarkMode);
        }

        private void InitializeComponent(bool darkMode)
        {
            this.Text = "⚙️ 设置";
            this.Size = new Size(420, 320);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.BackColor = SystemColors.Window;

            // ---- 暗黑模式 ----
            var groupAppearance = new GroupBox
            {
                Text = "外观",
                Location = new Point(16, 16),
                Size = new Size(372, 80),
                Font = new Font("Microsoft YaHei", 9)
            };

            _chkDarkMode = new CheckBox
            {
                Text = "启用暗黑模式",
                Location = new Point(16, 28),
                Size = new Size(160, 24),
                Checked = darkMode,
                Font = new Font("Microsoft YaHei", 9)
            };

            var lblThemeNote = new Label
            {
                Text = "需要重新打开日记查看效果",
                Location = new Point(16, 52),
                Size = new Size(240, 16),
                Font = new Font("Microsoft YaHei", 8),
                ForeColor = Color.Gray
            };

            groupAppearance.Controls.AddRange(new Control[] { _chkDarkMode, lblThemeNote });

            // ---- 关于 ----
            var groupAbout = new GroupBox
            {
                Text = "关于",
                Location = new Point(16, 108),
                Size = new Size(372, 100),
                Font = new Font("Microsoft YaHei", 9)
            };

            _lblAbout = new Label
            {
                Text = "刻简 v1.0\n\n" +
                      "• C# WinForms 原生开发\n" +
                      "• Markdown 语法 + 实时预览\n" +
                      "• 单文件 exe，无需安装\n" +
                      "• 所有数据存储在本地",
                Location = new Point(16, 24),
                Size = new Size(340, 68),
                Font = new Font("Microsoft YaHei", 9),
                ForeColor = SystemColors.WindowText
            };

            groupAbout.Controls.Add(_lblAbout);

            // ---- 底部按钮 ----
            _btnSave = new Button
            {
                Text = "保存",
                Location = new Point(220, 228),
                Size = new Size(80, 30),
                Font = new Font("Microsoft YaHei", 9)
            };
            _btnSave.Click += (s, e) =>
            {
                var newDark = _chkDarkMode.Checked;
                DarkModeChanged?.Invoke(this, newDark);
                this.DialogResult = DialogResult.OK;
                this.Close();
            };

            _btnCancel = new Button
            {
                Text = "取消",
                Location = new Point(310, 228),
                Size = new Size(80, 30),
                Font = new Font("Microsoft YaHei", 9)
            };
            _btnCancel.Click += (s, e) => {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };

            // ---- 快捷键提示 ----
            var lblShortcuts = new Label
            {
                Text = "⌨ 快捷键:  ←/→ 切换日期  |  Ctrl+S 保存  |  Ctrl+B 加粗  |  Ctrl+I 斜体  |  Ctrl+T 今天",
                Location = new Point(16, 264),
                Size = new Size(380, 16),
                Font = new Font("Microsoft YaHei", 8),
                ForeColor = Color.Gray
            };

            this.Controls.AddRange(new Control[] {
                groupAppearance, groupAbout,
                _btnSave, _btnCancel, lblShortcuts
            });
        }
    }
}
