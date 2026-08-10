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
        _windowList.DoubleClick += (_, _) => MoveSelectedWindow();

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

        AcceptButton = _moveButton;
        Shown += (_, _) => RefreshWindowList();
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

                if (selectedHandle != IntPtr.Zero && window.Handle == selectedHandle)
                {
                    item.Selected = true;
                    item.Focused = true;
                    item.EnsureVisible();
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
}
