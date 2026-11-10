# Puzzle variant rules and player input spec

## 1. Purpose

Define the rules and player-input behavior for the six puzzle choices in the variant selector: Classic, Diagonal, Hyper, Irregular, Thermo, and Killer Sudoku. This is a rules reference for implementation and testing; it does not mean all six variants are included in the Diagonal MVP.

The Diagonal MVP plan is in [diagonal-sudoku-spec.md](diagonal-sudoku-spec.md). That document recommends New Game as the first supported mode. Journey and Daily Challenge require a separate scope decision.

## 2. Shared board and input behavior

- Every variant uses the same 9×9 board, digits 1–9, selected-cell flow, number picker, pencil mode, erase, undo/redo, SOS, save/resume, and win/loss flow.
- Given clues cannot be edited or erased.
- In the current game, an entered conflicting value remains visible, is marked as a conflict, and incurs the existing mistake penalty. The game does not accept a filled board as a win while any variant constraint is violated. Keep this behavior consistent across variants.
- A conflict is a rule violation involving the entered value, including conflicts with already-filled cells. Conflict feedback should identify the affected cells where practical, not only the cell most recently edited.
- Manual pencil marks are notes and may be toggled by the player. Auto-candidates must include only values currently allowed by every active rule.
- Entering or erasing a value updates the board, conflict state, auto-candidates, and affected pencil candidates. Undo and redo restore the previous values and candidate changes, then recalculate conflicts.
- SOS uses the saved solution for the active puzzle variant. A hint must not insert a value that violates the active variant rules.
- A puzzle is complete only when every cell is filled and all constraints for that variant are satisfied.
- Selecting a cell should highlight relevant peers/constraints without hiding its digit, pencil marks, selection, or conflict state.

## 3. Rules and entry behavior by variant

| Variant | Required constraints | Illegal entry and candidate behavior |
|---|---|---|
| **Classic** | Each row, column, and standard 3×3 box contains digits 1–9 at most once. A completed unit contains each digit exactly once. | Mark duplicates in the same row, column, or box as conflicts. Auto-candidates exclude digits already in those three units. |
| **Diagonal** | All Classic constraints, plus each main diagonal contains digits 1–9 at most once. The diagonals are top-left→bottom-right and top-right→bottom-left. | On a main-diagonal cell, mark a repeated value on that diagonal as a conflict. The center cell belongs to both diagonals. Auto-candidates exclude digits used on either diagonal containing the cell. Cells on neither diagonal get no added diagonal restriction. |
| **Hyper** | All Classic constraints, plus four extra 3×3 regions each contain digits 1–9 at most once. Using 1-based row/column coordinates, the regions are rows 2–4 with columns 2–4; rows 2–4 with columns 6–8; rows 6–8 with columns 2–4; and rows 6–8 with columns 6–8. | Mark duplicates in any extra region as conflicts. A cell in an extra region must exclude digits used anywhere in that region, in addition to its row, column, and standard box. Cells outside the extra regions receive no added Hyper restriction. |
| **Irregular (Jigsaw)** | Each row and column contains digits 1–9 at most once. The board’s nine irregular regions replace the standard 3×3 boxes; each region contains exactly nine cells and digits 1–9 at most once. A region map assigns every cell to exactly one region. | Mark duplicates in the same irregular region as conflicts. Auto-candidates use the cell’s assigned region instead of a standard 3×3 box. A shipped region map must be valid: nine regions, nine cells per region, full board coverage, and each region connected orthogonally. |
| **Thermo** | All Classic constraints, plus each thermometer path strictly increases from its bulb to its tip. Paths are ordered lists of cells; consecutive path cells must be orthogonally adjacent. | An entry conflicts if it is not greater than the previous path value or less than the next path value. Candidate digits must also leave enough room for the remaining path cells: at path position `i` in a path of length `n` (zero-based), the value must be at least `i + 1` and at most `10 - (n - i)`. Apply every path constraint when a cell belongs to multiple paths. |
| **Killer** | All Classic constraints, plus each cage sums to its displayed target when complete. Digits cannot repeat within a cage. Standard Killer puzzles use cages that partition the board: every cell belongs to exactly one cage. | Mark repeated cage digits, sums above the target, and partial cages that cannot still reach their target as conflicts. Do not mark a partial cage invalid merely because its current sum is below target. Auto-candidates must respect row/column/box restrictions, cage no-repeat, and whether the remaining cage cells can still reach the target. A complete cage must equal its target exactly. |

## 4. Constraint-aware candidate and conflict model

The input layer should not duplicate variant logic in separate UI handlers. It should ask one rule service for:

1. Whether a value is locally allowed at a cell, considering current board values and every rule active for the puzzle.
2. Which cells are related to a selected/changed cell for peer candidate removal and highlighting.
3. Whether a partial board or special constraint is currently in conflict.
4. Whether a filled board is a valid solution.

For Classic, Diagonal, Hyper, and Irregular, the additional rules are all-different units, so duplicate checks and peer candidates can share a unit-based implementation. Thermo adds ordered inequalities. Killer adds cage membership, no-repeat, and sum feasibility; it cannot be handled by peer removal alone.

Candidate sets must be recomputed or invalidated for every cell whose legality depends on a changed value. This includes all cells in affected units, all remaining cells in an affected cage, and cells on affected thermometer paths. Avoid relying only on row/column/box candidate removal.

## 5. Puzzle-definition data

Each saved/generated puzzle has a stable variant identifier. Store any non-derivable rule data alongside it or reference an immutable definition ID:

- **Classic:** no extra data.
- **Diagonal:** no per-puzzle data; both diagonals are fixed.
- **Hyper:** no per-puzzle data; the four regions are fixed as defined above.
- **Irregular:** 81 region IDs, or an immutable region-layout ID that resolves to the same 81-cell map.
- **Thermo:** ordered cell coordinates for every thermometer path, including its bulb cell.
- **Killer:** cage ID for each cell, cage target for each cage, and a designated location for drawing the target label.

The rule definition must be available during generation, solving, gameplay, save validation, and resume. A save that lacks a variant field is treated as Classic for backwards compatibility.

## 6. Presentation requirements for input feedback

- Use the shared board and input controls. Variant-specific visuals communicate the extra rules but do not receive pointer/touch input.
- Diagonal: show both diagonals.
- Hyper: show the four additional regions while preserving standard box borders.
- Irregular: emphasize the nine region boundaries.
- Thermo: show each bulb and continuous path in the correct order.
- Killer: show cage boundaries and a readable target sum at each cage start.
- Keep variant overlays responsive in portrait and landscape. Selection, matching digits, pencil candidates, and conflicts must remain distinguishable from the rule overlay.

## 7. Acceptance tests

For each variant, tests should include:

- A complete valid board and a board violating each unique rule of that variant.
- A legal and illegal entry in a cell affected by the extra rule, plus a legal entry in a cell not affected by it.
- Auto-candidate results before and after entering/erasing a value.
- Conflict recomputation after undo and redo.
- Save/resume preserving the variant and its complete rule definition.
- Rejection of a completed board that violates any active constraint.

Additional tests:

- Diagonal: test both diagonals and the shared center cell independently.
- Hyper: test each of the four regions and cells outside them.
- Irregular: reject malformed maps (missing cells, duplicate region assignment, non-nine-cell region, disconnected region).
- Thermo: test increasing, equal, and decreasing adjacent values; test minimum/maximum position bounds; test intersecting paths if definitions permit them.
- Killer: test repeated digits in a cage, exact completed sums, over-target sums, unreachable partial sums, and feasible partial sums.

All six variants must also retain the existing conflict presentation and mistake-penalty behavior. Classic regression tests must confirm that no additional constraints leak into Classic puzzles.
