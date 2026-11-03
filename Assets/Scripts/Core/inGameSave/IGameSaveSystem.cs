/// <summary>Reads and writes the saved state of an in-progress game.</summary>
public interface IGameSaveSystem
{
    void Save(SaveGameData data, SaveSlot slot);
    SaveGameData Load(SaveSlot slot);
    bool HasSave(SaveSlot slot);
    void Delete(SaveSlot slot);
    bool IsValidSave(SaveGameData data, SaveSlot slot);
}
