# MPQ Name Breaker
## Introduction

This tool is a .NET Standard 2.1 PowerShell module that can be used to brute force Blizzard MPQ archives' name hashes.  
This experimental work and has been successfully tested on:
- Windows 10 x64
- PowerShell 7.1 x64

The tool also supports GPU accelerated name breaking.  
It relies on the [ILGPU 0.9.2]() library.


## Quick start

1. If you don't have it yet, download and install [PowerShell 7](https://github.com/PowerShell/PowerShell/releases/latest).
2. Download and unzip the MpqNameBreaker PowerShell module.
3. Launch PowerShell 7 and run `Import-Module MpqNameBreaker.dll`
4. See usage section below to launch name breaking.


## Usage

```powershell
# Name breaking for "gendata\cuttt.pal"
Invoke-MpqNameBreaking -HashA 0xD50A0BCCu -HashB 0xB94F2DD2u `
    -Prefix 'gendata\' -Suffix '.pal' -Verbose
```

The `-HashA` and `-HashB` parameters are unsigned 32-bit integers, the `0x` prefix and `u` suffix are needed.

The case does not matter for `-Prefix`, `-Suffix` and parameters.  

```powershell
# Name breaking without suffix for "monsters\mega\balr.trn"
Invoke-MpqNameBreaking -HashA 0x26BBF734u -HashB 0x2C785839u `
    -Prefix 'MONSTERS\MEGA\' -AdditionalChars '.' -Verbose
```

The default charset used for the name breaking is `0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_-`  
It can be extended with the `-AdditionalChars` parameter.


## Build

```powershell
dotnet msbuild -property:Configuration=Release
```
