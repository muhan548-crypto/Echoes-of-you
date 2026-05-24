using UnityEngine;
using UnityEditor;

public class FixAnimations 
{
    [MenuItem("Tools/Fix Animations")]
    public static void Fix()
    {
        string[] paths = new string[] {
            "Assets/3D Models/Character Base/characterBase.fbx",
            "Assets/3D Models/Animaciones/Locomotion/idle.fbx",
            "Assets/3D Models/Animaciones/Locomotion/walking.fbx",
            "Assets/3D Models/Animaciones/Locomotion/running.fbx",
            "Assets/3D Models/Animaciones/Locomotion/jump.fbx"
        };

        foreach (var p in paths)
        {
            var importer = AssetImporter.GetAtPath(p) as ModelImporter;
            if (importer != null)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.globalScale = 1f;
                importer.SaveAndReimport();
                Debug.Log("Reimported to Humanoid: " + p);
            }
        }
    }
}
