using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    [Header("Login")]
    [SerializeField] private TMP_InputField nickNameInput;
    [SerializeField] private Button connectButton;

    [Header("Room - Create")]
    [SerializeField] private TMP_InputField roomNameCreateInput;
    [SerializeField] private Button createButton;

    [Header("Room - Join")]
    [SerializeField] private TMP_InputField roomNameJoinInput;
    [SerializeField] private Button joinButton;

    [Header("Room List")]
    [SerializeField] private Transform roomListContent;
    [SerializeField] private UIRoomProfile roomProfilePrefab;

    [Header("Status")]
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Canvas Navigation")]
    [SerializeField] private GameObject canvasMainMenu;
    [SerializeField] private GameObject canvasLobby;
    [SerializeField] private GameObject canvasSettings;
    [SerializeField] private GameObject canvasRoom;

    // Lưu trong memory, không dùng PlayerPrefs để tránh share giữa 2 instance
    private static string _sessionNickName = "";
    private static string _sessionRoomName = "";

    void Start()
    {
        // Mỗi lần Start, nếu chưa có tên thì random mới
        if (string.IsNullOrEmpty(_sessionNickName))
            _sessionNickName = "Player" + Random.Range(100, 999);

        nickNameInput.text = _sessionNickName;
        roomNameCreateInput.text = "Room" + Random.Range(1, 100);
        roomNameJoinInput.text = _sessionRoomName;

        connectButton.onClick.AddListener(OnConnectClick);
        createButton.onClick.AddListener(OnCreateClick);
        joinButton.onClick.AddListener(OnJoinClick);

        createButton.interactable = false;
        joinButton.interactable = false;

        LobbyManager.instance.OnRoomListUpdated += RefreshRoomList;
        LobbyManager.instance.OnJoinFailed += OnJoinFailed;
        LobbyManager.instance.OnConnected += OnConnectedSuccess;
        LobbyManager.instance.OnRoomJoined += OnRoomJoined;

        var existing = LobbyManager.instance.GetRoomList();
        if (existing != null && existing.Count > 0)
            RefreshRoomList(existing);
    }

    void OnEnable()
    {
        if (LobbyManager.instance == null) return;
        var existing = LobbyManager.instance.GetRoomList();
        if (existing != null && existing.Count > 0)
            RefreshRoomList(existing);
    }

    void OnDestroy()
    {
        if (LobbyManager.instance == null) return;
        LobbyManager.instance.OnRoomListUpdated -= RefreshRoomList;
        LobbyManager.instance.OnJoinFailed -= OnJoinFailed;
        LobbyManager.instance.OnConnected -= OnConnectedSuccess;
        LobbyManager.instance.OnRoomJoined -= OnRoomJoined;
    }

    void OnConnectClick()
    {
        string nick = nickNameInput.text.Trim();
        if (string.IsNullOrEmpty(nick)) nick = "Player" + Random.Range(100, 999);

        // Lưu vào session memory, KHÔNG ghi PlayerPrefs
        _sessionNickName = nick;

        SetStatus("Đang kết nối...");
        connectButton.interactable = false;

        // Truyền nick trực tiếp, không qua PlayerPrefs
        LobbyManager.instance.Connect(nick);
    }

    void OnConnectedSuccess()
    {
        createButton.interactable = true;
        joinButton.interactable = true;
        SetStatus("Đã kết nối. Sẵn sàng chơi.");
    }

    void OnCreateClick()
    {
        string room = roomNameCreateInput.text.Trim();
        if (string.IsNullOrEmpty(room)) room = "Room" + Random.Range(1, 999);
        SetStatus("Đang tạo phòng: " + room);
        createButton.interactable = false;
        joinButton.interactable = false;
        LobbyManager.instance.CreateRoom(room);
    }

    void OnJoinClick()
    {
        string room = roomNameJoinInput.text.Trim();
        if (string.IsNullOrEmpty(room))
        {
            SetStatus("Nhập tên phòng hoặc chọn từ danh sách!");
            return;
        }
        _sessionRoomName = room;
        SetStatus("Đang vào phòng: " + room);
        createButton.interactable = false;
        joinButton.interactable = false;
        LobbyManager.instance.JoinRoom(room);
    }

    void OnRoomJoined()
    {
        canvasLobby.SetActive(false);
        canvasRoom.SetActive(true);
    }

    void RefreshRoomList(List<SessionInfo> sessions)
    {
        foreach (Transform child in roomListContent)
            Destroy(child.gameObject);

        if (sessions == null || sessions.Count == 0)
        {
            SetStatus("Chưa có phòng nào. Hãy tạo phòng mới!");
            return;
        }

        foreach (SessionInfo session in sessions)
        {
            if (!session.IsOpen || !session.IsVisible) continue;
            UIRoomProfile item = Instantiate(roomProfilePrefab, roomListContent);
            item.Setup(new RoomProfile
            {
                name = session.Name,
                playerCount = session.PlayerCount,
                maxPlayers = session.MaxPlayers
            }, OnRoomClick);
        }
    }

    void OnRoomClick(string roomName)
    {
        roomNameJoinInput.text = roomName;
        SetStatus($"Đã chọn: {roomName} — Nhấn JOIN để vào.");
    }

    void OnJoinFailed(string reason)
    {
        SetStatus("Thất bại: " + reason);
        createButton.interactable = true;
        joinButton.interactable = true;
        connectButton.interactable = true;
    }

    void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }
}