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
Invoke-MpqNameBreaking -Hash 0xB29FC135u -Type MpqHashNameA -Prefix 'LEVELS\L1DATA\' -Suffix '.DUN' -Verbose
```
