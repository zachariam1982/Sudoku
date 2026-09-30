using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Scrolls a randomly selected Gospel verse while the gameplay HUD is visible.
/// </summary>
[DisallowMultipleComponent]
public sealed class VerseTicker : MonoBehaviour
{
    [Header("Unity-authored view")]
    [SerializeField] private RectTransform viewport;
    [SerializeField] private RectTransform textRect;
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private GameObject hudRoot;

    [Header("Ticker")]
    [SerializeField] private string verseText =
        "John 3:16 (KJV) — For God so loved the world, that he gave his only begotten Son, that whosoever believeth in him should not perish, but have everlasting life.";
    [SerializeField] private float scrollSpeed = 28f;
    [SerializeField] private float textPadding = 10f;

    private const string BibleApiBaseUrl =
        "https://cdn.jsdelivr.net/gh/wldeh/bible-api@main/bibles/en-kjv/books";

    private static readonly Gospel[] Gospels =
    {
        new Gospel("Matthew", "matthew", 28),
        new Gospel("Mark", "mark", 16),
        new Gospel("Luke", "luke", 24),
        new Gospel("John", "john", 21)
    };

    private bool _gameActive;
    private bool _hasLoadedVerse;
    private bool _isLoadingVerse;
    private float _start;
    private float _end;

    [System.Serializable]
    private sealed class ChapterResponse
    {
        public VerseData[] data;
    }

    [System.Serializable]
    private sealed class VerseData
    {
        public string verse;
        public string text;
    }

    private struct Gospel
    {
        public readonly string displayName;
        public readonly string apiName;
        public readonly int chapterCount;

        public Gospel(string displayName, string apiName, int chapterCount)
        {
            this.displayName = displayName;
            this.apiName = apiName;
            this.chapterCount = chapterCount;
        }
    }

    private void Awake()
    {
        RefreshVisibility(true);
    }

    public bool IsGameActive => _gameActive;

    public void SetGameActive(bool active)
    {
        _gameActive = active;
        RefreshVisibility(true);

        if (active && !_hasLoadedVerse && !_isLoadingVerse)
            StartCoroutine(LoadRandomGospelVerse());
    }

    private void Update()
    {
        bool hudVisible = hudRoot != null && hudRoot.activeInHierarchy;
        bool shouldBeVisible = _gameActive && hudVisible;

        if (viewport != null && viewport.gameObject.activeSelf != shouldBeVisible)
            RefreshVisibility(true);

        if (!shouldBeVisible || viewport == null || textRect == null) return;

        Vector2 position = textRect.anchoredPosition;
        position.x -= scrollSpeed * Time.unscaledDeltaTime;
        if (position.x <= _end) position.x = _start;
        textRect.anchoredPosition = position;
    }

    private IEnumerator LoadRandomGospelVerse()
    {
        _isLoadingVerse = true;

        // Retry with a fresh random chapter if the API request fails.
        for (int attempt = 0; attempt < 3 && !_hasLoadedVerse; attempt++)
        {
            Gospel gospel = Gospels[Random.Range(0, Gospels.Length)];
            int chapter = Random.Range(1, gospel.chapterCount + 1);
            string url = $"{BibleApiBaseUrl}/{gospel.apiName}/chapters/{chapter}.json";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = 12;
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    ChapterResponse response =
                        JsonUtility.FromJson<ChapterResponse>(request.downloadHandler.text);

                    if (response != null && response.data != null && response.data.Length > 0)
                    {
                        VerseData verse = response.data[Random.Range(0, response.data.Length)];
                        if (verse != null && !string.IsNullOrWhiteSpace(verse.text))
                        {
                            verseText = $"{gospel.displayName} {chapter}:{verse.verse} (KJV) — {verse.text.Trim()}";
                            _hasLoadedVerse = true;
                            RefreshVisibility(true);
                            break;
                        }
                    }
                }

                Debug.LogWarning($"VerseTicker: Bible API request failed ({request.error}).");
            }
        }

        _isLoadingVerse = false;
    }

    private void RefreshVisibility(bool resetPosition)
    {
        bool visible = _gameActive && hudRoot != null && hudRoot.activeInHierarchy;
        if (viewport != null)
            viewport.gameObject.SetActive(visible);

        if (viewport == null || textRect == null || label == null) return;

        label.text = verseText;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.ForceMeshUpdate();

        float width = label.preferredWidth;
        textRect.sizeDelta = new Vector2(width, viewport.rect.height - 2f * textPadding);
        _start = viewport.rect.width * 0.5f + width * 0.5f;
        _end = -viewport.rect.width * 0.5f - width * 0.5f;

        if (resetPosition)
            textRect.anchoredPosition = new Vector2(_start, 0f);
    }
}
