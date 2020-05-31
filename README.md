# MPQ Name Breaker
## Information

This tool a .NET Standard 2.0 PowerShell module that can be used to brute force Blizzard MPQ archives' name hashes.  
This experimental work and has been successfully tested on:
- .NET Core 3.1
- PowerShell 7


## Build

```pwsh
dotnet msbuild -property:Configuration=Release
```


## Usage

```pwsh
# Name breaking for "gendata\cuttt.pal"
Invoke-MpqNameBreaking -HashA 0xD50A0BCCu -HashB 0xB94F2DD2u -Prefix 'gendata\' -Suffix '.pal' -Verbose
```
