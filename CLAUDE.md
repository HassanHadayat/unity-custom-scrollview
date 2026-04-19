# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A Unity UPM library (`com.custom.scrollview`) implementing an infinite/virtualized scroll view UI component. Distributed as a reusable package requiring Unity 2021.3+, targeting .NET 4.7.1.

## Build

This is a Unity project — there is no standalone CLI build command. Open `UI-ScrollView.sln` in Visual Studio or Rider to build the C# solution:

- **Solution:** `UI-ScrollView.sln`
- **Projects:** `CustomScrollView.Runtime.csproj` (the library), `Assembly-CSharp.csproj` (Unity auto-generated)
- **Output:** `Temp\Bin\Debug\CustomScrollView.Runtime\`
- **Tests:** Unity Test Framework package (`com.unity.test-framework 1.6.0`) is available but no test suites are currently configured.

## Architecture

The library implements MVC with three clear layers:

**Model** — `Runtime/Data/` and `Runtime/Interfaces/`
- `IScrollDataSource`: contract for sections, item counts, and sizes
- `SimpleDataSource`: manual population via `AddSection()` / `AddItems()`
- `DelegateDataSource`: callback-based data binding

**Controller** — `Runtime/Controller/ScrollViewController.cs`
- MonoBehaviour attached to a `ScrollRect` GameObject
- Orchestrates the full lifecycle: listens to `ScrollRect.onValueChanged`, queries layout for the visible range, and manages cell visibility
- Entry point: `controller.Initialize(dataSource, cellPrefabSelector, headerPrefabSelector)`

**View** — `Runtime/View/`, `Runtime/Core/`, `Runtime/Layout/`
- `DefaultCellProvider` + `CellPool`: instantiate/recycle cell prefabs (object pooling)
- `LinearLayoutStrategy` / `GridLayoutStrategy`: calculate element positions and detect the visible range via binary search
- `ElementMap`: O(1) flat-index ↔ section/item-index lookup
- `LayoutStrategyFactory`: selects strategy based on `GridConstraint` enum

## Key Extension Points

All three primary behaviors are interface-driven and injectable:

| Interface | Swap to... |
|---|---|
| `IScrollDataSource` | custom async/paginated data source |
| `ICellProvider` | custom prefab loading (e.g., Addressables) |
| `ILayoutStrategy` | custom layout algorithm |

Optional cell-side contracts: implement `IScrollCell` on a cell's MonoBehaviour to receive auto-fill callbacks; implement `IScrollSectionElement` on headers/footers.

## Fluent Helper

`Runtime/Extensions/ScrollViewExtensions.cs` provides a one-call setup for simple single-section uniform lists:

```csharp
scrollViewController.SetupSimpleList(items, cellPrefab, itemHeight: 80f);
```

## Demos

Eight fully working examples live in `Samples~/Demo/` covering vertical list, horizontal list, fixed-column grid, fixed-row grid, multi-section, and section-grid layouts. Use these as integration references when modifying layout or cell lifecycle logic.

## Package Metadata

- Package ID: `com.custom.scrollview`
- Version: `1.0.0`
- Full API reference and architecture diagram: `Packages/com.custom.scrollview/index.md`