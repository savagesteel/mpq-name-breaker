# Notes

## Unknown file in DIABDAT.MPQ (pre-release demo)
### Hashes

Hash A: `0xB29FC135`  
Hash B: `0x22575C4A`

### Name

The name is alphabetically between:
- `items\wshield.cel`
- `levels\l1data\hero1.dun`

The file is a dungeon map so the extension is likely `.dun`

### Command line

```powershell
Invoke-MpqNameBreaking -HashA 0xB29FC135u -HashB 0x22575C4Au -Prefix 'levels\l1data\' -Suffix '.dun' -AdditionalChars " " -Verbose
```

### Already checked

Charset:
```
0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_-()\
```

Brute force up-to:

| Name Pattern              | Pattern Chars |
| ------------------------- | ------------- |
| `x`                       | `28H-J000`    |
| `[I-J]x.DUN`              | `00000000`    |
| `LEVELS\x.DUN`            | `BKT8SO000`   |
| `LEVELS\Lx.DUN`           | `L5GKY7000`   |
| `LEVELS\DATA\x.DUN`       | `0D5DT_Q_`    |
| `LEVELS\LDATA\x.DUN`      | `IIA6QZLQ`    |
| `LEVELS\L0DATA\x.DUN`     | `3JPR72BC`    |
| `LEVELS\L1DATA\x`         | `1K0Y0000`    |
| `LEVELS\L1DATA\x.DUN`     | `LP-KS000`    |
| `LEVELS\L1DATA\HERx.DUN`  | `3JS28UR`     |
| `LEVELS\L1DATA\xHERO.DUN` | `2RF0-PK`     |
| `LEVELS\L1DATA\HALx.DUN`  | `2C3WHUQ`     |
| `LEVELS\L1DATA\xHALL.DUN` | `0HVHIGY`     |
| `LEVELS2\L1DATA\x.DUN`    | `RRXHC_FQ`    |
| `LEVELS2\L1DATA\HERx.DUN` | `1-FDIB3`     |
|  |  |
|  |  |
|  |  |
|  |  |


## Unknown files in Game.mpq (HFS volume on Diable CD-ROM)
### Hashes

| # | Hash A       | Hash B       | File                          |
| - | ------------ | ------------ | ----------------------------- |
| 0 | `0xF613E692` | `0xA39AA926` | up to `DIABLO\DIABLO8WN54000` |
| 1 | `0xB52E34D9` | `0x50F05662` |  |
| 2 | `0x2BB19F2B` | `0x9D34D933` |  |
| 3 | `0x795B8056` | `0x89E1AAD7` |  |
| 4 | `0x0834756A` | `0x2538A0E4` |  |
| 5 | `0xB4D9C305` | `0x034B45F8` |  |
| 6 | `0x446927F1` | `0x2495ED45` |  |
| 7 | `0x8EF894EC` | `0xA76EF1CF` | up to `9CR--000.SNP` |

### Commands

```powershell
# File 0
Invoke-MpqNameBreaking -HashA 0xF613E692u -HashB 0xA39AA926u -Verbose

# File 7
Invoke-MpqNameBreaking -HashA 0x8EF894ECu -HashB 0xA76EF1CFu -Suffix '.snp' -Verbose
```

