# Manual EXE Packaging Readiness

Final EXE creation remains intentionally manual.

## Before packaging

1. Build Link-Core for the target Windows architecture.
2. Create `release-manifest.json`.
3. Stage the prepared release into `staging/link-core`.
4. Verify every staged hash and file size.
5. Build and test the installer source.
6. Run the packaging-readiness gate.

```powershell
.\scripts\new-release-manifest.ps1 -ReleaseFolder "<Link-Core Release>" -Version "1.0.0" -Architecture x64 -EntryPoint "the_link.exe"
.\scripts\stage-link-core.ps1 -Source "<Link-Core Release>"
.\scripts\manual-package-check.ps1
```

## Production gates

Do not release when CI is not green, payload hashes do not verify, production signing is missing where required, a production Supabase service-role secret is present, staging contains source/developer secrets, or upgrade/repair/uninstall acceptance testing has not been completed.

## Minimum Windows acceptance matrix

- Windows 10 x64 clean install
- Windows 11 x64 clean install
- ARM64 when a signed ARM64 Link-Core payload exists
- offline-safe setup path
- standard cloud + SQLite
- approved existing/local PostgreSQL branch-server mode
- upgrade from previous release
- same-version repair
- failed-deployment rollback
- uninstall preserving data
- explicit purge-data uninstall
- Start Menu/Desktop shortcut options
- startup registration on/off

The final packaged EXE is produced manually by MeetWell Technologies.
