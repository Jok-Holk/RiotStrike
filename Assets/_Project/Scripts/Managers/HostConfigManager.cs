using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HostConfigManager : MonoBehaviour
{
    [Header("Sliders")]
    public Slider waitTimeSlider;
    public Slider pistolTimeSlider;
    public Slider rifleTimeSlider;

    [Header("Display Text")]
    public TextMeshProUGUI waitTimeText;
    public TextMeshProUGUI pistolTimeText;
    public TextMeshProUGUI rifleTimeText;
    public TextMeshProUGUI roundTimeText;
    public GameObject      warningText;  // hiện khi RoundTime = 0

    public int WaitTime   { get; private set; } = 30;
    public int PistolTime { get; private set; } = 60;
    public int RifleTime  { get; private set; } = 120;
    public int RoundTime  => PistolTime + RifleTime;

    void Start()
    {
        // Min = 0 để tự do, max theo thiết kế
        waitTimeSlider.minValue   = 10;  waitTimeSlider.maxValue   = 60;
        pistolTimeSlider.minValue = 0;   pistolTimeSlider.maxValue = 180;
        rifleTimeSlider.minValue  = 0;   rifleTimeSlider.maxValue  = 420;

        waitTimeSlider.wholeNumbers   = true;
        pistolTimeSlider.wholeNumbers = true;
        rifleTimeSlider.wholeNumbers  = true;

        // Default
        waitTimeSlider.value   = 30;
        pistolTimeSlider.value = 60;
        rifleTimeSlider.value  = 120;

        waitTimeSlider.onValueChanged.AddListener(_   => UpdateConfig());
        pistolTimeSlider.onValueChanged.AddListener(_ => UpdateConfig());
        rifleTimeSlider.onValueChanged.AddListener(_  => UpdateConfig());

        UpdateConfig();
    }

    public void UpdateConfig()
    {
        WaitTime   = Mathf.RoundToInt(waitTimeSlider.value);
        PistolTime = Mathf.RoundToInt(pistolTimeSlider.value);
        RifleTime  = Mathf.RoundToInt(rifleTimeSlider.value);

        waitTimeText.text   = FormatTime(WaitTime);
        pistolTimeText.text = FormatTime(PistolTime);
        rifleTimeText.text  = FormatTime(RifleTime);
        roundTimeText.text  = FormatTime(RoundTime);

        // Warning khi cả 2 đều = 0 → trận không có thời gian
        if (warningText != null)
            warningText.SetActive(RoundTime == 0);

        // Sync lên network nếu là host
        if (RoomPlayerData.instance != null &&
            RoomPlayerData.instance.Object.HasStateAuthority)
        {
            RoomPlayerData.instance.RPC_UpdateRoomConfig(WaitTime, PistolTime, RifleTime);
        }
    }

    string FormatTime(int seconds)
    {
        int m = seconds / 60;
        int s = seconds % 60;
        return $"{m}:{s:00}";
    }
}