using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Timers;
using System.Windows.Forms;
using KeJian.Core;
using KeJian.Markdown;
using Microsoft.Web.WebView2.WinForms;

namespace KeJian.Forms
{
    /// <summary>
    /// 主窗体 - 日记本核心界面
    /// 全代码实现（无设计器），布局：上(工具栏) | 中(编辑+预览分栏) | 下(标签/搜索/状态)
    /// </summary>
    public class MainForm : Form
    {
        // ========== 依赖 ==========
        private readonly DiaryStorage _storage;
        private readonly SearchEngine _search;
        private readonly MarkdownRenderer _renderer;

        // ========== 当前状态 ==========
        private DateTime _currentDate = DateTime.Today;
        private bool _isModified = false;
        private bool _isLoading = false; // 防止加载触发修改标记
        private bool _darkMode = false;

        // ========== 自动保存定时器 ==========
        private System.Timers.Timer _autoSaveTimer;
        private const int AutoSaveDelayMs = 3000;

        // ========== UI 控件 ==========
        private Panel _topToolbar;
        private SplitContainer _splitContainer;
        private TextBox _txtEditor;
        private WebView2 _webViewPreview;
        private Label _lblDate;
        private DateTimePicker _dtpDate;
        private Button _btnPrevDay;
        private Button _btnNextDay;
        private Button _btnToday;
        private ToolStrip _modeToolbar;
        private ToolStripButton _btnEditMode;
        private ToolStripButton _btnPreviewMode;
        private ToolStripButton _btnSplitMode;
        private FlowLayoutPanel _tagPanel;
        private TextBox _txtTagInput;
        private Button _btnAddTag;
        private TextBox _txtSearch;
        private ListBox _lstSearchResults;
        private StatusStrip _statusStrip;
        private ToolStripStatusLabel _lblWordCount;
        private ToolStripStatusLabel _lblSaveStatus;
        private ToolStripStatusLabel _lblEntryCount;

        // ========== 上下文菜单 ==========
        private ContextMenuStrip _editorContextMenu;
        private ContextMenuStrip _tagContextMenu;
        private string _rightClickedTag;

        // ========== 设置引用 ==========
        private SettingsForm _settingsForm;

        // ========== 尺寸常量 ==========
        private const int ToolbarHeight = 40;
        private const int BottomPanelHeight = 36;
        private const int MinWindowWidth = 900;
        private const int MinWindowHeight = 550;

        public MainForm()
        {
            _storage = new DiaryStorage();
            _search = new SearchEngine(_storage);
            _renderer = new MarkdownRenderer();

            InitializeComponent();
            SetupAutoSave();
            LoadSettings();

            // 默认加载今天
            NavigateToDate(DateTime.Today);
            UpdateEntryCount();
        }

        // ==================================================================
        // 初始化 UI
        // ==================================================================
        private void InitializeComponent()
        {
            this.Text = "📖 刻简";
            this.MinimumSize = new Size(MinWindowWidth, MinWindowHeight);
            this.Size = new Size(1200, 760);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = SystemColors.Window;
            this.KeyPreview = true;
            this.KeyDown += MainForm_KeyDown;
            this.FormClosing += MainForm_FormClosing;

            // ---- 上方工具栏 ----
            BuildTopToolbar();

            // ---- 中间分栏 ----
            BuildSplitContainer();

            // ---- 底部面板 ----
            BuildBottomBar();

            // ---- 状态栏 ----
            BuildStatusStrip();

            // ---- 上下文菜单 ----
            BuildContextMenus();
        }

        // ==================================================================
        // 构建上方工具栏
        // ==================================================================
        private void BuildTopToolbar()
        {
            _topToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = ToolbarHeight,
                BackColor = SystemColors.Control,
                Padding = new Padding(8, 4, 8, 4)
            };

            // 日期导航区
            _btnPrevDay = CreateToolButton("◀", 28, 28, "前一天 (←)");
            _btnPrevDay.Click += (s, e) => NavigateRelative(-1);
            _btnPrevDay.Location = new Point(8, 6);

            _lblDate = new Label
            {
                AutoSize = false,
                Size = new Size(160, 28),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Microsoft YaHei", 12, FontStyle.Bold),
                Location = new Point(40, 6),
                Text = DateTime.Today.ToString("yyyy年M月d日 dddd"),
                ForeColor = SystemColors.WindowText
            };

            _btnNextDay = CreateToolButton("▶", 28, 28, "后一天 (→)");
            _btnNextDay.Click += (s, e) => NavigateRelative(1);
            _btnNextDay.Location = new Point(204, 6);

            _btnToday = CreateToolButton("今天", 56, 28, "回到今天 (T)");
            _btnToday.Font = new Font("Microsoft YaHei", 9, FontStyle.Regular);
            _btnToday.Click += (s, e) => NavigateToDate(DateTime.Today);
            _btnToday.Location = new Point(240, 6);

            // 日期选择器
            _dtpDate = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Size = new Size(110, 24),
                Location = new Point(304, 8),
                ShowCheckBox = false
            };
            _dtpDate.ValueChanged += (s, e) =>
            {
                if (!_isLoading)
                    NavigateToDate(_dtpDate.Value);
            };

            // 模式切换工具栏
            _modeToolbar = new ToolStrip
            {
                Dock = DockStyle.None,
                Location = new Point(430, 4),
                GripStyle = ToolStripGripStyle.Hidden,
                BackColor = SystemColors.Control,
                RenderMode = ToolStripRenderMode.Professional
            };

            _btnEditMode = new ToolStripButton("✏️ 编辑", null, (s, e) => SetViewMode(ViewMode.Edit))
            {
                Checked = true,
                DisplayStyle = ToolStripItemDisplayStyle.Text
            };
            _btnPreviewMode = new ToolStripButton("👁️ 预览", null, (s, e) => SetViewMode(ViewMode.Preview))
            {
                DisplayStyle = ToolStripItemDisplayStyle.Text
            };
            _btnSplitMode = new ToolStripButton("📐 分栏", null, (s, e) => SetViewMode(ViewMode.Split))
            {
                DisplayStyle = ToolStripItemDisplayStyle.Text
            };
            _modeToolbar.Items.Add(_btnEditMode);
            _modeToolbar.Items.Add(new ToolStripSeparator { Size = new Size(6, 28) });
            _modeToolbar.Items.Add(_btnPreviewMode);
            _modeToolbar.Items.Add(new ToolStripSeparator { Size = new Size(6, 28) });
            _modeToolbar.Items.Add(_btnSplitMode);

            // 右侧功能按钮
            var btnSettings = new Button
            {
                Text = "⚙️",
                Size = new Size(32, 28),
                Location = new Point(_topToolbar.Width - 120, 6),
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                ToolTipText = "设置"
            };
            btnSettings.Click += (s, e) => ShowSettings();

            var btnExport = new Button
            {
                Text = "📤",
                Size = new Size(32, 28),
                Location = new Point(_topToolbar.Width - 84, 6),
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                ToolTipText = "导出当前日记"
            };
            btnExport.Click += (s, e) => ExportCurrentDiary();

            var btnBackup = new Button
            {
                Text = "💾",
                Size = new Size(32, 28),
                Location = new Point(_topToolbar.Width - 48, 6),
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                ToolTipText = "备份所有数据"
            };
            btnBackup.Click += (s, e) => BackupAll();

            _topToolbar.Controls.AddRange(new Control[] {
                _btnPrevDay, _lblDate, _btnNextDay, _btnToday, _dtpDate,
                _modeToolbar, btnSettings, btnExport, btnBackup
            });

            // 响应大小变化调整右侧按钮位置
            _topToolbar.Resize += (s, e) => {
                btnSettings.Left = _topToolbar.Width - 120;
                btnExport.Left = _topToolbar.Width - 84;
                btnBackup.Left = _topToolbar.Width - 48;
            };

            this.Controls.Add(_topToolbar);
        }

        // ==================================================================
        // 构建中间分栏（编辑器 + 预览）
        // ==================================================================
        private void BuildSplitContainer()
        {
            _splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = this.ClientSize.Width / 2,
                Panel1MinSize = 200,
                Panel2MinSize = 200,
                BackColor = SystemColors.Control,
                SplitterWidth = 4
            };

            // ---- 编辑器 ----
            _txtEditor = new TextBox
            {
                Multiline = true,
                Dock = DockStyle.Fill,
                Font = new Font("Cascadia Code, 'Fira Code', Consolas, 'Microsoft YaHei Mono'", 14, FontStyle.Regular),
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(250, 250, 250),
                ForeColor = Color.FromArgb(36, 41, 47),
                AcceptsTab = false, // Tab 输入空格
                WordWrap = true,
                ScrollBars = ScrollBars.Vertical,
                Text = "",
                MaxLength = 0  // 无限制
            };
            _txtEditor.TextChanged += Editor_TextChanged;
            _txtEditor.KeyDown += Editor_KeyDown;
            _txtEditor.Enter += (s, e) => UpdateStatusBar();

            // 编辑器容器 - 加边框
            var editorPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 12, 16, 12),
                BackColor = Color.FromArgb(250, 250, 250)
            };
            editorPanel.Controls.Add(_txtEditor);
            _splitContainer.Panel1.Controls.Add(editorPanel);

            // ---- 预览区 ----
            _webViewPreview = new WebView2
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };
            _webViewPreview.NavigationStarting += (s, e) =>
            {
                // 拦截外部链接，用默认浏览器打开
                if (e.Uri != null && !e.Uri.StartsWith("about:") && !e.Uri.StartsWith("data:"))
                {
                    e.Cancel = true;
                    try { System.Diagnostics.Process.Start(e.Uri); } catch { }
                }
            };
            _splitContainer.Panel2.Controls.Add(_webViewPreview);

            this.Controls.Add(_splitContainer);
            _splitContainer.SendToBack();

            // 初始化 WebView2
            InitializeWebView();
        }

        private async void InitializeWebView()
        {
            try
            {
                var env = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync(
                    userDataFolder: Path.Combine(_storage.DataRoot, ".webview2"));
                await _webViewPreview.EnsureCoreWebView2Async(env);
                _webViewPreview.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = false;
                _webViewPreview.CoreWebView2.Settings.IsWebMessageEnabled = false;
                _webViewPreview.CoreWebView2.Settings.IsStatusBarEnabled = false;
                _webViewPreview.CoreWebView2.Settings.AreDevToolsEnabled = false;
                RefreshPreview();
            }
            catch (Exception ex)
            {
                // WebView2 不可用，显示提示
                _webViewPreview.Visible = false;
                var lbl = new Label
                {
                    Text = $"⚠️ WebView2 未安装或初始化失败\n\n请在 Microsoft Edge 官网下载 WebView2 Runtime\n(约 1.5MB，安装后重启本软件)\n\n错误: {ex.Message}",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Microsoft YaHei", 11),
                    ForeColor = Color.FromArgb(100, 100, 100)
                };
                _splitContainer.Panel2.Controls.Add(lbl);
            }
        }

        // ==================================================================
        // 构建底部面板（标签 + 搜索）
        // ==================================================================
        private void BuildBottomBar()
        {
            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = BottomPanelHeight + 40,
                BackColor = SystemColors.Control,
                Padding = new Padding(8, 4, 8, 4)
            };

            // 标签区域
            var lblTags = new Label
            {
                Text = "🏷️",
                AutoSize = true,
                Location = new Point(8, 10),
                Font = new Font("Microsoft YaHei", 10)
            };

            _tagPanel = new FlowLayoutPanel
            {
                Location = new Point(36, 6),
                Size = new Size(400, 30),
                AutoSize = false,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BorderStyle = BorderStyle.None
            };
            _tagPanel.MouseWheel += (s, e) => {
                // 滚轮水平滚动
                if (_tagPanel.HorizontalScroll.Visible)
                    _tagPanel.HorizontalScroll.Value = Math.Max(0,
                        Math.Min(_tagPanel.HorizontalScroll.Maximum,
                            _tagPanel.HorizontalScroll.Value - e.Delta));
            };

            _txtTagInput = new TextBox
            {
                Size = new Size(80, 22),
                Location = new Point(440, 8),
                Font = new Font("Microsoft YaHei", 9),
                Text = ""
            };
            _txtTagInput.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter)
                    AddCurrentTag();
                if (e.KeyCode == Keys.Escape)
                {
                    _txtTagInput.Text = "";
                    _txtEditor.Focus();
                }
            };

            _btnAddTag = new Button
            {
                Text = "+",
                Size = new Size(26, 22),
                Location = new Point(526, 7),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei", 10, FontStyle.Bold)
            };
            _btnAddTag.Click += (s, e) => AddCurrentTag();

            // 搜索区域
            var lblSearch = new Label
            {
                Text = "🔍",
                AutoSize = true,
                Location = new Point(570, 10),
                Font = new Font("Microsoft YaHei", 10)
            };

            _txtSearch = new TextBox
            {
                Size = new Size(160, 22),
                Location = new Point(594, 8),
                Font = new Font("Microsoft YaHei", 9)
            };
            _txtSearch.TextChanged += (s, e) => PerformSearch(_txtSearch.Text);
            _txtSearch.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Escape)
                {
                    _txtSearch.Text = "";
                    _txtEditor.Focus();
                }
            };

            // 搜索结果（下拉列表）
            _lstSearchResults = new ListBox
            {
                Location = new Point(594, 32),
                Size = new Size(450, 0),
                Visible = false,
                Font = new Font("Microsoft YaHei", 9),
                BorderStyle = BorderStyle.FixedSingle,
                DisplayMember = "DisplayText"
            };
            _lstSearchResults.SelectedIndexChanged += (s, e) => {
                if (_lstSearchResults.SelectedItem is SearchEngine.SearchResult result)
                {
                    if (DateTime.TryParseExact(result.Date, "yyyy-MM-dd", null,
                        System.Globalization.DateTimeStyles.None, out var dt))
                    {
                        NavigateToDate(dt);
                        _lstSearchResults.Visible = false;
                        _txtSearch.Text = "";
                    }
                }
            };
            _lstSearchResults.LostFocus += (s, e) => {
                // 延迟隐藏，让点击选中生效
                this.BeginInvoke((Action)(() => _lstSearchResults.Visible = false));
            };

            // 标签和搜索的父面板锚定
            bottomPanel.Resize += (s, e) => {
                // 动态调整搜索框位置
                _lstSearchResults.Width = Math.Min(450, bottomPanel.Width - _lstSearchResults.Left - 20);
            };

            bottomPanel.Controls.AddRange(new Control[] {
                lblTags, _tagPanel, _txtTagInput, _btnAddTag,
                lblSearch, _txtSearch, _lstSearchResults
            });

            // 分隔线
            var separator = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = SystemColors.ControlDark
            };

            this.Controls.Add(bottomPanel);
            this.Controls.Add(separator);
        }

        // ==================================================================
        // 构建状态栏
        // ==================================================================
        private void BuildStatusStrip()
        {
            _statusStrip = new StatusStrip
            {
                Dock = DockStyle.Bottom,
                BackColor = SystemColors.Control,
                SizingGrip = false
            };

            _lblWordCount = new ToolStripStatusLabel("字数: 0");
            _lblSaveStatus = new ToolStripStatusLabel("✅ 已保存");
            _lblEntryCount = new ToolStripStatusLabel("📚 总篇数: 0");

            _statusStrip.Items.Add(_lblWordCount);
            _statusStrip.Items.Add(new ToolStripSeparator());
            _statusStrip.Items.Add(_lblSaveStatus);
            _statusStrip.Items.Add(new ToolStripSeparator());
            _statusStrip.Items.Add(_lblEntryCount);

            this.Controls.Add(_statusStrip);
        }

        // ==================================================================
        // 上下文菜单
        // ==================================================================
        private void BuildContextMenus()
        {
            // 编辑器右键菜单
            _editorContextMenu = new ContextMenuStrip();
            _editorContextMenu.Items.Add("全部选中", null, (s, e) => _txtEditor.SelectAll());
            _editorContextMenu.Items.Add("剪切", null, (s, e) => _txtEditor.Cut());
            _editorContextMenu.Items.Add("复制", null, (s, e) => _txtEditor.Copy());
            _editorContextMenu.Items.Add("粘贴", null, (s, e) => _txtEditor.Paste());
            _editorContextMenu.Items.Add(new ToolStripSeparator());
            _editorContextMenu.Items.Add("撤销", null, (s, e) => _txtEditor.Undo());
            _editorContextMenu.Items.Add("重做", null, (s, e) => { try { _txtEditor.Undo(); _txtEditor.Undo(); } catch { } });
            _editorContextMenu.Items.Add(new ToolStripSeparator());
            _editorContextMenu.Items.Add("清空内容", null, (s, e) => {
                if (MessageBox.Show("确定清空当前日记内容？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    _txtEditor.Text = "";
            });
            _txtEditor.ContextMenuStrip = _editorContextMenu;

            // 标签右键菜单
            _tagContextMenu = new ContextMenuStrip();
            _tagContextMenu.Items.Add("删除标签", null, (s, e) => {
                if (!string.IsNullOrEmpty(_rightClickedTag))
                    RemoveTag(_rightClickedTag);
            });
            _tagContextMenu.Items.Add("筛选同类", null, (s, e) => {
                if (!string.IsNullOrEmpty(_rightClickedTag))
                    FilterByTag(_rightClickedTag);
            });
            _tagContextMenu.Opening += (s, e) => {
                _rightClickedTag = _tagContextMenu.SourceControl?.Tag as string;
                if (string.IsNullOrEmpty(_rightClickedTag))
                    e.Cancel = true;
            };
        }

        // ==================================================================
        // 自动保存
        // ==================================================================
        private void SetupAutoSave()
        {
            _autoSaveTimer = new System.Timers.Timer(AutoSaveDelayMs)
            {
                AutoReset = false,
                Enabled = false
            };
            _autoSaveTimer.Elapsed += (s, e) =>
            {
                try
                {
                    this.Invoke((Action)SaveCurrentDiary);
                }
                catch { /* form closed */ }
            };
        }

        // ==================================================================
        // 编辑器事件
        // ==================================================================
        private void Editor_TextChanged(object sender, EventArgs e)
        {
            if (_isLoading) return;

            _isModified = true;
            UpdateStatusBar();

            // 重置自动保存计时器
            _autoSaveTimer.Stop();
            _autoSaveTimer.Start();

            // 实时预览更新（仅在分栏或预览模式时）
            if (!_btnEditMode.Checked)
            {
                RefreshPreview();
            }
        }

        private void Editor_KeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+S 保存
            if (e.Control && e.KeyCode == Keys.S)
            {
                SaveCurrentDiary();
                e.SuppressKeyPress = true;
                return;
            }

            // Tab 输入 4 空格
            if (e.KeyCode == Keys.Tab)
            {
                int selStart = _txtEditor.SelectionStart;
                _txtEditor.Text = _txtEditor.Text.Substring(0, selStart) + "    " +
                                   _txtEditor.Text.Substring(selStart + _txtEditor.SelectionLength);
                _txtEditor.SelectionStart = selStart + 4;
                e.SuppressKeyPress = true;
                return;
            }

            // Ctrl+B 加粗
            if (e.Control && e.KeyCode == Keys.B)
            {
                InsertMarkdownSyntax("**", "**");
                e.SuppressKeyPress = true;
                return;
            }

            // Ctrl+I 斜体
            if (e.Control && e.KeyCode == Keys.I)
            {
                InsertMarkdownSyntax("*", "*");
                e.SuppressKeyPress = true;
                return;
            }

            // Ctrl+K 链接
            if (e.Control && e.KeyCode == Keys.K)
            {
                var selection = _txtEditor.SelectedText;
                InsertMarkdownSyntax("[", "](" + (string.IsNullOrEmpty(selection) ? "url" : selection) + ")");
                e.SuppressKeyPress = true;
                return;
            }
        }

        // ==================================================================
        // 全局快捷键
        // ==================================================================
        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.D)
            {
                NavigateToDate(DateTime.Today);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Left && e.Control)
            {
                NavigateRelative(-1);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Right && e.Control)
            {
                NavigateRelative(1);
                e.Handled = true;
            }
        }

        // ==================================================================
        // 视图模式
        // ==================================================================
        private enum ViewMode { Edit, Preview, Split }
        private ViewMode _currentViewMode = ViewMode.Split;

        private void SetViewMode(ViewMode mode)
        {
            _currentViewMode = mode;
            _btnEditMode.Checked = mode == ViewMode.Edit;
            _btnPreviewMode.Checked = mode == ViewMode.Preview;
            _btnSplitMode.Checked = mode == ViewMode.Split;

            switch (mode)
            {
                case ViewMode.Edit:
                    _splitContainer.Panel1Collapsed = false;
                    _splitContainer.Panel2Collapsed = true;
                    break;
                case ViewMode.Preview:
                    _splitContainer.Panel1Collapsed = true;
                    _splitContainer.Panel2Collapsed = false;
                    RefreshPreview();
                    break;
                case ViewMode.Split:
                    _splitContainer.Panel1Collapsed = false;
                    _splitContainer.Panel2Collapsed = false;
                    _splitContainer.SplitterDistance = this.ClientSize.Width / 2;
                    RefreshPreview();
                    break;
            }
        }

        // ==================================================================
        // 日期导航
        // ==================================================================
        private void NavigateRelative(int offset)
        {
            var target = _currentDate.AddDays(offset);
            NavigateToDate(target);
        }

        private bool NavigateToDate(DateTime date)
        {
            if (_isModified)
            {
                SaveCurrentDiary();
            }

            _currentDate = date.Date;
            _isLoading = true;

            _lblDate.Text = date.ToString("yyyy年M月d日 dddd");
            _dtpDate.Value = date;

            var entry = _storage.Load(date.ToString("yyyy-MM-dd"));

            // 清空编辑器并加载内容
            _txtEditor.Text = "";
            _txtEditor.Text = entry.Content ?? "";
            _txtEditor.Tag = entry;
            _isModified = false;
            _isLoading = false;

            // 更新标签
            RefreshTags(entry.Tags);

            // 更新预览
            RefreshPreview();

            // 更新状态栏
            UpdateStatusBar();
            _lblSaveStatus.Text = "✅ 已加载";

            return true;
        }

        // ==================================================================
        // 保存
        // ==================================================================
        private void SaveCurrentDiary()
        {
            if (_isLoading) return;

            var dateStr = _currentDate.ToString("yyyy-MM-dd");
            var entry = _storage.Load(dateStr);

            entry.Content = _txtEditor.Text;

            // 自动提取标题（首行 # 内容或第一行文字）
            var firstLine = GetFirstLine(_txtEditor.Text);
            entry.Title = firstLine ?? "(无标题)";

            // 重新收集标签（从控件）
            entry.Tags.Clear();
            foreach (Control ctrl in _tagPanel.Controls)
            {
                if (ctrl is Label lbl && lbl.Tag is string tag)
                    entry.Tags.Add(tag);
            }

            _storage.Save(entry);
            _isModified = false;
            _lblSaveStatus.Text = "✅ 已保存 " + DateTime.Now.ToString("HH:mm:ss");
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_isModified)
            {
                var result = MessageBox.Show("当前日记未保存，是否保存？",
                    "提示", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                    SaveCurrentDiary();
                else if (result == DialogResult.Cancel)
                    e.Cancel = true;
            }

            _autoSaveTimer?.Stop();
            _autoSaveTimer?.Dispose();
        }

        // ==================================================================
        // 预览刷新
        // ==================================================================
        private void RefreshPreview()
        {
            if (_webViewPreview?.CoreWebView2 == null) return;

            try
            {
                var html = _renderer.RenderToHtml(_txtEditor.Text);
                _webViewPreview.NavigateToString(html);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"预览刷新失败: {ex.Message}");
            }
        }

        // ==================================================================
        // 标签系统
        // ==================================================================
        private void RefreshTags(List<string> tags)
        {
            _tagPanel.Controls.Clear();
            if (tags == null) return;

            foreach (var tag in tags)
            {
                AddTagChip(tag);
            }
        }

        private void AddTagChip(string tag)
        {
            var chip = new Label
            {
                Text = $"#{tag}  ×",
                AutoSize = true,
                Padding = new Padding(6, 2, 6, 2),
                Font = new Font("Microsoft YaHei", 9),
                BackColor = Color.FromArgb(225, 240, 255),
                ForeColor = Color.FromArgb(30, 100, 200),
                Tag = tag,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 2, 4, 2)
            };
            chip.TextAlign = ContentAlignment.MiddleCenter;

            // 用 Panel 包裹实现圆角效果
            var wrapper = new Panel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(0),
                Tag = tag
            };
            chip.MouseClick += (s, e) => {
                if (e.Button == MouseButtons.Left)
                {
                    // 点击 X 删除
                    if (s is Label lbl && lbl.Text.EndsWith("×") &&
                        e.X > lbl.Width - 20)
                    {
                        RemoveTag(lbl.Tag as string);
                    }
                    else if (s is Label label)
                    {
                        FilterByTag(label.Tag as string);
                    }
                }
            };
            chip.ContextMenuStrip = _tagContextMenu;
            chip.ContextMenuStrip.SourceControl = chip;

            wrapper.Controls.Add(chip);
            _tagPanel.Controls.Add(wrapper);
        }

        private void AddCurrentTag()
        {
            var tag = _txtTagInput.Text.Trim();
            if (string.IsNullOrEmpty(tag)) return;

            // 去重检查
            foreach (Control ctrl in _tagPanel.Controls)
            {
                if (ctrl is Panel p && p.Tag is string t &&
                    string.Equals(t, tag, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            AddTagChip(tag);
            _txtTagInput.Text = "";
            _txtTagInput.Focus();
            _isModified = true;
        }

        private void RemoveTag(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return;

            foreach (Control ctrl in _tagPanel.Controls.ToArray())
            {
                if (ctrl is Panel p && p.Tag is string t &&
                    string.Equals(t, tag, StringComparison.OrdinalIgnoreCase))
                {
                    _tagPanel.Controls.Remove(ctrl);
                    _isModified = true;
                    break;
                }
            }
        }

        private void FilterByTag(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return;

            var dates = _search.GetDatesByTag(tag);
            if (dates.Count == 0)
            {
                _lblSaveStatus.Text = $"🏷️ 标签 \"{tag}\" 下无日记";
                return;
            }

            // 跳转到最近的匹配日记
            if (DateTime.TryParseExact(dates[0], "yyyy-MM-dd", null,
                System.Globalization.DateTimeStyles.None, out var dt))
            {
                NavigateToDate(dt);
                _lblSaveStatus.Text = $"🏷️ 标签 \"{tag}\": {dates.Count} 篇";
            }
        }

        // ==================================================================
        // 搜索
        // ==================================================================
        private async void PerformSearch(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                _lstSearchResults.Visible = false;
                return;
            }

            var results = await _search.SearchAsync(query);
            _lstSearchResults.Items.Clear();

            if (results.Count == 0)
            {
                _lstSearchResults.Items.Add(new { DisplayText = "🔍 未找到匹配结果" });
                _lstSearchResults.Visible = true;
                _lstSearchResults.Height = 24;
                return;
            }

            foreach (var r in results.Take(20))
            {
                _lstSearchResults.Items.Add(new {
                    DisplayText = $"{r.Date}  {r.Title}  [{string.Join(", ", r.Tags)}]",
                    Result = r
                });
            }

            _lstSearchResults.Visible = true;
            _lstSearchResults.Height = Math.Min(results.Count * 24 + 4, 300);
            _lstSearchResults.BringToFront();
        }

        // ==================================================================
        // 导出和备份
        // ==================================================================
        private void ExportCurrentDiary()
        {
            SaveCurrentDiary();

            using (var dlg = new SaveFileDialog
            {
                Filter = "HTML 文件|*.html|纯文本|*.txt|Markdown|*.md",
                FileName = $"日记_{_currentDate:yyyyMMdd}.html",
                Title = "导出当前日记"
            })
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var entry = _storage.Load(_currentDate.ToString("yyyy-MM-dd"));
                        switch (Path.GetExtension(dlg.FileName).ToLower())
                        {
                            case ".html":
                                var html = _renderer.ExportToHtmlFile(entry.Content, entry.Title);
                                File.WriteAllText(dlg.FileName, html, Encoding.UTF8);
                                break;
                            case ".txt":
                                File.WriteAllText(dlg.FileName, entry.PlainText, Encoding.UTF8);
                                break;
                            case ".md":
                                File.WriteAllText(dlg.FileName,
                                    $"# {entry.Title}\n\n{entry.Content}", Encoding.UTF8);
                                break;
                        }
                        _lblSaveStatus.Text = $"📤 已导出到 {Path.GetFileName(dlg.FileName)}";
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"导出失败: {ex.Message}", "错误",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BackupAll()
        {
            using (var dlg = new FolderBrowserDialog
            {
                Description = "选择备份保存位置",
                UseDescriptionForTitle = true
            })
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var backupDir = Path.Combine(dlg.SelectedPath,
                            $"日记备份_{DateTime.Now:yyyyMMdd_HHmmss}");
                        Directory.CreateDirectory(backupDir);

                        // 复制 data 目录
                        CopyDirectory(_storage.DataRoot, Path.Combine(backupDir, "data"));

                        // 生成统计信息
                        var stats = new StringBuilder();
                        stats.AppendLine($"导出时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                        stats.AppendLine($"总篇数: {_storage.TotalCount}");
                        stats.AppendLine();
                        File.WriteAllText(Path.Combine(backupDir, "统计信息.txt"), stats.ToString(), Encoding.UTF8);

                        _lblSaveStatus.Text = $"💾 备份完成: {backupDir}";

                        // 打开备份目录
                        System.Diagnostics.Process.Start(backupDir);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"备份失败: {ex.Message}", "错误",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        // ==================================================================
        // 设置
        // ==================================================================
        private void ShowSettings()
        {
            if (_settingsForm == null || _settingsForm.IsDisposed)
            {
                _settingsForm = new SettingsForm(_darkMode);
                _settingsForm.DarkModeChanged += (s, darkMode) =>
                {
                    _darkMode = darkMode;
                    ApplyTheme();
                };
            }
            _settingsForm.ShowDialog();
        }

        private void LoadSettings()
        {
            var settingsPath = Path.Combine(_storage.DataRoot, ".settings.json");
            if (File.Exists(settingsPath))
            {
                try
                {
                    var json = File.ReadAllText(settingsPath, Encoding.UTF8);
                    var settings = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                    if (settings != null)
                    {
                        if (settings.TryGetValue("darkMode", out var dark) && dark is bool b)
                            _darkMode = b;
                    }
                }
                catch { }
            }

            ApplyTheme();
        }

        private void SaveSettings()
        {
            var settingsPath = Path.Combine(_storage.DataRoot, ".settings.json");
            try
            {
                var settings = new Dictionary<string, object>
                {
                    ["darkMode"] = _darkMode,
                    ["lastVersion"] = "1.0.0"
                };
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(settings,
                    Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(settingsPath, json, Encoding.UTF8);
            }
            catch { }
        }

        private void ApplyTheme()
        {
            if (_darkMode)
            {
                this.BackColor = Color.FromArgb(30, 30, 30);
                _topToolbar.BackColor = Color.FromArgb(45, 45, 45);
                _modeToolbar.BackColor = Color.FromArgb(45, 45, 45);
                _lblDate.ForeColor = Color.White;
                _txtEditor.BackColor = Color.FromArgb(30, 30, 30);
                _txtEditor.ForeColor = Color.FromArgb(220, 220, 220);
                _autoSaveTimer.Stop();

                // 底部面板
                if (this.Controls[2] is Panel bp)
                    bp.BackColor = Color.FromArgb(45, 45, 45);

                _statusStrip.BackColor = Color.FromArgb(45, 45, 45);
                foreach (ToolStripItem item in _statusStrip.Items)
                {
                    if (item is ToolStripStatusLabel lbl)
                        lbl.ForeColor = Color.White;
                }

                _txtTagInput.BackColor = Color.FromArgb(60, 60, 60);
                _txtTagInput.ForeColor = Color.White;
                _btnAddTag.BackColor = Color.FromArgb(60, 60, 60);
                _btnAddTag.ForeColor = Color.White;
                _txtSearch.BackColor = Color.FromArgb(60, 60, 60);
                _txtSearch.ForeColor = Color.White;
                _lstSearchResults.BackColor = Color.FromArgb(60, 60, 60);
                _lstSearchResults.ForeColor = Color.White;
            }
            else
            {
                this.BackColor = SystemColors.Window;
                _topToolbar.BackColor = SystemColors.Control;
                _modeToolbar.BackColor = SystemColors.Control;
                _lblDate.ForeColor = SystemColors.WindowText;
                _txtEditor.BackColor = Color.FromArgb(250, 250, 250);
                _txtEditor.ForeColor = Color.FromArgb(36, 41, 47);
                _statusStrip.BackColor = SystemColors.Control;
                foreach (ToolStripItem item in _statusStrip.Items)
                {
                    if (item is ToolStripStatusLabel lbl)
                        lbl.ForeColor = SystemColors.WindowText;
                }

                _txtTagInput.BackColor = Color.White;
                _txtTagInput.ForeColor = SystemColors.WindowText;
                _btnAddTag.BackColor = SystemColors.Control;
                _btnAddTag.ForeColor = SystemColors.WindowText;
                _txtSearch.BackColor = Color.White;
                _txtSearch.ForeColor = SystemColors.WindowText;
                _lstSearchResults.BackColor = Color.White;
                _lstSearchResults.ForeColor = SystemColors.WindowText;
            }

            SaveSettings();
        }

        // ==================================================================
        // 状态栏更新
        // ==================================================================
        private void UpdateStatusBar()
        {
            _lblWordCount.Text = $"📝 字数: {_txtEditor.Text.Length}";
        }

        private void UpdateEntryCount()
        {
            _lblEntryCount.Text = $"📚 总篇数: {_storage.TotalCount}";
        }

        // ==================================================================
        // 帮助方法
        // ==================================================================
        private Button CreateToolButton(string text, int width, int height, string toolTip)
        {
            return new Button
            {
                Text = text,
                Size = new Size(width, height),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei", 9, FontStyle.Regular),
                ToolTipText = toolTip,
                UseVisualStyleBackColor = true,
                TabStop = false
            };
        }

        private void InsertMarkdownSyntax(string prefix, string suffix)
        {
            var selStart = _txtEditor.SelectionStart;
            var selLength = _txtEditor.SelectionLength;

            if (selLength > 0)
            {
                var selected = _txtEditor.SelectedText;
                _txtEditor.SelectedText = prefix + selected + suffix;
                _txtEditor.SelectionStart = selStart + prefix.Length + selLength + suffix.Length;
            }
            else
            {
                _txtEditor.SelectedText = prefix + suffix;
                _txtEditor.SelectionStart = selStart + prefix.Length;
                _txtEditor.SelectionLength = 0;
            }

            _txtEditor.Focus();
        }

        private string GetFirstLine(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;

            // 尝试获取 Markdown 标题 (# 开头)
            using (var reader = new StringReader(text))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("# "))
                        return trimmed.Substring(2).Trim();
                    if (!string.IsNullOrWhiteSpace(trimmed) && !trimmed.StartsWith("-") && !trimmed.StartsWith(">"))
                        return trimmed.Length > 50 ? trimmed.Substring(0, 50) + "..." : trimmed;
                }
            }
            return null;
        }

        private void CopyDirectory(string source, string dest)
        {
            Directory.CreateDirectory(dest);
            foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                var relPath = Path.GetRelativePath(source, file);
                var target = Path.Combine(dest, relPath);
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                File.Copy(file, target, true);
            }
        }
    }
}
