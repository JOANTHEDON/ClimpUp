using System.Collections.Generic;
using UnityEngine;

public class WordCollectManager : MonoBehaviour
{
    public static WordCollectManager Instance { get; private set; }

    [Header("Word Settings")]
    [Tooltip("Words pool to assemble (e.g. JUMP, CLIMB, CHRISTMAS, VICTORY, REACH).")]
    public string[] targetWords = new string[] { "JUMP", "CLIMB", "SUPER", "CHRISTMAS", "VICTORY" };

    [Tooltip("How many platforms between consecutive letter spawns.")]
    public int platformInterval = 3;

    [Tooltip("Altitude threshold (jumps) before letter collection starts appearing.")]
    public int startAfterJumps = 4;

    [Header("Audio (Optional)")]
    [Tooltip("Sound played when collecting a letter.")]
    public AudioClip collectLetterSound;

    [Tooltip("Sound played when a full word is completed!")]
    public AudioClip wordCompleteSound;

    // Current Word State
    public string CurrentTargetWord { get; private set; }
    private bool[] collectedStatus;
    private int currentWordIndex = 0;
    private int wordsCompleted = 0;
    private float completionBannerTimer = 0f;
    private string lastCompletedWord = "";

    private AudioSource audioSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;

        StartNextWord();
    }

    private void Update()
    {
        if (completionBannerTimer > 0f)
        {
            completionBannerTimer -= Time.deltaTime;
            if (completionBannerTimer <= 0f && GameHUDCanvas.Instance != null)
            {
                GameHUDCanvas.Instance.ShowWordCompletePopup("", false);
            }
        }
    }

    public void StartNextWord()
    {
        if (targetWords == null || targetWords.Length == 0)
        {
            targetWords = new string[] { "JUMP" };
        }

        CurrentTargetWord = targetWords[currentWordIndex % targetWords.Length].ToUpper();
        collectedStatus = new bool[CurrentTargetWord.Length];
        currentWordIndex++;

        if (GameHUDCanvas.Instance != null)
        {
            GameHUDCanvas.Instance.UpdateWordProgress(CurrentTargetWord, collectedStatus, wordsCompleted);
        }

        if (CustomCanvasUIBridge.Instance != null)
        {
            CustomCanvasUIBridge.Instance.UpdateWord(CurrentTargetWord, collectedStatus, wordsCompleted);
        }
    }

    /// <summary>
    /// Checks if a platform should receive a letter item based on jump/platform index.
    /// Returns the letter to spawn, or '\0' if none.
    /// </summary>
    public bool TryGetNextLetterToSpawn(int platformIndex, out char letter, out int letterIndex)
    {
        letter = '\0';
        letterIndex = -1;

        if (platformIndex < startAfterJumps) return false;

        int step = platformIndex - startAfterJumps;
        if (step % platformInterval != 0) return false;

        // Find the next uncollected letter in the current word
        for (int i = 0; i < CurrentTargetWord.Length; i++)
        {
            if (!collectedStatus[i])
            {
                letter = CurrentTargetWord[i];
                letterIndex = i;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Spawns a 3D letter collectible floating above the given platform.
    /// </summary>
    public void AttachLetterToPlatform(GameObject platform, char letter, int letterIndex)
    {
        if (platform == null) return;

        // Create container for letter
        GameObject letterObj = new GameObject($"Letter_{letter}");
        letterObj.transform.SetParent(platform.transform);
        // Positioned 1.1 units above the platform surface
        letterObj.transform.localPosition = new Vector3(0f, 1.1f, 0f);

        // Add Sphere Collider for pickup trigger
        SphereCollider col = letterObj.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 0.9f;

        // Add 3D TextMesh so the letter is visible in the 3D world
        TextMesh tm = letterObj.AddComponent<TextMesh>();
        tm.text = letter.ToString();
        tm.fontSize = 64;
        tm.characterSize = 0.28f;
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.fontStyle = FontStyle.Bold;
        tm.color = new Color(1f, 0.85f, 0.1f); // Glowing Golden Yellow

        // Add subtle background glowing billboard quad or secondary text outline
        GameObject shadowObj = new GameObject("ShadowText");
        shadowObj.transform.SetParent(letterObj.transform);
        shadowObj.transform.localPosition = new Vector3(0.04f, -0.04f, 0.05f);
        TextMesh shadowTm = shadowObj.AddComponent<TextMesh>();
        shadowTm.text = letter.ToString();
        shadowTm.fontSize = 64;
        shadowTm.characterSize = 0.28f;
        shadowTm.alignment = TextAlignment.Center;
        shadowTm.anchor = TextAnchor.MiddleCenter;
        shadowTm.fontStyle = FontStyle.Bold;
        shadowTm.color = new Color(0f, 0f, 0f, 0.8f);

        // Add LetterItem script
        LetterItem item = letterObj.AddComponent<LetterItem>();
        item.letter = letter;
        item.letterIndex = letterIndex;
        item.manager = this;
    }

    public void CollectLetter(char letter, int index)
    {
        if (index >= 0 && index < collectedStatus.Length)
        {
            collectedStatus[index] = true;
        }

        if (GameHUDCanvas.Instance != null)
        {
            GameHUDCanvas.Instance.UpdateWordProgress(CurrentTargetWord, collectedStatus, wordsCompleted);
        }

        if (CustomCanvasUIBridge.Instance != null)
        {
            CustomCanvasUIBridge.Instance.UpdateWord(CurrentTargetWord, collectedStatus, wordsCompleted);
        }

        if (collectLetterSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(collectLetterSound, 0.9f);
        }

        // Check if full word is completed!
        bool allDone = true;
        for (int i = 0; i < collectedStatus.Length; i++)
        {
            if (!collectedStatus[i])
            {
                allDone = false;
                break;
            }
        }

        if (allDone)
        {
            OnWordCompleted();
        }
    }

    private void OnWordCompleted()
    {
        wordsCompleted++;
        lastCompletedWord = CurrentTargetWord;
        completionBannerTimer = 3.5f;

        if (GameHUDCanvas.Instance != null)
        {
            GameHUDCanvas.Instance.ShowWordCompletePopup(lastCompletedWord, true);
        }

        if (wordCompleteSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(wordCompleteSound, 1f);
        }

        // Advance to next word
        StartNextWord();
    }

    private void OnGUI()
    {
        // If UGUI GameHUDCanvas or CustomCanvasUIBridge is present, don't draw legacy IMGUI
        if (GameHUDCanvas.Instance != null || CustomCanvasUIBridge.Instance != null) return;

        if (Time.timeScale == 0f) return;

        // 1. Draw Word Collection progress banner in Top-Center of Screen
        float bannerWidth = 320f;
        float bannerHeight = 65f;
        float startX = (Screen.width - bannerWidth) * 0.5f;
        float startY = 15f;

        GUI.Box(new Rect(startX, startY, bannerWidth, bannerHeight), GUIContent.none);

        GUILayout.BeginArea(new Rect(startX + 10, startY + 8, bannerWidth - 20, bannerHeight - 12));

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        labelStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f);
        GUILayout.Label($"ASSEMBLE WORD  •  COMPLETED: {wordsCompleted}", labelStyle);

        GUILayout.Space(2);

        // Format letters: [J] [U] [M] [P] with collected letters in bright gold and uncollected dimmed
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        GUIStyle letterCollected = new GUIStyle(GUI.skin.label)
        {
            fontSize = 24,
            fontStyle = FontStyle.Bold
        };
        letterCollected.normal.textColor = new Color(1f, 0.88f, 0.2f); // Gold

        GUIStyle letterEmpty = new GUIStyle(GUI.skin.label)
        {
            fontSize = 24,
            fontStyle = FontStyle.Bold
        };
        letterEmpty.normal.textColor = new Color(1f, 1f, 1f, 0.25f); // Gray/Ghosted

        for (int i = 0; i < CurrentTargetWord.Length; i++)
        {
            char c = CurrentTargetWord[i];
            bool done = i < collectedStatus.Length && collectedStatus[i];

            GUILayout.Label(c.ToString(), done ? letterCollected : letterEmpty, GUILayout.Width(28));
        }

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.EndArea();

        // 2. Celebratory Banner when word is completed!
        if (completionBannerTimer > 0f)
        {
            float popWidth = 450f;
            float popHeight = 60f;
            float popX = (Screen.width - popWidth) * 0.5f;
            float popY = 88f;

            GUI.color = new Color(0.1f, 0.7f, 0.3f, 0.85f);
            GUI.DrawTexture(new Rect(popX, popY, popWidth, popHeight), Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUIStyle completeStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            completeStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(popX, popY + 12, popWidth, 40), $"★ WORD COMPLETED: {lastCompletedWord}! ★", completeStyle);
        }
    }
}
