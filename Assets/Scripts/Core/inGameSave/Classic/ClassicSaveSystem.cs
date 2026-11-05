public sealed class ClassicSaveSystem : JsonGameSaveSystem
{
    protected override string VariantName => "Classic";

    protected override string GetFileName(SaveSlot slot)
    {
        if (slot == SaveSlot.NewGame) return "new_game_save.json";
        if (slot == SaveSlot.DailyChallenge) return "daily_challenge_save.json";
        return "save.json";
    }

    protected override bool IsValidSavedPuzzle(int[] puzzleFlat, int[] solutionFlat) =>
        ClassicSudokuModel.IsValidSavedPuzzle(puzzleFlat, solutionFlat);

    // Existing Classic saves were written before SaveVariant was serialized.
    protected override bool AllowsLegacyMissingVariant => true;

    protected override bool AllowsLegacyMissingPuzzle(SaveGameData data, SaveSlot slot) =>
        data.SaveVersion == 0 && slot == SaveSlot.Journey;

    protected override bool AllowsLegacyEmptyPuzzle(SaveGameData data, SaveSlot slot) =>
        data.SaveVersion == 0 && slot == SaveSlot.Journey;
}
