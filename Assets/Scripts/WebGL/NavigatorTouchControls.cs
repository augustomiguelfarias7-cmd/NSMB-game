#if UNITY_WEBGL && !UNITY_EDITOR
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Photon.Pun;

/// <summary>
/// Touch controls for Super Mario Bros Navigator.
/// The controls feed the existing Gamepad bindings in Controls.inputactions,
/// leaving the original PlayerController input logic untouched.
/// </summary>
public sealed class NavigatorTouchControls : MonoBehaviour
{
    private static NavigatorTouchControls instance;
    private Canvas canvas;
    private float nextPlayerCheck;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (instance != null) return;
        var root = new GameObject("Navigator Touch Controls");
        DontDestroyOnLoad(root);
        instance = root.AddComponent<NavigatorTouchControls>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        BuildControls();
        SceneManager.sceneLoaded += OnSceneLoaded;
        RefreshVisibility();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        if (Time.unscaledTime < nextPlayerCheck) return;
        nextPlayerCheck = Time.unscaledTime + 0.5f;
        RefreshVisibility();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshVisibility();
    }

    private void RefreshVisibility()
    {
        if (canvas == null) return;

        bool hasLocalPlayer = false;
        foreach (var player in FindObjectsOfType<PlayerController>())
        {
            if (player != null && player.photonView != null && player.photonView.IsMine)
            {
                hasLocalPlayer = true;
                break;
            }
        }

        canvas.enabled = hasLocalPlayer;
    }

    private void BuildControls()
    {
        var eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            var eventObject = new GameObject("Navigator EventSystem");
            eventSystem = eventObject.AddComponent<EventSystem>();
            DontDestroyOnLoad(eventObject);
        }

        if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();

        var canvasObject = new GameObject("Navigator Touch Canvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        CreateStick(canvasObject.transform);
        CreateButton(canvasObject.transform, "A", "<Gamepad>/buttonSouth",
            new Vector2(0.82f, 0.16f), new Vector2(104, 104));
        CreateButton(canvasObject.transform, "B", "<Gamepad>/rightShoulder",
            new Vector2(0.92f, 0.28f), new Vector2(92, 92));
    }

    private void CreateStick(Transform parent)
    {
        var baseImage = CreateImage(parent, "Virtual Stick",
            new Vector2(0.14f, 0.18f), new Vector2(190, 190),
            new Color(0.08f, 0.10f, 0.16f, 0.48f));

        var knob = CreateImage(baseImage.transform, "Stick Knob",
            new Vector2(0.5f, 0.5f), new Vector2(76, 76),
            new Color(0.88f, 0.91f, 1f, 0.82f));
        knob.raycastTarget = true;

        var stick = knob.gameObject.AddComponent<OnScreenStick>();
        stick.controlPath = "<Gamepad>/leftStick";
        stick.movementRange = 54f;
        stick.behaviour = OnScreenStick.Behaviour.RelativePositionWithStaticOrigin;
    }

    private void CreateButton(Transform parent, string label, string controlPath,
        Vector2 anchor, Vector2 size)
    {
        var image = CreateImage(parent, "Button " + label, anchor, size,
            label == "A"
                ? new Color(0.15f, 0.55f, 0.95f, 0.72f)
                : new Color(0.92f, 0.42f, 0.12f, 0.76f));

        var button = image.gameObject.AddComponent<OnScreenButton>();
        button.controlPath = controlPath;

        var textObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(image.transform, false);
        var text = textObject.GetComponent<Text>();
        text.text = label;
        text.alignment = TextAnchor.MiddleCenter;
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 42;
        text.fontStyle = FontStyle.Bold;
        text.color = Color.white;
        text.raycastTarget = false;

        var rect = text.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private Image CreateImage(Transform parent, string objectName,
        Vector2 anchor, Vector2 size, Color color)
    {
        var imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);

        var rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;

        var image = imageObject.GetComponent<Image>();
        image.color = color;
        return image;
    }
}
#endif
