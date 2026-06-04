using System.IO;
using UnityEngine;

public static class SaveSystem
{
    private const string FileName = "save.json";

    private static string FilePath =>
        Path.Combine(Application.persistentDataPath, FileName);

    public static void Save(SaveGameData data)
    {
        try
        {
            string json = JsonUtility.ToJson(data, prettyPrint: false);
            File.WriteAllText(FilePath, json);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SaveSystem] Save failed: {ex.Message}");
        }
    }

    public static SaveGameData Load()
    {
        if (!File.Exists(FilePath))
            return null;

        try
        {
            string json = File.ReadAllText(FilePath);
            return JsonUtility.FromJson<SaveGameData>(json);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[SaveSystem] Load failed (corrupt save?): {ex.Message}");
            return null;
        }
    }
    public static bool HasSave() => File.Exists(FilePath);
    public static void Delete()
    {
        if (File.Exists(FilePath))
            File.Delete(FilePath);
    }
}