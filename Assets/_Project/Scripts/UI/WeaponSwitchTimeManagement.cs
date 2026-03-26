using UnityEngine;
using TMPro; // Nếu bạn dùng TextMeshPro (khuyên dùng)
using UnityEngine.UI;

public class TimerManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI phaseText;
    public TextMeshProUGUI timerText;

    [Header("Settings")]
    public float totalTime = 600f; // 10 phút tính bằng giây (600s)
    public float rifleUnlockTime = 480f; // Mở khóa Rifle khi còn 8 phút (480s)
    private float currentTime;
    private bool isRifleUnlocked = false;



    void Start()
    {
        currentTime = totalTime;
        UpdateUI();
    }

    void Update()
    {
        if (currentTime > 0)
        {
            currentTime -= Time.deltaTime; // Trừ dần thời gian theo thời gian thực
            UpdateUI();
        }
        else
        {
            currentTime = 0;
            // Xử lý khi hết sạch thời gian trận đấu ở đây
        }
    }
    void UpdateUI()
    {
        // 1. Xử lý định dạng thời gian 00:00
        int minutes = Mathf.FloorToInt(currentTime / 60);
        int seconds = Mathf.FloorToInt(currentTime % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

        // 2. Xử lý logic giai đoạn (Phase)
        // Nếu thời gian còn lại thấp hơn mốc mở khóa Rifle
        if (currentTime <= rifleUnlockTime && !isRifleUnlocked)
        {
            SetRiflePhase();
        }
        else if (!isRifleUnlocked)
        {
            SetPistolPhase();
        }
    }

    void SetPistolPhase()
    {
        phaseText.text = "PISTOL ONLY";
        phaseText.color = Color.yellow; // Màu vàng
        timerText.color = Color.yellow;
    }

    void SetRiflePhase()
    {
        isRifleUnlocked = true;
        phaseText.text = "RIFLE UNLOCKED";
        phaseText.color = Color.green; // Màu xanh (Green trong Unity là xanh lá)
        timerText.color = Color.green;

        // Bạn có thể thêm code mở khóa súng cho người chơi ở đây
        Debug.Log("Rifles have been unlocked!");
    }
}