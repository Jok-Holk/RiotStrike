using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

public class PlayerAnimatorSetup : EditorWindow
{
    [MenuItem("RiotStrike/Setup Player Animator")]
    public static void SetupAnimator()
    {
        string animPath = "Assets/_Project/Animations";
        string controllerPath = "Assets/_Project/Animations/PlayerAnimator.controller";
        string maskPath = "Assets/_Project/Animations/UpperBodyMask.mask";

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            Debug.Log("Created new AnimatorController");
        }

        SetupParameters(controller);
        SetupBaseLayer(controller, animPath);
        SetupWeaponLayer(controller, animPath, maskPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("PlayerAnimator setup complete!");
    }

    static void SetupParameters(AnimatorController c)
    {
        AddParamIfMissing(c, "Speed", AnimatorControllerParameterType.Float);
        AddParamIfMissing(c, "Forward", AnimatorControllerParameterType.Float);
        AddParamIfMissing(c, "Strafe", AnimatorControllerParameterType.Float);
        AddParamIfMissing(c, "Crouch", AnimatorControllerParameterType.Bool);
        AddParamIfMissing(c, "Jump", AnimatorControllerParameterType.Trigger);
        AddParamIfMissing(c, "Death", AnimatorControllerParameterType.Trigger);
        AddParamIfMissing(c, "Hit", AnimatorControllerParameterType.Trigger);
        AddParamIfMissing(c, "WeaponType", AnimatorControllerParameterType.Int);
        AddParamIfMissing(c, "Fire", AnimatorControllerParameterType.Trigger);
        AddParamIfMissing(c, "Reload", AnimatorControllerParameterType.Trigger);
    }

    static void AddParamIfMissing(AnimatorController c, string name, AnimatorControllerParameterType type)
    {
        foreach (var p in c.parameters)
            if (p.name == name) return;
        c.AddParameter(name, type);
    }

    static void SetupBaseLayer(AnimatorController c, string animPath)
    {
        var layer = c.layers[0];
        layer.name = "Base Layer";
        var stateMachine = layer.stateMachine;
        stateMachine.anyStatePosition = new Vector2(0, 0);
        stateMachine.entryPosition = new Vector2(0, 50);

        // --- Locomotion Blend Tree ---
        var locomotionState = stateMachine.AddState("Locomotion", new Vector2(250, 50));
        var locomotionTree = new BlendTree();
        AssetDatabase.AddObjectToAsset(locomotionTree, c);
        locomotionTree.name = "Locomotion";
        locomotionTree.blendType = BlendTreeType.SimpleDirectional2D;
        locomotionTree.blendParameter = "Forward";
        locomotionTree.blendParameterY = "Strafe";

        AddClipToTree(locomotionTree, c, animPath + "/Rifle/Rifle Idle.fbx", 0, 0);
        AddClipToTree(locomotionTree, c, animPath + "/Rifle/Walk With Rifle.fbx", 0, 1);
        AddClipToTree(locomotionTree, c, animPath + "/Rifle/Walking Backwards.fbx", 0, -1);
        AddClipToTree(locomotionTree, c, animPath + "/Rifle/Strafe Left.fbx", -1, 0);
        AddClipToTree(locomotionTree, c, animPath + "/Rifle/Strafe Right.fbx", 1, 0);
        AddClipToTree(locomotionTree, c, animPath + "/Rifle/Rifle Run.fbx", 0, 2);

        locomotionState.motion = locomotionTree;
        stateMachine.defaultState = locomotionState;

        // --- Crouch Blend Tree ---
        var crouchState = stateMachine.AddState("Crouch Locomotion", new Vector2(250, 150));
        var crouchTree = new BlendTree();
        AssetDatabase.AddObjectToAsset(crouchTree, c);
        crouchTree.name = "Crouch Locomotion";
        crouchTree.blendType = BlendTreeType.SimpleDirectional2D;
        crouchTree.blendParameter = "Forward";
        crouchTree.blendParameterY = "Strafe";

        AddClipToTree(crouchTree, c, animPath + "/Rifle/Idle Crouching Aiming.fbx", 0, 0);
        AddClipToTree(crouchTree, c, animPath + "/Rifle/Rifle Crouch Walk.fbx", 0, 1);
        AddClipToTree(crouchTree, c, animPath + "/Rifle/Walk Crouching Backward.fbx", 0, -1);
        AddClipToTree(crouchTree, c, animPath + "/Rifle/Crouch Walk Strafe Left.fbx", -1, 0);
        AddClipToTree(crouchTree, c, animPath + "/Rifle/Crouch Walk Strafe Right.fbx", 1, 0);

        crouchState.motion = crouchTree;

        // Locomotion <-> Crouch transitions
        var toCrouch = locomotionState.AddTransition(crouchState);
        toCrouch.AddCondition(AnimatorConditionMode.If, 0, "Crouch");
        toCrouch.duration = 0.1f;

        var toStand = crouchState.AddTransition(locomotionState);
        toStand.AddCondition(AnimatorConditionMode.IfNot, 0, "Crouch");
        toStand.duration = 0.1f;

        // --- Jump ---
        var jumpState = stateMachine.AddState("Jump", new Vector2(250, 300));
        jumpState.motion = LoadClip(animPath + "/Rifle/Pistol Jump.fbx");

        var jumpTrans = stateMachine.AddAnyStateTransition(jumpState);
        jumpTrans.AddCondition(AnimatorConditionMode.If, 0, "Jump");
        jumpTrans.duration = 0.1f;
        jumpTrans.canTransitionToSelf = false;

        var jumpToLoco = jumpState.AddTransition(locomotionState);
        jumpToLoco.hasExitTime = true;
        jumpToLoco.exitTime = 0.9f;
        jumpToLoco.duration = 0.1f;

        // --- Death ---
        var deathState = stateMachine.AddState("Death", new Vector2(250, 400));
        deathState.motion = LoadClip(animPath + "/Rifle/Dying.fbx");

        var deathTrans = stateMachine.AddAnyStateTransition(deathState);
        deathTrans.AddCondition(AnimatorConditionMode.If, 0, "Death");
        deathTrans.duration = 0.1f;
        deathTrans.canTransitionToSelf = false;

        var layers = c.layers;
        layers[0] = layer;
        c.layers = layers;
    }

    static void SetupWeaponLayer(AnimatorController c, string animPath, string maskPath)
    {
        // Remove existing weapon layer if any
        var layerList = new System.Collections.Generic.List<AnimatorControllerLayer>(c.layers);
        layerList.RemoveAll(l => l.name == "Weapon Layer");
        c.layers = layerList.ToArray();

        var weaponLayer = new AnimatorControllerLayer();
        weaponLayer.name = "Weapon Layer";
        weaponLayer.defaultWeight = 1f;
        weaponLayer.blendingMode = AnimatorLayerBlendingMode.Override;

        AvatarMask mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(maskPath);
        if (mask != null) weaponLayer.avatarMask = mask;
        else Debug.LogWarning("UpperBodyMask not found at: " + maskPath);

        var sm = new AnimatorStateMachine();
        sm.name = "Weapon Layer";
        AssetDatabase.AddObjectToAsset(sm, c);
        sm.hideFlags = HideFlags.HideInHierarchy;
        weaponLayer.stateMachine = sm;

        // Rifle state
        var rifleState = sm.AddState("Rifle", new Vector2(250, 50));
        rifleState.motion = LoadClip(animPath + "/Rifle/Rifle Aiming Idle.fbx");
        sm.defaultState = rifleState;

        // Pistol state
        var pistolState = sm.AddState("Pistol", new Vector2(250, 150));
        pistolState.motion = LoadClip(animPath + "/Pistol/In-Place/W1_Stand_Aim_Idle_IPC.fbx");

        // Rifle <-> Pistol
        var toPistol = rifleState.AddTransition(pistolState);
        toPistol.AddCondition(AnimatorConditionMode.Equals, 1, "WeaponType");
        toPistol.duration = 0.15f;

        var toRifle = pistolState.AddTransition(rifleState);
        toRifle.AddCondition(AnimatorConditionMode.Equals, 0, "WeaponType");
        toRifle.duration = 0.15f;

        // Fire
        var fireState = sm.AddState("Fire", new Vector2(500, 50));
        fireState.motion = LoadClip(animPath + "/Rifle/Firing Rifle.fbx");

        var fireTrans = sm.AddAnyStateTransition(fireState);
        fireTrans.AddCondition(AnimatorConditionMode.If, 0, "Fire");
        fireTrans.duration = 0.05f;
        fireTrans.canTransitionToSelf = false;

        var fireExit = fireState.AddTransition(rifleState);
        fireExit.hasExitTime = true;
        fireExit.exitTime = 1f;
        fireExit.duration = 0.05f;

        // Reload
        var reloadState = sm.AddState("Reload", new Vector2(500, 150));
        reloadState.motion = LoadClip(animPath + "/Rifle/Reloading.fbx");

        var reloadTrans = sm.AddAnyStateTransition(reloadState);
        reloadTrans.AddCondition(AnimatorConditionMode.If, 0, "Reload");
        reloadTrans.duration = 0.1f;
        reloadTrans.canTransitionToSelf = false;

        var reloadExit = reloadState.AddTransition(rifleState);
        reloadExit.hasExitTime = true;
        reloadExit.exitTime = 1f;
        reloadExit.duration = 0.1f;

        // Hit — upper body only via mask
        var hitState = sm.AddState("Hit", new Vector2(500, 250));
        hitState.motion = LoadClip(animPath + "/Rifle/Hit Reaction.fbx");

        var hitTrans = sm.AddAnyStateTransition(hitState);
        hitTrans.AddCondition(AnimatorConditionMode.If, 0, "Hit");
        hitTrans.duration = 0.05f;
        hitTrans.canTransitionToSelf = false;

        var hitToRifle = hitState.AddTransition(rifleState);
        hitToRifle.hasExitTime = true;
        hitToRifle.exitTime = 0.9f;
        hitToRifle.duration = 0.1f;

        var layers = new System.Collections.Generic.List<AnimatorControllerLayer>(c.layers);
        layers.Add(weaponLayer);
        c.layers = layers.ToArray();
    }

    static void AddClipToTree(BlendTree tree, AnimatorController c, string path, float x, float y)
    {
        var clip = LoadClip(path);
        if (clip == null)
        {
            Debug.LogWarning("Clip not found: " + path);
            return;
        }
        tree.AddChild(clip, new Vector2(x, y));
    }

    static AnimationClip LoadClip(string path)
    {
        var objs = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (var obj in objs)
            if (obj is AnimationClip ac && !ac.name.StartsWith("__preview__"))
                return ac;
        Debug.LogWarning("No clip found at: " + path);
        return null;
    }
}

// Append this class vào file hoặc tạo file riêng