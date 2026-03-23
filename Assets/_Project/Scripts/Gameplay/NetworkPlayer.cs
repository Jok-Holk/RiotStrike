using Fusion;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour
{
    [Networked] public int Team { get; set; }
    [Networked] public NetworkString<_32> NickName { get; set; }

    [SerializeField] private Material teamAMat;
    [SerializeField] private Material teamBMat;

    private SkinnedMeshRenderer _meshRenderer;

    public override void Spawned()
    {
        // Xóa NetworkTransform nếu còn
        var nt = GetComponent<NetworkTransform>();
        if (nt != null) nt.enabled = false;

        _meshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();

        Debug.Log($"Spawned — HasInputAuthority: {Object.HasInputAuthority}, HasStateAuthority: {Object.HasStateAuthority}");

        var cam = GetComponentInChildren<Camera>();
        Debug.Log($"Camera found: {cam != null}, Camera enabled: {cam?.enabled}");

        if (!Object.HasInputAuthority)
        {
            if (cam) cam.enabled = false;
            var audioListener = GetComponentInChildren<AudioListener>();
            if (audioListener) audioListener.enabled = false;
        }

        if (Object.HasStateAuthority)
        {
            NickName = PlayerPrefs.GetString("NickName", "Player");
        }

        ApplyTeamMaterial();
    }

    void ApplyTeamMaterial()
    {
        if (_meshRenderer == null) return;
        if (Team == 0 && teamAMat != null) _meshRenderer.material = teamAMat;
        else if (Team == 1 && teamBMat != null) _meshRenderer.material = teamBMat;
    }

    public override void Render()
    {
        ApplyTeamMaterial();
    }
}