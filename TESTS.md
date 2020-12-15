# Tests

## Main tests

```powershell
# 5 chars
# game
Invoke-MpqNameBreaking -HashA 0x23D15381u -HashB 0x4E85FBB6u -Verbose

# 6 chars, suffix
# diablo.exe
Invoke-MpqNameBreaking -HashA 0x882D1BA3u -HashB 0xC2303F7Du -Suffix '.EXE' -Verbose

# 6 chars, prefix
# temps09
Invoke-MpqNameBreaking -HashA 0xD0F46DB3u -HashB 0x86F1A056u -Prefix 'T' -Verbose

# 7 chars, prefix, additional chars
# monsters\mega\balr.trn
Invoke-MpqNameBreaking -HashA 0x26BBF734u -HashB 0x2C785839u -Prefix 'MONSTERS\MEGA\' -AdditionalChars '.' -Verbose

# 5 chars, prefix, suffix
# levels2\l1data\hero1.dun
Invoke-MpqNameBreaking -HashA 0xFA1E3FAAu -HashB 0x45E2A9B7u -Prefix 'LEVELS2\L1DATA\' -Suffix '.DUN' -Verbose

# 7 chars, prefix, suffix
# gendata\create4.pal
Invoke-MpqNameBreaking -HashA 0xF9D2098Cu -HashB 0x89706FB2u -Prefix 'GENDATA\' -Suffix '.PAL' -Verbose



# 10 chars, additional chars
# (listfile)
Invoke-MpqNameBreaking -HashA 0xFD657910u -HashB 0x4E9B98A7u -AdditionalChars '()' -Verbose



Invoke-MpqNameBreaking -HashA u -HashB u -Prefix '' -Suffix '' -Verbose
Invoke-MpqNameBreaking -HashA u -HashB u -Prefix '' -Suffix '' -Verbose
Invoke-MpqNameBreaking -HashA u -HashB u -Prefix '' -Suffix '' -Verbose
Invoke-MpqNameBreaking -HashA u -HashB u -Prefix '' -Suffix '' -Verbose
Invoke-MpqNameBreaking -HashA u -HashB u -Prefix '' -Suffix '' -Verbose
Invoke-MpqNameBreaking -HashA u -HashB u -Prefix '' -Suffix '' -Verbose


```


## Additional tests

```powershell
# ctrlpan\talkpanl.cel
Invoke-MpqNameBreaking -HashA 0x097BB9AEu -HashB 0xE3B01F82u -Prefix 'CTRLPAN\' -Suffix '.CEL' -Verbose
```
