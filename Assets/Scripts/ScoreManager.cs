using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Player Reference")]
    [Tooltip("Reference to the player Transform. If null, auto-finds 'Player'.")]
    public Transform playerTransform;

    [Header("Score Display Settings")]
    [Tooltip("Units multiplier for displayed altitude (e.g., 1 unity unit = 1 meter).")]
    public float altitudeMultiplier = 1.0f;

    // Highest altitude reached during this run
    public float CurrentAltitude { get; private set; }
    public float MaxAltitude { get; private set; }
    public float HighScore { get; private set; }

    private float startingY = 0f;
    private const string HighScoreKey = "HighestAltitudeScore";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        HighScore = PlayerPrefs.GetFloat(HighScoreKey, 0f);
    }

    private void Start()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }

        if (playerTransform != null)
        {
            startingY = playerTransform.position.y;
        }
    }

    private void Update()
    {
        if (playerTransform == null) return;

        // Current height climbed above spawn point
        float climbed = (playerTransform.position.y - startingY) * altitudeMultiplier;
        CurrentAltitude = Mathf.Max(0f, climbed);

        // Keep track of peak altitude achieved
        if (CurrentAltitude > MaxAltitude)
        {
            MaxAltitude = CurrentAltitude;

            if (MaxAltitude > HighScore)
            {
                HighScore = MaxAltitude;
                PlayerPrefs.SetFloat(HighScoreKey, HighScore);
            }
        }

        // Update UGUI Canvas if present
        if (GameHUDCanvas.Instance != null)
        {
            GameHUDCanvas.Instance.UpdateAltitude(CurrentAltitude, MaxAltitude, HighScore);
        }

        // Update Custom User Canvas if present
        if (CustomCanvasUIBridge.Instance != null)
        {
            CustomCanvasUIBridge.Instance.UpdateAltitude(CurrentAltitude, MaxAltitude, HighScore);
        }
    }

    private void OnGUI()
    {
        // If UGUI GameHUDCanvas or CustomCanvasUIBridge is present, don't draw legacy IMGUI
        if (GameHUDCanvas.Instance != null || CustomCanvasUIBridge.Instance != null) return;

        // Fallback IMGUI
        if (Time.timeScale == 0f) return;

        // HUD panel in the top-left corner
        float panelWidth = 220f;
        float panelHeight = 85f;

        GUI.Box(new Rect(15, 15, panelWidth, panelHeight), GUIContent.none);

        GUILayout.BeginArea(new Rect(25, 20, panelWidth - 20, panelHeight - 10));

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold
        };
        titleStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
        GUILayout.Label("ALTITUDE CLIMBED", titleStyle);

        GUIStyle heightStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold
        };
        heightStyle.normal.textColor = new Color(0.2f, 0.9f, 0.4f); // Vivid green
        GUILayout.Label($"{MaxAltitude:F1} m", heightStyle);

        GUIStyle bestStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12
        };
        bestStyle.normal.textColor = new Color(1f, 0.85f, 0.3f); // Golden yellow
        GUILayout.Label($"BEST: {HighScore:F1} m", bestStyle);

        GUILayout.EndArea();
    }
}
