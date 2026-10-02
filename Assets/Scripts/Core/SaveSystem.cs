using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public enum SaveSlot
{
    Journey,
    NewGame
}

public static class SaveSystem
{
    private const string JourneyFileName = "save.json";
    private const string NewGameFileName = "new_game_save.json";
#if UNITY_WEBGL && !UNITY_EDITOR
    private const string JourneyKey = "sudoku_current_game";
    private const string NewGameKey = "sudoku_new_game";
#endif

    private static string GetFilePath(SaveSlot slot) => Path.Combine(
        Application.persistentDataPath,
        slot == SaveSlot.NewGame ? NewGameFileName : JourneyFileName);

    private static string GetMode(SaveSlot slot) =>
        slot == SaveSlot.NewGame ? "NewGame" : "Journey";

    public static void Save(SaveGameData data) => Save(data, SaveSlot.Journey);

    public static void Save(SaveGameData data, SaveSlot slot)
    {
        if (!IsValidSave(data, slot))
        {
            Debug.LogWarning($"[SaveSystem] Refused invalid {GetMode(slot)} save.");
            return;
        }

        data.SaveVersion = 2;
        data.SaveMode = GetMode(slot);

        try
        {
            string json = JsonUtility.ToJson(data, prettyPrint: false);
#if UNITY_WEBGL && !UNITY_EDITOR
            string key = GetPlayerPrefsKey(slot);
            string tempKey = key + ".tmp";
            string backupKey = key + ".bak";
            PlayerPrefs.SetString(tempKey, json);
            PlayerPrefs.Save();
            if (PlayerPrefs.HasKey(key))
                PlayerPrefs.SetString(backupKey, PlayerPrefs.GetString(key));
            PlayerPrefs.SetString(key, PlayerPrefs.GetString(tempKey));
            PlayerPrefs.DeleteKey(tempKey);
            PlayerPrefs.Save();
#else
            string path = GetFilePath(slot);
            string tempPath = path + ".tmp";
            string backupPath = path + ".bak";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(tempPath, json);

            if (File.Exists(path))
            {
                try
                {
                    if (File.Exists(backupPath)) File.Delete(backupPath);
                    File.Replace(tempPath, path, backupPath);
                }
                catch (NotSupportedException)
                {
                    if (File.Exists(tempPath)) ReplaceWithBackup(tempPath, path, backupPath);
                }
                catch (IOException)
                {
                    if (File.Exists(tempPath)) ReplaceWithBackup(tempPath, path, backupPath);
                }
            }
            else
            {
                File.Move(tempPath, path);
            }
#endif
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveSystem] {GetMode(slot)} save failed: {ex.Message}");
        }
    }

    private static void ReplaceWithBackup(string tempPath, string path, string backupPath)
    {
        if (File.Exists(backupPath)) File.Delete(backupPath);
        if (File.Exists(path)) File.Copy(path, backupPath);
        if (File.Exists(path)) File.Delete(path);
        File.Move(tempPath, path);
    }

    public static SaveGameData Load() => Load(SaveSlot.Journey);

    public static SaveGameData Load(SaveSlot slot)
    {
        foreach (string candidate in GetCandidates(slot))
        {
            SaveGameData data = TryRead(candidate, slot);
            if (data == null) continue;
            PromoteRecoveredCandidate(candidate, slot);
            return data;
        }
        return null;
    }

    public static bool HasSave() => HasSave(SaveSlot.Journey);

    public static bool HasSave(SaveSlot slot)
    {
        foreach (string candidate in GetCandidates(slot))
            if (TryRead(candidate, slot) != null) return true;
        return false;
    }

    public static void Delete() => Delete(SaveSlot.Journey);

    public static void Delete(SaveSlot slot)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        string key = GetPlayerPrefsKey(slot);
        PlayerPrefs.DeleteKey(key);
        PlayerPrefs.DeleteKey(key + ".tmp");
        PlayerPrefs.DeleteKey(key + ".bak");
        PlayerPrefs.Save();
#else
        string path = GetFilePath(slot);
        DeleteIfExists(path);
        DeleteIfExists(path + ".tmp");
        DeleteIfExists(path + ".bak");
#endif
    }

    public static bool IsValidSave(SaveGameData data, SaveSlot slot)
    {
        if (data == null || data.BoardFlat == null || data.BoardFlat.Length != 81 ||
            data.PencilCandidateMasksFlat == null || data.PencilCandidateMasksFlat.Length != 81)
            return false;
        if (data.SaveVersion < 0 || data.SaveVersion > 2 ||
            data.Difficulty < 0 || data.Difficulty > 8 || data.Level < 1)
            return false;
        if (float.IsNaN(data.ElapsedSeconds) || float.IsInfinity(data.ElapsedSeconds) ||
            data.ElapsedSeconds < 0f || data.LivesRemaining < 0)
            return false;
        if (slot == SaveSlot.NewGame && data.SaveMode != "NewGame") return false;
        if (!string.IsNullOrEmpty(data.SaveMode) && data.SaveMode != GetMode(slot)) return false;

        for (int i = 0; i < 81; i++)
        {
            if (data.BoardFlat[i] < 0 || data.BoardFlat[i] > 9 ||
                data.PencilCandidateMasksFlat[i] < 0 || data.PencilCandidateMasksFlat[i] > 0x3FE)
                return false;
        }
        if (!IsValidHistory(data.UndoHistory) || !IsValidHistory(data.RedoHistory)) return false;

        bool puzzleMissing = data.OriginalPuzzleFlat == null && data.SolutionFlat == null;
        if (puzzleMissing)
            return data.SaveVersion == 0 && slot == SaveSlot.Journey;

        if (data.OriginalPuzzleFlat == null || data.SolutionFlat == null ||
            data.OriginalPuzzleFlat.Length != 81 || data.SolutionFlat.Length != 81)
            return false;
        if (data.SaveVersion == 0 && slot == SaveSlot.Journey &&
            IsAllZero(data.OriginalPuzzleFlat) && IsAllZero(data.SolutionFlat))
            return true;

        return SudokuModel.IsValidSavedPuzzle(data.OriginalPuzzleFlat, data.SolutionFlat);
    }

    private static bool IsAllZero(int[] values)
    {
        if (values == null) return true;
        for (int i = 0; i < values.Length; i++)
            if (values[i] != 0) return false;
        return true;
    }

    private static bool IsValidHistory(List<SaveGameHistoryEntry> history)
    {
        if (history == null) return true;
        foreach (SaveGameHistoryEntry entry in history)
        {
            if (entry == null) return false;
            bool cellChange = entry.Row >= 0 && entry.Row < 9 && entry.Col >= 0 && entry.Col < 9;
            bool candidatesOnly = entry.Row == -1 && entry.Col == -1;
            if ((!cellChange && !candidatesOnly) || entry.BeforeValue < 0 || entry.BeforeValue > 9 ||
                entry.AfterValue < 0 || entry.AfterValue > 9)
                return false;

            if (entry.CandidateChanges == null) continue;
            foreach (SaveGameCandidateChange change in entry.CandidateChanges)
                if (change == null || change.Row < 0 || change.Row >= 9 ||
                    change.Col < 0 || change.Col >= 9 || change.BeforeMask < 0 ||
                    change.BeforeMask > 0x3FE || change.AfterMask < 0 || change.AfterMask > 0x3FE)
                    return false;
        }
        return true;
    }

    private static SaveGameData TryRead(string candidate, SaveSlot slot)
    {
        try
        {
            string json = ReadCandidate(candidate);
            if (string.IsNullOrEmpty(json) || !json.Contains("\"BoardFlat\"")) return null;
            SaveGameData data = JsonUtility.FromJson<SaveGameData>(json);
            return IsValidSave(data, slot) ? data : null;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SaveSystem] Ignoring unreadable {GetMode(slot)} save: {ex.Message}");
            return null;
        }
    }

    private static string[] GetCandidates(SaveSlot slot)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        string key = GetPlayerPrefsKey(slot);
        return new[] { key, key + ".tmp", key + ".bak" };
#else
        string path = GetFilePath(slot);
        return new[] { path, path + ".tmp", path + ".bak" };
#endif
    }

    private static string ReadCandidate(string candidate)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return PlayerPrefs.HasKey(candidate) ? PlayerPrefs.GetString(candidate) : null;
#else
        return File.Exists(candidate) ? File.ReadAllText(candidate) : null;
#endif
    }

    private static void PromoteRecoveredCandidate(string candidate, SaveSlot slot)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        string key = GetPlayerPrefsKey(slot);
        if (candidate == key) return;
        PlayerPrefs.SetString(key, PlayerPrefs.GetString(candidate));
        PlayerPrefs.Save();
#else
        string path = GetFilePath(slot);
        if (candidate == path) return;
        try
        {
            string recovered = File.ReadAllText(candidate);
            string tempPath = path + ".recover";
            File.WriteAllText(tempPath, recovered);
            if (File.Exists(path)) File.Delete(path);
            File.Move(tempPath, path);
            Debug.LogWarning($"[SaveSystem] Recovered {GetMode(slot)} save from {Path.GetFileName(candidate)}.");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SaveSystem] Could not restore recovered save: {ex.Message}");
        }
#endif
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path)) File.Delete(path);
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    private static string GetPlayerPrefsKey(SaveSlot slot) =>
        slot == SaveSlot.NewGame ? NewGameKey : JourneyKey;
#endif
}
