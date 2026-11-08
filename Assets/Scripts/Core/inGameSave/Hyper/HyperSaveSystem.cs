/// <summary>Stores Hyper Sudoku sessions separately and validates hyper rules.</summary>
public sealed class HyperSaveSystem : JsonGameSaveSystem
{
    protected override string VariantName => "Hyper";

    protected override string GetFileName(SaveSlot slot)
    {
        if (slot == SaveSlot.NewGame) return "hyper_new_game_save.json";
        if (slot == SaveSlot.DailyChallenge) return "hyper_daily_challenge_save.json";
        return "hyper_save.json";
    }

    protected override bool IsValidSavedPuzzle(int[] puzzleFlat, int[] solutionFlat) =>
        HyperSudokuModel.IsValidSavedPuzzle(puzzleFlat, solutionFlat);
}
