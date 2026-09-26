# Windows Installer UI Reference

Canonical reference: `the_link_windows_installer_comprehensive_mockup.html`.

## Required visual language
- dark charcoal Link system header
- "The Link" wordmark with bright green Link accent
- "Windows Setup" module title
- pale grey-blue background
- white rounded wizard surface
- dark left setup rail
- green current/completed states
- white cards with thin grey borders
- blue information, amber warning and red blocking states
- bottom Back / Next / Cancel controls

## Canonical 12-step flow
1. Welcome
2. System Check
3. Install Type
4. Components
5. Database / SQL
6. Install Location
7. Device & Services
8. Network & Security
9. Updates & Privacy
10. Review
11. Installing
12. Complete

P0-P2 implement the shell plus the first three experiences.

## Profiles
- Standard Workstation
- POS / Sharing Point
- Back-Office / Admin
- Branch Server / Advanced

A profile changes setup defaults only. It never grants application privileges.

## Database rule
Normal workstation = Cloud Supabase/PostgreSQL + local SQLite. Existing/local PostgreSQL = advanced branch/server deployment.

## Attribution
Use **By MeetWell Technologies**.
