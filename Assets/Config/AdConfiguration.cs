using UnityEngine;

[CreateAssetMenu(menuName = "Sudoku/Ad Configuration")]
public class AdConfiguration : ScriptableObject
{
    public string androidAppKey;
    public string iosAppKey;
    public string rewardedAdUnitId;
}