using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class TutorialPopup : MonoBehaviour
{

    [Header("Optional")]
    [Tooltip("Optional CanvasGroup for fade/blocks raycasts.")]
    public CanvasGroup canvasGroup;
    [Tooltip("Optional close button to wire up automatically.")]
    public Button closeButton;
    public Button okayButton;

    bool _isShown;
    bool _pausedByMe;

    void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseClicked);

        if (okayButton != null)
            okayButton.onClick.AddListener(OnCloseClicked);

        // Ensure hidden by default in edit mode if canvasGroup is set
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    void Start()
    {
        // Show tutorial with a 5-second delay after start
        StartCoroutine(ShowAfterDelay(5f));
    }

    // Public API: call this from MainMenu or Game bootstrap
    public void ShowIfFirstTime()
    {
        // If SaveManager exists and tutorial already seen, do nothing
        if (SaveManager.Instance != null && SaveManager.Instance.TutorialWatched)
            return;

        Show();
    }

    System.Collections.IEnumerator ShowAfterDelay(float seconds)
    {
        // Use real-time to ensure delay is unaffected by timeScale
        float t = 0f;
        while (t < seconds)
        {
            yield return null;
            t += Time.unscaledDeltaTime;
        }
        ShowIfFirstTime();
    }

    public void Show()
    {
        _isShown = true;

        // Pause gameplay and sounds while tutorial is visible
        if (!Game.GameManager.isPaused)
        {
            Game.GameManager.Pause(true);
            _pausedByMe = true;
        }
        else
        {
            _pausedByMe = false;
        }
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PauseAllSounds();
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }
        else
        {
            gameObject.SetActive(true);
        }
    }

    public void Hide()
    {
        _isShown = false;
        // Resume gameplay/sound only if we initiated the pause
        if (_pausedByMe)
        {
            Game.GameManager.Pause(false);
            _pausedByMe = false;
        }
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.UnPauseAllSounds();
        }
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    // UI hook for close button
    public void OnCloseClicked()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayButtonSound(SoundManager.ButtonUIType.confirm);
        }
        if (SaveManager.Instance != null)
            SaveManager.Instance.TutorialWatched = true;
        Hide();
    }
}
