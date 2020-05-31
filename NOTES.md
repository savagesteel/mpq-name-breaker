# Notes

## Unknown file in DIABDAT.MPQ (pre-release demo)
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

Brute force up-to:

| Name Pattern              | Pattern Chars |
| ------------------------- | ------------- |
| `LEVELS\x.DUN`            | `2XE-PNIJ`    |
| `LEVELS\DATA\x.DUN`       | `0D5DT_Q_`    |
| `LEVELS\LDATA\x.DUN`      | `2OHOP_R_`    |
| `LEVELS\L0DATA\x.DUN`     | `3JPR72BC`    |
| `LEVELS\L1DATA\x.DUN`     | `07HWXBLC`    |
| `LEVELS\L1DATA\HERx.DUN`  | `3JS28UR`     |
| `LEVELS\L1DATA\xHERO.DUN` | `2RF0-PK`     |
| `LEVELS\L1DATA\HALx.DUN`  | `2C3WHUQ`     |
| `LEVELS\L1DATA\xHALL.DUN` | `0HVHIGY`     |
| `LEVELS2\L1DATA\x.DUN`    | `018VM3JJ`    |
| `LEVELS2\L1DATA\HERx.DUN` | `1-FDIB3`     |
|  |  |
|  |  |
|  |  |
|  |  |
