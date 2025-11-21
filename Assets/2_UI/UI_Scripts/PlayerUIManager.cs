using UnityEngine;
using UnityEngine.UI;
using ENDURE;

public class PlayerUIManager : MonoBehaviour
{
    public Slider slider_Health;
    public Slider slider_Stamina;
    public Slider slider_SystemExposure;
    public Slider slider_Hunger;

    [SerializeField] private PlayerManager playerManager;

    void Awake()
    {
        if (playerManager == null)
        {
            playerManager = GetComponentInParent<PlayerManager>();
        }

        if (slider_Health == null)
            slider_Health = transform.Find("slider_Health")?.GetComponent<Slider>();
        if (slider_Stamina == null)
            slider_Stamina = transform.Find("slider_Stamina")?.GetComponent<Slider>();
        if (slider_SystemExposure == null)
            slider_SystemExposure = transform.Find("slider_SystemExposure")?.GetComponent<Slider>();
        if (slider_Hunger == null)
            slider_Hunger = transform.Find("slider_Hunger")?.GetComponent<Slider>();

        Debug.Log($"PlayerUIManager Awake - Sliders found: Health={slider_Health != null}, Stamina={slider_Stamina != null}, Exposure={slider_SystemExposure != null}, Hunger={slider_Hunger != null}");
    }

    void Start()
    {
        if (playerManager != null)
        {
            Debug.Log($"PlayerUIManager Start - Health: {playerManager.Health.current}/{playerManager.Health.max}");
            Debug.Log($"PlayerUIManager Start - Stamina: {playerManager.Stamina.current}/{playerManager.Stamina.max}");
            Debug.Log($"PlayerUIManager Start - Exposure: {playerManager.SystemExposure.current}/{playerManager.SystemExposure.max}");
            Debug.Log($"PlayerUIManager Start - Hunger: {playerManager.Hunger.current}/{playerManager.Hunger.max}");

            InitializeSlider(slider_Health, playerManager.Health);
            InitializeSlider(slider_Stamina, playerManager.Stamina);
            InitializeSlider(slider_SystemExposure, playerManager.SystemExposure);
            InitializeSlider(slider_Hunger, playerManager.Hunger);
        }
    }

    void Update()
    {
        if (playerManager == null) return;

        UpdateSliderValue(slider_Health, playerManager.Health);
        UpdateSliderValue(slider_Stamina, playerManager.Stamina);
        UpdateSliderValue(slider_SystemExposure, playerManager.SystemExposure);
        UpdateSliderValue(slider_Hunger, playerManager.Hunger);
    }

    private void InitializeSlider(Slider slider, Stat stat)
    {
        if (slider == null) return;

        slider.minValue = stat.min;
        slider.maxValue = stat.max;
        slider.value = stat.current;
        slider.gameObject.SetActive(!stat.isHidden);

        Debug.Log($"Initialized {slider.name}: value={slider.value}, min={slider.minValue}, max={slider.maxValue}");
    }

    private void UpdateSliderValue(Slider slider, Stat stat)
    {
        if (slider == null) return;

        slider.value = stat.current;

        if (slider.gameObject.activeSelf != !stat.isHidden)
            slider.gameObject.SetActive(!stat.isHidden);
    }
}
