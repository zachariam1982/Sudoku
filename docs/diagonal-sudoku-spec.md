# Diagonal Sudoku implementation spec

## 1. Purpose

Add Diagonal Sudoku as a selectable puzzle variant while preserving existing Classic Sudoku behavior. A Diagonal Sudoku uses the normal 9×9 grid, rows, columns, and 3×3 boxes, plus two extra all-different groups: the main diagonal from top-left to bottom-right and the main diagonal from top-right to bottom-left.

This document defines gameplay, generation, difficulty, presentation, persistence, and verification requirements. It does not implement the feature.

## 2. Current project baseline

- `SudokuDifficulty` already defines nine tiers: Simple, Beginner, Easy, Novice, Moderate, Advanced, Hard, Expert, and Hardest.
- `SudokuGenerator`, `SudokuSolver`, `SudokuDifficultyAnalyzer`, and `SudokuModel` currently apply Classic rules only.
- The generator searches by requested tier and can return a lower tier when it cannot create the requested one. The returned `SudokuResult.Difficulty` is the actual tier.
- `SudokuGrid` and `SudokuCell` are shared by the game modes. The board UI can remain shared, with a variant-aware overlay for the diagonals.
- `SaveGameData` stores the puzzle, solution, board, seed, and difficulty, but does not store a puzzle variant.
- New Game result records do not currently identify a variant. Their aggregate stats are keyed by difficulty only, so adding a variant also requires separating those aggregates.
- The variant selector already has scene-authored cards; only Classic currently has navigation wired.

## 3. Scope

### MVP recommendation

- Selecting Classic continues to open the existing Classic home flow.
- Selecting Diagonal opens the existing New Game difficulty flow and starts a Diagonal puzzle at the selected difficulty.
- Journey progression and Daily Challenge remain Classic-only in this MVP. They have their own progression, saved-game, daily-puzzle, and results rules. Enabling Diagonal in either mode should be a separate explicit scope decision.
- Use the existing nine difficulty labels for Diagonal. Do not add or reorder `SudokuDifficulty` enum values.

### Out of scope

- Implementing Hyper, Killer, Thermo, or Irregular rules.
- Changing Classic puzzle generation, Classic scoring, or Classic saves beyond backwards-compatible variant defaults.
- Reworking the existing game modes or their state machines.

## 4. Functional requirements

### 4.1 Variant selection and game start

1. The Diagonal card is wired in the Unity scene to select the Diagonal variant and open New Game difficulty selection.
2. The selected variant stays active through difficulty selection, puzzle creation, gameplay, pause/resume, win/loss, retry, and return-to-home navigation.
3. Choosing Classic always follows the existing Classic path.
4. The game displays the actual generated difficulty if generation falls back to an easier tier.

### 4.2 Rules and player input

For every completed Diagonal board:

- Each row contains digits 1–9 once.
- Each column contains digits 1–9 once.
- Each standard 3×3 box contains digits 1–9 once.
- Each of the two main diagonals contains digits 1–9 once.

The center cell belongs to both diagonals. The two diagonals are independent units and each must contain all digits exactly once.

Entering duplicate values on a diagonal must be reported as a conflict using the same feedback behavior as row, column, or box conflicts. Candidate calculation, auto-candidates, candidate removal from peers, erase/undo/redo, completion validation, and SOS must all respect the diagonal rules. Non-diagonal cells receive no diagonal constraint.

### 4.3 Puzzle generation and solving

- Generate a completed solution that satisfies all Classic and Diagonal rules before removing clues.
- Count solutions with the Diagonal rules active. Accept a puzzle only when it has exactly one solution under those rules.
- Keep generation deterministic for a given seed, variant, difficulty, and generator version. Include the variant in seed derivation so Classic and Diagonal seeds do not unintentionally share puzzles.
- Preserve the requested difficulty when possible. Any fallback must be explicit in the returned puzzle metadata and displayed difficulty.
- Generation must have bounded attempts and be measured on target devices. It must not cause a noticeable gameplay frame stall; use the existing asynchronous/coroutine boundary if measured generation time requires it.

### 4.4 Difficulty and scoring

- Support all nine existing difficulty tiers for Diagonal.
- The difficulty analyzer must build candidates and recognize placements using both diagonal units. Unit-based techniques such as hidden singles and pairs must consider diagonal units where applicable.
- Calibrate the Diagonal analyzer and clue-search ranges independently from Classic. Clue count by itself is not a reliable tier classification.
- Score calculation continues to use the existing tier values and par times unless balance testing identifies a specific adjustment. Any adjustment must not change Classic scoring.
- A puzzle must never be labeled with a harder tier than the analyzer reports. If a requested tier cannot be generated, display and persist the actual fallback tier.

### 4.5 Board presentation

- Show both main diagonals with a clear cyan/turquoise treatment consistent with the approved Diagonal Sudoku mockup.
- Keep the standard box boundaries visible. Diagonal lines must align exactly to cell centers or edges, intersect cleanly in the center cell, and not obscure digits, pencil marks, selection, conflict, or matching-digit feedback.
- Keep the overlay aligned during portrait/landscape rotation and supported screen-size changes.
- Keep all interactive controls and board input on the existing shared grid; the overlay must not intercept pointer or touch input.
- Create and wire visual objects in the Unity scene where practical; use code only to show/configure the active variant overlay.

## 5. Technical design requirements

### 5.1 Variant-aware rules

Introduce a stable variant identifier with Classic as value `0` and Diagonal as a new value. Keep the rule application centralized so generation, solving, validation, and candidate logic cannot disagree. A suitable first step is to represent the board's all-different units (rows, columns, boxes, and optional diagonals) once and have each consumer use those units. Do not implement rules for the other variants in this change.

The variant must be passed into or owned by:

- `SudokuGenerator` and `SudokuResult`;
- `SudokuSolver` solution counting and candidate validation;
- `SudokuDifficultyAnalyzer` candidate state and techniques;
- `SudokuModel` conflict detection, validation, candidate calculation, and saved-puzzle validation;
- New Game view model, save/load, replay, and results.

### 5.2 Saves and records

- Add variant metadata to `SaveGameData`. A save without the new field must load as Classic so existing saves remain valid.
- Validate the saved puzzle and solution using the saved variant's rules.
- Persist variant identity in New Game result/history rows so Classic and Diagonal statistics are not combined incorrectly.
- Change New Game aggregate-stat keys from difficulty-only to variant plus difficulty (or calculate those aggregates from result rows). Update the insert/completion triggers and result queries to use both values.
- On SQLite, add the variant column to `new_game_results` with a Classic default using the project's idempotent column-migration pattern; preserve existing result IDs and completion rows. Existing difficulty-only aggregate rows must be attributed to Classic.
- On WebGL, old PlayerPrefs result/stat JSON without a variant field must deserialize as Classic; keep the desktop and WebGL query/stat behavior consistent.
- Do not change Journey or Daily Challenge schemas as part of the MVP unless those modes are explicitly brought into scope.

### 5.3 Compatibility

- Existing Classic saves without variant metadata load as Classic.
- Existing Classic generation output, difficulty behavior, validation, and result interpretation remain unchanged.
- Enum numeric values already persisted for `SudokuDifficulty` remain unchanged.
- If implementation changes the global database version, migration must be transactional and tested against a copy of a version-2 database. If the existing idempotent column-ensuring pattern is used without a global version bump, test it against both fresh and existing databases.

## 6. Verification and acceptance criteria

### Rules and solver

- A valid completed Diagonal solution is accepted.
- A board with a duplicate on either main diagonal is rejected, even when rows, columns, and boxes are otherwise valid.
- A duplicate on a non-main diagonal is not rejected solely for being on a diagonal.
- A center-cell value is checked against both diagonals.
- The solver returns correct zero/one/multiple-solution counts for diagonal fixtures and does not mutate the caller's board.

### Generator and difficulty

- Generated solutions satisfy all rows, columns, boxes, and both diagonals.
- Every generated puzzle has exactly one Diagonal solution.
- Each of the nine requested tiers has representative generation tests; actual tier and fallback behavior are asserted.
- Repeatable inputs produce repeatable puzzles for the same generator version.
- Generation attempt count, duration, and fallback frequency are measured for all tiers on target devices.

### Gameplay and UI

- Diagonal conflicts, pencil candidates, auto-candidates, undo/redo, SOS, and completion state behave correctly.
- Classic regression cases still pass.
- The two visual diagonals align in portrait and landscape, remain legible over givens and pencil marks, and do not block cell taps.
- The selected variant and difficulty remain correct through start, save, pause, resume, retry, finish, and return to the selector.

### Persistence

- A legacy save with no variant field loads as Classic.
- A Diagonal save restores the same puzzle, solution, variant, board, candidate masks, and history.
- Existing result rows survive migration and are interpreted as Classic.
- New Classic and Diagonal results are separately identifiable in history and statistics.

## 7. Open decisions before implementation

1. Confirm whether the MVP is **New Game only** as recommended above, or whether Diagonal must also be available in Journey and/or Daily Challenge.
2. Confirm whether result statistics should be separate by variant. This spec assumes they should be, and therefore includes a New Game results migration.
3. Confirm whether the displayed diagonal lines should be solid or dashed. The approved mockup uses cyan dashed lines; this spec treats that as the default visual direction.

## 8. Suggested implementation order

1. Confirm mode scope and result-stat behavior.
2. Add variant identity and scene navigation from the Diagonal selector card.
3. Make model rules, solver, and unique-solution counting variant-aware; add rule tests.
4. Update solution generation and difficulty analysis; calibrate all nine tiers.
5. Update New Game saves, result persistence, and migrations.
6. Add scene-authored diagonal visuals and responsive alignment.
7. Run Classic regression tests and end-to-end portrait/landscape checks.
