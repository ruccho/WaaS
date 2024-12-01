---
title: インストール
sidebar_position: 1
---

# インストール

## Unity

WaaS は Unity 2022.3.12f1 以降に対応しています。

### 1. 依存関係のインストール

WaaS は [**System.Runtime.CompilerServices.Unsafe**](https://www.nuget.org/packages/system.runtime.compilerservices.unsafe/) に依存しています。プロジェクトにインストールされていない場合は、手動で追加する必要があります。

#### オプション 1: NuGet for Unity でインストール

[NuGet for Unity](https://github.com/GlitchEnzo/NuGetForUnity) をインストールし、NuGet パッケージマネージャから `System.Runtime.CompilerServices.Unsafe` を追加してください。

#### オプション 2: 手動インストール

[NuGet ページ](https://www.nuget.org/packages/system.runtime.compilerservices.unsafe/) から **Download package** をクリックし、ダウンロードしたファイルを `.zip` にリネームします。ファイルを展開し、`lib/netstandard2.0/System.Runtime.CompilerServices.Unsafe.dll` を Unity プロジェクトにコピーしてください。

### 2. WaaS のインストール

Package Manager より以下の git URL を追加してください。

```
https://github.com/ruccho/WaaS.git?path=/WaaS.Unity/Packages/com.ruccho.waas
```

## .NET

NuGet よりパッケージを追加します。

https://www.nuget.org/packages/WaaS/

