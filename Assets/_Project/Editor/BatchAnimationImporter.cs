using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Collections.Generic;

public class BatchAnimationImporter : EditorWindow
{
    public Avatar sourceAvatar;

    [MenuItem("RiotStrike/Batch Fix Animation Import")]
    public static void OpenWindow()
    {
        GetWindow<BatchAnimationImporter>("Batch Anim Importer");
    }

    void OnGUI()
    {
        GUILayout.Label("Kéo Avatar của Vanguard model vào đây:", EditorStyles.boldLabel);
        sourceAvatar = (Avatar)EditorGUILayout.ObjectField("Source Avatar", sourceAvatar, typeof(Avatar), false);

        if (sourceAvatar == null)
        {
            EditorGUILayout.HelpBox("Chưa có Avatar! Kéo VanguardAvatar vào trước.", MessageType.Warning);
        }

        if (GUILayout.Button("1. Create Upper Body Mask"))
            CreateUpperBodyMask();

        EditorGUI.BeginDisabledGroup(sourceAvatar == null);
        if (GUILayout.Button("2. Batch Fix All Animations"))
            FixAll(sourceAvatar);
        EditorGUI.EndDisabledGroup();

        if (GUILayout.Button("3. Run Both"))
        {
            CreateUpperBodyMask();
            if (sourceAvatar != null) FixAll(sourceAvatar);
        }
    }

    static void CreateUpperBodyMask()
    {
        string maskPath = "Assets/_Project/Animations/UpperBodyMask.mask";

        // Xoá file cũ nếu tồn tại
        if (File.Exists(maskPath))
        {
            AssetDatabase.DeleteAsset(maskPath);
            AssetDatabase.Refresh();
        }

        AvatarMask mask = new AvatarMask();

        // Tắt tất cả body parts trước
        for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
            mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, false);

        // Bật chỉ Upper Body cho FPS weapon layer
        // Root và Hips TẮT - để Base Layer control locomotion
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Body, true);        // Spine/Chest
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Head, true);        // Head/Neck
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm, true);     // Left arm
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm, true);    // Right arm
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFingers, true); // Left fingers
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers, true);// Right fingers

        // Tắt hoàn toàn Lower Body
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Root, false);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftLeg, false);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightLeg, false);

        AssetDatabase.CreateAsset(mask, maskPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("UpperBodyMask created at: " + maskPath);
    }

    static void FixAll(Avatar sourceAvatar)
    {
        string[] folders = {
            "Assets/_Project/Animations/Rifle",
            "Assets/_Project/Animations/Pistol",
        };

        var loopSet = new HashSet<string> {
            // Rifle locomotion
            "Rifle Aiming Idle", "Walking", "Walking Backwards", "Walk With Rifle",
            "Strafe Left", "Strafe Right", "Rifle Run", "Rifle Idle",
            "Idle Crouching Aiming", "Rifle Crouch Walk", "Walk Crouching Backward",
            "Walk Crouching Forward", "Crouch Walk Strafe Left", "Crouch Walk Strafe Right",
            // Pistol locomotion
            "Pistol Idle", "Pistol Walk", "Pistol Walk Backward",
            "Pistol Strafe Left", "Pistol Strafe Right", "Pistol Run",
            "Pistol Kneeling Idle", "Pistol Left Strafe", "Pistol Right Strafe",
            // W1 locomotion loops
            "W1_Stand_Aim_Idle_IPC", "W1_Crouch_Aim_Idle_IPC", "W1_Crouch_Idle_IPC",
            "W1_Walk_Aim_F_Loop_IPC", "W1_CrouchWalk_Aim_F_Loop_IPC",
            "W1_Jog_Aim_F_Loop_IPC", "W1_Stand_Relaxed_Idle_IPC",
            "W1_Stand_Aim_Turn_In_Place_L_Loop_IPC", "W1_Stand_Aim_Turn_In_Place_R_Loop_IPC",
        };

        // Clip 1 lần (không loop)
        var oneShot = new HashSet<string> {
            "Firing Rifle", "Fire Crouch Rifle", "Crouch Rapid Fire",
            "Reloading", "Reload", "Crouch Reloading",
            "Hit Reaction", "Crouch Hit React",
            "Dying", "Crouch Death", "Walking To Dying",
            "Stand To Crouch", "Crouch To Standing With Rifle",
            "Pistol Jump",
            "W1_Stand_Fire_Single", "W1_Crouch_Fire_Single",
            "W1_Stand_Aim_Jump_Start_IPC", "W1_Stand_Aim_Jump_Air_IPC",
            "W1_Stand_Aim_Jump_End_IPC", "W1_Walk_Aim_F_Jump_RU_End_IPC",
            "W1_Jog_Aim_F_Jump_RU_End_IPC",
        };

        string[] guids = AssetDatabase.FindAssets("t:Object", folders);
        int count = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase)) continue;

            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) continue;

            string clipName = Path.GetFileNameWithoutExtension(path);
            bool isW1 = clipName.StartsWith("W1_");
            bool needsLoop = loopSet.Contains(clipName);

            // Rig
            importer.animationType = ModelImporterAnimationType.Human;
            if (isW1)
            {
                // W1_ clips có bone naming khác - tạo avatar riêng
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            }
            else
            {
                // Mixamo clips - copy từ Vanguard
                importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                importer.sourceAvatar = sourceAvatar;
            }

            // Clips
            if (importer.clipAnimations == null || importer.clipAnimations.Length == 0)
                importer.clipAnimations = importer.defaultClipAnimations;

            var clips = importer.clipAnimations;
            foreach (var clip in clips)
            {
                if (clip.name.StartsWith("__preview__")) continue;

                clip.loopTime = needsLoop;
                clip.loopPose = needsLoop;

                // Bake root motion vào pose - không cho character trượt
                clip.lockRootRotation = true;
                clip.lockRootHeightY = true;
                clip.lockRootPositionXZ = true;

                clip.keepOriginalOrientation = false;
                clip.keepOriginalPositionY = false;
                clip.keepOriginalPositionXZ = false;
            }

            importer.clipAnimations = clips;
            importer.SaveAndReimport();
            count++;
            Debug.Log($"Fixed: {clipName} | Loop={needsLoop} | W1={isW1}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Done. Fixed {count} FBX files.");
    }
}