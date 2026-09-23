# Quick Selector

Part of [Toolbelt](../../README.md). An editor window that finds GameObjects in the open scenes, or in the prefab you are editing, and selects them.

Menu: **Tools/Toolbelt/Quick Selector**, shortcut **Ctrl+Shift+Alt+Q**.

## Filters

| Filter | Behaviour |
|---|---|
| Name | Case-insensitive. Plain text matches any part of the name (`enemy` finds `BigEnemy_01`). With `*` / `?` the pattern must match the whole name (`Enemy_*`). |
| Tag / Layer | Exact. |
| Active State | Any, Active or Inactive (`activeInHierarchy`). Inactive objects are always searched. |
| Component | The object has a component of this type or a subclass. |

Results are sorted by hierarchy path. Selecting rows in the list selects the objects in the editor.

## From code

```csharp
using Toolbelt.QuickSelector;

var filter = new QuickSelectorFilter { Name = "Enemy_*", ComponentType = typeof(Collider) };
foreach (var result in filter.Find())
    Debug.Log(result.path);
```

## Install

```json
"com.yuankunhuang.quickselector": "https://github.com/YuankunHuang/Toolbelt.git?path=/Packages/com.yuankunhuang.quickselector#quickselector/1.0.1"
```

Requires Unity 2022.3 or newer.
