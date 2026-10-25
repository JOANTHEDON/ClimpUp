using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[DefaultExecutionOrder(-100)]
public class GameHUDCanvas : MonoBehaviour
{
    public static GameHUDCanvas Instance { get; private set; }

    [Header("1. Altitude UI Panel")]
    [Tooltip("Background Image for Altitude panel (drop your custom sprite here)")]
    public Image altitudeBackground;
    public Text altitudeTitleText;
    public Text altitudeValueText;
    public Text altitudeBestText;

    [Header("2. Word Collection UI Panel")]
    [Tooltip("Background Image for Word Assemble panel (drop your custom sprite here)")]
    public Image wordBackground;
    public Text wordHeaderTitle;
    public Transform lettersContainer;
    public List<Text> letterTextSlots = new List<Text>();

    [Header("3. Word Completed Celebration Banner")]
    [Tooltip("Background Image for the Word Complete popup banner (drop your custom sprite here)")]
    public Image wordCompleteBannerImage;
    public Text wordCompleteText;

    [Header("4. Game Over Modal Panel")]
    [Tooltip("The entire Game Over panel GameObject")]
    public GameObject gameOverPanel;
    [Tooltip("Background Image for Game Over box (drop your custom sprite here)")]
    public Image gameOverBoxBackground;
    public Text gameOverTitleText;
    public Text gameOverSubtitleText;
    public Text gameOverScoreText;
    public Text gameOverBestText;
    public Button restartButton;
    public Button homeButton;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        EnsureEventSystem();
        AutoBuildUIIfEmpty();
    }

    private void Start()
    {
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(() =>
            {
                if (GameOverManager.Instance != null) GameOverManager.Instance.RestartGame();
            });
        }

        if (homeButton != null)
        {
            homeButton.onClick.AddListener(() =>
            {
                if (GameOverManager.Instance != null) GameOverManager.Instance.GoHome();
            });
        }

        if (wordCompleteBannerImage != null)
        {
            wordCompleteBannerImage.gameObject.SetActive(false);
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }
    }

    public void UpdateAltitude(float current, float max, float best)
    {
        if (altitudeValueText != null) altitudeValueText.text = $"{max:F1} m";
        if (altitudeBestText != null) altitudeBestText.text = $"BEST: {best:F1} m";
    }

    public void UpdateWordProgress(string word, bool[] collectedStatus, int wordsCompleted)
    {
        if (wordHeaderTitle != null)
        {
            wordHeaderTitle.text = $"ASSEMBLE WORD  •  COMPLETED: {wordsCompleted}";
        }

        if (lettersContainer == null) return;

        // Ensure we have enough letter Text objects in container
        while (letterTextSlots.Count < word.Length)
        {
            GameObject letterObj = new GameObject($"Letter_{letterTextSlots.Count}");
            letterObj.transform.SetParent(lettersContainer, false);
            Text t = letterObj.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            t.fontSize = 28;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            RectTransform rt = letterObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(36, 40);
            letterTextSlots.Add(t);
        }

        // Update slots
        for (int i = 0; i < letterTextSlots.Count; i++)
        {
            if (i < word.Length)
            {
                letterTextSlots[i].gameObject.SetActive(true);
                letterTextSlots[i].text = word[i].ToString();

                bool isDone = i < collectedStatus.Length && collectedStatus[i];
                letterTextSlots[i].color = isDone ? new Color(1f, 0.88f, 0.15f) : new Color(1f, 1f, 1f, 0.25f);
            }
            else
            {
                letterTextSlots[i].gameObject.SetActive(false);
            }
        }
    }

    public void ShowWordCompletePopup(string completedWord, bool show)
    {
        if (wordCompleteBannerImage != null)
        {
            wordCompleteBannerImage.gameObject.SetActive(show);
            if (show && wordCompleteText != null)
            {
                wordCompleteText.text = $"★ WORD COMPLETED: {completedWord}! ★";
            }
        }
    }

    public void ShowGameOver(bool show, float climbed, float best)
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(show);
        }

        if (gameOverScoreText != null) gameOverScoreText.text = $"Climbed: {climbed:F1} m";
        if (gameOverBestText != null) gameOverBestText.text = $"Best Record: {best:F1} m";
    }

    /// <summary>
    /// Creates complete UI hierarchy with clean customizable Image backgrounds and Text components
    /// so the user can easily swap sprites/colors/fonts in the Unity Inspector.
    /// </summary>
    public void AutoBuildUIIfEmpty()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

        // 1. Altitude Panel
        if (altitudeBackground == null)
        {
            GameObject altObj = CreateUIPanel("Altitude_Panel", transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(25, -25), new Vector2(240, 95));
            altitudeBackground = altObj.GetComponent<Image>();
            altitudeBackground.color = new Color(0f, 0f, 0f, 0.55f);

            GameObject titleObj = CreateUIText("Altitude_Title", altObj.transform, defaultFont, "ALTITUDE CLIMBED", 13, FontStyle.Bold, new Color(0.9f, 0.9f, 0.9f), TextAnchor.UpperLeft, new Vector2(12, -10), new Vector2(216, 20));
            altitudeTitleText = titleObj.GetComponent<Text>();

            GameObject valObj = CreateUIText("Altitude_Value", altObj.transform, defaultFont, "0.0 m", 24, FontStyle.Bold, new Color(0.2f, 0.95f, 0.45f), TextAnchor.MiddleLeft, new Vector2(12, -35), new Vector2(216, 32));
            altitudeValueText = valObj.GetComponent<Text>();

            GameObject bestObj = CreateUIText("Altitude_Best", altObj.transform, defaultFont, "BEST: 0.0 m", 12, FontStyle.Normal, new Color(1f, 0.85f, 0.3f), TextAnchor.LowerLeft, new Vector2(12, -70), new Vector2(216, 18));
            altitudeBestText = bestObj.GetComponent<Text>();
        }

        // 2. Word Assemble Panel
        if (wordBackground == null)
        {
            GameObject wordObj = CreateUIPanel("Word_Panel", transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -25), new Vector2(360, 85));
            wordBackground = wordObj.GetComponent<Image>();
            wordBackground.color = new Color(0f, 0f, 0f, 0.55f);

            GameObject headerObj = CreateUIText("Word_Header", wordObj.transform, defaultFont, "ASSEMBLE WORD", 12, FontStyle.Bold, new Color(0.9f, 0.9f, 0.9f), TextAnchor.UpperCenter, new Vector2(0, -8), new Vector2(340, 20));
            wordHeaderTitle = headerObj.GetComponent<Text>();

            GameObject containerObj = new GameObject("Letters_Container");
            containerObj.transform.SetParent(wordObj.transform, false);
            HorizontalLayoutGroup hlg = containerObj.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 8f;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            RectTransform cRt = containerObj.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0f, 0f);
            cRt.anchorMax = new Vector2(1f, 0.65f);
            cRt.offsetMin = new Vector2(10, 5);
            cRt.offsetMax = new Vector2(-10, -5);
            lettersContainer = containerObj.transform;
        }

        // 3. Word Completed Popup Banner
        if (wordCompleteBannerImage == null)
        {
            GameObject bannerObj = CreateUIPanel("Word_Complete_Banner", transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -125), new Vector2(480, 60));
            wordCompleteBannerImage = bannerObj.GetComponent<Image>();
            wordCompleteBannerImage.color = new Color(0.1f, 0.75f, 0.35f, 0.92f);

            GameObject txtObj = CreateUIText("Complete_Text", bannerObj.transform, defaultFont, "★ WORD COMPLETED! ★", 22, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(460, 50));
            wordCompleteText = txtObj.GetComponent<Text>();
            bannerObj.SetActive(false);
        }

        // 4. Game Over Modal
        if (gameOverPanel == null)
        {
            // Dark screen overlay
            gameOverPanel = CreateUIPanel("GameOver_Overlay", transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            RectTransform ovRt = gameOverPanel.GetComponent<RectTransform>();
            ovRt.offsetMin = Vector2.zero;
            ovRt.offsetMax = Vector2.zero;
            gameOverPanel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

            // Modal dialog box
            GameObject boxObj = CreateUIPanel("GameOver_Box", gameOverPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400, 320));
            gameOverBoxBackground = boxObj.GetComponent<Image>();
            gameOverBoxBackground.color = new Color(0.12f, 0.12f, 0.14f, 0.95f);

            GameObject title = CreateUIText("Title", boxObj.transform, defaultFont, "GAME OVER", 32, FontStyle.Bold, new Color(1f, 0.25f, 0.25f), TextAnchor.UpperCenter, new Vector2(0, -20), new Vector2(360, 45));
            gameOverTitleText = title.GetComponent<Text>();

            GameObject sub = CreateUIText("Subtitle", boxObj.transform, defaultFont, "You fell!", 14, FontStyle.Normal, new Color(0.85f, 0.85f, 0.85f), TextAnchor.UpperCenter, new Vector2(0, -65), new Vector2(360, 25));
            gameOverSubtitleText = sub.GetComponent<Text>();

            GameObject score = CreateUIText("Score", boxObj.transform, defaultFont, "Climbed: 0.0 m", 20, FontStyle.Bold, new Color(0.2f, 0.95f, 0.45f), TextAnchor.MiddleCenter, new Vector2(0, -100), new Vector2(360, 30));
            gameOverScoreText = score.GetComponent<Text>();

            GameObject best = CreateUIText("Best", boxObj.transform, defaultFont, "Best Record: 0.0 m", 14, FontStyle.Bold, new Color(1f, 0.85f, 0.3f), TextAnchor.MiddleCenter, new Vector2(0, -135), new Vector2(360, 25));
            gameOverBestText = best.GetComponent<Text>();

            // Buttons
            GameObject restartObj = CreateUIButton("Restart_Button", boxObj.transform, defaultFont, "Restart", new Vector2(0, -185), new Vector2(260, 42), new Color(0.2f, 0.6f, 0.9f));
            restartButton = restartObj.GetComponent<Button>();

            GameObject homeObj = CreateUIButton("Home_Button", boxObj.transform, defaultFont, "Home", new Vector2(0, -240), new Vector2(260, 42), new Color(0.35f, 0.35f, 0.4f));
            homeButton = homeObj.GetComponent<Button>();

            gameOverPanel.SetActive(false);
        }
    }

    private GameObject CreateUIPanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        Image img = go.AddComponent<Image>();
        img.color = Color.white;
        return go;
    }

    private GameObject CreateUIText(string name, Transform parent, Font font, string content, int fontSize, FontStyle style, Color color, TextAnchor align, Vector2 anchoredPos, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        Text t = go.AddComponent<Text>();
        t.font = font;
        t.text = content;
        t.fontSize = fontSize;
        t.fontStyle = style;
        t.color = color;
        t.alignment = align;
        return go;
    }

    private GameObject CreateUIButton(string name, Transform parent, Font font, string label, Vector2 anchoredPos, Vector2 size, Color bgColor)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        Image img = btnObj.AddComponent<Image>();
        img.color = bgColor;

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = bgColor * 1.2f;
        cb.pressedColor = bgColor * 0.8f;
        btn.colors = cb;

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRt = txtObj.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;

        Text t = txtObj.AddComponent<Text>();
        t.font = font;
        t.text = label;
        t.fontSize = 18;
        t.fontStyle = FontStyle.Bold;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;

        return btnObj;
    }
}
