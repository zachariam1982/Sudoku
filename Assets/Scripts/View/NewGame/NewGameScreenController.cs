using UnityEngine;

/// <summary>Controls the serialized New Game difficulty selector.</summary>
public sealed class NewGameScreenController : MonoBehaviour
{
    [SerializeField] private HomeScreenController homeScreen;
    [SerializeField] private GameContext gameContext;

    public void SelectDifficulty(int difficultyIndex)
    {
        if (difficultyIndex < (int)SudokuDifficulty.Simple ||
            difficultyIndex > (int)SudokuDifficulty.Hardest)
        {
            return;
        }

        if (gameContext == null)
        {
            Debug.LogError("NewGameScreenController requires a GameContext reference.", this);
            return;
        }

        gameContext.ActivateNewGame((SudokuDifficulty)difficultyIndex);
        gameObject.SetActive(false);
    }

    public void Show() => gameObject.SetActive(true);

    public void Back()
    {
        homeScreen.OpenHome();
        gameObject.SetActive(false);
    }
}
