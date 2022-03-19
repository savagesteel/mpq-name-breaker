# MPQ Name Breaker

![GitHub](https://img.shields.io/github/license/savagesteel/mpq-name-breaker?style=flat-square)
![Azure DevOps builds (branch)](https://img.shields.io/azure-devops/build/savagesteel/de630615-07d3-4ee6-a39a-a0b580dedaca/4/dev?label=build%3Adev&logo=azuredevops&style=flat-square)
[![CodeFactor Grade](https://img.shields.io/codefactor/grade/github/savagesteel/mpq-name-breaker/dev?label=code%20quality%3Adev&logo=codefactor&style=flat-square)](https://www.codefactor.io/repository/github/savagesteel/mpq-name-breaker/overview/dev)


## Introduction

This tool is a PowerShell module that can be used to brute force Blizzard MPQ archives' name hashes.  
This has been successfully tested on:
- Windows 10 x64
- PowerShell 7.2 x64

The tool also supports GPU accelerated name breaking.  
It relies on the [ILGPU](http://www.ilgpu.net) library.


## Quick start

1. If you don't have it yet, download and install [PowerShell 7](https://github.com/PowerShell/PowerShell/releases/latest).
2. Download and unzip the [MpqNameBreaker](https://github.com/savagesteel/mpq-name-breaker/releases) PowerShell module.
3. Launch PowerShell 7, `cd` to the folder where you unzipped the module and run `Import-Module .\MpqNameBreaker`
4. See usage section below to launch name breaking.


## Usage

The `-HashA` and `-HashB` parameters are unsigned 32-bit integers, the `0x` prefix and `u` suffix are needed.  
Parameters are *not* case sensitive.  

```powershell
# Name breaking for "gendata\cuttt.pal"
Invoke-MpqNameBreaking -HashA 0xD50A0BCCu -HashB 0xB94F2DD2u -Prefix 'gendata\' -Suffix '.pal' -Verbose
```

The default charset used for the name breaking is `0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_-`.  
It can be extended with the `-AdditionalChars` parameter, or overridden with the `-Charset` parameter.

```powershell
# Name breaking without suffix but with additional "." character for "monsters\mega\balr.trn"
Invoke-MpqNameBreaking -HashA 0x26BBF734u -HashB 0x2C785839u `
  -Prefix 'MONSTERS\MEGA\' -AdditionalChars '.' -Verbose

# Name breaking with prefix, suffix and a custom charset containing 
# only letters + "\" for "plrgfx\rogue\rls\rlsas.cl2"
Invoke-MpqNameBreaking -HashA 0xCB636CF4u -HashB 0x7B3E6451u `
  -Prefix 'plrgfx\rogue\r' -Charset 'ABCDEFGHIJKLMNOPQRSTUVWXYZ\' -Suffix '.cl2'  -Verbose
```

## Build

```powershell
dotnet msbuild -property:Configuration=Release
```
