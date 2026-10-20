using UnityEngine;

/// <summary>Controls the first, ready-to-play page of the Daily Challenge screen.</summary>
public sealed class DailyChallengeScreenController : MonoBehaviour
{
    [SerializeField] private GameObject readyPage;
    [SerializeField] private GameObject portraitLayout;
    [SerializeField] private GameObject landscapeLayout;

    public void Show()
    {
        gameObject.SetActive(true);
        if (readyPage != null) readyPage.SetActive(true);
    }

    public void Hide() => gameObject.SetActive(false);

    public void SetLandscape(bool landscape)
    {
        if (portraitLayout != null) portraitLayout.SetActive(!landscape);
        if (landscapeLayout != null) landscapeLayout.SetActive(landscape);
    }
}
