using UnityEditor;
using UnityEngine;

public static class RigSetup
{
    [MenuItem("Echoes/Setup Generic Locomotion Animations")]
    public static void EnsureGenericAnimationRigs()
    {
        string[] paths =
        {
            "Assets/3D Models/Character Base/characterBase.fbx",
            "Assets/3D Models/Animaciones/Locomotion/idle.fbx",
            "Assets/3D Models/Animaciones/Locomotion/walking.fbx",
            "Assets/3D Models/Animaciones/Locomotion/running.fbx",
            "Assets/3D Models/Animaciones/Locomotion/jump.fbx",
            "Assets/3D Models/Animaciones/Locomotion/left strafe walking.fbx",
            "Assets/3D Models/Animaciones/Locomotion/right strafe walking.fbx"
        };

        for (int i = 0; i < paths.Length; i++)
        {
            string path = paths[i];
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
                continue;

            bool changed = false;
            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                changed = true;
            }

            if (!Mathf.Approximately(importer.globalScale, 1f))
            {
                importer.globalScale = 1f;
                changed = true;
            }

            if (!changed)
                continue;

            importer.SaveAndReimport();
            Debug.Log("[Echoes] Rig y escala corregidos: " + path);
        }
    }
}
