using System.Collections.Generic;
using UnityEngine;

public class PlayerSettings : MonoBehaviour
{
    public static PlayerSettings Instance
    {
        get;
        private set;
    }

    public static readonly string PlayerID = "player_id";

    public Dictionary<string, object> Dict { get; private set; }

    public int Version { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Version = 0;
            Dict = new Dictionary< string, object>();
        }

        if (!PlayerPrefs.HasKey(PlayerID))
        {
            string newId = System.Guid.NewGuid().ToString();

            PlayerPrefs.SetString( PlayerID, newId);
            PlayerPrefs.Save();
        }
    }
}