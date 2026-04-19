# Demo Setup Guide — CustomScrollView

This guide walks you through creating 6 demo scenes. You'll first create
2 reusable prefabs, then set up each scene step by step.

---

## Step 0 — Import demo scripts

Copy the `Samples~/Demo/Scripts/` folder into your project's `Assets/Demo/Scripts/`.
Files you need:

```
DemoCell.cs
DemoHeader.cs
Demo01_VerticalList.cs
Demo02_HorizontalList.cs
Demo03_GridFixedCols.cs
Demo04_GridFixedRows.cs
Demo05_MultipleSections.cs
Demo06_SectionGrid.cs
```

---

## Step 1 — Create prefabs (used by all demos)

### 1A — Cell prefab (`CellPrefab`)

1. In Hierarchy: **right-click → UI → Canvas** (if you don't have one yet)
2. Under Canvas: **right-click → Create Empty → name it `CellPrefab`**
3. Add **`RectTransform`** (already there), set:
   - Width: `300`, Height: `80`
   - Pivot: `(0, 1)` — top-left
4. Add component: **`Image`** (this is the `_background`)
   - Color: white (will be overridden at runtime)
5. **Right-click `CellPrefab` → UI → Text** — name it `Label`
   - Anchor: Stretch-Stretch (hold Alt, bottom-right icon)
   - Left/Right/Top/Bottom offsets: `10, 10, 5, 5`
   - Font size: `18`
   - Alignment: Middle-Left
   - Color: `#333333`
6. Add component to `CellPrefab`: **`DemoCell`**
   - Drag `Label` → `_label` field
   - Drag `CellPrefab`'s Image → `_background` field
7. **Drag `CellPrefab` from Hierarchy → Project panel** (Assets/Demo/Prefabs/)
8. **Delete** `CellPrefab` from Hierarchy

### 1B — Header prefab (`HeaderPrefab`)

1. Under Canvas: **right-click → Create Empty → name it `HeaderPrefab`**
2. Set `RectTransform`:
   - Width: `300`, Height: `50`
   - Pivot: `(0, 1)`
3. Add: **`Image`**
   - Color: `#333333` (dark background)
4. **Right-click `HeaderPrefab` → UI → Text** — name it `HeaderLabel`
   - Anchor: Stretch-Stretch
   - Offsets: `15, 15, 5, 5`
   - Font size: `20`, **Bold**
   - Color: `#FFFFFF`
   - Alignment: Middle-Left
5. Add component: **`DemoHeader`**
   - Drag `HeaderLabel` → `_label`
   - Drag `HeaderPrefab`'s Image → `_background`
6. **Drag to Project** (Assets/Demo/Prefabs/) → **Delete from Hierarchy**

---

## Step 2 — Scene setup (shared steps for every scene)

Every demo scene starts the same way. Repeat these steps for each scene,
then apply the scene-specific settings from Step 3.

1. **File → New Scene → Save As** `Demo0X_[Name]`

2. **Create Canvas**:
   - Hierarchy → right-click → UI → Canvas
   - Canvas Scaler → **Scale With Screen Size**
   - Reference Resolution: `1080 × 1920`
   - Match: `0.5`

3. **Create Scroll View** (under Canvas):
   - Right-click Canvas → UI → **Scroll View**
   - Rename to `ScrollView`
   - Anchor: **Stretch-Stretch** (hold Alt, bottom-right preset)
   - Left: `20`, Right: `20`, Top: `20`, Bottom: `20`
   - **Uncheck** `Horizontal` (for vertical scenes) or `Vertical` (for horizontal scenes)
   - Delete the default `Scrollbar Horizontal` and `Scrollbar Vertical` children if you want a clean look (optional)

4. **Delete** the `Content` child under `Viewport`
   - The `ScrollViewController` creates its own Content at runtime

5. **Add component to `ScrollView`**: **`ScrollViewController`**
   - This auto-attaches after `ScrollRect`

6. **Create empty `DemoManager`** (under Canvas or root):
   - Add the appropriate `Demo0X_...` script
   - Drag `ScrollView` → `_scrollView` field
   - Drag `CellPrefab` from Project → `_cellPrefab` field
   - (If sections demo) Drag `HeaderPrefab` → `_headerPrefab` field

---

## Step 3 — Scene-specific settings

### Scene 1: Vertical List (`Demo01_VerticalList`)

**ScrollViewController inspector:**
| Field              | Value            |
|--------------------|------------------|
| Direction          | **Vertical**     |
| Grid Constraint    | **None**         |
| Spacing            | `4`              |
| Padding            | `10, 10, 10, 10` |

**DemoManager (`Demo01_VerticalList`):**
| Field       | Value |
|-------------|-------|
| Item Count  | `200` |
| Item Height | `80`  |

**Result:** A simple scrollable list with 200 items.

---

### Scene 2: Horizontal List (`Demo02_HorizontalList`)

**ScrollViewController inspector:**
| Field              | Value              |
|--------------------|--------------------|
| Direction          | **Horizontal**     |
| Grid Constraint    | **None**           |
| Spacing            | `8`                |
| Padding            | `10, 10, 10, 10`  |

**ScrollRect:** Make sure `Horizontal` is checked, `Vertical` unchecked.

**DemoManager (`Demo02_HorizontalList`):**
| Field       | Value |
|-------------|-------|
| Item Count  | `100` |
| Item Width  | `200` |

**Result:** Horizontal scrolling cards.

---

### Scene 3: Grid — Fixed Columns (`Demo03_GridFixedCols`)

**ScrollViewController inspector:**
| Field              | Value                |
|--------------------|----------------------|
| Direction          | **Vertical**         |
| Grid Constraint    | **FixedColumnCount** |
| Constraint Count   | **3**                |
| Spacing            | `8`                  |
| Grid Cross Spacing | `8`                  |
| Padding            | `10, 10, 10, 10`    |

**DemoManager (`Demo03_GridFixedCols`):**
| Field       | Value |
|-------------|-------|
| Item Count  | `150` |
| Item Height | `120` |

**Result:** 3-column grid, scrolls vertically.

---

### Scene 4: Grid — Fixed Rows (`Demo04_GridFixedRows`)

**ScrollViewController inspector:**
| Field              | Value             |
|--------------------|-------------------|
| Direction          | **Horizontal**    |
| Grid Constraint    | **FixedRowCount** |
| Constraint Count   | **2**             |
| Spacing            | `8`               |
| Grid Cross Spacing | `8`               |
| Padding            | `10, 10, 10, 10`  |

**ScrollRect:** `Horizontal` checked, `Vertical` unchecked.

**DemoManager (`Demo04_GridFixedRows`):**
| Field       | Value |
|-------------|-------|
| Item Count  | `120` |
| Item Width  | `140` |

**Result:** 2-row grid, scrolls horizontally.

---

### Scene 5: Multiple Sections (`Demo05_MultipleSections`)

**ScrollViewController inspector:**
| Field              | Value        |
|--------------------|--------------|
| Direction          | **Vertical** |
| Grid Constraint    | **None**     |
| Spacing            | `2`          |
| Padding            | `0, 0, 0, 0` |

**DemoManager (`Demo05_MultipleSections`):**
| Field             | Value |
|-------------------|-------|
| Section Count     | `5`   |
| Items Per Section | `15`  |
| Item Height       | `70`  |
| Header Height     | `50`  |

**Don't forget:** Drag `HeaderPrefab` → `_headerPrefab` slot.

**Result:** 5 sections (Fruits, Vegetables, Dairy...) each with a dark header bar.

---

### Scene 6: Section Grid (`Demo06_SectionGrid`)

**ScrollViewController inspector:**
| Field              | Value                |
|--------------------|----------------------|
| Direction          | **Vertical**         |
| Grid Constraint    | **FixedColumnCount** |
| Constraint Count   | **3**                |
| Spacing            | `8`                  |
| Grid Cross Spacing | `8`                  |
| Padding            | `10, 10, 10, 10`    |

**DemoManager (`Demo06_SectionGrid`):**
| Field             | Value |
|-------------------|-------|
| Section Count     | `4`   |
| Items Per Section | `18`  |
| Item Height       | `100` |
| Header Height     | `50`  |

**Don't forget:** Drag `HeaderPrefab` → `_headerPrefab` slot.

**Result:** Sectioned grid — full-width headers, 3-column items underneath.

---

## Hierarchy reference (every scene)

```
Canvas
├── ScrollView                      ← ScrollRect + ScrollViewController
│   └── Viewport
│       └── (Content created at runtime)
└── DemoManager                     ← Demo0X script
```

---

## Tips

- **Prefab pivot matters:** Both prefabs must have pivot `(0, 1)` (top-left).
  The controller positions elements using top-left anchoring.
- **ScrollRect content:** Don't assign a Content object in the inspector.
  `ScrollViewController.Initialize()` creates and configures it automatically.
- **Viewport mask:** Make sure the Viewport has a `Mask` or `RectMask2D`
  component so off-screen cells are clipped.
- **Play & scroll:** Hit Play, then scroll. You'll see cells recycling in
  the Hierarchy — only visible cells exist at any time.
