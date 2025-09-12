using UnityEngine;
using UnityEngine.UI;
using ENDURE;

public class PlayerUIManager : MonoBehaviour
{
    // Player stat sliders under group_PlayerStats
    public Slider slider_Health;
    public Slider slider_Stamina;
    public Slider slider_SystemExposure;
    public Slider slider_Hunger;

    [SerializeField] private PlayerManager playerManager; // Source of player stats

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Initialize slider ranges from player stats
        if (playerManager != null)
        {
            SetupSlider(slider_Health, playerManager.Health);
            SetupSlider(slider_Stamina, playerManager.Stamina);
            SetupSlider(slider_SystemExposure, playerManager.SystemExposure);
            SetupSlider(slider_Hunger, playerManager.Hunger);
        }
    }

    // Update is called once per frame
    void Update()
    {
        UpdateSliders();
    }

    //Updates all sliders
    private void UpdateSliders()
    {
        if (playerManager == null) return;

        UpdateSlider(slider_Health, playerManager.Health);
        UpdateSlider(slider_Stamina, playerManager.Stamina);
        UpdateSlider(slider_SystemExposure, playerManager.SystemExposure);
        UpdateSlider(slider_Hunger, playerManager.Hunger);
    }

    // Configure slider min/max and initial value based on Stat
    private void SetupSlider(Slider slider, Stat stat)
    {
        if (slider == null) return;
        slider.minValue = stat.min;
        slider.maxValue = stat.max;
        slider.value = Mathf.Clamp(stat.current, stat.min, stat.max);
        if (slider.gameObject != null) slider.gameObject.SetActive(!stat.isHidden);
    }

    // Update slider value and visibility each frame
    private void UpdateSlider(Slider slider, Stat stat)
    {
        if (slider == null) return;
        
        if (slider.minValue != stat.min) slider.minValue = stat.min;
        if (slider.maxValue != stat.max) slider.maxValue = stat.max;

        slider.value = stat.current;
        
        bool shouldBeActive = !stat.isHidden;
        if (slider.gameObject.activeSelf != shouldBeActive) slider.gameObject.SetActive(shouldBeActive);
    }
}
