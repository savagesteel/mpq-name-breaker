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

# 8 chars, prefix, suffix
# gendata\create4.pal
Invoke-MpqNameBreaking -HashA 0xF9D2098Cu -HashB 0x89706FB2u -Prefix 'GENDATA\' -Suffix '.PAL' -Verbose

# first batch, firt name line, last name (before second name: 10000)
# 0---
Invoke-MpqNameBreaking -HashA 0xF860087Bu -HashB 0x2164B6E2u -BatchSize 20480 -BatchCharCount 4 -Verbose

# first batch, last name line, last name ()
# D5Z----
Invoke-MpqNameBreaking -HashA 0xEA899001u -HashB 0x7CDC1219u -BatchSize 20480 -BatchCharCount 4 -Verbose

# 7 chars, full range benchmark
# -------
Invoke-MpqNameBreaking -HashA 0x87095B2Du -HashB 0xD6507679u -Verbose

```

## Benchmark tests

```powershell
# 8 chars, full range benchmark
# --------
Invoke-MpqNameBreaking -HashA 0xD9F109CEu -HashB 0x4E950A6Au -Verbose

# 8 chars, full range with 4-chars suffix benchmark
# --------.---
Invoke-MpqNameBreaking -HashA 0x70369F9Fu -HashB 0xD6614847u -Suffix '.---' -Verbose


```

## Additional tests

```powershell
# ctrlpan\talkpanl.cel
Invoke-MpqNameBreaking -HashA 0x097BB9AEu -HashB 0xE3B01F82u -Prefix 'CTRLPAN\' -Suffix '.CEL' -Verbose

# 10 chars, additional chars
# (listfile)
Invoke-MpqNameBreaking -HashA 0xFD657910u -HashB 0x4E9B98A7u -AdditionalChars '()' -Verbose
```
