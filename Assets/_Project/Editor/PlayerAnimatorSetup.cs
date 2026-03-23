using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class PlayerAnimatorSetup : EditorWindow
{
    [MenuItem("RiotStrike/Setup Player Animator")]
    public static void SetupAnimator()
    {
        string animPath = "Assets/_Project/Animations";
        string controllerPath = "Assets/_Project/Animations/PlayerAnimator.controller";
        string maskPath = "Assets/_Project/Animations/UpperBodyMask.mask";

        AssetDatabase.DeleteAsset(controllerPath);
        AssetDatabase.Refresh();

        AnimatorController c = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        c.AddParameter("Forward", AnimatorControllerParameterType.Float);
        c.AddParameter("Strafe", AnimatorControllerParameterType.Float);
        c.AddParameter("Crouch", AnimatorControllerParameterType.Bool);
        c.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
        c.AddParameter("Death", AnimatorControllerParameterType.Trigger);
        c.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
        c.AddParameter("WeaponType", AnimatorControllerParameterType.Int);
        c.AddParameter("Fire", AnimatorControllerParameterType.Trigger);
        c.AddParameter("Reload", AnimatorControllerParameterType.Trigger);

        SetupBaseLayer(c, animPath);
        SetupWeaponLayer(c, animPath, maskPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("PlayerAnimator setup complete!");
    }

    static void SetupBaseLayer(AnimatorController c, string animPath)
    {
        var sm = c.layers[0].stateMachine;
        sm.entryPosition = new Vector2(-200, 0);
        sm.anyStatePosition = new Vector2(-200, 60);

        var standState = sm.AddState("Stand", new Vector2(100, 0));
        var standTree = new BlendTree();
        AssetDatabase.AddObjectToAsset(standTree, c);
        standTree.name = "Stand";
        standTree.blendType = BlendTreeType.FreeformCartesian2D;
        standTree.blendParameter = "Strafe";
        standTree.blendParameterY = "Forward";
        standTree.useAutomaticThresholds = false;

        AddMotion(standTree, c, animPath + "/Rifle/Rifle Idle.fbx", 0, 0);
        AddMotion(standTree, c, animPath + "/Rifle/Walk With Rifle.fbx", 0, 1);
        AddMotion(standTree, c, animPath + "/Rifle/Walking Backwards.fbx", 0, -1);
        AddMotion(standTree, c, animPath + "/Rifle/Strafe Left.fbx", -1, 0);
        AddMotion(standTree, c, animPath + "/Rifle/Strafe Right.fbx", 1, 0);
        AddMotion(standTree, c, animPath + "/Rifle/Rifle Run.fbx", 0, 2);
        standState.motion = standTree;
        sm.defaultState = standState;

        var crouchState = sm.AddState("Crouch", new Vector2(100, 120));
        var crouchTree = new BlendTree();
        AssetDatabase.AddObjectToAsset(crouchTree, c);
        crouchTree.name = "Crouch";
        crouchTree.blendType = BlendTreeType.FreeformCartesian2D;
        crouchTree.blendParameter = "Strafe";
        crouchTree.blendParameterY = "Forward";
        crouchTree.useAutomaticThresholds = false;

        AddMotion(crouchTree, c, animPath + "/Rifle/Idle Crouching Aiming.fbx", 0, 0);
        AddMotion(crouchTree, c, animPath + "/Rifle/Rifle Crouch Walk.fbx", 0, 1);
        AddMotion(crouchTree, c, animPath + "/Rifle/Walk Crouching Backward.fbx", 0, -1);
        AddMotion(crouchTree, c, animPath + "/Rifle/Crouch Walk Strafe Left.fbx", -1, 0);
        AddMotion(crouchTree, c, animPath + "/Rifle/Crouch Walk Strafe Right.fbx", 1, 0);
        crouchState.motion = crouchTree;

        var toC = standState.AddTransition(crouchState);
        toC.hasExitTime = false;
        toC.duration = 0.15f;
        toC.AddCondition(AnimatorConditionMode.If, 0, "Crouch");

        var toS = crouchState.AddTransition(standState);
        toS.hasExitTime = false;
        toS.duration = 0.15f;
        toS.AddCondition(AnimatorConditionMode.IfNot, 0, "Crouch");

        var jumpState = sm.AddState("Jump", new Vector2(350, 0));
        jumpState.motion = LoadClip(animPath + "/Rifle/Pistol Jump.fbx");
        var jumpTrans = sm.AddAnyStateTransition(jumpState);
        jumpTrans.hasExitTime = false;
        jumpTrans.duration = 0.05f;
        jumpTrans.canTransitionToSelf = false;
        jumpTrans.AddCondition(AnimatorConditionMode.If, 0, "Jump");
        var jumpExit = jumpState.AddTransition(standState);
        jumpExit.hasExitTime = true;
        jumpExit.exitTime = 0.85f;
        jumpExit.duration = 0.15f;

        var deathState = sm.AddState("Death", new Vector2(350, 120));
        deathState.motion = LoadClip(animPath + "/Rifle/Dying.fbx");
        var deathTrans = sm.AddAnyStateTransition(deathState);
        deathTrans.hasExitTime = false;
        deathTrans.duration = 0.1f;
        deathTrans.canTransitionToSelf = false;
        deathTrans.AddCondition(AnimatorConditionMode.If, 0, "Death");

        var layers = c.layers;
        layers[0].stateMachine = sm;
        c.layers = layers;
    }

    static void SetupWeaponLayer(AnimatorController c, string animPath, string maskPath)
    {
        var weaponLayer = new AnimatorControllerLayer();
        weaponLayer.name = "Weapon Layer";
        weaponLayer.defaultWeight = 1f;
        weaponLayer.blendingMode = AnimatorLayerBlendingMode.Override;

        AvatarMask mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(maskPath);
        if (mask != null) weaponLayer.avatarMask = mask;

        var sm = new AnimatorStateMachine();
        sm.name = "Weapon Layer";
        AssetDatabase.AddObjectToAsset(sm, c);
        sm.hideFlags = HideFlags.HideInHierarchy;
        sm.entryPosition = new Vector2(-200, 0);
        sm.anyStatePosition = new Vector2(-200, 60);
        weaponLayer.stateMachine = sm;

        var rifleState = sm.AddState("Rifle", new Vector2(100, 0));
        rifleState.motion = LoadClip(animPath + "/Rifle/Rifle Aiming Idle.fbx");
        sm.defaultState = rifleState;

        var pistolState = sm.AddState("Pistol", new Vector2(100, 120));
        pistolState.motion = LoadClip(animPath + "/Pistol/In-Place/W1_Stand_Aim_Idle_IPC.fbx");

        var toPistol = rifleState.AddTransition(pistolState);
        toPistol.hasExitTime = false;
        toPistol.duration = 0.1f;
        toPistol.AddCondition(AnimatorConditionMode.Equals, 1, "WeaponType");

        var toRifle = pistolState.AddTransition(rifleState);
        toRifle.hasExitTime = false;
        toRifle.duration = 0.1f;
        toRifle.AddCondition(AnimatorConditionMode.Equals, 0, "WeaponType");

        var fireState = sm.AddState("Fire", new Vector2(350, 0));
        fireState.motion = LoadClip(animPath + "/Rifle/Firing Rifle.fbx");
        var fireTrans = sm.AddAnyStateTransition(fireState);
        fireTrans.hasExitTime = false;
        fireTrans.duration = 0.05f;
        fireTrans.canTransitionToSelf = false;
        fireTrans.AddCondition(AnimatorConditionMode.If, 0, "Fire");
        var fireExit = fireState.AddTransition(rifleState);
        fireExit.hasExitTime = true;
        fireExit.exitTime = 1f;
        fireExit.duration = 0.05f;

        var reloadState = sm.AddState("Reload", new Vector2(350, 120));
        reloadState.motion = LoadClip(animPath + "/Rifle/Reloading.fbx");
        var reloadTrans = sm.AddAnyStateTransition(reloadState);
        reloadTrans.hasExitTime = false;
        reloadTrans.duration = 0.1f;
        reloadTrans.canTransitionToSelf = false;
        reloadTrans.AddCondition(AnimatorConditionMode.If, 0, "Reload");
        var reloadExit = reloadState.AddTransition(rifleState);
        reloadExit.hasExitTime = true;
        reloadExit.exitTime = 1f;
        reloadExit.duration = 0.1f;

        var hitState = sm.AddState("Hit", new Vector2(350, 240));
        hitState.motion = LoadClip(animPath + "/Rifle/Hit Reaction.fbx");
        var hitTrans = sm.AddAnyStateTransition(hitState);
        hitTrans.hasExitTime = false;
        hitTrans.duration = 0.05f;
        hitTrans.canTransitionToSelf = false;
        hitTrans.AddCondition(AnimatorConditionMode.If, 0, "Hit");
        var hitExit = hitState.AddTransition(rifleState);
        hitExit.hasExitTime = true;
        hitExit.exitTime = 0.9f;
        hitExit.duration = 0.1f;

        var layers = new System.Collections.Generic.List<AnimatorControllerLayer>(c.layers);
        layers.Add(weaponLayer);
        c.layers = layers.ToArray();
    }

    static void AddMotion(BlendTree tree, AnimatorController c, string path, float x, float y)
    {
        var clip = LoadClip(path);
        if (clip == null) { Debug.LogWarning("Clip not found: " + path); return; }
        tree.AddChild(clip, new Vector2(x, y));
    }

    static AnimationClip LoadClip(string path)
    {
        foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
            if (obj is AnimationClip ac && !ac.name.StartsWith("__preview__"))
                return ac;
        Debug.LogWarning("No clip at: " + path);
        return null;
    }
}