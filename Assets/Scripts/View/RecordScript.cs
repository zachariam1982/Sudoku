using System;
using Unity.VisualScripting;
using UnityEngine;
using TMPro;

public class RecordScript : MonoBehaviour
{
    [Header("DB Values")]
    [SerializeField] private TextMeshProUGUI Level;
    [SerializeField] private TextMeshProUGUI Difficulty;
    [SerializeField] private TextMeshProUGUI Points;
    [SerializeField] private TextMeshProUGUI Restart;

    public void Setup(string arg1, string arg2, string arg3)
    {
        Level.text = arg1;
        Difficulty.text = arg2;
        Points.text = arg3;
    }
}
