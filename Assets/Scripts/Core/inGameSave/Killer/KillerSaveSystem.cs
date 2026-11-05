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

    // SaveGameData currently stores only the flattened grid and solution. Cage-layout
    // serialization must be added before saved Killer sessions can validate cage sums.
    protected override bool IsValidSavedPuzzle(int[] puzzleFlat, int[] solutionFlat) =>
        KillerSudokuModel.IsValidSavedPuzzle(puzzleFlat, solutionFlat);
}
