using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class HomeScreenController : MonoBehaviour
{
    [Header("Scene to Load")]
    [Tooltip("Name of the gameplay scene to start when PlayBtn is pressed. Defaults to 'GameScene'.")]
    public string gameSceneName = "GameScene";

    [Header("Buttons")]
    public Button playButton;
    public Button quitButton;

    private void Awake()
    {
        // Unlock and display cursor on Home Screen
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (playButton == null)
        {
            GameObject playObj = GameObject.Find("PlayBtn");
            if (playObj != null) playButton = playObj.GetComponent<Button>();
        }

        if (quitButton == null)
        {
            GameObject quitObj = GameObject.Find("QuitBtn");
            if (quitObj != null) quitButton = quitObj.GetComponent<Button>();
        }
    }

    private void Start()
    {
        if (playButton != null)
        {
            playButton.onClick.AddListener(OnPlayClicked);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitClicked);
        }
    }

    public void OnPlayClicked()
    {
        Time.timeScale = 1f;
        if (!string.IsNullOrEmpty(gameSceneName))
        {
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            // Load index 1 (GameScene)
            SceneManager.LoadScene(1);
        }
    }

    public void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
