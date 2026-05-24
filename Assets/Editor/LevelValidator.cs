using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LevelValidator
{
    [MenuItem("Echoes of You/Production/Validate Current Scene", false, 210)]
    public static void ValidateCurrentScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        Debug.Log($"[LevelValidator] Validating: {scene.name}");
        ValidateScene(scene);
    }

    public static bool ValidateScene(Scene scene)
    {
        bool passed = true;
        string name = scene.name;

        if (name == "MainMenu" || name == "Level_07")
        {
            Debug.Log($"[LevelValidator] Skipping {name} (non-gameplay)");
            return true;
        }

        PressurePlate[] plates = Object.FindObjectsOfType<PressurePlate>();
        if (plates.Length < 1)
        {
            Debug.LogWarning($"[LevelValidator] {name}: Missing pressure plates.");
            passed = false;
        }

        PuzzleIntent intent = Object.FindObjectOfType<PuzzleIntent>();
        if (intent == null)
        {
            Debug.LogWarning($"[LevelValidator] {name}: Missing PuzzleIntent.");
            passed = false;
        }
        else
        {
            if (intent.requiredActions < 2)
            {
                Debug.LogWarning($"[LevelValidator] {name}: requiredActions={intent.requiredActions} is too low.");
                passed = false;
            }
            if (!intent.requiresMovement)
            {
                Debug.LogWarning($"[LevelValidator] {name}: Puzzle does not require movement.");
                passed = false;
            }
        }

        LevelGoal goal = Object.FindObjectOfType<LevelGoal>();
        if (goal == null)
        {
            Debug.LogWarning($"[LevelValidator] {name}: Missing LevelGoal.");
            passed = false;
        }

        LevelExit[] exits = Object.FindObjectsOfType<LevelExit>();
        if (exits.Length == 0)
        {
            Debug.LogWarning($"[LevelValidator] {name}: No LevelExit found.");
            passed = false;
        }

        Light[] lights = Object.FindObjectsOfType<Light>();
        int pointLights = 0;
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] != null && lights[i].type == LightType.Point)
                pointLights++;
        }
        if (pointLights == 0)
        {
            Debug.LogWarning($"[LevelValidator] {name}: No guiding point lights found.");
            passed = false;
        }

        float[] heights = CollectPlatformHeights();
        HashSet<float> uniqueHeights = new HashSet<float>();
        for (int i = 0; i < heights.Length; i++)
            uniqueHeights.Add(Mathf.Round(heights[i] * 10f) / 10f);

        if (uniqueHeights.Count < 1)
        {
            Debug.LogWarning($"[LevelValidator] {name}: No grounded geometry detected.");
            passed = false;
        }

        EchoPathHint pathHint = Object.FindObjectOfType<EchoPathHint>();
        if (pathHint == null)
            Debug.LogWarning($"[LevelValidator] {name}: No EchoPathHint.");

        if (passed)
            Debug.Log($"[LevelValidator] PASS: {name}");
        else
            Debug.LogError($"[LevelValidator] FAIL: {name}");

        return passed;
    }

    static float[] CollectPlatformHeights()
    {
        List<float> heights = new List<float>();
        GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
        for (int i = 0; i < allObjects.Length; i++)
        {
            GameObject go = allObjects[i];
            if (go.isStatic && go.GetComponent<MeshRenderer>() != null && go.GetComponent<Collider>() != null)
                heights.Add(go.transform.position.y);
        }
        return heights.ToArray();
    }

    [MenuItem("Echoes of You/Production/Validate All Levels", false, 211)]
    public static void ValidateAllLevels()
    {
        string[] levelScenes =
        {
            "Assets/Scenes/Level_01.unity",
            "Assets/Scenes/Level_02.unity",
            "Assets/Scenes/Level_03.unity",
            "Assets/Scenes/Level_04.unity",
            "Assets/Scenes/Level_05.unity",
            "Assets/Scenes/Level_06.unity",
            "Assets/Scenes/Level_07.unity",
            "Assets/Scenes/Level_08.unity"
        };

        int passed = 0;
        int total = levelScenes.Length;

        for (int i = 0; i < levelScenes.Length; i++)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(levelScenes[i]) == null)
            {
                Debug.LogWarning($"[LevelValidator] Scene not found: {levelScenes[i]}");
                continue;
            }

            Scene scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(levelScenes[i], UnityEditor.SceneManagement.OpenSceneMode.Single);
            if (ValidateScene(scene))
                passed++;
        }

        Debug.Log($"[LevelValidator] Results: {passed}/{total} levels passed validation.");
    }
}
