# mpq-name-breaker
Blizzard MPQ Name Breaking Tool

## Information

This tool a .NET Standard 2.0 PowerShell module that can be used to brute force MPQ name hashes.  
This experimental work and has been successfully tested on:
- .NET Core 3.1
- PowerShell 7

## Usage

```pwsh
Invoke-MpqNameBreaking -Hash 0xB29FC135u -Type MpqHashNameA -Prefix 'LEVELS\L1DATA\' -Suffix '.DUN' -Verbose
```
