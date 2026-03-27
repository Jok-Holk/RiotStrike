// PlayerAnimatorSetup.cs
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

        if (File.Exists(controllerPath))
        {
            AssetDatabase.DeleteAsset(controllerPath);
            AssetDatabase.Refresh();
        }

        AnimatorController c = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        c.AddParameter("Forward",    AnimatorControllerParameterType.Float);
        c.AddParameter("Strafe",     AnimatorControllerParameterType.Float);
        c.AddParameter("Crouch",     AnimatorControllerParameterType.Bool);
        c.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
        c.AddParameter("Death",      AnimatorControllerParameterType.Trigger);
        c.AddParameter("Hit",        AnimatorControllerParameterType.Trigger);
        c.AddParameter("Fire",       AnimatorControllerParameterType.Trigger);
        c.AddParameter("Reload",     AnimatorControllerParameterType.Trigger);
        c.AddParameter("WeaponType", AnimatorControllerParameterType.Int); // 0=Rifle, 1=Pistol

        SetupBaseLayer(c, animPath);
        SetupWeaponLayer(c, animPath, maskPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("PlayerAnimator setup complete! (Jump removed)");
    }

    // ─── BASE LAYER (locomotion full body) ───────────────────────────────────
    static void SetupBaseLayer(AnimatorController c, string p)
    {
        var sm = c.layers[0].stateMachine;
        sm.entryPosition    = new Vector2(-300, 0);
        sm.anyStatePosition = new Vector2(-300, 80);
        sm.exitPosition     = new Vector2(-300, 160);

        // Stand Blend Tree
        var standState = sm.AddState("Stand", new Vector2(100, 0));
        standState.motion = BuildLocomotionTree(c, p, false);
        sm.defaultState = standState;

        // Crouch Blend Tree
        var crouchState = sm.AddState("Crouch", new Vector2(100, 140));
        crouchState.motion = BuildLocomotionTree(c, p, true);

        // Stand ↔ Crouch
        var toC = standState.AddTransition(crouchState);
        toC.hasExitTime = false; toC.duration = 0.15f;
        toC.AddCondition(AnimatorConditionMode.If, 0, "Crouch");

        var toS = crouchState.AddTransition(standState);
        toS.hasExitTime = false; toS.duration = 0.15f;
        toS.AddCondition(AnimatorConditionMode.IfNot, 0, "Crouch");

        // Death
        var deathState = sm.AddState("Death", new Vector2(350, 140));
        deathState.motion = LoadClip(p + "/Rifle/Dying.fbx");
        var deathIn = sm.AddAnyStateTransition(deathState);
        deathIn.hasExitTime = false; deathIn.duration = 0.1f;
        deathIn.canTransitionToSelf = false;
        deathIn.AddCondition(AnimatorConditionMode.If, 0, "Death");

        var layers = c.layers;
        layers[0].stateMachine = sm;
        c.layers = layers;
    }

    static BlendTree BuildLocomotionTree(AnimatorController c, string p, bool crouch)
    {
        var tree = new BlendTree();
        AssetDatabase.AddObjectToAsset(tree, c);
        tree.name = crouch ? "CrouchLocomotion" : "StandLocomotion";
        tree.blendType           = BlendTreeType.FreeformCartesian2D;
        tree.blendParameter      = "Strafe";
        tree.blendParameterY     = "Forward";
        tree.useAutomaticThresholds = false;

        if (!crouch)
        {
            // Stand — forward/back/strafe/run — không có jump
            AddMotion(tree, c, p + "/Rifle/Rifle Aiming Idle.fbx",   0,  0);
            AddMotion(tree, c, p + "/Rifle/Walk With Rifle.fbx",      0,  1);
            AddMotion(tree, c, p + "/Rifle/Walking Backwards.fbx",    0, -1);
            AddMotion(tree, c, p + "/Rifle/Strafe Left.fbx",         -1,  0);
            AddMotion(tree, c, p + "/Rifle/Strafe Right.fbx",         1,  0);
            AddMotion(tree, c, p + "/Rifle/Rifle Run.fbx",            0,  2); // sprint
        }
        else
        {
            AddMotion(tree, c, p + "/Rifle/Idle Crouching Aiming.fbx",    0,  0);
            AddMotion(tree, c, p + "/Rifle/Walk Crouching Forward.fbx",   0,  1);
            AddMotion(tree, c, p + "/Rifle/Walk Crouching Backward.fbx",  0, -1);
            AddMotion(tree, c, p + "/Rifle/Crouch Walk Strafe Left.fbx",  -1, 0);
            AddMotion(tree, c, p + "/Rifle/Crouch Walk Strafe Right.fbx",  1, 0);
        }

        return tree;
    }

    // ─── WEAPON LAYER (upper body override) ──────────────────────────────────
    static void SetupWeaponLayer(AnimatorController c, string p, string maskPath)
    {
        var wLayer = new AnimatorControllerLayer();
        wLayer.name          = "WeaponLayer";
        wLayer.defaultWeight = 1f;
        wLayer.blendingMode  = AnimatorLayerBlendingMode.Override;

        var mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(maskPath);
        if (mask != null) wLayer.avatarMask = mask;
        else Debug.LogWarning("UpperBodyMask not found: " + maskPath);

        var sm = new AnimatorStateMachine();
        sm.name = "WeaponLayer";
        AssetDatabase.AddObjectToAsset(sm, c);
        sm.hideFlags        = HideFlags.HideInHierarchy;
        sm.entryPosition    = new Vector2(-300, 0);
        sm.anyStatePosition = new Vector2(-300, 80);
        wLayer.stateMachine = sm;

        // ── Rifle Idle (default) ──
        var rifleIdle = sm.AddState("RifleIdle", new Vector2(0, 0));
        rifleIdle.motion = LoadClip(p + "/Rifle/Rifle Aiming Idle.fbx");
        sm.defaultState = rifleIdle;

        // ── Pistol Idle ──
        var pistolIdle = sm.AddState("PistolIdle", new Vector2(0, 140));
        pistolIdle.motion = LoadClip(p + "/Pistol/In-Place/W1_Stand_Aim_Idle_IPC.fbx");

        // Rifle ↔ Pistol switch
        AddWeaponSwitch(rifleIdle,  pistolIdle, "WeaponType", 1);
        AddWeaponSwitch(pistolIdle, rifleIdle,  "WeaponType", 0);

        // ── Rifle Fire (stand) ──
        var rifleFire = sm.AddState("RifleFire", new Vector2(300, 0));
        rifleFire.motion = LoadClip(p + "/Rifle/Firing Rifle.fbx");
        AddAnyTrigger(sm, rifleFire, "Fire", "WeaponType", 0, false);
        AddExitToState(rifleFire, rifleIdle, 1f, 0.05f);

        // ── Rifle Fire (crouch) ──
        var rifleCrouchFire = sm.AddState("RifleCrouchFire", new Vector2(300, 70));
        rifleCrouchFire.motion = LoadClip(p + "/Rifle/Fire Crouch Rifle.fbx");
        AddAnyTrigger(sm, rifleCrouchFire, "Fire", "WeaponType", 0, true);
        AddExitToState(rifleCrouchFire, rifleIdle, 1f, 0.1f);

        // ── Rifle Reload (stand) ──
        var rifleReload = sm.AddState("RifleReload", new Vector2(300, 140));
        rifleReload.motion = LoadClip(p + "/Rifle/Reloading.fbx");
        AddAnyTrigger(sm, rifleReload, "Reload", "WeaponType", 0, false);
        AddExitToState(rifleReload, rifleIdle, 1f, 0.1f);

        // ── Rifle Reload (crouch) ──
        var rifleCrouchReload = sm.AddState("RifleCrouchReload", new Vector2(300, 210));
        rifleCrouchReload.motion = LoadClip(p + "/Rifle/Crouch Reloading.fbx");
        AddAnyTrigger(sm, rifleCrouchReload, "Reload", "WeaponType", 0, true);
        AddExitToState(rifleCrouchReload, rifleIdle, 1f, 0.1f);

        // ── Pistol Fire (stand) ──
        var pistolFire = sm.AddState("PistolFire", new Vector2(0, 280));
        pistolFire.motion = LoadClip(p + "/Pistol/Fire/W1_Stand_Fire_Single.fbx");
        AddAnyTrigger(sm, pistolFire, "Fire", "WeaponType", 1, false);
        AddExitToState(pistolFire, pistolIdle, 1f, 0.05f);

        // ── Pistol Fire (crouch) ──
        var pistolCrouchFire = sm.AddState("PistolCrouchFire", new Vector2(0, 350));
        pistolCrouchFire.motion = LoadClip(p + "/Pistol/Fire/W1_Crouch_Fire_Single.fbx");
        AddAnyTrigger(sm, pistolCrouchFire, "Fire", "WeaponType", 1, true);
        AddExitToState(pistolCrouchFire, pistolIdle, 1f, 0.05f);

        // ── Pistol Reload ──
        var pistolReload = sm.AddState("PistolReload", new Vector2(0, 420));
        pistolReload.motion = LoadClip(p + "/Rifle/Reloading.fbx"); // dùng tạm rifle reload
        AddAnyTrigger(sm, pistolReload, "Reload", "WeaponType", 1, false);
        AddExitToState(pistolReload, pistolIdle, 1f, 0.1f);

        // ── Hit Reaction ──
        var hitState = sm.AddState("Hit", new Vector2(600, 0));
        hitState.motion = LoadClip(p + "/Rifle/Hit Reaction.fbx");
        var hitTrans = sm.AddAnyStateTransition(hitState);
        hitTrans.hasExitTime = false; hitTrans.duration = 0.05f;
        hitTrans.canTransitionToSelf = false;
        hitTrans.AddCondition(AnimatorConditionMode.If, 0, "Hit");
        hitTrans.AddCondition(AnimatorConditionMode.IfNot, 0, "Crouch");
        AddExitToState(hitState, rifleIdle, 0.9f, 0.1f);

        // ── Crouch Hit ──
        var crouchHit = sm.AddState("CrouchHit", new Vector2(600, 70));
        crouchHit.motion = LoadClip(p + "/Rifle/Crouch Hit React.fbx");
        var chtTrans = sm.AddAnyStateTransition(crouchHit);
        chtTrans.hasExitTime = false; chtTrans.duration = 0.05f;
        chtTrans.canTransitionToSelf = false;
        chtTrans.AddCondition(AnimatorConditionMode.If, 0, "Hit");
        chtTrans.AddCondition(AnimatorConditionMode.If, 0, "Crouch");
        AddExitToState(crouchHit, rifleIdle, 0.9f, 0.1f);

        var layers = new System.Collections.Generic.List<AnimatorControllerLayer>(c.layers);
        layers.Add(wLayer);
        c.layers = layers.ToArray();
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────
    static void AddMotion(BlendTree tree, AnimatorController c, string path, float x, float y)
    {
        var clip = LoadClip(path);
        if (clip == null) { Debug.LogWarning("Clip not found: " + path); return; }
        tree.AddChild(clip, new Vector2(x, y));
    }

    static void AddWeaponSwitch(AnimatorState from, AnimatorState to, string param, int value)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = false; t.duration = 0.1f;
        t.AddCondition(AnimatorConditionMode.Equals, value, param);
    }

    /// <summary>
    /// crouch=true thêm condition Crouch=true, crouch=false thêm Crouch=false
    /// để state stand và crouch không lẫn nhau
    /// </summary>
    static void AddAnyTrigger(AnimatorStateMachine sm, AnimatorState target,
        string trigger, string weaponParam, int weaponValue, bool crouch)
    {
        var t = sm.AddAnyStateTransition(target);
        t.hasExitTime = false; t.duration = 0.05f;
        t.canTransitionToSelf = false;
        t.AddCondition(AnimatorConditionMode.If, 0, trigger);
        t.AddCondition(AnimatorConditionMode.Equals, weaponValue, weaponParam);
        if (crouch)
            t.AddCondition(AnimatorConditionMode.If, 0, "Crouch");
        else
            t.AddCondition(AnimatorConditionMode.IfNot, 0, "Crouch");
    }

    static void AddExitToState(AnimatorState from, AnimatorState to, float exitTime, float duration)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = true;
        t.exitTime    = exitTime;
        t.duration    = duration;
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