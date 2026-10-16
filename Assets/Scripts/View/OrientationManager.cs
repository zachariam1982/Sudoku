using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Allows free rotation. Whenever the device orientation changes,
/// it recalculates the grid size for the new orientation and tells
/// GridBuilder to rebuild the board.
/// 
/// Attach this to the Canvas alongside GridBuilder.
/// </summary>
public class OrientationManager : MonoBehaviour
{
    [Header("Bar heights in Canvas reference units")]
    [SerializeField] private float topBarHeight    = 160f;
    [SerializeField] private float bottomBarHeight = 160f;
    [SerializeField] private float screenPadding   = 8f;

    [Header("References")]
    [SerializeField] private GridBuilder gridBuilder;
    [SerializeField] private Sprite landscapeBackground;
    [SerializeField] private Sprite portraitBackground;
    [SerializeField] private Image[] backgroundImages;

    private RectTransform canvasRect;
    private Canvas canvas;
    private Vector2 lastScreenSize;
    private Vector2 lastCanvasSize;
    private Rect lastSafeArea;
    private bool layoutReady;
    private int skySeed = 1;
    private Texture2D landscapeSkyTexture;
    private Texture2D portraitSkyTexture;
    private Sprite landscapeCompositeSprite;
    private Sprite portraitCompositeSprite;

    void Start()
    {
        // Allow all rotations
        Screen.autorotateToPortrait           = true;
        Screen.autorotateToPortraitUpsideDown = true;
        Screen.autorotateToLandscapeLeft      = true;
        Screen.autorotateToLandscapeRight     = true;
        Screen.orientation = ScreenOrientation.AutoRotation;

        canvas = GetComponent<Canvas>();
        canvasRect = GetComponent<RectTransform>();

        // Build for the current orientation immediately
        RebuildForCurrentOrientation();
    }

    void LateUpdate()
    {
        // AutoRotation and Editor window resizing need not change the orientation
        // enum. Android may also deliver the new dimensions on a later frame.
        if (!layoutReady ||
            lastScreenSize != new Vector2(Screen.width, Screen.height) ||
            lastCanvasSize != canvasRect.rect.size ||
            lastSafeArea != Screen.safeArea)
        {
            RebuildForCurrentOrientation();
        }
    }

    private void UpdateBackgroundImages()
    {
        bool portrait = Screen.height >= Screen.width;
        Sprite foreground = portrait ? portraitBackground : landscapeBackground;
        if (foreground == null || backgroundImages == null) return;

        Sprite composite = GetCompositeSprite(foreground, portrait);
        foreach (Image image in backgroundImages)
        {
            if (image == null) continue;
            if (image.sprite != composite)
                image.sprite = composite;
            image.color = Color.white;
            image.material = null;
        }
    }

    private Sprite GetCompositeSprite(Sprite foreground, bool portrait)
    {
        Sprite cachedSprite = portrait ? portraitCompositeSprite : landscapeCompositeSprite;
        if (cachedSprite != null) return cachedSprite;

        Texture2D texture = CreateCompositeTexture(foreground, portrait);
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(
                foreground.pivot.x / foreground.rect.width,
                foreground.pivot.y / foreground.rect.height),
            foreground.pixelsPerUnit,
            0,
            SpriteMeshType.FullRect);

        if (portrait)
        {
            portraitSkyTexture = texture;
            portraitCompositeSprite = sprite;
        }
        else
        {
            landscapeSkyTexture = texture;
            landscapeCompositeSprite = sprite;
        }

        return sprite;
    }

    private Texture2D CreateCompositeTexture(Sprite foreground, bool portrait)
    {
        Texture2D source = foreground.texture;
        Rect sourceRect = foreground.textureRect;
        int width = Mathf.RoundToInt(foreground.rect.width);
        int height = Mathf.RoundToInt(foreground.rect.height);
        Color32[] sourcePixels = source.GetPixels32();
        int sourceWidth = source.width;
        Color32[] skyPixels = GenerateSkyPixels(portrait ? 288 : 512, portrait ? 512 : 288, portrait);
        int skyWidth = portrait ? 288 : 512;
        int skyHeight = portrait ? 512 : 288;
        Color32[] output = new Color32[width * height];

        for (int y = 0; y < height; y++)
        {
            int sourceY = Mathf.Clamp(
                Mathf.RoundToInt(sourceRect.y + (y + 0.5f) * sourceRect.height / height - 0.5f),
                0, source.height - 1);
            int skyY = Mathf.Clamp(y * skyHeight / height, 0, skyHeight - 1);
            for (int x = 0; x < width; x++)
            {
                int sourceX = Mathf.Clamp(
                    Mathf.RoundToInt(sourceRect.x + (x + 0.5f) * sourceRect.width / width - 0.5f),
                    0, source.width - 1);
                Color32 foregroundPixel = sourcePixels[sourceY * sourceWidth + sourceX];
                Color32 skyPixel = skyPixels[skyY * skyWidth + Mathf.Clamp(x * skyWidth / width, 0, skyWidth - 1)];
                int alpha = foregroundPixel.a;
                int inverseAlpha = 255 - alpha;
                int index = y * width + x;
                output[index] = new Color32(
                    (byte)((foregroundPixel.r * alpha + skyPixel.r * inverseAlpha) / 255),
                    (byte)((foregroundPixel.g * alpha + skyPixel.g * inverseAlpha) / 255),
                    (byte)((foregroundPixel.b * alpha + skyPixel.b * inverseAlpha) / 255),
                    255);
            }
        }

        Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
        result.name = portrait ? "ProceduralNightSky-Portrait" : "ProceduralNightSky-Landscape";
        result.SetPixels32(output);
        result.Apply(false, true);
        return result;
    }

    private Color32[] GenerateSkyPixels(int width, int height, bool portrait)
    {
        Color32[] pixels = new Color32[width * height];
        float seedOffset = (skySeed % 10007) * 0.0037f;
        float[] milkyWayCenter = new float[width];

        for (int x = 0; x < width; x++)
        {
            float u = x / (float)(width - 1);
            milkyWayCenter[x] = 0.67f + 0.12f * Mathf.Sin(u * 5.2f + seedOffset * 1.7f);
        }

        for (int y = 0; y < height; y++)
        {
            float v = y / (float)(height - 1);
            float horizon = 1f - Mathf.Clamp01((v - 0.16f) / 0.74f);
            for (int x = 0; x < width; x++)
            {
                float u = x / (float)(width - 1);
                float distance = v - milkyWayCenter[x];
                float band = Mathf.Exp(-distance * distance * 115f);
                float cloud = Mathf.PerlinNoise(u * 5.2f + seedOffset, v * 7.1f - seedOffset * 0.61f);
                float detail = Mathf.PerlinNoise(u * 17f - seedOffset * 0.5f, v * 21f + seedOffset * 0.8f);
                float nebula = band * (0.28f + 0.48f * cloud + 0.24f * detail);
                float glow = band * band * 0.18f;

                float r = 3f + horizon * 5f + nebula * 38f + glow * 19f;
                float g = 5f + horizon * 6f + nebula * 29f + glow * 21f;
                float b = 13f + horizon * 16f + nebula * 64f + glow * 46f;
                pixels[y * width + x] = new Color32(
                    (byte)Mathf.Clamp(r, 0f, 255f),
                    (byte)Mathf.Clamp(g, 0f, 255f),
                    (byte)Mathf.Clamp(b, 0f, 255f),
                    255);
            }
        }

        DrawStars(pixels, width, height);
        DrawMoon(pixels, width, height, portrait);
        return pixels;
    }

    private void DrawStars(Color32[] pixels, int width, int height)
    {
        System.Random random = new System.Random(skySeed);
        int starCount = width * height / 390;
        for (int i = 0; i < starCount; i++)
        {
            int x = random.Next(width);
            int y = random.Next(height);
            int radius = random.Next(1, 4);
            byte brightness = (byte)random.Next(135, 256);
            Color32 color = random.Next(5) == 0
                ? new Color32(brightness, (byte)(brightness * 0.88f), (byte)(brightness * 0.70f), 255)
                : new Color32((byte)(brightness * 0.72f), (byte)(brightness * 0.82f), brightness, 255);
            DrawGlow(pixels, width, height, x, y, radius, color, 1f);
            if (radius == 3)
            {
                DrawGlow(pixels, width, height, x - 2, y, 1, color, 0.55f);
                DrawGlow(pixels, width, height, x + 2, y, 1, color, 0.55f);
                DrawGlow(pixels, width, height, x, y - 2, 1, color, 0.55f);
                DrawGlow(pixels, width, height, x, y + 2, 1, color, 0.55f);
            }
        }
    }

    private void DrawMoon(Color32[] pixels, int width, int height, bool portrait)
    {
        float u = portrait ? 0.42f : 0.86f;
        float v = portrait ? 0.84f : 0.78f;
        int centerX = Mathf.RoundToInt(u * (width - 1));
        int centerY = Mathf.RoundToInt(v * (height - 1));
        int radius = Mathf.RoundToInt(height * 0.037f);
        int glowRadius = radius * 6;

        for (int y = centerY - glowRadius; y <= centerY + glowRadius; y++)
        {
            for (int x = centerX - glowRadius; x <= centerX + glowRadius; x++)
            {
                if (x < 0 || x >= width || y < 0 || y >= height) continue;
                float dx = (x - centerX) / (float)radius;
                float dy = (y - centerY) / (float)radius;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                int index = y * width + x;

                if (distance <= 1f)
                {
                    float crater = Mathf.PerlinNoise(dx * 7f + 4.1f, dy * 7f + 2.8f);
                    Color32 moon = new Color32(
                        (byte)(208f + crater * 36f),
                        (byte)(216f + crater * 34f),
                        (byte)(231f + crater * 24f),
                        255);
                    pixels[index] = moon;
                }
                else if (distance <= 6f)
                {
                    float alpha = Mathf.Pow(1f - distance / 6f, 2f) * 0.48f;
                    pixels[index] = Blend(pixels[index], new Color32(112, 146, 222, 255), alpha);
                }
            }
        }
    }

    private static void DrawGlow(Color32[] pixels, int width, int height, int centerX, int centerY, int radius, Color32 color, float strength)
    {
        for (int y = centerY - radius; y <= centerY + radius; y++)
        {
            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                if (x < 0 || x >= width || y < 0 || y >= height) continue;
                float distance = Mathf.Sqrt((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY));
                if (distance > radius) continue;
                float alpha = (1f - distance / (radius + 1f)) * strength;
                int index = y * width + x;
                pixels[index] = Blend(pixels[index], color, alpha);
            }
        }
    }

    private static Color32 Blend(Color32 background, Color32 foreground, float alpha)
    {
        float inverse = 1f - alpha;
        return new Color32(
            (byte)(background.r * inverse + foreground.r * alpha),
            (byte)(background.g * inverse + foreground.g * alpha),
            (byte)(background.b * inverse + foreground.b * alpha),
            255);
    }

    /// <summary>Creates a fresh pattern for each newly started New Game.</summary>
    public void GenerateNightSky()
    {
        skySeed = Random.Range(1, 1000000);
        ClearGeneratedBackgrounds();
        UpdateBackgroundImages();
    }

    private void ClearGeneratedBackgrounds()
    {
        if (landscapeCompositeSprite != null) Destroy(landscapeCompositeSprite);
        if (portraitCompositeSprite != null) Destroy(portraitCompositeSprite);
        if (landscapeSkyTexture != null) Destroy(landscapeSkyTexture);
        if (portraitSkyTexture != null) Destroy(portraitSkyTexture);
        landscapeCompositeSprite = null;
        portraitCompositeSprite = null;
        landscapeSkyTexture = null;
        portraitSkyTexture = null;
    }

    /// <summary>
    /// Calculates the grid size for the current screen dimensions
    /// and triggers a full grid rebuild.
    /// </summary>
    public void RebuildForCurrentOrientation()
    {
        if (gridBuilder == null || canvas == null || canvasRect == null ||
            Screen.width <= 0 || Screen.height <= 0) return;

        UpdateBackgroundImages();
        Canvas.ForceUpdateCanvases();
        Vector2 size = canvasRect.rect.size;
        if (size.x <= 0f || size.y <= 0f || canvas.scaleFactor <= 0f) return;

        Rect safeArea = Screen.safeArea;
        float unsafeWidth = Screen.width - safeArea.width;
        float unsafeHeight = Screen.height - safeArea.height;
        float gridSize = CalculateGridSize(
            size.x - unsafeWidth / canvas.scaleFactor,
            size.y - unsafeHeight / canvas.scaleFactor);

        using (new Benchmark("Grid rebuild")){
            gridBuilder.Rebuild(gridSize);
        }

        lastScreenSize = new Vector2(Screen.width, Screen.height);
        lastCanvasSize = size;
        lastSafeArea = safeArea;
        layoutReady = true;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[M-01 Layout] screen={Screen.width}x{Screen.height}, " +
                  $"canvas={size}, safeArea={safeArea}, grid={gridSize}, " +
                  $"orientation={Screen.orientation}");
#endif
    }

    /// <summary>
    /// Converts screen pixels to canvas units and returns the largest
    /// square grid that fits between the top and bottom bars.
    /// </summary>
    private float CalculateGridSize(float canvasW, float canvasH)
    {
        float available = Mathf.Min(
            canvasW  - 2f * screenPadding,
            canvasH  - topBarHeight - bottomBarHeight - 2f * screenPadding
        );

        return Mathf.Max(available, 1f);
    }

    private void OnDestroy()
    {
        ClearGeneratedBackgrounds();
    }
}
