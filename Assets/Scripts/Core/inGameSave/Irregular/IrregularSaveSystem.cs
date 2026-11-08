/// <summary>Stores Irregular Sudoku sessions separately and validates irregular rules.</summary>
public sealed class IrregularSaveSystem : JsonGameSaveSystem
{
    protected override string VariantName => "Irregular";

    protected override string GetFileName(SaveSlot slot)
    {
        if (slot == SaveSlot.NewGame) return "irregular_new_game_save.json";
        if (slot == SaveSlot.DailyChallenge) return "irregular_daily_challenge_save.json";
        return "irregular_save.json";
    }

    protected override bool IsValidSavedPuzzle(int[] puzzleFlat, int[] solutionFlat) =>
        IrregularSudokuModel.IsValidSavedPuzzle(puzzleFlat, solutionFlat);
    protected override bool IsValidSavedPuzzle(SaveGameData data, SaveSlot slot)
    {
        return IrregularSudokuModel.IsValidSavedPuzzle(data.OriginalPuzzleFlat, data.SolutionFlat,
            data.PuzzleSeed != 0 ? data.PuzzleSeed : data.Level, data.IrregularLayoutVersion);
    }
}
