using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// TODO khi game hoàn thiện:
/// - MouseSensitivity thông với FPSCamera.cs
/// - MasterVolume thông với AudioMixer
/// </summary>
public class SettingsUI : MonoBehaviour
{
    [Header("Sliders — tên trong scene")]
    [SerializeField] private Slider masterVolumeSlider;   // Slider_MasterVolume
    [SerializeField] private Slider mouseSensSlider;      // Slider_MouseSens

    [Header("Value Display (optional)")]
    [SerializeField] private TextMeshProUGUI volumeValueText;
    [SerializeField] private TextMeshProUGUI sensValueText;

    [Header("Buttons")]
    [SerializeField] private Button closeButton;          // Button_Close

    // Keys PlayerPrefs
    private const string KEY_VOLUME = "MasterVolume";
    private const string KEY_SENS   = "MouseSensitivity";

    // Default values
    private const float DEFAULT_VOLUME = 1f;
    private const float DEFAULT_SENS   = 2f;

    void Start()
    {
        // Load giá trị đã lưu
        masterVolumeSlider.value = PlayerPrefs.GetFloat(KEY_VOLUME, DEFAULT_VOLUME);
        mouseSensSlider.value    = PlayerPrefs.GetFloat(KEY_SENS,   DEFAULT_SENS);

        UpdateDisplayTexts();

        // Gắn listener
        masterVolumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        mouseSensSlider.onValueChanged.AddListener(OnSensChanged);
        closeButton.onClick.AddListener(OnClose);
    }

    void OnVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat(KEY_VOLUME, value);
        UpdateDisplayTexts();

        // TODO: AudioMixer.SetFloat("MasterVolume", Mathf.Log10(value) * 20);
    }

    void OnSensChanged(float value)
    {
        PlayerPrefs.SetFloat(KEY_SENS, value);
        UpdateDisplayTexts();

        // TODO: thông với FPSCamera khi có đầy đủ
        // FPSCamera.SetSensitivity(value);
    }

    void UpdateDisplayTexts()
    {
        if (volumeValueText != null)
            volumeValueText.text = Mathf.RoundToInt(masterVolumeSlider.value * 100) + "%";

        if (sensValueText != null)
            sensValueText.text = mouseSensSlider.value.ToString("F1");
    }

    void OnClose()
    {
        PlayerPrefs.Save();
        gameObject.SetActive(false);
    }
}