using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance { get; private set; }

    [Header("Game Over Settings")]
    [Tooltip("Scene name to load when Home is clicked. Leave empty to reload current scene or first build scene.")]
    public string homeSceneName = "";

    private bool isGameOver = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Ensure clean state upon loading
        Time.timeScale = 1f;
        isGameOver = false;
    }

    public void TriggerGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        // Unlock and show mouse cursor for UI interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Freeze game time so player/camera stops
        Time.timeScale = 0f;

        if (GameHUDCanvas.Instance != null && ScoreManager.Instance != null)
        {
            GameHUDCanvas.Instance.ShowGameOver(true, ScoreManager.Instance.MaxAltitude, ScoreManager.Instance.HighScore);
        }
    }

    public void RestartGame()
    {
        // Unfreeze time and reset cursor lock before reloading
        Time.timeScale = 1f;
        isGameOver = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoHome()
    {
        // Unfreeze time and reset cursor lock before reloading
        Time.timeScale = 1f;
        isGameOver = false;
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

    private void OnGUI()
    {
        // If UGUI GameHUDCanvas is present, it handles drawing cleanly
        if (GameHUDCanvas.Instance != null) return;

        if (!isGameOver) return;

        // Draw translucent dark overlay over the entire screen
        Color oldColor = GUI.color;
        GUI.color = new Color(0, 0, 0, 0.75f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = oldColor;

        // Center modal box
        float panelWidth = 380f;
        float panelHeight = 280f;
        float startX = (Screen.width - panelWidth) * 0.5f;
        float startY = (Screen.height - panelHeight) * 0.5f;

        GUILayout.BeginArea(new Rect(startX, startY, panelWidth, panelHeight), GUI.skin.box);

        GUILayout.Space(12);
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 32,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        titleStyle.normal.textColor = new Color(1f, 0.25f, 0.25f);
        GUILayout.Label("GAME OVER", titleStyle);

        GUILayout.Space(4);
        GUIStyle subtitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        };
        subtitleStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f);
        GUILayout.Label("You fell!", subtitleStyle);

        // Score display in Game Over panel
        if (ScoreManager.Instance != null)
        {
            GUILayout.Space(10);
            GUIStyle scoreStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            scoreStyle.normal.textColor = new Color(0.2f, 0.9f, 0.4f);
            GUILayout.Label($"Climbed: {ScoreManager.Instance.MaxAltitude:F1} m", scoreStyle);

            GUIStyle bestStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            bestStyle.normal.textColor = new Color(1f, 0.85f, 0.3f);
            GUILayout.Label($"Best Record: {ScoreManager.Instance.HighScore:F1} m", bestStyle);
        }

        GUILayout.Space(15);

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold
        };

        if (GUILayout.Button("Restart", buttonStyle, GUILayout.Height(40)))
        {
            RestartGame();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Home", buttonStyle, GUILayout.Height(40)))
        {
            GoHome();
        }

        GUILayout.EndArea();
    }
}
