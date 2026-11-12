using UnityEditor;
using UnityEngine;

/// <summary>Resets the first-run tutorial preference for Editor testing.</summary>
public static class BeginnerTutorialResetCommand
{
    [MenuItem("Tools/Tutorial/Reset Beginner Tutorial")]
    public static void ResetBeginnerTutorial()
    {
        PlayerPrefs.DeleteKey(BeginnerTutorialOverlay.TutorialSeenPreferenceKey);
        PlayerPrefs.Save();

        Debug.Log("[BeginnerTutorial] Reset. The tutorial will appear the next time the variant selector opens.");
        EditorUtility.DisplayDialog(
            "Beginner Tutorial Reset",
            "The tutorial will appear the next time the variant selector opens.",
            "OK");
    }

    [MenuItem("Tools/Tutorial/Reset Game Mode Tutorial")]
    public static void ResetGameModeTutorial()
    {
        PlayerPrefs.DeleteKey(GameModeTutorialOverlay.SeenPreferenceKey);
        PlayerPrefs.Save();

        Debug.Log("[GameModeTutorial] Reset. The mode tutorial will appear after a variant is selected.");
        EditorUtility.DisplayDialog(
            "Game Mode Tutorial Reset",
            "The mode tutorial will appear after the next variant selection.",
            "OK");
    }
}
