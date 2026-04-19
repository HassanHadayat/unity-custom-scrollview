# CustomScrollView — Documentation

## Architecture

```
┌─────────────────────────────────────────────────────┐
│                  User Code (MonoBehaviour)           │
│  Implements IScrollDataSource + provides prefabs    │
└───────────────┬─────────────────────────────────────┘
                │ Initialize(dataSource, cellProvider)
                ▼
┌─────────────────────────────────────────────────────┐
│           ScrollViewController (Controller)          │
│  • Listens to ScrollRect.onValueChanged             │
│  • Queries ILayoutStrategy for visible range        │
│  • Calls ICellProvider to show/recycle cells         │
│  • Positions RectTransforms from ElementRect        │
└──────┬──────────────┬───────────────┬───────────────┘
       │              │               │
       ▼              ▼               ▼
┌────────────┐ ┌──────────────┐ ┌────────────────┐
│ ElementMap │ │ILayoutStrategy│ │ ICellProvider  │
│  (Core)    │ │  (Layout)    │ │   (View)       │
│            │ │              │ │                │
│ flat index │ │ Linear or    │ │ DefaultCell-   │
│ ↔ section/ │ │ Grid layout  │ │ Provider +     │
│   index    │ │ + binary     │ │ CellPool       │
│            │ │   search     │ │                │
└────────────┘ └──────────────┘ └────────────────┘
```

## Design Principles

| Principle | Application |
|-----------|-------------|
| **S** — Single Responsibility | Controller manages lifecycle; Layout calculates positions; CellPool handles recycling; ElementMap handles indexing |
| **O** — Open/Closed | New layout modes by implementing `ILayoutStrategy`; new cell creation by implementing `ICellProvider` |
| **L** — Liskov Substitution | Any `ILayoutStrategy` works in the controller without changes |
| **I** — Interface Segregation | `IScrollDataSource`, `ICellProvider`, `IScrollCell`, `IScrollSectionElement` are small, focused |
| **D** — Dependency Inversion | Controller depends on interfaces, not concrete layout/pool classes |

## MVC Mapping

- **Model**: `IScrollDataSource` (+ `SimpleDataSource`, `DelegateDataSource`)
- **View**: `ICellProvider`, `IScrollCell`, `IScrollSectionElement` (cell GameObjects)
- **Controller**: `ScrollViewController`

---

## Quick Start

### 1. Single Section, Uniform Items

```csharp
using CustomScrollView.Controller;
using CustomScrollView.Data;
using UnityEngine;

public class BasicList : MonoBehaviour
{
    [SerializeField] ScrollViewController scrollView;
    [SerializeField] GameObject cellPrefab;

    void Start()
    {
        var ds = new SimpleDataSource();
        int section = ds.AddSection();
        ds.AddItems(section, 1000, 80f); // 1000 items, each 80px tall

        scrollView.Initialize(ds, (s, i) => cellPrefab);
        scrollView.OnCellVisible += (section, index, cell) =>
        {
            cell.GetComponentInChildren<TMPro.TMP_Text>().text = $"Item {index}";
        };
    }
}
```

### 2. Multiple Sections with Headers

```csharp
using CustomScrollView.Controller;
using CustomScrollView.Data;
using UnityEngine;

public class SectionList : MonoBehaviour
{
    [SerializeField] ScrollViewController scrollView;
    [SerializeField] GameObject cellPrefab;
    [SerializeField] GameObject headerPrefab;

    string[] categories = { "Fruits", "Vegetables", "Dairy" };

    void Start()
    {
        var ds = new SimpleDataSource();
        foreach (var cat in categories)
        {
            int s = ds.AddSection(headerSize: 50f);
            ds.AddItems(s, 20, 70f);
        }

        scrollView.Initialize(
            ds,
            cellPrefabSelector: (s, i) => cellPrefab,
            headerPrefabSelector: (s) => headerPrefab
        );

        scrollView.OnCellVisible += (section, index, cell) =>
        {
            cell.GetComponentInChildren<TMPro.TMP_Text>().text =
                $"{categories[section]} #{index}";
        };
    }
}
```

### 3. Grid Layout (Fixed Columns)

Set on the `ScrollViewController` inspector:
- Direction: **Vertical**
- Grid Constraint: **FixedColumnCount**
- Constraint Count: **3**

```csharp
var ds = new SimpleDataSource();
int s = ds.AddSection();
ds.AddItems(s, 99, 100f);

scrollView.Initialize(ds, (_, _) => gridCellPrefab);
```

### 4. Horizontal List

Set on the inspector:
- Direction: **Horizontal**
- Grid Constraint: **None**

### 5. Delegate-Based Data Source

```csharp
var ds = new DelegateDataSource
{
    SectionCount = () => myData.Length,
    ItemCount    = (s) => myData[s].items.Length,
    ItemSize     = (s, i) => myData[s].items[i].height,
    HeaderSize   = (s) => 40f,
    FooterSize   = (s) => 0f
};
scrollView.Initialize(ds, (s, i) => GetPrefabFor(myData[s].items[i]));
```

### 6. IScrollCell Interface (self-filling cells)

```csharp
using CustomScrollView.Interfaces;
using UnityEngine;
using TMPro;

public class MyCell : MonoBehaviour, IScrollCell
{
    [SerializeField] TMP_Text label;
    [SerializeField] Image icon;

    public void OnFill(int section, int index)
    {
        var data = DataStore.Get(section, index);
        label.text = data.Name;
        icon.sprite = data.Icon;
    }
}
```

---

## ScrollTo API

```csharp
scrollView.ScrollTo(section: 2, index: 0, duration: 0.5f);
scrollView.ScrollToTop();
scrollView.ScrollToBottom(duration: 0f); // instant
```

---

## Layout Modes Summary

| Mode | Direction | Grid Constraint | Constraint Count |
|------|-----------|-----------------|------------------|
| Vertical List | Vertical | None | — |
| Horizontal List | Horizontal | None | — |
| Grid (3 cols) | Vertical | FixedColumnCount | 3 |
| Grid (2 rows) | Horizontal | FixedRowCount | 2 |

---

## Extending

### Custom Layout Strategy

```csharp
public class MyHexLayout : ILayoutStrategy { ... }

// Then inject before Initialize:
// Or modify LayoutStrategyFactory
```

### Custom Cell Provider

```csharp
public class AddressableCellProvider : ICellProvider
{
    // Load cells via Addressables instead of Instantiate
}
```
