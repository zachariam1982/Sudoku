/// <summary>Stores Diagonal Sudoku sessions separately and validates diagonal rules.</summary>
public sealed class DiagonalSaveSystem : JsonGameSaveSystem
{
    protected override string VariantName => "Diagonal";

    protected override string GetFileName(SaveSlot slot)
    {
        if (slot == SaveSlot.NewGame) return "diagonal_new_game_save.json";
        if (slot == SaveSlot.DailyChallenge) return "diagonal_daily_challenge_save.json";
        return "diagonal_save.json";
    }

    protected override bool IsValidSavedPuzzle(int[] puzzleFlat, int[] solutionFlat) =>
        DiagonalSudokuModel.IsValidSavedPuzzle(puzzleFlat, solutionFlat);
}
