using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls an in-scene loading screen UI that can be shown/hidden by SceneLoader.
/// Attach this script to the root of your loading screen canvas object.
/// </summary>
public class LoadingScreenUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Root object that should be enabled/disabled for the loading screen. Defaults to this GameObject.")]
    public GameObject root;

    [Tooltip("Slider used to display loading progress (0-1).")]
    public Slider progressBar;

    [Tooltip("Optional text element that shows percentage (e.g. 'Loading... 42%').")]
    public TMP_Text progressText;

    [Tooltip("Optional text element for lore or flavor text.")]
    public TMP_Text loreText;

    private void Awake()
    {
        if (root == null)
        {
            root = gameObject;
        }

        SetVisible(false);
        UpdateProgress(0f);
    }

    /// <summary>
    /// Show the loading screen.
    /// </summary>
    public void Show()
    {
        SetVisible(true);
        UpdateProgress(0f);
        SetLore(string.Empty);
    }

    /// <summary>
    /// Update the progress bar/text.
    /// </summary>
    public void UpdateProgress(float progress)
    {
        float clamped = Mathf.Clamp01(progress);

        if (progressBar != null)
        {
            progressBar.value = clamped;
        }

        if (progressText != null)
        {
            progressText.text = $"Loading... {Mathf.RoundToInt(clamped * 100f)}%";
        }
    }

    /// <summary>
    /// Update the lore/flavor text shown on the loading screen.
    /// </summary>
    public void SetLore(string text)
    {
        if (loreText != null)
        {
            loreText.text = text;
        }
    }

    /// <summary>
    /// Hide the loading screen.
    /// </summary>
    public void Hide()
    {
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        if (root != null && root.activeSelf != visible)
        {
            root.SetActive(visible);
        }
    }
}

