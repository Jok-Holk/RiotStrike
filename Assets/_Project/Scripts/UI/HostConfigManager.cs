using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HostConfigManager : MonoBehaviour
{
    [Header("Sliders")]
    public Slider roundTimeSlider;
    public Slider waitTimeSlider;
    public Slider pistolTimeSlider;
    public Slider rifleTimeSlider;

    [Header("Display Text")]
    public TextMeshProUGUI roundTimeText;
    public TextMeshProUGUI rifleTimeText;
    public GameObject warningText; // Kéo Text cảnh báo đỏ vào đây

    

    void Start()
    {
        // Gán sự kiện khi thay đổi giá trị
        roundTimeSlider.onValueChanged.AddListener(delegate { UpdateConfig(); });
        pistolTimeSlider.onValueChanged.AddListener(delegate { UpdateConfig(); });

        UpdateConfig(); // Chạy lần đầu để đồng bộ
    }

    public void UpdateConfig()
    {
        float roundT = roundTimeSlider.value;
        float pistolT = pistolTimeSlider.value;

        // 1. Hiển thị giá trị text
        roundTimeText.text = roundT.ToString() + " giây";

        // 2. Kiểm tra điều kiện logic
        if (pistolT >= roundT)
        {
            warningText.SetActive(true);
            rifleTimeSlider.value = 0;
            rifleTimeText.text = "0 giây";
        }
        else
        {
            warningText.SetActive(false);
            // 3. Tự tính RifleTime = RoundTime - PistolTime
            float rifleT = roundT - pistolT;
            rifleTimeSlider.value = rifleT;
            rifleTimeText.text = rifleT.ToString() + " giây";
        }
    }
}