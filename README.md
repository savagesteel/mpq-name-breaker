# MPQ Name Breaker
## Information

This tool is a .NET Standard 2.1 PowerShell module that can be used to brute force Blizzard MPQ archives' name hashes.  
This experimental work and has been successfully tested on:
- Windows 10 x64
- PowerShell 7.1 x64

The tool also supports GPU accelerated name breaking.  
It relies on the [ILGPU 0.9.2]() library


## Installation

1. Download and unzip the module.
2. Launch PowerShell 7.1 and run `Import-Module MpqNameBreaker.dll`.
3. See usage section below to launh name breaking.


## Usage

The default charset used for the namebreaking is `0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_-`  
It can be extended with the `-AdditionalChars` parameter.

```powershell
# Name breaking for "gendata\cuttt.pal"
Invoke-MpqNameBreaking -HashA 0xD50A0BCCu -HashB 0xB94F2DD2u -Prefix 'gendata\' -Suffix '.pal' -Verbose

# Name breaking for "levels2\l1data\hero1.dun"
Invoke-MpqNameBreaking -HashA 0xFA1E3FAAu -HashB 0x45E2A9B7u -Prefix 'LEVELS2\L1DATA\' -Suffix '.DUN' -Verbose
```

## Build

```powershell
dotnet msbuild -property:Configuration=Release
```
