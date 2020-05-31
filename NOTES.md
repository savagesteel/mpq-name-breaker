# Notes

## Unknown file in DIABDAT.MPQ pre-release demo
### Hashes

Hash A: `0xB29FC135`  
Hash B: `0x22575C4A`

### Command line

```pwsh
Invoke-MpqNameBreaking -HashA 0xB29FC135u -HashB 0x22575C4Au -Prefix 'LEVELS\L1DATA\' -Suffix '.DUN' -Verbose
```

### Already checked

Charset:
```
0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_-
```

- `LEVELS\L1DATA\xxxxxxx.DUN`



