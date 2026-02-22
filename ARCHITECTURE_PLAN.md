# NT_Mindmap Multi-File Architecture Plan

## 1) Data Layer
- **`ProjectEntry.cs`**
  - Serializable node record:
    - `Id`
    - `Title`
    - `Note`
    - `Position` (stored as `PositionX` and `PositionY` for JSON)
    - `ConnectionIds` (`List<int>`)
- **`MindMapData.cs`**
  - Owns full in-memory map state:
    - `Entries` collection
    - `NextId` counter
  - Core operations:
    - `CreateEntry(...)`
    - `GetEntry(id)`
    - `RemoveEntry(id)`
    - `Clear()`
    - `EnsureNextId()`

## 2) UI + Interaction Layer
- **`MindMapManager.cs`** (attach to `GraphEdit`)
  - Creates visual nodes (`GraphNode`) for each `ProjectEntry`.
  - `AddEntry(Vector2 position)` creates model + UI node in one call.
  - Each `GraphNode` contains:
    - `LineEdit` for title
    - `TextEdit` for note
  - Input updates model live via `TextChanged` callbacks.

## 3) To-Do Aggregation Layer
- `RefreshTodoList()` scans all notes.
- Lines starting with `Todo:` are extracted.
- A side `VBoxContainer` is repopulated with `Label` items.

## 4) Persistence Layer
- `SaveProject(path)`
  - Pulls current visual state into model (`Title`, `Note`, `Position`).
  - Rebuilds `ConnectionIds` from GraphEdit connections.
  - Writes JSON using `FileAccess`.
- `LoadProject(path)`
  - Reads JSON using `FileAccess`.
  - Recreates all `GraphNode` UI from `MindMapData`.
  - Restores visual graph connections.
  - Refreshes To-Do side panel.

## 5) Runtime Optimization
- `EnableLowProcessorMode()` in manager `_Ready()`:
  - Enables `OS.LowProcessorUsageMode`.
  - Sets `application/run/low_processor_mode` in `ProjectSettings`.
  - Calls `ProjectSettings.Save()`.

## 6) Expected Scene Wiring
- Root has a `GraphEdit` with script `MindMapManager.cs`.
- Side panel has a `VBoxContainer` assigned to `TodoListContainerPath` export on `MindMapManager`.
- Optional UI controls (buttons) can call:
  - `AddEntry(...)`
  - `SaveProject(path)`
  - `LoadProject(path)`
