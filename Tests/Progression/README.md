# Progression regression checks

Run with .NET 8 from the repository root:

```sh
dotnet run --project Tests/Progression/Progression.csproj
```

This dependency-free console harness compiles the production SudokuModel,
ScoringSystem, GameRecord, WinState and LoseState. Unity, database, view model,
state machine and persistence infrastructure are substitutes. It does not test
Unity lifecycle, puzzle generation, SQLite, WebGL persistence or actual save recovery.
The project stays outside Assets and adds nothing to game builds.

Coverage: all nine tiers, all win counts and attainable total-score values,
80% promotion and 45% demotion boundaries, tier caps, idempotence, mixed/short/
empty history, historical replay exit, and normal/historical Retry handlers.

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

The fix prevents future stacked adjustments; it does not rewrite previously saved
incorrect game records or infer the intended tier of already completed puzzles.
