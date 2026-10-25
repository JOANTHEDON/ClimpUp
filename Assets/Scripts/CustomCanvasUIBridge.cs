using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CustomCanvasUIBridge : MonoBehaviour
{
    public static CustomCanvasUIBridge Instance { get; private set; }

    [Header("Altitude UI Binding")]
    [Tooltip("Text component in Container1 for showing ONLY the altitude value (e.g. '12.5')")]
    public TMP_Text altitudeScoreText;

    [Tooltip("Text component in Container2 for showing Best Score (e.g. 'Best: 25.0')")]
    public TMP_Text bestScoreText;

    [Header("Word Assemble UI Binding")]
    [Tooltip("Text component named AssembleWord")]
    public TMP_Text assembleWordHeader;

    [Tooltip("Text component named WORD that displays the target word and collected letters")]
    public TMP_Text wordDisplayText;

    [Header("Format Settings")]
    [Tooltip("Prefix for the best score text")]
    public string bestPrefix = "Best: ";

    [Tooltip("Include 'm' or units in the altitude value? Uncheck if you only want the raw number.")]
    public bool showUnits = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        AutoFindReferencesIfNull();
    }

    private void Start()
    {
        AutoFindReferencesIfNull();
    }

    public void AutoFindReferencesIfNull()
    {
        // 1. Find AltitudeScore in Container1 under AltitudeClimed
        if (altitudeScoreText == null)
        {
            GameObject altObj = GameObject.Find("AltitudeScore");
            if (altObj != null)
            {
                altitudeScoreText = altObj.GetComponent<TMP_Text>();
            }
        }

        // 2. Find Score in Container2 under AltitudeClimed
        if (bestScoreText == null)
        {
            GameObject bestObj = GameObject.Find("Score");
            if (bestObj != null)
            {
                bestScoreText = bestObj.GetComponent<TMP_Text>();
            }
        }

        // 3. Find AssembleWord
        if (assembleWordHeader == null)
        {
            GameObject awObj = GameObject.Find("Text (TMP)");
            if (awObj != null && awObj.transform.parent != null && awObj.transform.parent.name == "AssembleWord")
            {
                assembleWordHeader = awObj.GetComponent<TMP_Text>();
            }
            else
            {
                GameObject p = GameObject.Find("AssembleWord");
                if (p != null) assembleWordHeader = p.GetComponentInChildren<TMP_Text>();
            }
        }

        // 4. Find WORD
        if (wordDisplayText == null)
        {
            GameObject wObj = GameObject.Find("WORD");
            if (wObj != null)
            {
                wordDisplayText = wObj.GetComponent<TMP_Text>();
            }
        }
    }

    /// <summary>
    /// Updates AltitudeScore with just the numerical value, and Score with Best score.
    /// </summary>
    public void UpdateAltitude(float current, float max, float best)
    {
        if (altitudeScoreText == null || bestScoreText == null)
        {
            AutoFindReferencesIfNull();
        }

        if (altitudeScoreText != null)
        {
            altitudeScoreText.text = showUnits ? $"{max:F1} m" : $"{max:F1}";
        }

        if (bestScoreText != null)
        {
            bestScoreText.text = $"{bestPrefix}{best:F1}";
        }
    }

    /// <summary>
    /// Updates AssembleWord header and the WORD text.
    /// Collected letters are highlighted in gold; uncollected letters are shown faded or with spacing.
    /// </summary>
    public void UpdateWord(string targetWord, bool[] collectedStatus, int completedCount)
    {
        if (wordDisplayText == null || assembleWordHeader == null)
        {
            AutoFindReferencesIfNull();
        }

        if (assembleWordHeader != null)
        {
            assembleWordHeader.text = "ASSEMBLE WORD";
        }

        if (wordDisplayText != null && !string.IsNullOrEmpty(targetWord))
        {
            // Format word with Rich Text: collected letters bright gold, uncollected semi-transparent
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            for (int i = 0; i < targetWord.Length; i++)
            {
                char c = targetWord[i];
                bool collected = (collectedStatus != null && i < collectedStatus.Length && collectedStatus[i]);

                if (collected)
                {
                    sb.Append($"<color=#FFD700><b>{c}</b></color> ");
                }
                else
                {
                    sb.Append($"<color=#777777>{c}</color> ");
                }
            }

            wordDisplayText.text = sb.ToString().TrimEnd();
        }
    }
}
