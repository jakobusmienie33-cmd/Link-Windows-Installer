# Windows Installation Layout

## Per-machine defaults

| Purpose | Default |
|---|---|
| Application | `C:\Program Files\The Link` |
| Mutable data | `C:\ProgramData\The Link` |
| SQLite/cache | `C:\ProgramData\The Link\Data` |
| Setup logs | `C:\ProgramData\The Link\Logs\Setup` |
| Migration backups | `C:\ProgramData\The Link\Backups` |
| Start Menu | `The Link` |

Current-user installation switches the application/data roots to the current user's LocalAppData area.

## Validation rules

- All directories must be absolute.
- Standard profiles reject UNC/network install/data paths.
- Application and mutable data folders may not be identical.
- The application drive must have at least the current baseline install-space requirement.
- Existing directories are detected and preserved for upgrade/repair compatibility.
- Per-machine planning can occur without elevation; elevation is requested only when privileged changes are actually applied.
- Data/cache/log/backup directories are created by the execution phase, not simply by opening the installer screen.
