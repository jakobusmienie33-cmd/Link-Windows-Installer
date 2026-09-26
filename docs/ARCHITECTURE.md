# Link Windows Installer Architecture

## Purpose
`Link-Windows-Installer` owns Windows setup orchestration. It does not own Link-Core business logic. Final EXE packaging remains manual.

## Project boundaries

### Link.Windows.Installer.Core
Platform-neutral installer rules and models:
- system snapshot model
- compatibility evaluation
- blocker / warning / informational states

### Link.Windows.Installer
Windows WPF application:
- installer shell
- UI state
- Windows system probing
- setup logging
- workstation-profile selection

Later phases add prerequisites, database setup, deployment, services, upgrade/repair/uninstall and rollback.

## Deployment defaults
Normal workstation:
- Link Windows application
- authoritative cloud Supabase/PostgreSQL backend
- encrypted local SQLite cache/queue
- outbound HTTPS/WSS
- no general inbound listener

Advanced branch/server may use an approved local PostgreSQL service.

## Security boundaries
The installer must not embed/log production Supabase service_role secrets, perform production cloud DDL from ordinary workstation setup, log credentials/PINs/payment secrets, or grant app privileges from installer profile selection.

## UI contract
`the_link_windows_installer_comprehensive_mockup.html` is canonical.
Attribution: **By MeetWell Technologies**.
