# Changelog

## [1.0.0] - 2026-09-23

First release as a Toolbelt package (was Unity-QuickSelector-Pro).

### Changed
- Menu moved to **Tools/Toolbelt/Quick Selector** (shortcut Ctrl+Shift+Alt+Q unchanged).
- Name filter: plain text matches any part of the name; `*` and `?` match the whole name. Previously a pattern only matched the start of the name.
- Component filter matches subclasses (filtering `Collider` finds `BoxCollider`).
- Filtering logic moved to `QuickSelectorFilter`, usable from your own editor scripts and covered by tests.

### Fixed
- Objects with missing scripts threw a `NullReferenceException` when filtering by component.
- Hierarchy paths were rebuilt on every comparison while sorting results.
