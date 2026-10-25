using UnityEngine;

[CreateAssetMenu(fileName = "DailyChallengeResultArt", menuName = "Daily Challenge/Result Art")]
public sealed class DailyChallengeResultArt : ScriptableObject
{
    public Sprite winLandscape;
    public Sprite winPortrait;
    public Sprite loseLandscape;
    public Sprite losePortrait;
}
