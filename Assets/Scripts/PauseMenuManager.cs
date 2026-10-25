using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseMenuManager : MonoBehaviour
{
    public static PauseMenuManager Instance { get; private set; }

    [Header("Scenes Configuration")]
    [Tooltip("Name of the home scene to load. Defaults to 'Home'.")]
    public string homeSceneName = "Home";

    [Header("UI References (Optional - will auto-create if null)")]
    public Button pauseButton;
    public GameObject pauseModalPanel;
    public Button resumeButton;
    public Button restartButton;
    public Button homeButton;
    public Button quitButton;

    public bool IsPaused { get; private set; } = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        AutoSetupPauseUI();
    }

    private void Update()
    {
        // Toggle pause with Escape key or P key
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            if (IsPaused)
            {
                Resume();
            }
            else
            {
                // Only pause if not Game Over
                if (Time.timeScale > 0f)
                {
                    Pause();
                }
            }
        }
    }

    public void Pause()
    {
        IsPaused = true;
        Time.timeScale = 0f;

        // Unlock cursor for UI
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (pauseModalPanel != null)
        {
            pauseModalPanel.SetActive(true);
        }

        if (pauseButton != null)
        {
            pauseButton.gameObject.SetActive(false);
        }
    }

    public void Resume()
    {
        IsPaused = false;
        Time.timeScale = 1f;

        // Re-lock cursor for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (pauseModalPanel != null)
        {
            pauseModalPanel.SetActive(false);
        }

        if (pauseButton != null)
        {
            pauseButton.gameObject.SetActive(true);
        }
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        IsPaused = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoHome()
    {
        Time.timeScale = 1f;
        IsPaused = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (!string.IsNullOrEmpty(homeSceneName))
        {
            SceneManager.LoadScene(homeSceneName);
        }
        else
        {
            SceneManager.LoadScene(0);
        }
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void AutoSetupPauseUI()
    {
        // Find existing canvas in scene
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindObjectOfType<Canvas>();
        }

        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("PauseCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // ensure on top

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();
        }

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

        // 1. Create Top-Right Pause Button if null
        if (pauseButton == null)
        {
            GameObject btnObj = new GameObject("Pause_Button");
            btnObj.transform.SetParent(canvas.transform, false);
            RectTransform rt = btnObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-25, -25);
            rt.sizeDelta = new Vector2(60, 60);

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.15f, 0.15f, 0.18f, 0.85f);

            pauseButton = btnObj.AddComponent<Button>();
            pauseButton.onClick.AddListener(Pause);

            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(btnObj.transform, false);
            RectTransform iconRt = iconObj.AddComponent<RectTransform>();
            iconRt.anchorMin = Vector2.zero;
            iconRt.anchorMax = Vector2.one;
            iconRt.sizeDelta = Vector2.zero;
            Text iconText = iconObj.AddComponent<Text>();
            iconText.font = defaultFont;
            iconText.text = "❚❚";
            iconText.fontSize = 24;
            iconText.color = Color.white;
            iconText.alignment = TextAnchor.MiddleCenter;
        }

        // 2. Create Pause Modal Panel if null
        if (pauseModalPanel == null)
        {
            // Dark Overlay
            pauseModalPanel = new GameObject("Pause_Modal_Overlay");
            pauseModalPanel.transform.SetParent(canvas.transform, false);
            RectTransform ovRt = pauseModalPanel.AddComponent<RectTransform>();
            ovRt.anchorMin = Vector2.zero;
            ovRt.anchorMax = Vector2.one;
            ovRt.sizeDelta = Vector2.zero;
            Image ovImg = pauseModalPanel.AddComponent<Image>();
            ovImg.color = new Color(0f, 0f, 0f, 0.75f);

            // Dialog Box
            GameObject boxObj = new GameObject("Pause_Box");
            boxObj.transform.SetParent(pauseModalPanel.transform, false);
            RectTransform boxRt = boxObj.AddComponent<RectTransform>();
            boxRt.anchorMin = new Vector2(0.5f, 0.5f);
            boxRt.anchorMax = new Vector2(0.5f, 0.5f);
            boxRt.pivot = new Vector2(0.5f, 0.5f);
            boxRt.anchoredPosition = Vector2.zero;
            boxRt.sizeDelta = new Vector2(380, 420);

            Image boxImg = boxObj.AddComponent<Image>();
            boxImg.color = new Color(0.12f, 0.12f, 0.15f, 0.95f);

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(boxObj.transform, false);
            RectTransform titleRt = titleObj.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.5f, 1f);
            titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0, -25);
            titleRt.sizeDelta = new Vector2(340, 45);

            Text titleText = titleObj.AddComponent<Text>();
            titleText.font = defaultFont;
            titleText.text = "PAUSED";
            titleText.fontSize = 32;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = Color.white;
            titleText.alignment = TextAnchor.MiddleCenter;

            // 4 Buttons: Resume, Restart, Home, Quit
            resumeButton = CreateModalButton("Resume_Button", boxObj.transform, defaultFont, "Resume", new Vector2(0, -100), new Color(0.2f, 0.75f, 0.35f));
            resumeButton.onClick.AddListener(Resume);

            restartButton = CreateModalButton("Restart_Button", boxObj.transform, defaultFont, "Restart", new Vector2(0, -165), new Color(0.2f, 0.55f, 0.9f));
            restartButton.onClick.AddListener(Restart);

            homeButton = CreateModalButton("Home_Button", boxObj.transform, defaultFont, "Home", new Vector2(0, -230), new Color(0.4f, 0.4f, 0.45f));
            homeButton.onClick.AddListener(GoHome);

            quitButton = CreateModalButton("Quit_Button", boxObj.transform, defaultFont, "Quit", new Vector2(0, -295), new Color(0.85f, 0.25f, 0.25f));
            quitButton.onClick.AddListener(QuitGame);

            pauseModalPanel.SetActive(false);
        }
    }

    private Button CreateModalButton(string name, Transform parent, Font font, string label, Vector2 pos, Color color)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(280, 48);

        Image img = btnObj.AddComponent<Image>();
        img.color = color;

        Button btn = btnObj.AddComponent<Button>();

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

        return btn;
    }
}
