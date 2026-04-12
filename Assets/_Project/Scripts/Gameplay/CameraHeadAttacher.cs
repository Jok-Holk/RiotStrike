using Fusion;
using UnityEngine;

public class CameraHeadAttacher : NetworkBehaviour
{
    [SerializeField] private Vector3 offset = new Vector3(0, 0.1f, 0.15f);

    private Transform _headBone;

    public override void Spawned()
    {
        if (!Object.HasInputAuthority) return;

        var animator = GetComponentInParent<Animator>();
        if (animator == null)
            animator = transform.parent?.GetComponentInChildren<Animator>();

        if (animator != null)
            _headBone = animator.GetBoneTransform(HumanBodyBones.Head);

        if (_headBone != null)
            Debug.Log($"[CameraHolder] Head bone: {_headBone.name}");
        else
            Debug.LogWarning("[CameraHolder] Head bone not found!");
    }

    void LateUpdate()
    {
        // Luôn kiểm tra Object null trước khi truy cập thuộc tính của nó
        if (Object == null || !Object.HasInputAuthority) return;
        if (_headBone == null) return;

        transform.position = _headBone.position + offset;
    }
}