/// <summary>Stores Killer Sudoku sessions in files separate from other variants.</summary>
public sealed class KillerSaveSystem : JsonGameSaveSystem
{
    protected override string VariantName => "Killer";

    protected override string GetFileName(SaveSlot slot)
    {
        if (slot == SaveSlot.NewGame) return "killer_new_game_save.json";
        if (slot == SaveSlot.DailyChallenge) return "killer_daily_challenge_save.json";
        return "killer_save.json";
    }

    // Keep the array-only contract for base compatibility; Killer saves use the
    // SaveGameData overload below so cage sums and layout are validated too.
    protected override bool IsValidSavedPuzzle(int[] puzzleFlat, int[] solutionFlat) =>
        KillerSudokuModel.IsValidSavedPuzzle(puzzleFlat, solutionFlat);

    protected override bool IsValidSavedPuzzle(SaveGameData data, SaveSlot slot) =>
        KillerSudokuModel.IsValidSavedPuzzle(
            data.OriginalPuzzleFlat,
            data.SolutionFlat,
            KillerSudokuModel.DeserializeCages(data.KillerCages));
}
