# Sudoku model and progression regression checks

Run with .NET 8 from the repository root:

```sh
dotnet run --project Tests/Progression/Progression.csproj
```

This dependency-free console harness compiles the production SudokuModel,
SudokuSolver, SudokuDifficultyAnalyzer, ScoringSystem, and GameRecord. Unity, database, view model, state machine and persistence
infrastructure are substitutes. It does not test Unity lifecycle, state transitions, SQLite, WebGL persistence
or actual save recovery.
The project stays outside Assets and adds nothing to game builds.

Coverage: the bit-mask solver is checked against a known unique puzzle, a multiple-solution board, contradictory row/column/box givens, solution limits, and input preservation; generator fallback is checked for all nine tiers, puzzle validity, uniqueness and clue consistency; fallback order and actual-tier recording are checked; progression tests cover all nine tiers, all win counts and attainable total-score values,
80% promotion and 45% demotion boundaries, tier caps, idempotence, mixed/short/empty history, and historical replay exit.

## Manual Unity/device checks still required

1. Arrange the newest five records (descending ID) at Advanced with poor results
   (for example 0, 157, 40, 0, 0 points). Keep an older Moderate record outside
   that window. RETAKE that older record, lose, then choose New Game.
   Expect the latest recorded level + 1 at Moderate, never Novice.
2. Lose a normal game when demotion is due, then choose Retry. Verify the same
   level, difficulty and generated puzzle. New Game should apply progression.
3. Repeat with a historical replay. Retry retains its record identity; New Game
   clears it and returns to normal progression.
4. With four maximum-score wins and one loss at the same tier, expect promotion
   by one tier regardless of whether the latest result was the loss.
5. Restart with an ordinary saved game and with a saved historical replay. Check
   level, difficulty, board and replay identity. Repeat recovery from completed
   history when no active save exists: expect the same next tier as New Game.
6. Repeat leaving an old replay on WebGL: its next normal level must match Android
   (latest recorded level + 1), not the old replay's level + 1.
7. Win a historical replay and verify the existing record is updated, not appended.
   Records outside the latest five must not affect progression; improvements
   inside that window remain eligible under the existing product behavior.

The generator tries the selected tier for up to 100 deterministic attempts, then tries each easier tier in descending order. It records the analyzer-rated tier actually generated, never exceeds the selected tier, and throws only if all tiers fail.
