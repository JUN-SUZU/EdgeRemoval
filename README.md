# EdgeRemovalService

Windows 11 Proで、指定された **Microsoft Edgeブラウザー本体の実行ファイル** を監視し、該当プロセスの終了および実行ファイルの削除を試みるC#製Windowsサービスです。WebView2 RuntimeやEdge Updateを対象に含めないことを設計方針とします。

> [!WARNING]
> Windowsコンポーネントへの介入を伴います。正常動作・削除成功・WebView2への無影響を保証するものではありません。導入前に必ず [Caution.md](Caution.md) を確認してください。

## 概要

- Windows起動時に自動起動するWorker Service（`BackgroundService`）
- 1秒ごとに `msedge` プロセスを列挙し、**実行ファイルのフルパスが一致したものだけ**を終了
- 指定した `msedge.exe` の削除を試行し、失敗時はログを記録して次周期で再試行
- 起動自体のブロックは、別途設定するAppLockerの**パスルール**に担当させる（サービスだけでは起動を完全に防げません）
- WebView2の `msedgewebview2.exe` およびEdgeUpdateを意図的に停止・削除しない

## 対象

| 項目 | パス・名称 | 処理 |
| --- | --- | --- |
| Edgeブラウザー | `C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe` | 終了・削除を試行 |
| WebView2 Runtime | `C:\Program Files (x86)\Microsoft\EdgeWebView\` | 対象外 |
| Edge Update | `C:\Program Files (x86)\Microsoft\EdgeUpdate\` | 対象外 |
| EdgeCore | `C:\Program Files (x86)\Microsoft\EdgeCore\` | 対象外 |

**対象パスは固定値です。** 別のインストール先・コピー・別名ファイルは対象になりません。パスの変更方法は [Howtosetup.md](Howtosetup.md) に記載しています。

## 動作環境

- Windows 11 Pro（64-bit想定）
- .NET SDK 9以上のうち、選択したターゲットフレームワークに対応するもの
- 管理者権限（Windowsサービス登録・AppLocker構成用）

このプロジェクトは当初 `.NET 9` を使用して作成しました。継続運用にはサポート期間の長い **.NET 10 LTS** を推奨します。ターゲットフレームワークは `.csproj` の `TargetFramework` で選択できます。SDK・NuGetパッケージ・ビルド設定の変更方法はセットアップガイドを参照してください。

## クイックスタート

1. [Caution.md](Caution.md) で影響と復旧手順を確認する。
2. [Howtosetup.md](Howtosetup.md) に従い、対応する.NET SDKとパッケージを準備する。
3. `Worker.cs` と `Program.cs` の実装をビルドしてサービス登録する。
4. AppLockerをまず**監査モード**で設定し、影響を確認した後に強制へ切り替える。
5. Edge本体とWebView2利用アプリをそれぞれテストする。

## リポジトリ内ドキュメント

- [Howtosetup.md](Howtosetup.md)：ビルド、.NETバージョン変更、サービス登録、AppLocker設定、テストと撤去
- [Caution.md](Caution.md)：管理者権限、Windows更新、削除のリスク、WebView2、復旧方法

## 制約

本サービスは**Windowsの正式なEdgeアンインストーラーではありません**。削除はWindowsの保護・アクセス権・ファイル使用状況により失敗することがあります。強制的な所有権の奪取やACL変更は行いません。AppLockerは指定パスでの起動を抑止しますが、異なるパスからの実行などを全面的に制御するものではありません。

## 参考資料

- [Windows ServiceとしてBackgroundServiceを実行する方法（Microsoft Learn）](https://learn.microsoft.com/en-us/dotnet/core/extensions/windows-service)
- [AppLocker概要（Microsoft Learn）](https://learn.microsoft.com/en-us/windows/security/application-security/application-control/app-control-for-business/applocker/applocker-overview)
- [.NETのサポートポリシー](https://dotnet.microsoft.com/ja-jp/platform/support/policy)

## ライセンス

ライセンスはリポジトリの管理者が決定してください。本READMEは特定のライセンスの適用を宣言しません。
