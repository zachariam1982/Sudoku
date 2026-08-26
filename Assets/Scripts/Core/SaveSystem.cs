using System.IO;
using UnityEngine;

public static class SaveSystem
{
    private const string FileName = "save.json";
    #if UNITY_WEBGL && !UNITY_EDITOR
    private const string WebGLSaveKey = "sudoku_current_game";
    #endif

    private static string FilePath =>
        Path.Combine(Application.persistentDataPath, FileName);

    public static void Save(SaveGameData data)
    {
        try
        {
            string json = JsonUtility.ToJson(data, prettyPrint: false);
            #if UNITY_WEBGL && !UNITY_EDITOR
                PlayerPrefs.SetString(WebGLSaveKey, json);
                PlayerPrefs.Save();
            #else
                File.WriteAllText(FilePath, json);
            #endif
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SaveSystem] Save failed: {ex.Message}");
        }
    }

    public static SaveGameData Load()
    {
        try
        {
            #if UNITY_WEBGL && !UNITY_EDITOR
            if (!PlayerPrefs.HasKey(WebGLSaveKey)) return null;

            string json = PlayerPrefs.GetString(WebGLSaveKey);
            #else
            if (!File.Exists(FilePath)) return null;

            string json = File.ReadAllText(FilePath);
            #endif

            if(string.IsNullOrEmpty(json)) return null;

            return JsonUtility.FromJson<SaveGameData>(json);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[SaveSystem] Load failed (corrupt save?): {ex.Message}");
            return null;
        }
    }
    public static bool HasSave()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
        return PlayerPrefs.HasKey(WebGLSaveKey);
        #else
        return File.Exists(FilePath);
        #endif
    }
    public static void Delete()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR

        if (PlayerPrefs.HasKey(WebGLSaveKey))
        {
            PlayerPrefs.DeleteKey(WebGLSaveKey);
            PlayerPrefs.Save();
        }

        #else
        if (File.Exists(FilePath)) File.Delete(FilePath);
        #endif
    }
}