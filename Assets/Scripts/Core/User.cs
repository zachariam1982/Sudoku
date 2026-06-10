using System.Collections;
using System.Threading;
using UnityEngine;

public class User : MonoBehaviour
{
    public static User Instance { get; private set;}
    public SudokuViewModel ViewModel { get; set; }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
 
        Instance = this;
        DontDestroyOnLoad(gameObject);  // survive scene reloads
 
        GameDatabase.Init(); 
        StartCoroutine(SaveLoop());
    }
    public bool TryLoadSave()
    {
        if (!SaveSystem.HasSave()) return false;
 
        SaveGameData data = SaveSystem.Load();
        if (data == null) return false;
 
        ViewModel?.LoadSaveData(data);
        //ViewModel?.ResetPuzzle();
        return true;
    }
    private IEnumerator SaveLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(5f);
            TrySave();
        }
    }
    private void TrySave()
    {
        if (ViewModel == null)
        {
            Debug.LogWarning("[User] SaveLoop fired but ViewModel is not assigned.");
            return;
        }
 
        SaveGameData data = ViewModel.GetSaveData();
        SaveSystem.Save(data);
    }
}
