using Fusion;
using TMPro;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour
{
    [Networked] public int Team { get; set; }
    [Networked] public NetworkString<_32> NickName { get; set; }

    [Header("Team Materials")]
    [SerializeField] private Material teamAMat;
    [SerializeField] private Material teamBMat;

    [Header("Nameplate")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Transform nameplateRoot;
    [SerializeField] private Vector3 nameplateOffset = new Vector3(0, 0.3f, 0);

    private SkinnedMeshRenderer _meshRenderer;
    private Animator _animator;
    private Transform _headBone;
    private int _lastTeam = -1;
    private string _lastNickName = "";

    public override void Spawned()
    {
        var nt = GetComponent<NetworkTransform>();
        if (nt != null) nt.enabled = false;

        _meshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        _animator     = GetComponentInChildren<Animator>();

        if (_animator != null)
            _headBone = _animator.GetBoneTransform(HumanBodyBones.Head);

        var cam = GetComponentInChildren<Camera>();
        Debug.Log($"Spawned — HasInputAuthority: {Object.HasInputAuthority}, HasStateAuthority: {Object.HasStateAuthority}");

        if (!Object.HasInputAuthority)
        {
            if (cam) cam.enabled = false;
            var audio = GetComponentInChildren<AudioListener>();
            if (audio) audio.enabled = false;
        }
        else
        {
            foreach (var r in GetComponentsInChildren<SkinnedMeshRenderer>())
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        }

        if (nameplateRoot != null)
            nameplateRoot.gameObject.SetActive(!Object.HasInputAuthority);

        if (Object.HasStateAuthority)
            NickName = PlayerPrefs.GetString("NickName", "Player");

        ApplyTeamMaterial();
        ApplyNickName();
    }

    // ── Gọi từ UIRoomManager khi player bấm Join Team ──
    public void RequestChangeTeam(int newTeam)
    {
        // Chỉ local player của mình mới gọi được
        if (!Object.HasInputAuthority) return;
        RPC_RequestTeam(newTeam);
    }

    // ── Client gửi lên host ──
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_RequestTeam(int newTeam)
    {
        // Host kiểm tra hợp lệ rồi set
        Team = newTeam;
        // Sau khi set xong, broadcast để mọi client refresh UI
        RPC_NotifyTeamChanged();
    }

    // ── Host broadcast cho tất cả ──
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_NotifyTeamChanged()
    {
        // Mỗi client tìm UIRoomManager và refresh
        FindFirstObjectByType<UIRoomManager>()?.RefreshPlayerList();
    }

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
        if (_meshRenderer == null) return;
        if (Team == 0 && teamAMat != null)
            _meshRenderer.material = teamAMat;
        else if (Team == 1 && teamBMat != null)
            _meshRenderer.material = teamBMat;
    }

    void ApplyNickName()
    {
        if (nameText == null) return;
        nameText.text = NickName.ToString();

        int localTeam = -1;
        foreach (var p in FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None))
        {
            if (p.Object.HasInputAuthority) { localTeam = p.Team; break; }
        }

        nameText.color = (localTeam == -1 || Team == localTeam)
            ? Color.white : Color.red;
    }
}