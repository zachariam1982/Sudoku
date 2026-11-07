/// <summary>Stores Thermo Sudoku sessions in files separate from other variants.</summary>
public sealed class ThermoSaveSystem : JsonGameSaveSystem
{
    protected override string VariantName => "Thermo";

    protected override string GetFileName(SaveSlot slot)
    {
        if (slot == SaveSlot.NewGame) return "thermo_new_game_save.json";
        if (slot == SaveSlot.DailyChallenge) return "thermo_daily_challenge_save.json";
        return "thermo_save.json";
    }

    // Keep the array-only contract for base compatibility; Thermo saves use the
    // SaveGameData overload below so thermometer ordering and layout are validated too.
    protected override bool IsValidSavedPuzzle(int[] puzzleFlat, int[] solutionFlat) =>
        ThermoSudokuModel.IsValidSavedPuzzle(puzzleFlat, solutionFlat);

    protected override bool IsValidSavedPuzzle(SaveGameData data, SaveSlot slot) =>
        ThermoSudokuModel.IsValidSavedPuzzle(
            data.OriginalPuzzleFlat,
            data.SolutionFlat,
            ThermoSudokuModel.DeserializeThermometers(data.Thermometers));
}
