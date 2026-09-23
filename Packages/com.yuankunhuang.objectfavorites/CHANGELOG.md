# Changelog

## [1.0.0] - 2026-09-23

First release as a Toolbelt package (was the Level 2 tool of Unity_CoreToolkit).

### Changed
- Menu moved to **Tools/Toolbelt/Object Favorites** (shortcut Ctrl+Shift+Alt+W unchanged).
- Favorites are saved to `UserSettings/Toolbelt/ObjectFavorites.asset` instead of an asset under `Assets/`: they are per user, stay out of version control, and work when the tool is installed as a package.
- Search: plain text matches any part of the name; `*` and `?` match the whole name. Previously plain text had to match the whole name.

### Fixed
- While searching, editing or removing an entry changed the wrong object of the page.
