using System.Collections;
using WindowGotoZero.Services;

namespace WindowGotoZero;

internal sealed class MainForm : Form
{
    private readonly WindowService _windowService = new();
    private readonly ListView _windowList;
    private readonly Button _refreshButton;
    private readonly Button _moveButton;
    private readonly Label _statusLabel;
    private readonly Label _hintLabel;
    private int _sortColumn;
    private SortOrder _sortOrder = SortOrder.Ascending;

    public MainForm()
    {
        Text = "Window Goto Zero";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(640, 420);
        Size = new Size(780, 520);
        Font = new Font("Segoe UI", 9F);
        try
        {
            // Prefer embedded exe icon; fall back to loose asset while debugging.
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath)
                   ?? LoadLooseIcon();
        }
        catch
        {
            Icon = LoadLooseIcon();
        }

        var menuStrip = new MenuStrip
        {
            Dock = DockStyle.Top,
            Font = Font,
        };
        var helpMenu = new ToolStripMenuItem("ヘルプ");
        helpMenu.DropDownItems.Add(new ToolStripMenuItem(
            "バージョン情報",
            image: null,
            onClick: (_, _) => ShowAboutDialog()));
        menuStrip.Items.Add(helpMenu);
        MainMenuStrip = menuStrip;

        _hintLabel = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 36,
            Padding = new Padding(12, 10, 12, 0),
            Text = "画面外に消えたウィンドウを選び、「(0,0) へ移動」でプライマリディスプレイ左上へ強制移動します。",
        };

        _windowList = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            MultiSelect = false,
            HideSelection = false,
            GridLines = true,
        };
        _windowList.Columns.Add("タイトル", 360);
        _windowList.Columns.Add("プロセス", 140);
        _windowList.Columns.Add("位置 / サイズ", 200);
        _windowList.ColumnClick += (_, e) => SortWindowList(e.Column);
        _windowList.DoubleClick += (_, _) => MoveSelectedWindow();
        _windowList.ListViewItemSorter = new WindowListItemComparer(_sortColumn, _sortOrder);
        UpdateSortHeaders();

        _refreshButton = new Button
        {
            Text = "更新",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
        };
        _refreshButton.Click += (_, _) => RefreshWindowList();

        _moveButton = new Button
        {
            Text = "(0,0) へ移動",
            AutoSize = true,
            Anchor = AnchorStyles.Right,
        };
        _moveButton.Click += (_, _) => MoveSelectedWindow();

        _statusLabel = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "準備完了",
        };

        var buttonPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 52,
            ColumnCount = 3,
            RowCount = 1,
            Padding = new Padding(12, 8, 12, 8),
        };
        buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        buttonPanel.Controls.Add(_refreshButton, 0, 0);
        buttonPanel.Controls.Add(_statusLabel, 1, 0);
        buttonPanel.Controls.Add(_moveButton, 2, 0);

        var listHost = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12, 4, 12, 0),
        };
        listHost.Controls.Add(_windowList);

        Controls.Add(listHost);
        Controls.Add(buttonPanel);
        Controls.Add(_hintLabel);
        Controls.Add(menuStrip);

        AcceptButton = _moveButton;
        Shown += (_, _) => RefreshWindowList();
    }

    private void ShowAboutDialog()
    {
        using var aboutForm = new AboutForm(Icon);
        aboutForm.ShowDialog(this);
    }

    private void RefreshWindowList()
    {
        try
        {
            var selectedHandle = GetSelectedHandle();
            var windows = _windowService.EnumerateTopLevelWindows();

            _windowList.BeginUpdate();
            _windowList.Items.Clear();

            foreach (var window in windows)
            {
                var item = new ListViewItem(window.Title)
                {
                    Tag = window,
                };
                item.SubItems.Add(window.ProcessName);
                item.SubItems.Add(window.PositionText);
                _windowList.Items.Add(item);
            }

            _windowList.Sort();

            if (selectedHandle != IntPtr.Zero)
            {
                foreach (ListViewItem item in _windowList.Items)
                {
                    if (item.Tag is WindowInfo window && window.Handle == selectedHandle)
                    {
                        item.Selected = true;
                        item.Focused = true;
                        item.EnsureVisible();
                        break;
                    }
                }
            }

            _windowList.EndUpdate();
            _statusLabel.Text = $"{windows.Count} 件のウィンドウ";
        }
        catch (Exception ex)
        {
            _statusLabel.Text = "一覧の取得に失敗しました。";
            MessageBox.Show(this, ex.Message, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SortWindowList(int column)
    {
        if (_sortColumn == column)
        {
            _sortOrder = _sortOrder == SortOrder.Ascending
                ? SortOrder.Descending
                : SortOrder.Ascending;
        }
        else
        {
            _sortColumn = column;
            _sortOrder = SortOrder.Ascending;
        }

        _windowList.ListViewItemSorter = new WindowListItemComparer(_sortColumn, _sortOrder);
        _windowList.Sort();
        UpdateSortHeaders();
    }

    private void UpdateSortHeaders()
    {
        var headers = new[] { "タイトル", "プロセス", "位置 / サイズ" };
        var indicator = _sortOrder == SortOrder.Ascending ? " ▲" : " ▼";

        for (var i = 0; i < headers.Length; i++)
        {
            _windowList.Columns[i].Text = i == _sortColumn
                ? headers[i] + indicator
                : headers[i];
        }
    }

    private void MoveSelectedWindow()
    {
        if (_windowList.SelectedItems.Count == 0)
        {
            MessageBox.Show(this, "移動するウィンドウを選択してください。", "確認",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (_windowList.SelectedItems[0].Tag is not WindowInfo window)
        {
            return;
        }

        try
        {
            _windowService.MoveToPrimaryOrigin(window.Handle);
            _statusLabel.Text = $"移動しました: {window.Title}";
            RefreshWindowList();
        }
        catch (Exception ex)
        {
            _statusLabel.Text = "移動に失敗しました。";
            MessageBox.Show(this, ex.Message, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private IntPtr GetSelectedHandle()
    {
        if (_windowList.SelectedItems.Count == 0)
        {
            return IntPtr.Zero;
        }

        return _windowList.SelectedItems[0].Tag is WindowInfo window
            ? window.Handle
            : IntPtr.Zero;
    }

    private static Icon? LoadLooseIcon()
    {
        var candidate = Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
        if (!File.Exists(candidate))
        {
            candidate = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Assets", "app.ico"));
        }

        return File.Exists(candidate) ? new Icon(candidate) : null;
    }

    private sealed class WindowListItemComparer : IComparer
    {
        private readonly int _column;
        private readonly SortOrder _sortOrder;

        public WindowListItemComparer(int column, SortOrder sortOrder)
        {
            _column = column;
            _sortOrder = sortOrder;
        }

        public int Compare(object? x, object? y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (x is not ListViewItem leftItem || y is not ListViewItem rightItem
                || leftItem.Tag is not WindowInfo left
                || rightItem.Tag is not WindowInfo right)
            {
                return 0;
            }

            var result = _column switch
            {
                0 => CompareText(left.Title, right.Title),
                1 => CompareText(left.ProcessName, right.ProcessName),
                2 => ComparePosition(left, right),
                _ => 0,
            };

            if (result == 0 && _column != 0)
            {
                result = CompareText(left.Title, right.Title);
            }

            if (result == 0)
            {
                result = left.Handle.ToInt64().CompareTo(right.Handle.ToInt64());
            }

            return _sortOrder == SortOrder.Descending ? -result : result;
        }

        private static int CompareText(string left, string right)
        {
            var result = StringComparer.CurrentCultureIgnoreCase.Compare(left, right);
            return result != 0
                ? result
                : StringComparer.Ordinal.Compare(left, right);
        }

        private static int ComparePosition(WindowInfo left, WindowInfo right)
        {
            var result = left.Left.CompareTo(right.Left);
            if (result != 0) return result;

            result = left.Top.CompareTo(right.Top);
            if (result != 0) return result;

            result = left.Width.CompareTo(right.Width);
            return result != 0 ? result : left.Height.CompareTo(right.Height);
        }
    }
}
