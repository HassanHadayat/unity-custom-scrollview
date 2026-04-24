# CustomScrollView — Documentation

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    User Code (MonoBehaviour)                 │
│   Implements IScrollDataSource (+ ISectionLayoutProvider)   │
│   Provides cell/header/footer prefabs                       │
└───────────────┬─────────────────────────────────────────────┘
                │ Initialize(dataSource, cellProvider)
                ▼
┌─────────────────────────────────────────────────────────────┐
│              ScrollViewController (Controller)               │
│  • Listens to ScrollRect.onValueChanged                     │
│  • Queries ILayoutStrategy for visible range                │
│  • Calls ICellProvider to show/recycle cells                │
│  • Positions RectTransforms from ElementRect                │
└──────┬──────────────┬──────────────────┬────────────────────┘
       │              │                  │
       ▼              ▼                  ▼
┌────────────┐ ┌──────────────────┐ ┌────────────────┐
│ ElementMap │ │ CompositeLayout  │ │ ICellProvider  │
│  (Core)    │ │   Strategy       │ │   (View)       │
│            │ │                  │ │                │
│ flat index │ │ Linear or Grid   │ │ DefaultCell-   │
│ ↔ section/ │ │ per section,     │ │ Provider +     │
│   index    │ │ binary search    │ │ CellPool       │
│            │ │ visible range    │ │                │
└────────────┘ └──────────────────┘ └────────────────┘
```

## Design Principles

| Principle | Application |
|-----------|-------------|
| **S** — Single Responsibility | Controller manages lifecycle; Layout calculates positions; CellPool handles recycling; ElementMap handles indexing |
| **O** — Open/Closed | New layout modes via `ILayoutStrategy`; new cell creation via `ICellProvider`; new data shapes via `IScrollDataSource` |
| **L** — Liskov Substitution | Any `ILayoutStrategy` works in the controller without changes |
| **I** — Interface Segregation | `IScrollDataSource`, `ISectionLayoutProvider`, `ICellProvider`, `IScrollCell`, `IScrollSectionElement` are small and focused |
| **D** — Dependency Inversion | Controller depends on interfaces, not concrete layout/pool classes |

## MVC Mapping

- **Model**: `IScrollDataSource` (+ `SimpleDataSource`, `DelegateDataSource`)
- **View**: `ICellProvider`, `IScrollCell`, `IScrollSectionElement` (cell GameObjects)
- **Controller**: `ScrollViewController`

---

## Public API Reference

### `ScrollViewController`

The main MonoBehaviour. Attach it to a GameObject that also has a `ScrollRect`.

#### Initialization

```csharp
// Full control — implement ICellProvider yourself
void Initialize(IScrollDataSource dataSource, ICellProvider cellProvider)
```

Use this when you need custom cell instantiation (e.g. Addressables, custom pooling).

```csharp
// Convenience — pass delegate selectors, a DefaultCellProvider is built internally
void Initialize(
    IScrollDataSource dataSource,
    Func<int, int, GameObject> cellPrefabSelector,
    Func<int, GameObject>      headerPrefabSelector = null,
    Func<int, GameObject>      footerPrefabSelector = null)
```

Use this for the common case where prefabs are known up-front. Both overloads call `ReloadData()` automatically.

#### Data Refresh

```csharp
void ReloadData()
```

Rebuilds the layout and refreshes all visible cells from the current data source. Call this after mutating your data (adding/removing items, changing sizes). Safe to call multiple times.

**Use case — dynamic data:**
```csharp
_myData.Add(newItem);
_scrollView.ReloadData();
```

#### Navigation

```csharp
void ScrollTo(int section, int index, float duration = 0.3f)
```

Animates the scroll position so the item at `(section, index)` appears at the top (vertical) or left (horizontal) edge of the viewport. The target position is clamped to the valid scroll range, so items near the end of the list land correctly without elastic jitter. Silently ignored if the section/index does not exist.

```csharp
void ScrollToTop(float duration = 0.3f)
void ScrollToBottom(float duration = 0.3f)
```

Jump to the start or end of the content. Pass `duration: 0f` for an instant snap.

**Use cases:**
```csharp
// Jump to a section header — target item 0, it sits just below the header
_scrollView.ScrollTo(section: 2, index: 0);

// Instant jump to top (e.g. on tab switch)
_scrollView.ScrollToTop(duration: 0f);

// Smooth scroll to bottom on new message
_scrollView.ScrollToBottom(duration: 0.4f);
```

#### Events

```csharp
event Action<int, int, GameObject> OnCellVisible   // (section, index, cellGO)
event Action<int, int>             OnCellRecycled   // (section, index)
```

`OnCellVisible` fires each time a cell enters the viewport. Use it to fill cell content when you are not using `IScrollCell`.

`OnCellRecycled` fires when a cell leaves the viewport and is returned to the pool. Use it to cancel async load operations tied to that cell.

**Use case — filling cells via event:**
```csharp
_scrollView.OnCellVisible += (section, index, cell) =>
{
    cell.GetComponentInChildren<TMP_Text>().text = _data[section][index].Title;
};
```

#### Properties

```csharp
ScrollDirection Direction  // Scroll axis set in the Inspector (Vertical / Horizontal)
GridConstraint  Constraint // Global grid constraint set in the Inspector
```

---

### `IScrollDataSource`

Implement this to feed data to the scroll view without coupling to a concrete model.

```csharp
int   GetSectionCount()
int   GetItemCount(int section)
float GetItemSize(int section, int index)   // size along scroll axis
float GetHeaderSize(int section)             // return 0 for no header
float GetFooterSize(int section)             // return 0 for no footer
```

---

### `ISectionLayoutProvider`

**Optional.** Implement this alongside `IScrollDataSource` to give each section its own layout mode, overriding the global `ScrollViewController` inspector settings.

```csharp
bool TryGetSectionLayout(int section, out SectionLayoutDescriptor descriptor)
```

Return `true` and populate `descriptor` to override the section's layout. Return `false` to use the global config for that section.

**`SectionLayoutDescriptor` fields:**

| Field | Type | Meaning |
|---|---|---|
| `OverrideConstraint` | `bool` | Must be `true` for `Constraint` to take effect |
| `Constraint` | `GridConstraint` | `None` = linear, `FixedColumnCount`/`FixedRowCount` = grid |
| `LaneCount` | `int` | Columns (vertical) or rows (horizontal). `0` = inherit global |
| `Spacing` | `float` | Main-axis item spacing. `0` = inherit global |
| `CrossSpacing` | `float` | Grid cross-axis spacing. `0` = inherit global |

Both `SimpleDataSource` and `DelegateDataSource` implement `ISectionLayoutProvider` out of the box.

---

### `SimpleDataSource`

Convenient concrete data source. Populate it with sections and items directly.

```csharp
// Add a section; returns its index
int AddSection(float headerSize = 0f, float footerSize = 0f, SectionLayoutDescriptor? layout = null)

// Add items by size
void AddItems(int section, IEnumerable<float> sizes)
void AddItems(int section, int count, float uniformSize)

void Clear()
```

---

### `DelegateDataSource`

Callback-based data source for maximum flexibility with minimal boilerplate.

```csharp
var ds = new DelegateDataSource
{
    SectionCount  = () => myData.Count,
    ItemCount     = s  => myData[s].Items.Count,
    ItemSize      = (s, i) => myData[s].Items[i].Height,
    HeaderSize    = s  => 50f,
    FooterSize    = s  => 0f,
    SectionLayout = s  => myData[s].IsGrid
                        ? new SectionLayoutDescriptor
                          {
                              OverrideConstraint = true,
                              Constraint         = GridConstraint.FixedColumnCount,
                              LaneCount          = 3
                          }
                        : (SectionLayoutDescriptor?)null
};
```

---

### `IScrollCell`

Implement on a cell MonoBehaviour to receive fill callbacks automatically.

```csharp
public class MyCell : MonoBehaviour, IScrollCell
{
    public void OnFill(int section, int index)
    {
        // populate visuals from your data model
    }
}
```

Called by the controller when the cell becomes visible. Prefer this over `OnCellVisible` for self-contained cells.

---

### `IScrollSectionElement`

Implement on a header or footer MonoBehaviour to receive its section index.

```csharp
public class MyHeader : MonoBehaviour, IScrollSectionElement
{
    public void OnFill(int section)
    {
        GetComponentInChildren<TMP_Text>().text = _categories[section];
    }
}
```

---

## Quick Start

### 1. Single Section, Uniform Items

```csharp
var ds = new SimpleDataSource();
int section = ds.AddSection();
ds.AddItems(section, 1000, 80f);  // 1000 items, each 80 px tall

_scrollView.Initialize(ds, (s, i) => _cellPrefab);
_scrollView.OnCellVisible += (s, i, cell) =>
    cell.GetComponentInChildren<TMP_Text>().text = $"Item {i}";
```

### 2. Multiple Sections with Headers

```csharp
var ds = new SimpleDataSource();
foreach (var category in _categories)
{
    int s = ds.AddSection(headerSize: 50f);
    ds.AddItems(s, category.Items.Count, 70f);
}

_scrollView.Initialize(
    ds,
    cellPrefabSelector:   (s, i) => _cellPrefab,
    headerPrefabSelector: s      => _headerPrefab
);
```

### 3. Per-Section Layout (mixed linear + grid)

```csharp
var ds = new SimpleDataSource();

// Section 0: 3-column grid
ds.AddSection(headerSize: 50f, layout: new SectionLayoutDescriptor
{
    OverrideConstraint = true,
    Constraint         = GridConstraint.FixedColumnCount,
    LaneCount          = 3,
    CrossSpacing       = 10f
});
ds.AddItems(0, 12, 100f);

// Section 1: plain linear list (uses global inspector config)
ds.AddSection(headerSize: 50f);
ds.AddItems(1, 20, 70f);

_scrollView.Initialize(ds, (s, i) => _cellPrefab);
```

### 4. Delegate-Based Data Source

```csharp
var ds = new DelegateDataSource
{
    SectionCount = () => _data.Length,
    ItemCount    = s  => _data[s].Items.Length,
    ItemSize     = (s, i) => _data[s].Items[i].Height,
    HeaderSize   = s  => 40f,
    FooterSize   = _  => 0f
};
_scrollView.Initialize(ds, (s, i) => GetPrefabFor(_data[s].Items[i]));
```

### 5. Scroll Navigation

```csharp
// Scroll item (2, 0) to the top of the viewport, animated
_scrollView.ScrollTo(section: 2, index: 0, duration: 0.4f);

// Instant jump to top (e.g. on tab switch)
_scrollView.ScrollToTop(duration: 0f);

// Smooth scroll to bottom
_scrollView.ScrollToBottom(duration: 0.3f);
```

---

## Layout Modes Summary

| Mode | Direction | Grid Constraint | Constraint Count | Configured via |
|---|---|---|---|---|
| Vertical list | Vertical | None | — | Inspector |
| Horizontal list | Horizontal | None | — | Inspector |
| Grid — fixed columns | Vertical | FixedColumnCount | N | Inspector |
| Grid — fixed rows | Horizontal | FixedRowCount | N | Inspector |
| Per-section (mixed) | Either | Any (global default) | — | `SectionLayoutDescriptor` |

---

## Extension Points

| Interface | Swap to… |
|---|---|
| `IScrollDataSource` | async/paginated data source, server-driven lists |
| `ISectionLayoutProvider` | dynamic per-section layout from data or user settings |
| `ICellProvider` | Addressables loading, custom pooling strategies |
| `ILayoutStrategy` | custom layout algorithms (masonry, hex grid, etc.) |

### Custom Layout Strategy

```csharp
public class MasonryLayout : ILayoutStrategy
{
    public void Build(IScrollDataSource ds, ScrollViewConfig cfg) { ... }
    public VisibleRange GetVisibleRange(float pos, float size) { ... }
    public ElementRect GetElementRect(int flatIndex) { ... }
    public float GetContentSize() { ... }
    public float GetCrossSize() { ... }
    public int TotalElementCount { ... }
}
```

Inject it by modifying `LayoutStrategyFactory.Create()`.

### Custom Cell Provider

```csharp
public class AddressableCellProvider : ICellProvider
{
    public GameObject GetCell(int section, int index, Transform parent)
    {
        // load from Addressables, fall back to sync on cache hit
    }
    // ... implement remaining ICellProvider members
}
```