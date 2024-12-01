---
title: Installation
sidebar_position: 1
---

# Installation

## Unity

WaaS supports Unity 2022.3.12f1 and later.

### 1. Install dependencies

WaaS depends on [**System.Runtime.CompilerServices.Unsafe**](https://www.nuget.org/packages/system.runtime.compilerservices.unsafe/). If you don't have it in your project, you need to add it manually.

#### Option 1: Install with NuGet for Unity

Install [NuGet for Unity](https://github.com/GlitchEnzo/NuGetForUnity) and add `System.Runtime.CompilerServices.Unsafe` from the NuGet package manager.

#### Option 2: Manual installation

Click **Download package** on the [NuGet page](https://www.nuget.org/packages/system.runtime.compilerservices.unsafe/), and rename the downloaded file to `.zip`. Then, extract the contents and copy the `lib/netstandard2.0/System.Runtime.CompilerServices.Unsafe.dll` file to your Unity project.

### 2. Install WaaS

Add the following git URL from the Package Manager.

```
https://github.com/ruccho/WaaS.git?path=/WaaS.Unity/Packages/com.ruccho.waas
```

## .NET

Add the package from NuGet.

https://www.nuget.org/packages/WaaS/
