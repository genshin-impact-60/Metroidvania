using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Minimal screen text: bottom prompt (hold while set) + center flavor line (timed fade).
/// Creates its own canvas the first time it is needed.
/// </summary>
public class GameFlavorUI : MonoBehaviour
{
    public static GameFlavorUI Instance { get; private set; }

    [SerializeField] float flavorSeconds = 3.2f;
    [SerializeField] float fadeSeconds = 0.45f;

    Text _flavor;
    Text _prompt;
    CanvasGroup _flavorGroup;
    Coroutine _flavorRoutine;
    string _promptText = "";

    public static GameFlavorUI Ensure()
    {
        if (Instance != null)
            return Instance;

        var existing = FindFirstObjectByType<GameFlavorUI>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        var go = new GameObject("GameFlavorUI");
        return go.AddComponent<GameFlavorUI>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildUi();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static void ShowFlavor(string message, float? seconds = null)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;
        Ensure().ShowFlavorInternal(message, seconds);
    }

    public static void SetPrompt(string message)
    {
        Ensure().SetPromptInternal(message ?? "");
    }

    void ShowFlavorInternal(string message, float? seconds)
    {
        if (_flavorRoutine != null)
            StopCoroutine(_flavorRoutine);
        _flavorRoutine = StartCoroutine(FlavorRoutine(message, seconds ?? flavorSeconds));
    }

    void SetPromptInternal(string message)
    {
        _promptText = message;
        if (_prompt == null)
            return;
        _prompt.text = message;
        _prompt.enabled = !string.IsNullOrEmpty(message);
    }

    IEnumerator FlavorRoutine(string message, float hold)
    {
        _flavor.text = message;
        _flavor.enabled = true;
        _flavorGroup.alpha = 0f;

        float t = 0f;
        while (t < fadeSeconds)
        {
            t += Time.unscaledDeltaTime;
            _flavorGroup.alpha = Mathf.Clamp01(t / fadeSeconds);
            yield return null;
        }

        _flavorGroup.alpha = 1f;
        yield return new WaitForSecondsRealtime(Mathf.Max(0.2f, hold));

        t = 0f;
        while (t < fadeSeconds)
        {
            t += Time.unscaledDeltaTime;
            _flavorGroup.alpha = 1f - Mathf.Clamp01(t / fadeSeconds);
            yield return null;
        }

        _flavorGroup.alpha = 0f;
        _flavor.enabled = false;
        _flavorRoutine = null;
    }

    void BuildUi()
    {
        var canvasGo = new GameObject("Canvas");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (font == null)
            font = Font.CreateDynamicFontFromOSFont("Arial", 32);

        _flavor = CreateLabel(canvasGo.transform, "Flavor", 36, new Vector2(0.5f, 0.38f), new Color(0.95f, 0.93f, 0.88f, 1f));
        _flavor.font = font;
        _flavor.enabled = false;
        _flavorGroup = _flavor.gameObject.AddComponent<CanvasGroup>();
        _flavorGroup.alpha = 0f;
        _flavorGroup.blocksRaycasts = false;

        _prompt = CreateLabel(canvasGo.transform, "Prompt", 28, new Vector2(0.5f, 0.14f), new Color(0.86f, 0.82f, 0.72f, 0.95f));
        _prompt.font = font;
        _prompt.enabled = !string.IsNullOrEmpty(_promptText);
        _prompt.text = _promptText;
    }

    static Text CreateLabel(Transform parent, string name, int size, Vector2 anchor, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(1400f, 120f);
        rt.anchoredPosition = Vector2.zero;

        var text = go.AddComponent<Text>();
        text.fontSize = size;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }
}
