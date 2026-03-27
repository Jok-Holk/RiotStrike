using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gắn vào Canvas_Room trong Lobby.unity
/// Quản lý UI phòng: 2 cột team, nút join team, start game, host config panel
/// </summary>
public class UIRoomManager : MonoBehaviour
{
    [Header("Room Info")]
    [SerializeField] private TextMeshProUGUI roomNameText;
    [SerializeField] private TextMeshProUGUI roomStatusText; // "Đang chờ... (2/8)"

    [Header("Team A")]
    [SerializeField] private Transform teamAPlayerList;   // ScrollView content
    [SerializeField] private Button joinTeamAButton;

    [Header("Team B")]
    [SerializeField] private Transform teamBPlayerList;
    [SerializeField] private Button joinTeamBButton;

    [Header("Player Item Prefab")]
    [SerializeField] private GameObject playerNameItemPrefab; // Text đơn giản hiển thị tên

    [Header("Bottom Buttons")]
    [SerializeField] private Button startGameButton;   // Chỉ host thấy
    [SerializeField] private Button leaveRoomButton;

    [Header("Host Config Panel")]
    [SerializeField] private GameObject hostConfigPanel;  // Ẩn với client

    // Cached
    private NetworkRunner _runner;

    void Start()
    {
        _runner = FindFirstObjectByType<NetworkRunner>();

        joinTeamAButton.onClick.AddListener(() => RequestJoinTeam(0));
        joinTeamBButton.onClick.AddListener(() => RequestJoinTeam(1));
        leaveRoomButton.onClick.AddListener(OnLeaveRoom);

        if (startGameButton != null)
            startGameButton.onClick.AddListener(OnStartGame);

        // Chỉ host thấy Start + HostConfig
        bool isHost = _runner != null && _runner.IsServer;
        if (startGameButton) startGameButton.gameObject.SetActive(isHost);
        if (hostConfigPanel) hostConfigPanel.SetActive(isHost);

        RefreshPlayerList();

        // Cập nhật tên phòng
        if (_runner != null && roomNameText != null)
            roomNameText.text = _runner.SessionInfo.Name;
    }

    void OnEnable()
    {
        // Refresh mỗi lần panel hiện lên
        RefreshPlayerList();
    }

    public void RefreshPlayerList()
    {
        if (_runner == null) return;

        // Xóa cũ
        foreach (Transform c in teamAPlayerList) Destroy(c.gameObject);
        foreach (Transform c in teamBPlayerList) Destroy(c.gameObject);

        int totalPlayers = 0;

        foreach (var player in _runner.ActivePlayers)
        {
            if (!_runner.TryGetPlayerObject(player, out NetworkObject obj)) continue;
            var np = obj.GetComponent<NetworkPlayer>();
            if (np == null) continue;

            totalPlayers++;
            Transform parent = np.Team == 0 ? teamAPlayerList : teamBPlayerList;
            AddPlayerItem(parent, np.NickName.ToString(), np.Object.HasInputAuthority);
        }

        if (roomStatusText != null)
            roomStatusText.text = $"Đang chờ... ({totalPlayers}/{_runner.SessionInfo.MaxPlayers})";
    }

    void AddPlayerItem(Transform parent, string playerName, bool isLocalPlayer)
    {
        if (playerNameItemPrefab == null) return;
        var item = Instantiate(playerNameItemPrefab, parent);
        var text = item.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
            text.text = isLocalPlayer ? $"★ {playerName}" : playerName;
    }

    void RequestJoinTeam(int team)
    {
        if (_runner == null) return;

        var localObj = _runner.GetPlayerObject(_runner.LocalPlayer);
        if (localObj == null) return;

        var np = localObj.GetComponent<NetworkPlayer>();
        if (np == null) return;

        // Chỉ InputAuthority mới set được Team
        if (np.Object.HasInputAuthority)
            np.Team = team;

        RefreshPlayerList();
    }

    void OnLeaveRoom()
    {
        if (_runner != null)
            _ = _runner.Shutdown();
        // LobbyManager.OnShutdown sẽ load về MainMenu
    }

    void OnStartGame()
    {
        if (_runner == null || !_runner.IsServer) return;
        // Host load game scene — players follow tự động qua NetworkSceneManager
        SceneLoader.LoadGame();
    }
}