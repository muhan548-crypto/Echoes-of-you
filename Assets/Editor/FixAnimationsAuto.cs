using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class FixAnimationsAuto 
{
    static FixAnimationsAuto()
    {
        // Quitamos la condición de EditorPrefs temporalmente para forzar que se arregle el nuevo modelo
        // if (EditorPrefs.GetBool("AnimationsFixed", false)) return;
        RunFix();
    }

    [MenuItem("Echoes/Force Fix Character and Animations")]
    public static void RunFix()
    {
        string[] paths = new string[] {
            "Assets/3D Models/Character Base/characterBase.fbx",
            "Assets/3D Models/lowpoly-character-freerigged-/source/LowPolyCharacterModel/FBX/LowPolyCharacter.fbx",
            "Assets/3D Models/Animaciones/Locomotion/idle.fbx",
            "Assets/3D Models/Animaciones/Locomotion/walking.fbx",
            "Assets/3D Models/Animaciones/Locomotion/running.fbx",
            "Assets/3D Models/Animaciones/Locomotion/jump.fbx"
        };

        bool anyFixed = false;
        foreach (var p in paths)
        {
            var importer = AssetImporter.GetAtPath(p) as ModelImporter;
            if (importer != null)
            {
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
                Debug.Log("[Echoes] Reimported to Humanoid + Scale=1: " + p);
                anyFixed = true;
            }
        }

        if (anyFixed)
        {
            Debug.Log("[Echoes] Successfully configured Character and Animations as Humanoid.");
        }
        EditorPrefs.SetBool("AnimationsFixed", true);
    }
}
