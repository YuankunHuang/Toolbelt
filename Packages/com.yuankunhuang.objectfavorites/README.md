# Object Favorites

Part of [Toolbelt](../../README.md). An editor window with pages of favorite assets and scene objects.

Menu: **Tools/Toolbelt/Object Favorites**, shortcut **Ctrl+Shift+Alt+W**.

- **Pages**: `+` / `-` on the toolbar, double-click a page to rename it. Dropping objects on the page list creates a page with them.
- **Objects**: drop assets or scene objects on the right side to add them to the selected page; click an entry to ping it; `X` removes it.
- **Search**: case-insensitive on the name. Plain text matches any part; `*` and `?` match the whole name.

Favorites are saved per user and per project in `UserSettings/Toolbelt/ObjectFavorites.asset` (keep `UserSettings/` out of version control, as Unity's default `.gitignore` does). Objects are stored as `GlobalObjectId`s, so scene objects are found again when their scene is open.

## Install

```json
"com.yuankunhuang.objectfavorites": "https://github.com/YuankunHuang/Toolbelt.git?path=/Packages/com.yuankunhuang.objectfavorites#objectfavorites/1.0.1"
```

Requires Unity 2022.3 or newer.
