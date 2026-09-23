# Toolbelt

Small Unity tools I keep within reach. Each one is an independent UPM package: install only the ones you want, straight from this repository.

| Package | What it does | Namespace |
|---|---|---|
| [Quick Selector](Packages/com.yuankunhuang.quickselector) | Find and select GameObjects in the open scenes or prefab by name (wildcards), tag, layer, active state and component. | `Toolbelt.QuickSelector` |
| [Object Favorites](Packages/com.yuankunhuang.objectfavorites) | Pages of favorite assets and scene objects; drag in, search, click to ping. | `Toolbelt.ObjectFavorites` |

Requires Unity 2022.3 or newer.

## Install a package

Add it to `Packages/manifest.json` with its folder and a release tag:

```json
"com.yuankunhuang.quickselector": "https://github.com/YuankunHuang/Toolbelt.git?path=/Packages/com.yuankunhuang.quickselector#quickselector/1.0.0"
```

Or in Package Manager: **+ > Add package from git URL** with the same URL. Tags are `<package>/<version>`, one per package release.

## How this repository is organised

This is a Unity project used only to develop and test the packages.

```
Packages/
  com.yuankunhuang.<tool>/     one package per tool, nothing shared between them
    package.json  README.md  CHANGELOG.md
    Runtime/  Editor/          asmdef per folder, namespace Toolbelt.<Tool>
    Tests/Editor/              EditMode tests
Assets/                        development scene only
```

Rules every package follows:

- **Independent.** No package references another. Small helpers are duplicated rather than pulled into a shared "common" package.
- **UPM ready.** `package.json` named `com.yuankunhuang.<tool>`, `unity` set to the oldest supported version, semver, a changelog entry per release.
- **Tested.** EditMode tests for the logic; the editor window only wires UI to it.
- **Safe to install.** Nothing runs on load that changes project or editor settings, and nothing writes into `Assets/` (per-user data goes to `UserSettings/`).

A tool that grows its own users, docs and samples moves to a repository of its own, keeping its package name. Larger libraries live in their own repositories from the start.

## Development

Open the project with Unity 2022.3 (the oldest supported version). Run the tests from **Window > General > Test Runner**, or headless:

```
Unity.exe -batchmode -projectPath <this folder> -runTests -testPlatform EditMode -testResults results.xml
```

To release a package: bump `version` in its `package.json`, add a `CHANGELOG.md` entry, commit, then tag `<package>/<version>` (for example `quickselector/1.0.1`) and push the tag.
