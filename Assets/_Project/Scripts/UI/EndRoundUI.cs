using UnityEngine;
using UnityEngine.UI;
using Fusion;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Gắn vào Panel_EndRound trong Canvas_HUD của PlayerPrefab.
/// GameManager tìm qua FindFirstObjectByType<EndRoundUI>() và gọi ShowResult().
/// PlayerHUD đảm bảo chỉ local player mới thấy panel này.
/// </summary>
public class EndRoundUI : MonoBehaviour
{
    public static EndRoundUI instance;

    [Header("UI Elements")]
    [SerializeField] private GameObject panel;           // Panel_EndRound
    [SerializeField] private TextMeshProUGUI resultText;      // Text_Result
    [SerializeField] private TextMeshProUGUI scoreText;       // Text_Score
    [SerializeField] private Button backToLobbyBtn;  // Button_ReLobby
    [SerializeField] private Button restartBtn;      // Button_ReStart

    [Header("Team Colors (optional)")]
    [SerializeField] private Color teamAColor = new Color(0.2f, 0.6f, 1f);  // xanh
    [SerializeField] private Color teamBColor = new Color(1f, 0.3f, 0.3f);  // đỏ
    [SerializeField] private Color drawColor = Color.yellow;

    void Awake()
    {
        // Chỉ giữ instance của local player
        instance = this;
        if (panel) panel.SetActive(false);
    }

    void Start()
    {
        // Auto-find buttons nếu chưa gán trong Inspector (tìm theo tên)
        if (backToLobbyBtn == null || restartBtn == null)
        {
            foreach (var btn in GetComponentsInChildren<Button>(true))
            {
                string n = btn.gameObject.name.ToLower();
                if (backToLobbyBtn == null && (n.Contains("lobby") || n.Contains("back") || n.Contains("relobby")))
                    backToLobbyBtn = btn;
                else if (restartBtn == null && (n.Contains("restart") || n.Contains("reStart") || n.Contains("again")))
                    restartBtn = btn;
            }
        }

        if (backToLobbyBtn == null)
            Debug.LogError("[EndRoundUI] Không tìm thấy Button 'Back to Lobby'! Đặt tên button chứa 'lobby' hoặc kéo vào Inspector.");
        if (restartBtn == null)
            Debug.LogWarning("[EndRoundUI] Không tìm thấy Button 'Restart' (chỉ host cần). Đặt tên chứa 'restart' hoặc kéo vào Inspector.");

        var runner = FindFirstObjectByType<NetworkRunner>();
        bool isHost = runner != null && runner.IsServer;

        if (restartBtn != null) restartBtn.gameObject.SetActive(isHost);
        if (backToLobbyBtn != null) backToLobbyBtn.onClick.AddListener(OnBackToLobby);
        if (restartBtn != null)     restartBtn.onClick.AddListener(OnRestart);

        Debug.Log($"[EndRoundUI] Init | isHost={isHost} lobbyBtn={backToLobbyBtn?.name ?? "NULL"} restartBtn={restartBtn?.name ?? "NULL"}");
    }

    /// <summary>Gọi từ GameManager.ShowEndGameResult()</summary>
    public void ShowResult(string result, int scoreA, int scoreB)
    {
        if (panel) panel.SetActive(true);

        if (resultText)
        {
            resultText.text = result;
            // Tô màu chữ kết quả theo team thắng
            if (scoreA > scoreB) resultText.color = teamAColor;
            else if (scoreB > scoreA) resultText.color = teamBColor;
            else resultText.color = drawColor;
        }

        if (scoreText)
            scoreText.text = $"Xanh  {scoreA}  —  {scoreB}  Đỏ";

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void OnBackToLobby()
    {
        var runner = FindFirstObjectByType<NetworkRunner>();
        if (runner != null) _ = runner.Shutdown();
        // OnShutdown trong LobbyManager sẽ load lại scene Lobby
    }

    public void OnRestart()
    {
        var runner = FindFirstObjectByType<NetworkRunner>();
        if (runner != null && runner.IsServer)
            runner.LoadScene(SceneRef.FromIndex(
                SceneUtility.GetBuildIndexByScenePath("Assets/_Project/Scenes/Game.unity")));
    }
}