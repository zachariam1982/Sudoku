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

    // Killer Sudoku retains the standard row, column, box, and given-value rules.
    // Cage validation can be added when cage data and KillerSudokuModel are implemented.
    protected override bool IsValidSavedPuzzle(int[] puzzleFlat, int[] solutionFlat) =>
        ClassicSudokuModel.IsValidSavedPuzzle(puzzleFlat, solutionFlat);
}
