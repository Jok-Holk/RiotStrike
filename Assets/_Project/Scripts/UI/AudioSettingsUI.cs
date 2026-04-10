using UnityEngine;
using UnityEngine.UI;
public class AudioSettingsUI : MonoBehaviour
{
    [SerializeField] private Slider masterVolumeSlider;

    void Start()
    {
        float saved = PlayerPrefs.GetFloat("MasterVolume", 1f);
        masterVolumeSlider.value = saved;
        AudioListener.volume     = saved;
        masterVolumeSlider.onValueChanged.AddListener(v =>
        {
            AudioListener.volume = v;
            PlayerPrefs.SetFloat("MasterVolume", v);
        });
    }
}
