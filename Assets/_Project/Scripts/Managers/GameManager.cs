using System.Collections;
using Fusion;
using TMPro;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager instance;

    [Header("References")]
    [SerializeField] private TimerManager timerManager;

    // Score text: để trống OK — GameManager tự tìm theo tên object
    // Nếu muốn gán tay: kéo Text vào đây
    [Header("Score UI (để trống → tự tìm bằng tên Text_ScoreA / Text_ScoreB)")]
    [SerializeField] private TextMeshProUGUI teamAScoreText;
    [SerializeField] private TextMeshProUGUI teamBScoreText;

    [Networked] public int TeamAScore { get; set; }
    [Networked] public int TeamBScore { get; set; }
    [Networked] public bool GameStarted { get; set; }
    [Networked] public bool GameEnded { get; set; }

    private ChangeDetector _changeDetector;

    public override void Spawned()
    {
        instance = this;
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        // Tự tìm Score Text nếu chưa gán
        if (teamAScoreText == null) teamAScoreText = FindTMP("Text_ScoreA");
        if (teamBScoreText == null) teamBScoreText = FindTMP("Text_ScoreB");
        // Tự tìm TimerManager nếu SerializeField chưa kéo trong Inspector
        if (timerManager == null)
        {
            timerManager = FindFirstObjectByType<TimerManager>();
            if (timerManager == null)
                Debug.LogWarning("[GM] TimerManager không tìm thấy! Kéo vào Inspector hoặc đặt trong scene.");
        }

        if (!Object.HasStateAuthority) return;

        TeamAScore = 0;
        TeamBScore = 0;
        GameStarted = true;
        GameEnded = false;

        StartCoroutine(ApplyLobbyConfig());
    }

    static TextMeshProUGUI FindTMP(string name)
    {
        foreach (var t in FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None))
            if (t.gameObject.name == name) return t;
        return null;
    }

    IEnumerator ApplyLobbyConfig()
    {
        // Không chờ — áp dụng ngay lập tức để tránh timer nhảy từ 10:00 → 3:00.
        // TimerManager.Spawned() đã đọc GameConfig, nhưng gọi lại ở đây để đồng bộ nếu
        // RoomPlayerData vẫn còn sống (hiếm gặp — thường null sau khi load scene).
        int pistolTime, rifleTime;

        if (RoomPlayerData.instance != null)
        {
            pistolTime = RoomPlayerData.instance.PistolTime;
            rifleTime  = RoomPlayerData.instance.RifleTime;
            Debug.Log($"[GM] Config từ RoomPlayerData: pistol={pistolTime}s rifle={rifleTime}s");
        }
        else
        {
            pistolTime = GameConfig.PistolTime;
            rifleTime  = GameConfig.RifleTime;
            Debug.Log($"[GM] Config từ GameConfig: pistol={pistolTime}s rifle={rifleTime}s");
        }

        int roundTime = pistolTime + rifleTime;
        if (roundTime > 0 && timerManager != null)
        {
            timerManager.SetTimings(roundTime, pistolTime);
            Debug.Log($"[GM] Timer set: round={roundTime}s pistol={pistolTime}s");
        }
        else
            Debug.LogWarning($"[GM] roundTime={roundTime} hoặc timerManager null — dùng default");

        yield break;
    }

    public override void Render()
    {
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(TeamAScore):
                case nameof(TeamBScore):
                    UpdateScoreUI();
                    break;
                case nameof(GameEnded):
                    if (GameEnded) ShowEndGame();
                    break;
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;
        if (!GameStarted || GameEnded) return;
        if (timerManager != null && timerManager.IsTimeUp()) EndGame();
    }

    public void RegisterKill(int killerTeam)
    {
        if (!Object.HasStateAuthority || GameEnded) return;
        if (killerTeam == 0) TeamAScore++;
        else TeamBScore++;
        Debug.Log($"[GM] Kill — Xanh={TeamAScore} Đỏ={TeamBScore}");
    }

    void EndGame()
    {
        GameEnded = true;
        RPC_NotifyGameEnd(TeamAScore, TeamBScore);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_NotifyGameEnd(int scoreA, int scoreB) => ShowEndGameResult(scoreA, scoreB);

    void ShowEndGame() => ShowEndGameResult(TeamAScore, TeamBScore);

    void ShowEndGameResult(int scoreA, int scoreB)
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        string result;
        if (scoreA > scoreB) result = "TEAM XANH THẮNG!";
        else if (scoreB > scoreA) result = "TEAM ĐỎ THẮNG!";
        else result = "HÒA!";

        var endUI = FindFirstObjectByType<EndRoundUI>();
        if (endUI != null) endUI.ShowResult(result, scoreA, scoreB);
        else Debug.LogWarning("[GM] EndRoundUI không tìm thấy!");

        Debug.Log($"[GM] Ended — Xanh={scoreA} Đỏ={scoreB}");
    }

    void UpdateScoreUI()
    {
        if (teamAScoreText != null) teamAScoreText.text = TeamAScore.ToString();
        if (teamBScoreText != null) teamBScoreText.text = TeamBScore.ToString();
        FindFirstObjectByType<ScoreboardUI>()?.Refresh();
    }
}