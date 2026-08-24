# Window Goto Zero

<p align="center">
  <img src="Assets/app-preview.png" alt="Window Goto Zero icon" width="128" height="128" />
</p>

拡張ディスプレイを外したあとなど、**画面外に消えたウィンドウ**を一覧から選んで、プライマリディスプレイの左上 **(0, 0)** へ強制移動する Windows 用ユーティリティです。

## ダウンロード（利用者向け）

最新の配布物は [GitHub Releases](https://github.com/fumokmm/window-goto-zero/releases/tag/v1.1.0) から入手できます。

- [インストーラー（`WindowGotoZero-Setup-1.1.0.exe`）](https://github.com/fumokmm/window-goto-zero/releases/download/v1.1.0/WindowGotoZero-Setup-1.1.0.exe)（推奨）
- [ZIP（`WindowGotoZero-1.1.0.zip`）](https://github.com/fumokmm/window-goto-zero/releases/download/v1.1.0/WindowGotoZero-1.1.0.zip)
- [EXE（`WindowGotoZero-1.1.0.exe`）](https://github.com/fumokmm/window-goto-zero/releases/download/v1.1.0/WindowGotoZero-1.1.0.exe)

インストーラーを使う場合:

1. `WindowGotoZero-Setup-1.1.0.exe` を実行する
2. インストール後、スタートメニューから **Window Goto Zero** を起動する

ZIP / EXEを使う場合は、ZIPを展開するかEXEを直接起動してください。いずれも .NET 8 Desktop Runtime (x64) が必要です。

| 項目 | 内容 |
|------|------|
| 対応 OS | Windows 10 / 11（x64） |
| インストール先 | `%LocalAppData%\Programs\WindowGotoZero`（管理者権限は通常不要） |
| 必要ランタイム | [.NET 8 Desktop Runtime (x64)](https://aka.ms/dotnet/8.0/windowsdesktop-runtime-win-x64.exe) |

セットアップ開始時にランタイムが無い場合は、公式インストーラーを開く案内が出ます。

## 使い方

1. アプリを起動する  
2. 移動したいウィンドウを一覧から選ぶ（必要なら「更新」）  
3. **(0,0) へ移動** を押す（一覧のダブルクリックでも可）  

## できること

- トップレベルウィンドウの一覧（タイトル / プロセス / 位置）
- 列見出しをクリックした一覧のソート（タイトル / プロセス / 位置 / サイズ）
- 選択ウィンドウを座標 `(0, 0)` へ移動
- 最小化・最大化はいったん復元してから移動
- 「ヘルプ」→「バージョン情報」からアプリ情報と配布先を確認

## 制限

- 一部のシステム／権限の高いウィンドウは移動できないことがあります
- このツール自身のウィンドウは一覧から除外します

## 開発者向け

### ビルド

ホストに .NET SDK を入れなくても構いません。初回 `build.ps1` がリポジトリ内 `.tools\dotnet` にポータブル SDK を取得します（PATH は恒久変更しません）。

```powershell
# アプリ（framework-dependent / 小さい）
.\scripts\build.ps1

# ランタイム同梱（大きい・約 150MB）
.\scripts\build.ps1 -SelfContained

# インストーラー（Inno Setup を .tools に取得して生成）
.\scripts\build-installer.ps1
```

| 成果物 | パス |
|--------|------|
| 実行ファイル | `dist\WindowGotoZero.exe` |
| セットアップ | `installer\output\WindowGotoZero-Setup-1.1.0.exe` |

### 構成

```
WindowGotoZero.csproj      # WinForms / net8.0-windows
MainForm.cs
Native/                    # Win32 P/Invoke
Services/                  # 列挙・移動
Assets/                    # アイコン
installer/WindowGotoZero.iss
scripts/                   # build / icon / portable tools
```

### アイコン再生成

```powershell
.\scripts\generate-icon.ps1
```

## ライセンス

[MIT](LICENSE)
