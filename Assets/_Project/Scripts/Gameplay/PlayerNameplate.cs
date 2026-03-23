using Fusion;
using TMPro;
using UnityEngine;

public class PlayerNameplate : NetworkBehaviour
{
    [SerializeField] private GameObject nameplateRoot;
    [SerializeField] private TextMeshPro nameText;
    [SerializeField] private Color teammateColor = Color.white;
    [SerializeField] private Color enemyColor = Color.red;

    private Transform _cameraTransform;
    private Transform _headBone;

    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {
            nameplateRoot.SetActive(false);
            return;
        }

        _cameraTransform = Camera.main?.transform;

        // Lấy head bone để follow
        var animator = GetComponentInChildren<Animator>();
        if (animator != null)
            _headBone = animator.GetBoneTransform(HumanBodyBones.Head);

        nameplateRoot.SetActive(true);
    }

    public override void Render()
    {
        if (Object.HasInputAuthority || !nameplateRoot.activeSelf) return;

        // Follow head bone
        if (_headBone != null)
            nameplateRoot.transform.position = _headBone.position + Vector3.up * 0.3f;

        // Billboard
        if (_cameraTransform != null)
            nameplateRoot.transform.LookAt(
                nameplateRoot.transform.position + _cameraTransform.rotation * Vector3.forward,
                _cameraTransform.rotation * Vector3.up
            );

        // Update mỗi frame vì NickName và Team có thể sync trễ
        UpdateNameplate();
    }

    void UpdateNameplate()
    {
        var networkPlayer = GetComponent<NetworkPlayer>();
        if (networkPlayer == null) return;

        nameText.text = networkPlayer.NickName.ToString();

        foreach (var player in Runner.ActivePlayers)
        {
            if (!Runner.TryGetPlayerObject(player, out NetworkObject localObj)) continue;
            if (!localObj.HasInputAuthority) continue;

            var localNetPlayer = localObj.GetComponent<NetworkPlayer>();
            if (localNetPlayer == null) break;

            nameText.color = localNetPlayer.Team == networkPlayer.Team
                ? teammateColor
                : enemyColor;
            break;
        }
    }
}