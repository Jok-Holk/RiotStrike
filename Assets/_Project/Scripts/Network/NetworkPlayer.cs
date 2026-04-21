using Fusion;
using TMPro;
using UnityEngine;

/// <summary>
/// Team 0 = XANH | Team 1 = ĐỎ
/// </summary>
public class NetworkPlayer : NetworkBehaviour
{
    [Networked] public int Team { get; set; }
    [Networked] public NetworkString<_32> NickName { get; set; }

    [Header("Team Materials — 0=Xanh, 1=Đỏ")]
    [SerializeField] private Material teamAMat; // Xanh (Team 0)
    [SerializeField] private Material teamBMat; // Đỏ  (Team 1)

    [Header("Nameplate")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Transform nameplateRoot;
    [SerializeField] private Vector3 nameplateOffset = new Vector3(0, 0.3f, 0);

    private SkinnedMeshRenderer _bodyRenderer;
    private Animator  _animator;
    private Transform _headBone;
    private int    _lastTeam     = -1;
    private string _lastNickName = "";

    public override void Spawned()
    {
        var nt = GetComponent<NetworkTransform>();
        if (nt != null) nt.enabled = false;

        _bodyRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        _animator     = GetComponentInChildren<Animator>();
        if (_animator != null)
            _headBone = _animator.GetBoneTransform(HumanBodyBones.Head);

        if (!Object.HasInputAuthority)
        {
            // Remote player: tắt camera + AudioListener
            var cam = GetComponentInChildren<Camera>();
            if (cam) cam.enabled = false;

            // Tắt AudioListener — fix lỗi "2 audio listeners in scene"
            var audio = GetComponentInChildren<AudioListener>();
            if (audio) audio.enabled = false;
        }
        else
        {
            // Local player: tất cả SkinnedMeshRenderer → ShadowsOnly để không thấy body/tay mình trong FPV.
            // Áp dụng cho tất cả thay vì chỉ _bodyRenderer đầu tiên,
            // vì character có thể gồm nhiều mesh riêng (body, tay, phụ kiện...).
            foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>())
                smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;

            // Bật AudioListener cho local player — fix lỗi "no audio listeners in scene"
            var audioListener = GetComponentInChildren<AudioListener>(true);
            if (audioListener == null)
            {
                // Prefab không có AudioListener → thêm vào camera hoặc root object
                var cam = GetComponentInChildren<Camera>(true);
                audioListener = cam != null
                    ? cam.gameObject.AddComponent<AudioListener>()
                    : gameObject.AddComponent<AudioListener>();
                Debug.Log($"[NetworkPlayer] Tự thêm AudioListener vào {audioListener.gameObject.name}");
            }
            audioListener.enabled = true;
        }

        if (nameplateRoot != null)
            nameplateRoot.gameObject.SetActive(!Object.HasInputAuthority);

        if (Object.HasStateAuthority)
        {
            string nick = LobbyManager.instance != null
                ? LobbyManager.instance.GetLocalNickName() : "";
            if (string.IsNullOrEmpty(nick)) nick = "Player";
            NickName = nick;
        }

        ApplyTeamMaterial();
        ApplyNickName();
    }

    public void RequestChangeTeam(int newTeam)
    {
        if (!Object.HasInputAuthority) return;
        RPC_RequestTeam(newTeam);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_RequestTeam(int newTeam) { Team = newTeam; RPC_NotifyTeamChanged(); }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_NotifyTeamChanged() => FindFirstObjectByType<UIRoomManager>()?.RefreshPlayerList();

    public override void Render()
    {
        if (Team != _lastTeam)
        {
            _lastTeam = Team;
            ApplyTeamMaterial();
            ApplyNickName();
        }

        string currentNick = NickName.ToString();
        if (currentNick != _lastNickName)
        {
            _lastNickName = currentNick;
            ApplyNickName();
        }

        if (nameplateRoot != null && _headBone != null && nameplateRoot.gameObject.activeSelf)
        {
            nameplateRoot.position = _headBone.position + nameplateOffset;
            var cam = Camera.main;
            if (cam != null)
                nameplateRoot.rotation = Quaternion.LookRotation(
                    nameplateRoot.position - cam.transform.position);
        }
    }

    void ApplyTeamMaterial()
    {
        if (_bodyRenderer == null) return;
        // Team 0 = xanh = teamAMat, Team 1 = đỏ = teamBMat
        if (Team == 0 && teamAMat != null)      _bodyRenderer.material = teamAMat;
        else if (Team == 1 && teamBMat != null) _bodyRenderer.material = teamBMat;
    }

    void ApplyNickName()
    {
        if (nameText == null) return;
        nameText.text = NickName.ToString();
        int localTeam = -1;
        foreach (var p in FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None))
            if (p.Object.HasInputAuthority) { localTeam = p.Team; break; }
        nameText.color = (localTeam == -1 || Team == localTeam) ? Color.white : Color.red;
    }
}
