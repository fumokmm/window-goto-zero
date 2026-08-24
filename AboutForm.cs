using System.Diagnostics;

namespace WindowGotoZero;

internal sealed class AboutForm : Form
{
    private const string ApplicationTitle = "Window Goto Zero";
    private const string ApplicationVersion = "v1.1.0";
    private const string DistributionUrl = "https://thinktwice.tech/apps/window-goto-zero/";

    public AboutForm(Icon? applicationIcon)
    {
        Text = "バージョン情報";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        ClientSize = new Size(520, 220);
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;

        if (applicationIcon is not null)
        {
            Icon = (Icon)applicationIcon.Clone();
        }

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(24, 20, 24, 16),
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var titleLabel = new Label
        {
            AutoSize = true,
            Text = ApplicationTitle,
            Font = new Font(Font, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 8),
        };

        var versionLabel = new Label
        {
            AutoSize = true,
            Text = $"バージョン: {ApplicationVersion}",
            Margin = new Padding(0, 0, 0, 12),
        };

        var distributionLabel = new Label
        {
            AutoSize = true,
            Text = "配布先:",
            Margin = new Padding(0, 0, 0, 2),
        };

        var distributionLink = new LinkLabel
        {
            AutoSize = true,
            Text = DistributionUrl,
            Margin = new Padding(0, 0, 0, 12),
        };
        distributionLink.Links.Add(0, DistributionUrl.Length, DistributionUrl);
        distributionLink.LinkClicked += (_, _) => OpenDistributionPage();

        var buttonPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Margin = new Padding(0),
        };
        var closeButton = new Button
        {
            AutoSize = true,
            Text = "閉じる",
            DialogResult = DialogResult.OK,
        };
        buttonPanel.Controls.Add(closeButton);

        layout.Controls.Add(titleLabel, 0, 0);
        layout.Controls.Add(versionLabel, 0, 1);
        layout.Controls.Add(distributionLabel, 0, 2);
        layout.Controls.Add(distributionLink, 0, 3);
        layout.Controls.Add(buttonPanel, 0, 4);

        Controls.Add(layout);
        AcceptButton = closeButton;
        CancelButton = closeButton;
    }

    private static void OpenDistributionPage()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = DistributionUrl,
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"配布先URLを開けませんでした。\n{ex.Message}",
                "エラー",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
