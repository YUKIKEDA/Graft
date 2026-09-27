# Graft — WPF competitive scenario matrix

This is the source of truth that lines up **in-house WPF E2E scenarios** covered by FlaUI / WinAppDriver / TestStack.White / Appium (Windows) with what Graft does today.
Playwright DX (codegen / trace / video) and the whole of TestComplete Object Spy are not comparison axes (optional, later is fine).
See [`docs/design.md`](design.md) Q125 onward, and the caller surface in [`docs/graft-core.md`](graft-core.md).

**Gate:** Every **Must** row is implemented and the sample E2E is green. No Must row remains.

---

## Legend

| Mark             | Meaning                                                        |
| ---------------- | -------------------------------------------------------------- |
| Graft OK         | A representative scenario is covered by a sample or a contract |
| Graft PART       | Only part of it, a workaround, or a remaining hole             |
| Graft NO         | Not implemented, or intentionally unsupported                  |
| **Must**         | Required for the WPF coverage gate (**fixed**)                 |
| **Optional**     | Nice to have. Outside the gate                                 |
| **Out of scope** | The design does not do this / another product's area           |
| **Done**         | Already enough (counts as a finished Must)                     |

The competitor column means "is this a scenario that class of tool normally writes?" It is not a feature-name match.

---

## Fixed Must (review result)

Starting from the proposed list, K05 / V06 / W12 / A08 are optional. **X04 is Must (Done by the `-m:1` practice).** **D06 (action timeline) was added as Must** (finish one framework first). **P02 (element-clipped screenshot) was promoted to Must** (Phase 35).

`F02 F04 F05` · `M03–M08` · `K03 K04` · `V03 V05` · `T03 T04` · `L04 L05 L06` · `E04` · `H02 H03` · `U02 U03 U04` · `G06–G10` · `C01–C06` · `W06–W11` · `A04–A07` · `X04` · `D06` · `P02`

Inspector (F08) is **optional** for an in-house app that already has `getTree` (outside the gate, not required on the roadmap).

---

## 1. Session / launch

| ID  | Scenario                                      | Competitors | Graft | Priority     | Phase | Notes                                                          |
| --- | --------------------------------------------- | ----------- | ----- | ------------ | ----- | -------------------------------------------------------------- |
| S01 | Launch the app, connect, and start driving it | Yes         | OK    | Done         | —     | `Application.LaunchAsync`                                      |
| S02 | Connect to a process that is already running  | Yes         | PART  | Optional     | —     | `ConnectAsync` exists but is not a first-class documented step |
| S03 | Exit the process when the test ends           | Yes         | OK    | Done         | —     | Session dispose                                                |
| S04 | Reuse a session (keep the process)            | PART        | PART  | Optional     | —     | Opt-in policy only                                             |
| S05 | Black-box control of a third-party exe        | Yes         | NO    | Out of scope | —     | Assumes the agent is embedded first                            |

---

## 2. Find / selectors

| ID  | Scenario                                        | Competitors | Graft | Priority     | Phase | Notes                                   |
| --- | ----------------------------------------------- | ----------- | ----- | ------------ | ----- | --------------------------------------- |
| F01 | Unique match by AutomationId                    | Yes         | OK    | Done         | —     | Hard match                              |
| F02 | Match by Name / ControlType                     | Yes         | OK    | Done         | 27    | `GetByName` / `GetByControlType`        |
| F03 | Reduce ambiguity with an ancestor near-path     | PART        | PART  | Optional     | —     | `NearAutomationId`                      |
| F04 | Find a list item by visible text or item key    | Yes         | OK    | Done         | 27    | `SelectAsync(key)`                      |
| F05 | Relative selectors (child, sibling, nth)        | Yes         | OK    | Done         | 27    | `Child` / `Sibling` / `Nth`             |
| F06 | Self-heal with an alternate selector on failure | PART        | PART  | Optional     | —     | Phase 4. Fuzzy matching is out of scope |
| F07 | Fuzzy / edit-distance match                     | PART        | NO    | Out of scope | —     |                                         |
| F08 | Capture ids with an Inspector / Spy             | Yes         | NO    | Optional     | —     | A finding aid. `getTree` can stand in   |

---

## 3. Mouse / pointer

| ID  | Scenario                                             | Competitors | Graft | Priority | Phase | Notes                           |
| --- | ---------------------------------------------------- | ----------- | ----- | -------- | ----- | ------------------------------- |
| M01 | Click (button Invoke)                                | Yes         | OK    | Done     | —     | native → Peer → SendInput       |
| M02 | Right-click → ContextMenu (one level)                | Yes         | OK    | Done     | —     | Phase 16                        |
| M03 | ContextMenu submenu                                  | Yes         | OK    | Done     | 26    | `SelectMenuAsync`               |
| M04 | Double-click                                         | Yes         | OK    | Done     | 25    | `DoubleClickAsync`              |
| M05 | Hover / MouseEnter side effect                       | Yes         | OK    | Done     | 25    | Waiting for a ToolTip is C03    |
| M06 | Drag and drop                                        | Yes         | OK    | Done     | 25    | Element to element only         |
| M07 | Click at a point (outside the element, or an offset) | Yes         | OK    | Done     | 25    | DIP relative to the click point |
| M08 | Mouse wheel                                          | Yes         | OK    | Done     | 25    | `WheelAsync`                    |

---

## 4. Keyboard

| ID  | Scenario                           | Competitors | Graft | Priority | Phase | Notes                                                                  |
| --- | ---------------------------------- | ----------- | ----- | -------- | ----- | ---------------------------------------------------------------------- |
| K01 | Literal typing (SendKeys)          | Yes         | OK    | Done     | —     |                                                                        |
| K02 | Chord (Ctrl+A and similar)         | Yes         | OK    | Done     | —     | `PressAsync` / `pressKeys`                                             |
| K03 | Verify Tab / focus movement        | Yes         | OK    | Done     | 29a   | `ExpectFocusedAsync`                                                   |
| K04 | Special keys (F1–F12, Win, NumPad) | Yes         | PART  | Done     | 29a   | F1–F12 + NumPad. **Win excluded**                                      |
| K05 | typeHuman (delayed, human-paced)   | PART        | OK    | Done     | —     | `TypeHumanAsync(text, delay)`. The gap is waited on the request thread |

---

## 5. Text / value

| ID  | Scenario                     | Competitors | Graft | Priority | Phase | Notes                               |
| --- | ---------------------------- | ----------- | ----- | -------- | ----- | ----------------------------------- |
| V01 | TextBox replace via setValue | Yes         | OK    | Done     | —     |                                     |
| V02 | Clear and type again         | Yes         | OK    | Done     | —     |                                     |
| V03 | PasswordBox input            | Yes         | OK    | Done     | 29a   | Set only (not exposed on get)       |
| V04 | Slider / numeric range       | Yes         | OK    | Done     | —     |                                     |
| V05 | RichTextBox / formatted text | PART        | PART  | Done     | 29a   | **Plain text only** (no formatting) |
| V06 | Paste through the clipboard  | Yes         | NO    | Optional | —     |                                     |

---

## 6. Toggle / check

| ID  | Scenario                    | Competitors | Graft | Priority | Phase | Notes                  |
| --- | --------------------------- | ----------- | ----- | -------- | ----- | ---------------------- |
| T01 | CheckBox toggle             | Yes         | OK    | Done     | —     |                        |
| T02 | ExpectChecked               | Yes         | OK    | Done     | —     |                        |
| T03 | RadioButton group selection | Yes         | OK    | Done     | 29a   | Toggle + ExpectChecked |
| T04 | ToggleButton                | Yes         | OK    | Done     | 29a   | Toggle + ExpectChecked |

---

## 7. List / selection

| ID  | Scenario                                      | Competitors | Graft | Priority | Phase | Notes                                 |
| --- | --------------------------------------------- | ----------- | ----- | -------- | ----- | ------------------------------------- |
| L01 | ListBox single select (index)                 | Yes         | OK    | Done     | —     |                                       |
| L02 | ListBox multi-select (replace)                | Yes         | OK    | Done     | —     |                                       |
| L03 | ComboBox item select (index)                  | Yes         | OK    | Done     | —     |                                       |
| L04 | Open and close a ComboBox dropdown explicitly | Yes         | OK    | Done     | 29b   | Expand/Collapse + `IsDropDownOpen`    |
| L05 | Select by display name or key                 | Yes         | OK    | Done     | 27    | `SelectAsync(key)`                    |
| L06 | ListView / GridView                           | Yes         | OK    | Done     | 29b   | Row = ListBox API / cell is read-only |
| L07 | scroll+select on a virtualized list           | Yes         | OK    | Done     | —     |                                       |

---

## 8. Tree / expand

| ID  | Scenario                   | Competitors | Graft | Priority | Phase | Notes             |
| --- | -------------------------- | ----------- | ----- | -------- | ----- | ----------------- |
| E01 | TreeView expand / collapse | Yes         | OK    | Done     | —     |                   |
| E02 | ExpectExpanded / Selected  | Yes         | OK    | Done     | —     |                   |
| E03 | Expander                   | Yes         | OK    | Done     | —     |                   |
| E04 | Walk a deep tree by path   | Yes         | OK    | Done     | 27    | `SelectTreeAsync` |

---

## 9. Tabs / host switch

| ID  | Scenario                            | Competitors | Graft | Priority | Phase | Notes                                                                                             |
| --- | ----------------------------------- | ----------- | ----- | -------- | ----- | ------------------------------------------------------------------------------------------------- |
| H01 | TabControl selection                | Yes         | OK    | Done     | —     |                                                                                                   |
| H02 | Frame / NavigationWindow navigation | Yes         | OK    | Done     | 32    | **Frame only** (no dedicated DSL; existing WaitFor/Expect). NavigationWindow is outside this Must |
| H03 | Wait for a custom "page" swap       | Yes         | OK    | Done     | 24    | Visibility panel                                                                                  |

---

## 10. Menu

| ID  | Scenario                                | Competitors | Graft | Priority | Phase | Notes                   |
| --- | --------------------------------------- | ----------- | ----- | -------- | ----- | ----------------------- |
| U01 | Menu bar top level + one submenu        | Yes         | OK    | Done     | —     |                         |
| U02 | Menu at any depth / path DSL            | Yes         | OK    | Done     | 26    | `SelectMenuAsync` path  |
| U03 | ContextMenu submenu                     | Yes         | OK    | Done     | 26    | Bundled with M03        |
| U04 | Explicit error for a disabled menu item | Yes         | OK    | Done     | 26    | `element.notActionable` |

---

## 11. DataGrid / table

| ID  | Scenario                            | Competitors | Graft | Priority | Phase | Notes                                        |
| --- | ----------------------------------- | ----------- | ----- | -------- | ----- | -------------------------------------------- |
| G01 | Row select (single)                 | Yes         | OK    | Done     | —     |                                              |
| G02 | Multi-row select                    | Yes         | OK    | Done     | —     |                                              |
| G03 | Cell text read and write            | Yes         | OK    | Done     | —     |                                              |
| G04 | Cell CheckBox                       | Yes         | OK    | Done     | —     |                                              |
| G05 | Column key (Header)                 | Yes         | OK    | Done     | —     |                                              |
| G06 | Template column                     | Yes         | OK    | Done     | 28    | Get display text / Set = TextBox or CheckBox |
| G07 | Cell selection unit                 | Yes         | OK    | Done     | 28    | `SelectCellAsync` single                     |
| G08 | Find a row after a sort             | Yes         | OK    | Done     | 28    | `SelectRowAsync(columnKey, value)`           |
| G09 | Filter / column resize / reorder UI | Yes         | PART  | Done     | 28    | **Sort UI only** (header click)              |
| G10 | Add a new row / delete a row        | Yes         | OK    | Done     | 28    | `AddRowAsync` / `DeleteSelectedRowsAsync`    |

---

## 12. Other controls

| ID  | Scenario                                   | Competitors | Graft | Priority | Phase | Notes                                                                                      |
| --- | ------------------------------------------ | ----------- | ----- | -------- | ----- | ------------------------------------------------------------------------------------------ |
| C01 | DatePicker / Calendar                      | Yes         | OK    | Done     | 29b   | SelectedDate `yyyy-MM-dd` (no Calendar UI)                                                 |
| C02 | Read a ProgressBar and wait for completion | Yes         | OK    | Done     | 24    | `ExpectValue`                                                                              |
| C03 | Wait for a ToolTip                         | Yes         | OK    | Done     | 29b   | `ExpectToolTipAsync`                                                                       |
| C04 | Drive ToolBar / StatusBar items            | Yes         | OK    | Done     | 29b   | No dedicated API (sample)                                                                  |
| C05 | Popup / Flyout                             | Yes         | OK    | Done     | 29b   | Open popup merges into the child tree                                                      |
| C06 | Hyperlink / custom clickable               | Yes         | OK    | Done     | 29b   | Hyperlink inside a TextBlock + Click                                                       |
| C07 | Register actions for a third-party type    | PART        | OK    | Done     | —     | `WpfControlActions` (invoke / setValue / toggle). The app registers them. None are shipped |

---

## 13. Window / dialog / navigation / wait

| ID  | Scenario                                   | Competitors | Graft | Priority     | Phase | Notes                      |
| --- | ------------------------------------------ | ----------- | ----- | ------------ | ----- | -------------------------- |
| W01 | Child window list / switch / wait          | Yes         | OK    | Done         | —     |                            |
| W02 | Open a modal with ShowDialog               | Yes         | OK    | Done         | —     |                            |
| W03 | Open/Save/Folder dialogs (seam)            | Yes         | OK    | Done         | —     |                            |
| W04 | MessageBox (seam)                          | Yes         | OK    | Done         | —     |                            |
| W05 | Drive a real OS common dialog through UIA  | Yes         | NO    | Out of scope | —     |                            |
| W06 | Wait for an element to appear (generic)    | Yes         | OK    | Done         | 24    | `WaitForAsync`             |
| W07 | Wait for an element to disappear           | Yes         | OK    | Done         | 24    | `ExpectGoneAsync`          |
| W08 | Wait for a window to close                 | Yes         | OK    | Done         | 24    | `WaitForWindowClosedAsync` |
| W09 | Progress dialog → done → next screen       | Yes         | OK    | Done         | 24    | Sample `ProgressWindow`    |
| W10 | Stable check of an in-window screen change | Yes         | OK    | Done         | 24    | `NextScreenPanel`          |
| W11 | Auto-wait for async UI (Dispatcher delay)  | Yes         | PART  | Done         | 24    | No dedicated API (polling) |
| W12 | Toast / transient notification             | PART        | NO    | Optional     | —     |                            |

---

## 14. Screenshot / visual

| ID  | Scenario                   | Competitors | Graft | Priority     | Phase | Notes                                                                                                                                                                                                           |
| --- | -------------------------- | ----------- | ----- | ------------ | ----- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| P01 | Whole-window PNG           | Yes         | OK    | Done         | —     |                                                                                                                                                                                                                 |
| P02 | Element-clipped screenshot | Yes         | OK    | **Must**     | 35    | Window RTB intersection clip + popup root RTB. An open ToolTip is a child node. An open overlay is composited onto the element and window screenshots. Done |
| P03 | Image expect / diff        | Yes         | NO    | Optional     | —     |                                                                                                                                                                                                                 |
| P04 | Whole desktop              | PART        | NO    | Out of scope | —     |                                                                                                                                                                                                                 |
| P05 | Video / trace recording    | Yes         | NO    | Out of scope | —     |                                                                                                                                                                                                                 |

---

## 15. Assertion / Expect

| ID  | Scenario                                        | Competitors | Graft | Priority | Phase | Notes                                                                                              |
| --- | ----------------------------------------------- | ----------- | ----- | -------- | ----- | -------------------------------------------------------------------------------------------------- |
| A01 | ExpectName                                      | Yes         | OK    | Done     | —     |                                                                                                    |
| A02 | ExpectSelected / Expanded / Checked             | Yes         | OK    | Done     | —     |                                                                                                    |
| A03 | ExpectCellText                                  | Yes         | OK    | Done     | —     |                                                                                                    |
| A04 | ExpectEnabled / Disabled                        | Yes         | OK    | Done     | 24    |                                                                                                    |
| A05 | ExpectVisible / Hidden                          | Yes         | OK    | Done     | 24    |                                                                                                    |
| A06 | Partial text match / regex                      | Yes         | OK    | Done     | 24    | Contains / Matches                                                                                 |
| A07 | ExpectValue (Slider and similar, from the tree) | PART        | OK    | Done     | 24    | `TreeNode.value`                                                                                   |
| A08 | Soft assert (store failures)                    | PART        | OK    | Done     | —     | `GraftSession.SoftAssert` + `Check`. Dispose throws `expect.failed`. Individuals are in `failures` |

---

## 16. Failure diagnosis / report

| ID  | Scenario                                | Competitors | Graft | Priority     | Phase | Notes                                                                                                                            |
| --- | --------------------------------------- | ----------- | ----- | ------------ | ----- | -------------------------------------------------------------------------------------------------------------------------------- |
| D01 | Structured FailureReport                | Yes         | OK    | Done         | —     |                                                                                                                                  |
| D02 | Attach a screenshot on failure          | Yes         | OK    | Done         | —     |                                                                                                                                  |
| D03 | Attach the tree on failure              | Yes         | OK    | Done         | —     |                                                                                                                                  |
| D04 | Tree diff JSON                          | PART        | OK    | Done         | —     | `FailureReport.treeDiff` (added/removed/changed). Not JSON Patch. On by default; turn off with `IncludeTreeDiff`                 |
| D05 | Rewrite the scenario file automatically | PART        | NO    | Out of scope | —     |                                                                                                                                  |
| D06 | Action timeline (visual review)         | PART        | OK    | Done         | 33    | PNG sequence + HTML (pace and action-name captions). No GIF/FFmpeg/ImageSharp. |

---

## 17. Scenario / MCP / test DX

| ID  | Scenario                  | Competitors | Graft | Priority | Phase | Notes                                                                                                           |
| --- | ------------------------- | ----------- | ----- | -------- | ----- | --------------------------------------------------------------------------------------------------------------- |
| X01 | Declarative Scenario JSON | Yes         | OK    | Done     | —     |                                                                                                                 |
| X02 | MCP atomic tools          | Yes         | OK    | Done     | —     |                                                                                                                 |
| X03 | Codegen / recorder        | Yes         | NO    | Optional | —     |                                                                                                                 |
| X04 | Stable parallel E2E       | Yes         | PART  | Done     | 31    | Canonical command is `dotnet test Graft.slnx -m:1`. True parallel stability is out of scope (mutex was dropped) |

---

## 18. Platform (reference)

| ID  | Scenario              | Competitors | Graft | Priority      | Phase | Notes                             |
| --- | --------------------- | ----------- | ----- | ------------- | ----- | --------------------------------- |
| Z01 | Avalonia adapter      | —           | NO    | Out of scope  | —     | Not implemented                   |
| Z02 | .NET Framework WPF    | PART        | NO    | Out of scope  | —     |                                   |
| Z03 | Headless-only backend | PART        | NO    | Out of scope  | —     |                                   |

---

## Must implementation split (provisional phases)

| Phase | Bundle                                       | Main IDs                                        |
| ----- | -------------------------------------------- | ----------------------------------------------- |
| 23    | This matrix + roadmap (no implementation)    | —                                               |
| 24    | Waits / Expect / screen changes and progress | W06–W11, A04–A07, H03, C02 (H02 Frame excluded) |
| 25    | Advanced mouse                               | M04–M08                                         |
| 26    | Menu depth                                   | M03, U02–U04                                    |
| 27    | Find, paths, and key selection               | F02, F04, F05, L05, E04                         |
| 28    | Remaining DataGrid                           | G06–G10                                         |
| 29a   | Input, toggle, and key holes                 | V03, V05, T03, T04, K03, K04                    |
| 29b   | List and other UI holes                      | L04, L06, C01, C03–C06                          |
| 31    | SendInput parallelism                        | X04 (Done with `-m:1`)                          |
| 32    | Frame navigation                             | H02 (Frame only)                                |
| 33    | Action timeline                              | D06                                             |
| 35    | Element-clipped screenshot                   | P02                                             |

Remaining Must: **none** (P02 / H02 / X04 / D06 are Done).

---

## Review checklist

- [x] Must was fixed row by row
- [x] K05 / V06 / W12 / A08 are optional; X04 is Must (Done as a practice)
- [x] D06 was added as Must (finish one framework first)
- [x] P02 was promoted to Must (Phase 35. Avalonia blocked again until it landed)
- [x] Inspector is optional (outside the gate)
- [x] Agreed the Avalonia resume gate (every Must green)
- [x] The Phase 24 acceptance line is fixed in (and each later `task_phaseN.md`)
