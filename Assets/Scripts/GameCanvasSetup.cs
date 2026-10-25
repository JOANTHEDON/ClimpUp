using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[DefaultExecutionOrder(-50)]
public class GameCanvasSetup : MonoBehaviour
{
    public static GameCanvasSetup Instance { get; private set; }

    [Header("Canvas Settings")]
    [Tooltip("Render mode for the canvas.")]
    public RenderMode renderMode = RenderMode.ScreenSpaceOverlay;

    [Tooltip("Target reference resolution for UI scaling.")]
    public Vector2 referenceResolution = new Vector2(1920, 1080);

    [Header("Image Slot (Drop your custom image here)")]
    [Tooltip("Optional custom image/sprite you want displayed.")]
    public Sprite customImageSprite;

    // Public references to created UI elements for inspection / editing
    [HideInInspector] public Canvas CanvasComponent;
    [HideInInspector] public CanvasScaler ScalerComponent;
    [HideInInspector] public GraphicRaycaster RaycasterComponent;
    [HideInInspector] public GameObject ImageContainer;
    [HideInInspector] public Image CustomImage;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        EnsureEventSystem();
        SetupCanvas();
        SetupCustomImageSlot();
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

    private void SetupCanvas()
    {
        CanvasComponent = GetComponent<Canvas>();
        if (CanvasComponent == null)
        {
            CanvasComponent = gameObject.AddComponent<Canvas>();
        }
        CanvasComponent.renderMode = renderMode;

        ScalerComponent = GetComponent<CanvasScaler>();
        if (ScalerComponent == null)
        {
            ScalerComponent = gameObject.AddComponent<CanvasScaler>();
        }
        ScalerComponent.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        ScalerComponent.referenceResolution = referenceResolution;
        ScalerComponent.matchWidthOrHeight = 0.5f;

        RaycasterComponent = GetComponent<GraphicRaycaster>();
        if (RaycasterComponent == null)
        {
            RaycasterComponent = gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    private void SetupCustomImageSlot()
    {
        // Check if CustomImage child already exists
        Transform existing = transform.Find("MyCustomImage");
        if (existing == null)
        {
            ImageContainer = new GameObject("MyCustomImage");
            ImageContainer.transform.SetParent(transform, false);

            RectTransform rect = ImageContainer.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(300, 300);

            CustomImage = ImageContainer.AddComponent<Image>();
            if (customImageSprite != null)
            {
                CustomImage.sprite = customImageSprite;
                CustomImage.color = Color.white;
            }
            else
            {
                // Subtle preview placeholder: clear transparent until user sets sprite
                CustomImage.color = new Color(1f, 1f, 1f, 0.2f);
            }
        }
        else
        {
            ImageContainer = existing.gameObject;
            CustomImage = ImageContainer.GetComponent<Image>();
        }
    }

    /// <summary>
    /// Update image dynamically at runtime if desired.
    /// </summary>
    public void SetCustomImage(Sprite sprite)
    {
        customImageSprite = sprite;
        if (CustomImage != null)
        {
            CustomImage.sprite = sprite;
            CustomImage.color = Color.white;
        }
    }
}
