// AnimationClipRenamer.cs
using UnityEngine;
using UnityEditor;
using System.IO;

public class AnimationClipRenamer : EditorWindow
{
    [MenuItem("RiotStrike/Rename Animation Clips")]
    public static void RenameAllClips()
    {
        string[] folders = {
            "Assets/_Project/Animations/Rifle",
            "Assets/_Project/Animations/Pistol",
            "Assets/_Project/Animations/Pistol/In-Place",
            "Assets/_Project/Animations/Pistol/Fire",
            "Assets/_Project/Animations/Pistol/Aim_Offsets",
            "Assets/_Project/Animations/Pistol/In-Place/Split_Jumps",
        };

        int count = 0;
        foreach (string folder in folders)
        {
            string[] guids = AssetDatabase.FindAssets("t:Object", new[] { folder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase)) continue;

                string fileName = Path.GetFileNameWithoutExtension(path);
                var assets = AssetDatabase.LoadAllAssetsAtPath(path);

                foreach (var asset in assets)
                {
                    if (!(asset is AnimationClip clip)) continue;
                    if (clip.name.StartsWith("__preview__")) continue;
                    if (clip.name == fileName) continue;

                    clip.name = fileName;
                    EditorUtility.SetDirty(clip);
                    count++;
                    Debug.Log($"Renamed: '{clip.name}' → '{fileName}' ({path})");
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Done. Renamed {count} clips.");
    }
}