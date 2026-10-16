# セットアップガイド（Howtosetup）

Windows 11 Proに `EdgeRemovalService` を導入する手順です。以下のコマンド例は**PowerShell**を前提とします。

> [!IMPORTANT]
> 実行前に [Caution.md](Caution.md) を確認してください。サービスのコード (`Worker.cs` / `Program.cs`) がリポジトリに存在することを前提としています。コマンドはGitHubへソースコードを自動生成・アップロードするものではありません。

## 1. .NET SDKを確認する

```powershell
dotnet --list-sdks
dotnet --version
```

現時点の開発例は `.NET SDK 9.0.319` です。これしか導入されていない状態で `dotnet new worker -f net10.0` を指定すると、テンプレートがターゲットを認識せず失敗します。`.NET 9` なら `-f net9.0` を指定してください。

**長期運用には .NET 10 LTSを推奨**します。.NET 10を使うには、対応する.NET 10 SDKをインストールしてください。ランタイムだけではビルドできません。

## 2. 新規プロジェクトを作る場合

既にリポジトリに `.csproj` がある場合は、この手順をスキップして「3. バージョンの変更」から進めてください。

.NET 9の例：

```powershell
cd C:\task\EdgeRemoval
dotnet new worker -n EdgeRemovalService -f net9.0
cd EdgeRemovalService
dotnet add package Microsoft.Extensions.Hosting.WindowsServices --version 9.0.10
```

.NET 10の例（.NET 10 SDKのインストールが必要）：

```powershell
cd C:\task\EdgeRemoval
dotnet new worker -n EdgeRemovalService -f net10.0
cd EdgeRemovalService
dotnet add package Microsoft.Extensions.Hosting.WindowsServices --version 10.0.0
```

いずれも `--version` は**指定バージョンの例**です。実際には同じメジャーバージョンの最新の互換パッチを採用してください。

## 3. 任意の.NETバージョンに変更する方法

主な変更箇所は以下です。

| 変更箇所 | .NET 9の例 | .NET 10の例 | 必須性 |
| --- | --- | --- | --- |
| `EdgeRemovalService.csproj` 内の `TargetFramework` | `net9.0` | `net10.0` | 必須 |
| `Microsoft.Extensions.Hosting.WindowsServices` のパッケージ版 | `9.0.x` | `10.0.x` | 更新推奨 |
| `Microsoft.Extensions.Hosting` のパッケージ版（参照がある場合） | `9.0.x` | `10.0.x` | 更新推奨 |
| インストールするSDK | .NET 9 SDK | .NET 10 SDK | 必須 |
| `dotnet new ... -f`（新規作成時のみ） | `net9.0` | `net10.0` | 新規作成時のみ |
| `global.json`（存在する場合） | SDK 9系の実際のバージョン | SDK 10系の実際のバージョン | 存在する場合 |

`EdgeRemovalService.csproj` の例：

```xml
<Project Sdk="Microsoft.NET.Sdk.Worker">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Hosting.WindowsServices" Version="9.0.10" />
  </ItemGroup>
</Project>
```

.NET 10に変更する場合は、`TargetFramework` を `net10.0` に変更し、`PackageReference` のバージョンを `10.0.x` の実在するリリースに更新します（上記の `x` はプレースホルダーであり、そのまま書き込まないでください）。

コマンドでパッケージを更新する例：

```powershell
dotnet add package Microsoft.Extensions.Hosting.WindowsServices --version 10.0.0
dotnet restore
dotnet build
```

**注意：** プロジェクト作成後は `dotnet new` をやり直す必要はありません。`-f` は新規作成時のテンプレート指定であり、既存プロジェクトは `.csproj` を変更します。SDKのメジャーバージョン、ターゲットフレームワーク、NuGetの対応状況を確認してください。

`global.json` がある場合、`sdk.version` は `9.0.319` のような**実在する完全なSDKバージョン**を指定します。`9.0` や `net9.0` とは書きません。

## 4. 対象パスを確認する

`Worker.cs` 内の `TargetPath` が実環境のEdgeブラウザー本体を指していることを確認してください。

```csharp
private const string TargetPath =
    @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe";
```

変更するときは**この定数**と、後述する**AppLockerの拒否ルールのパス**を両方合わせます。`msedgewebview2.exe` や `EdgeWebView` に変更してはいけません。

## 5. 発行（publish）

プロジェクトディレクトリ（`EdgeRemovalService.csproj` のある場所）で実行：

```powershell
dotnet restore
dotnet publish -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true `
    -o C:\task\EdgeRemoval\publish
```

出力：

```text
C:\task\EdgeRemoval\publish\EdgeRemovalService.exe
```

`--self-contained true` ならターゲット端末への別途ランタイム導入は基本的に不要ですが、**ビルドに使用するSDKは必要**です。.NETのバージョンを変更しても、通常は `-r win-x64` とサービス登録先パスを変更する必要はありません。

## 6. Windowsサービス登録

以降は**管理者PowerShell**を使用します。

```powershell
sc.exe create EdgeRemovalService `
    binPath= "C:\task\EdgeRemoval\publish\EdgeRemovalService.exe" `
    start= auto `
    obj= LocalSystem `
    DisplayName= "Edge Removal Service"

sc.exe failure EdgeRemovalService `
    reset= 86400 `
    actions= restart/5000/restart/5000/restart/5000

sc.exe start EdgeRemovalService
sc.exe query EdgeRemovalService
```

`STATE : 4 RUNNING` が表示されるか確認してください。サービス実行ファイルを一般ユーザーが書き換えられないように、配置先フォルダーのACLを確認してください。

**更新時：** `sc.exe stop EdgeRemovalService` → 新しいバイナリを配置 → `sc.exe start EdgeRemovalService` の順に実施します。通常、同じパスならサービスの再登録は不要です。

## 7. AppLockerを設定（別途必要）

監視サービスだけではEdge起動を事前にブロックできません。Windows 11 ProのローカルセキュリティポリシーでAppLockerを別途設定します。

1. `Win + R` → `secpol.msc` を開く。
2. **アプリケーション制御ポリシー → AppLocker → 実行可能ファイルの規則** を開く。
3. 既存ルールを確認する。必要に応じて**既定の許可規則**を作成する（拒否ルールだけで運用しない）。
4. **新しい規則の作成**で「拒否」「Everyone」「パス」を指定する。
5. 拒否対象は `C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe` とする。
6. AppLockerのプロパティで、まず**実行可能ファイルの規則を「監査のみ」**にする。
7. `gpupdate /force` を実行し、イベントビューアーの **アプリケーションとサービス ログ → Microsoft → Windows → AppLocker → EXE and DLL** でイベントを確認する。
8. Edgeのみが対象であることを検証してから「規則の強制」に変更する。

AppLockerの評価にはApplication Identityサービスが必要です。構成・起動状態を確認してください。起動の可否はイベントログと実地テストで検証し、OS保護を回避するような変更はしないでください。

## 8. 動作確認

```powershell
sc.exe query EdgeRemovalService
Get-Process msedge -ErrorAction SilentlyContinue | Select-Object Id, Path
Get-Process msedgewebview2 -ErrorAction SilentlyContinue | Select-Object Id, Path
Test-Path "C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
```

- 対象Edgeプロセスが終了すること。
- 削除失敗時はApplicationイベントログを確認すること。
- AppLockerの強制が有効なら、指定パスのEdgeが起動できないこと。
- WebView2を利用するアプリが通常どおり動作すること。
- 再起動後もサービスが `RUNNING` になること。

`Test-Path` の結果が `True` でも、削除を試みた結果としてアクセス拒否が起きている可能性があります。サービスは**削除を保証しません**。

## 9. 停止・アンインストール

```powershell
sc.exe stop EdgeRemovalService
sc.exe delete EdgeRemovalService
```

**さらに `secpol.msc` でAppLockerの該当拒否規則を削除し、`gpupdate /force` を実行してください。** サービス削除だけではEdgeの起動禁止は解除されません。必要に応じてEdgeを修復・再インストールします。

## 10. トラブルシューティング

| 状況 | 確認すること |
| --- | --- |
| `-f net10.0` が無効 | `dotnet --list-sdks` を確認。対応する.NET 10 SDKを導入するか `-f net9.0` にする |
| `EdgeRemovalService` フォルダーがない | `dotnet new` が成功していない。作成成功を確認してから `cd` する |
| パッケージが復元できない | ネットワーク、NuGetソース、パッケージ版、SDKを確認 |
| サービスが起動しない | `sc.exe query` とApplicationイベントログ、実行ファイルパスを確認 |
| Edgeが削除されない | アクセス権・ロック・サービスログを確認。保護回避は標準手順外 |
| Edgeが起動する | AppLocker強制状態、Application Identity、ルール対象パスを確認 |
| 他のアプリが起動しない | AppLockerの許可規則とイベントログを確認し、監査モードへ戻す |

## 公式ドキュメント

- [Microsoft Learn：Windows ServiceとBackgroundService](https://learn.microsoft.com/en-us/dotnet/core/extensions/windows-service)
- [Microsoft Learn：AppLocker](https://learn.microsoft.com/en-us/windows/security/application-security/application-control/app-control-for-business/applocker/applocker-overview)
- [.NETのサポートポリシー](https://dotnet.microsoft.com/ja-jp/platform/support/policy)
