using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Editor tool: builds prefabs, materials and the 3 level scenes for "Echoes of You".
/// Menu: Echoes of You ▸ …
/// </summary>
public static class EchoesLevelBuilder
{
    // ─── paths ───────────────────────────────────────────────
    const string MAT_ROOT    = "Assets/Materials/Echoes";
    const string PREFAB_ROOT = "Assets/Prefabs";
    const string SCENE_ROOT  = "Assets/Scenes";
    const int    GROUND_LAYER = 6; // "Ground" layer index

    // ─── material cache ─────────────────────────────────────
    static Material _matFloor, _matPlate, _matDoor, _matExit, _matBridge, _matPlayer, _matEcho;

    class ModularRoomSpec
    {
        public string name;
        public Vector3 center;
        public Vector2 size;
        public float ceilingHeight;
        public float wallThickness;
        public bool addCeiling;
        public bool addSideWalls;

        public ModularRoomSpec(string name, Vector3 center, Vector2 size, float ceilingHeight = 6f)
        {
            this.name = name;
            this.center = center;
            this.size = size;
            this.ceilingHeight = ceilingHeight;
            wallThickness = 0.5f;
            addCeiling = false;
            addSideWalls = false;
        }
    }

    struct ModularCorridorSpec
    {
        public int fromRoomIndex;
        public int toRoomIndex;
        public float width;

        public ModularCorridorSpec(int fromRoomIndex, int toRoomIndex, float width)
        {
            this.fromRoomIndex = fromRoomIndex;
            this.toRoomIndex = toRoomIndex;
            this.width = width;
        }
    }

    // ================================================================
    //  MENU: 0 — Create everything at once
    // ================================================================
    [MenuItem("Echoes of You/3. Build All (Menu + 7 Levels)", false, 32)]
    public static void BuildAllLevels()
    {
        if (!CheckNotPlaying()) return;
        EnsureFolders();
        CreateMaterials();
        CreateAnimatorController();
        CreatePrefabs();
        BuildMainMenu();
        BuildLevel01();
        BuildLevel02();
        BuildLevel03();
        BuildLevel04();
        BuildLevel05();
        BuildLevel06();
        BuildLevel07();
        AddScenesToBuild();
        Debug.Log("[Echoes] ✔ Main menu, prefabs, materials, and 7 levels created successfully!");
    }

    // ================================================================
    //  MENU: individual items
    // ================================================================
    [MenuItem("Echoes of You/1. Create Materials", false, 20)]
    static void MenuMaterials() { EnsureFolders(); CreateMaterials(); }

    [MenuItem("Echoes of You/2. Create Prefabs", false, 21)]
    static void MenuPrefabs() { if (!CheckNotPlaying()) return; EnsureFolders(); CreateMaterials(); CreatePrefabs(); }

    [MenuItem("Echoes of You/3. Build Level 01", false, 40)]
    static void MenuL1() { if (!CheckNotPlaying()) return; EnsureFolders(); CreateMaterials(); BuildLevel01(); }

    [MenuItem("Echoes of You/4. Build Level 02", false, 41)]
    static void MenuL2() { if (!CheckNotPlaying()) return; EnsureFolders(); CreateMaterials(); BuildLevel02(); }

    [MenuItem("Echoes of You/5. Build Level 03", false, 42)]
    static void MenuL3() { if (!CheckNotPlaying()) return; EnsureFolders(); CreateMaterials(); BuildLevel03(); }

    [MenuItem("Echoes of You/6. Build Level 07 Modular", false, 43)]
    static void MenuL7() { if (!CheckNotPlaying()) return; EnsureFolders(); CreateMaterials(); BuildLevel07(); }

    [MenuItem("Echoes of You/7. Add Scenes to Build Settings", false, 60)]
    static void MenuBuild() { if (!CheckNotPlaying()) return; AddScenesToBuild(); }

    [MenuItem("Echoes of You/FIX — Recreate All Materials", false, 80)]
    static void MenuFixMaterials()
    {
        if (!CheckNotPlaying()) return;
        EnsureFolders();
        DeleteAllMaterials();
        CreateMaterials();
        Debug.Log("[Echoes] ✔ All materials recreated with Standard shader. Rebuild levels from the menu.");
    }

    [MenuItem("Echoes of You/FIX — Rebuild Everything From Scratch", false, 81)]
    static void MenuRebuildAll()
    {
        if (!CheckNotPlaying()) return;
        EnsureFolders();
        DeleteAllMaterials();
        AssetDatabase.DeleteAsset(PREFAB_ROOT + "/EchoPrefab.prefab");
        AssetDatabase.DeleteAsset(PREFAB_ROOT + "/PlayerAnimController.controller");
        CreateMaterials();
        CreateAnimatorController();
        CreatePrefabs();
        BuildMainMenu();
        BuildLevel01();
        BuildLevel02();
        BuildLevel03();
        BuildLevel04();
        BuildLevel05();
        BuildLevel06();
        BuildLevel07();
        AddScenesToBuild();
        Debug.Log("[Echoes] ✔ Full rebuild complete!");
    }

    static bool CheckNotPlaying()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[Echoes] Cannot build levels while in Play Mode! Stop the game first.");
            return false;
        }
        return true;
    }

    // ================================================================
    //  FOLDERS
    // ================================================================
    static void EnsureFolders()
    {
        EnsureFolder(MAT_ROOT);
        EnsureFolder(PREFAB_ROOT);
        EnsureFolder(SCENE_ROOT);
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    // ================================================================
    //  MATERIALS  (Built‑in Render Pipeline — Standard shader)
    // ================================================================
    static void DeleteAllMaterials()
    {
        string[] names = { "Mat_Floor", "Mat_Plate", "Mat_Door", "Mat_Exit", "Mat_Bridge", "Mat_Player", "Mat_Echo" };
        foreach (string n in names)
        {
            string p = MAT_ROOT + "/" + n + ".mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(p) != null)
                AssetDatabase.DeleteAsset(p);
        }
        AssetDatabase.Refresh();
    }

    static void CreateMaterials()
    {
        // Force Standard shader — project uses Built‑in Render Pipeline
        Shader standardShader = Shader.Find("Standard");
        if (standardShader == null)
        {
            Debug.LogError("[Echoes] Could not find Standard shader!");
            return;
        }

        _matFloor  = GetOrCreateMat("Mat_Floor",  standardShader, HexColor("1A1B26"), false);
        _matPlate  = GetOrCreateMat("Mat_Plate",  standardShader, HexColor("00E07A"), false);
        _matDoor   = GetOrCreateMat("Mat_Door",   standardShader, HexColor("E03050"), false);
        _matExit   = GetOrCreateMat("Mat_Exit",   standardShader, HexColor("FFD700"), true);
        _matBridge = GetOrCreateMat("Mat_Bridge", standardShader, HexColor("2A3B4C"), false);
        _matPlayer = GetOrCreateMat("Mat_Player", standardShader, HexColor("FFFFFF"), false);
        _matEcho   = GetOrCreateMat("Mat_Echo",   standardShader, HexColor("00FFFF", 0.5f), false, true);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Echoes] Materials created with Standard shader.");
    }

    static Material GetOrCreateMat(string name, Shader shader, Color color, bool emissive, bool transparent = false)
    {
        string path = MAT_ROOT + "/" + name + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null)
        {
            CacheMat(name, mat);
            return mat;
        }

        mat = new Material(shader);
        mat.name = name;
        mat.color = color; // Standard shader uses _Color

        if (emissive)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 2f);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        if (transparent)
        {
            // Standard shader transparent mode
            mat.SetFloat("_Mode", 3); // Transparent
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        AssetDatabase.CreateAsset(mat, path);
        CacheMat(name, mat);
        return mat;
    }

    static void CacheMat(string name, Material mat)
    {
        switch (name)
        {
            case "Mat_Floor":  _matFloor  = mat; break;
            case "Mat_Plate":  _matPlate  = mat; break;
            case "Mat_Door":   _matDoor   = mat; break;
            case "Mat_Exit":   _matExit   = mat; break;
            case "Mat_Bridge": _matBridge = mat; break;
            case "Mat_Player": _matPlayer = mat; break;
            case "Mat_Echo":   _matEcho   = mat; break;
        }
    }

    // ================================================================
    //  ANIMATOR CONTROLLER
    // ================================================================
    static void CreateAnimatorController()
    {
        SetupPlayerAnimator.Setup();
    }

    // ================================================================
    //  PREFABS
    // ================================================================
    static void CreatePrefabs()
    {
        CreateEchoPrefab();
        Debug.Log("[Echoes] Prefabs created / verified.");
    }

    static GameObject GetEchoPrefab()
    {
        string path = PREFAB_ROOT + "/EchoPrefab.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab != null) return prefab;
        return CreateEchoPrefab();
    }

    static PressurePlate MakePlate(string name, Vector3 pos, Vector3 size, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        go.transform.SetParent(parent, true);

        // Invisible Trigger for logic
        var bc = go.AddComponent<BoxCollider>();
        bc.size = size;
        bc.isTrigger = true;

        var rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        var plate = go.AddComponent<PressurePlate>();

        // 3D Visuals
        var btnPfb = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/3D Models/Models/FBX format/button-square.fbx");
        if (btnPfb != null)
        {
            var visual = PrefabUtility.InstantiatePrefab(btnPfb) as GameObject;
            visual.transform.SetParent(go.transform, false);
            visual.transform.localPosition = new Vector3(0, -size.y * 0.5f, 0);
            float scale = Mathf.Min(size.x, size.z) * 1.5f;
            visual.transform.localScale = new Vector3(scale, scale, scale);

            // Optional: Light for plate
            var lightGo = new GameObject("PlateLight");
            lightGo.transform.SetParent(visual.transform, false);
            lightGo.transform.localPosition = new Vector3(0, 1f, 0);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = Color.red;
            light.range = 3f;
            light.intensity = 2f;
        }
        else
        {
            // Fallback cube
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "PlateVisual";
            cube.transform.SetParent(go.transform, false);
            cube.transform.localPosition = Vector3.zero;
            cube.transform.localScale = size;
            Object.DestroyImmediate(cube.GetComponent<Collider>());
            if (_matPlate != null)
                cube.GetComponent<MeshRenderer>().sharedMaterial = _matPlate;
        }

        return plate;
    }

    static GameObject CreateEchoPrefab()
    {
        string path = PREFAB_ROOT + "/EchoPrefab.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        // Build in scene, then save as prefab
        GameObject root = new GameObject("EchoPrefab");
        root.tag = "Echo";

        var rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        var cc = root.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.3f;
        cc.center = new Vector3(0f, 0.9f, 0f);
        cc.skinWidth = 0.08f;

        var ep = root.AddComponent<EchoPlayback>();
        SetPrivateField(ep, "_matEcho", _matEcho);

        // Visual child
        var visual = new GameObject("PlayerVisual");
        visual.transform.SetParent(root.transform, false);
        visual.transform.localPosition = Vector3.zero;

        var polyChar = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/3D Models/Character Base/characterBase.fbx");
        if (polyChar != null)
        {
            var instance = PrefabUtility.InstantiatePrefab(polyChar) as GameObject;
            instance.transform.SetParent(visual.transform, false);
            instance.name = "EchoModel";
            instance.transform.localScale = Vector3.one * 1.2f;
            
            // Apply echo material
            Renderer[] rends = instance.GetComponentsInChildren<Renderer>();
            foreach (var r in rends) r.sharedMaterial = _matEcho;
            
            // Remove colliders from visual
            Collider[] visualCols = instance.GetComponentsInChildren<Collider>();
            foreach (var c in visualCols) Object.DestroyImmediate(c);
        }
        else
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "EchoVisual";
            cube.transform.SetParent(visual.transform, false);
            cube.transform.localPosition = new Vector3(0, 0.9f, 0);
            cube.transform.localScale = new Vector3(0.6f, 1.8f, 0.6f);
            Object.DestroyImmediate(cube.GetComponent<Collider>());
            cube.GetComponent<MeshRenderer>().sharedMaterial = _matEcho;
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    // ================================================================
    //  LEVEL 01 — Conceptos Básicos
    // ================================================================
    static void BuildLevel01()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Level_01";

        var envParent = CreateEmpty("--- ENVIRONMENT ---", Vector3.zero);
        MakeFloor("Floor_Start",   new Vector3(0,0,0),     new Vector3(6,0.5f,6),   envParent.transform);
        MakeFloor("Bridge_1",      new Vector3(0,0,5),     new Vector3(2,0.5f,4),   envParent.transform);
        
        // Parkour addition L01
        MakeFloor("Parkour_Platform", new Vector3(3, 1f, 5), new Vector3(2, 0.5f, 2), envParent.transform);

        MakeFloor("Floor_Button",  new Vector3(0,0,9),     new Vector3(4,0.5f,4),   envParent.transform);
        MakeFloor("Bridge_2",      new Vector3(0,0,13),    new Vector3(2,0.5f,4),   envParent.transform);
        MakeFloor("Floor_Gate",    new Vector3(0,0,17),    new Vector3(6,0.5f,4),   envParent.transform);
        MakeFloor("Floor_End",     new Vector3(0,0,21),    new Vector3(6,0.5f,4),   envParent.transform);

        var mechParent = CreateEmpty("--- MECHANICS ---", Vector3.zero);
        var plate = MakePlate("PressurePlate", new Vector3(0, 0.35f, 9), new Vector3(1.5f, 0.1f, 1.5f), mechParent.transform);

        var doorFrame = CreateEmpty("DoorFrame", new Vector3(0, 0.25f, 15));
        doorFrame.transform.SetParent(mechParent.transform, true);
        
        GameObject doorModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/3D Models/Models/FBX format/door-rotate.fbx");
        GameObject door;
        if (doorModel != null)
        {
            door = PrefabUtility.InstantiatePrefab(doorModel) as GameObject;
            door.name = "DoorVisual";
            door.transform.SetParent(doorFrame.transform, false);
            door.transform.localScale = new Vector3(3f, 3f, 1f);
            
            // Add collider to the visual or use a box collider on frame
            var bc = door.AddComponent<BoxCollider>();
            bc.size = new Vector3(1f, 2.5f, 0.2f);
            bc.center = new Vector3(0f, 1.25f, 0f);
        }
        else
        {
            door = MakeCube("Door", Vector3.zero, new Vector3(3, 8f, 0.3f), doorFrame.transform, _matDoor);
        }
        
        var dc = door.AddComponent<DoorController>();
        dc.plates = new PressurePlate[] { plate };

        var exit = MakeCube("LevelExit", new Vector3(0, 0.5f, 21), new Vector3(2, 2, 0.3f), mechParent.transform, _matExit);
        exit.GetComponent<BoxCollider>().isTrigger = true;
        var exitComp = exit.AddComponent<LevelExit>();
        exitComp.loadNextBuildIndex = true;

        SpawnLevelRuntime(mechParent.transform, "Deja un eco en la placa y cruza la puerta.", "Activa la placa con tu eco para abrir el paso.", "Primero recuerdas.");
        SpawnPuzzleIntent(mechParent.transform, 1, 2, true, false, false, 4f, "Introduccion: eco sostiene la placa y el objetivo queda visible al frente.");
        GoalTrigger l1Plate = CreateGoalTrigger("Goal_Plate", mechParent.transform, plate, null, true, false);
        GoalTrigger l1Door = CreateGoalTrigger("Goal_Door", mechParent.transform, null, dc, false, true);
        SpawnLevelGoal(mechParent.transform, new[] { exitComp }, new[] { l1Plate, l1Door }, "Activa la memoria con el eco.", "Salida abierta.", "Primero recuerdas.", 2);
        SpawnGuideBeacon("GoalLight_L1", new Vector3(0f, 3.4f, 21f), mechParent.transform, new Color(1f, 0.82f, 0.42f, 1f));

        SpawnPlayer(new Vector3(0, 1.5f, 0));
        SpawnCamera();
        SpawnHUD();
        SpawnPauseAndTutorial();
        SpawnLight();
        SpawnAtmosphere();

        var tutParent = CreateEmpty("--- TUTORIAL ---", Vector3.zero);
        SpawnTutorialTrigger("Tut_Movement", new Vector3(0, 1, 0), new Vector3(4, 3, 4),
            "Usa WASD para moverte", "Espacio para saltar", 5f, tutParent.transform);
        SpawnTutorialTrigger("Tut_Button", new Vector3(0, 1, 7), new Vector3(3, 3, 2),
            "Pisa el boton", "Observa que la puerta desaparece al presionarlo", 5f, tutParent.transform);
        SpawnTutorialTrigger("Tut_Record", new Vector3(0, 1, 9), new Vector3(3, 3, 3),
            "Mantén R para grabar tus movimientos", "Quédate sobre la placa y suelta para dejar un eco", 6f, tutParent.transform);
        
        SpawnParticles();
        SpawnDecorations(envParent.transform, 5, 0f, 20f);
        SaveScene(scene, "Level_01");
    }

    // ================================================================
    //  LEVEL 02 — Experimentación Segura
    // ================================================================
    static void BuildLevel02()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Level_02";

        // Everything is solid ground so player doesn't fall while learning
        var envParent = CreateEmpty("--- ENVIRONMENT ---", Vector3.zero);
        MakeFloor("Floor_Main", new Vector3(0,0,10), new Vector3(14,0.5f,24), envParent.transform);
        
        // Parkour addition L02
        MakeFloor("Parkour_Step", new Vector3(0, 0.5f, 3), new Vector3(4, 0.5f, 2), envParent.transform);

        var mechParent = CreateEmpty("--- MECHANICS ---", Vector3.zero);

        // Path A: Bridge (Shortcut)
        var plateA = MakePlate("Plate_A", new Vector3(-4, 0.35f, 6), new Vector3(1.5f, 0.1f, 1.5f), mechParent.transform);
        var anchor = CreateEmpty("BridgeAnchor", new Vector3(-4, 0, 12));
        anchor.transform.SetParent(mechParent.transform, true);
        var bridge = MakeCube("MovingBridge", new Vector3(0, -5, 0), new Vector3(2, 0.5f, 6), anchor.transform, _matBridge);
        bridge.layer = GROUND_LAYER;
        var tmp = bridge.AddComponent<TimedMovingPlatform>();
        tmp.plate = plateA;
        tmp.inactiveLocal = new Vector3(0, -5, 0);
        tmp.activeLocal = Vector3.zero;
        tmp.travelSpeed = 6f;

        // Path B: Door
        var plateB = MakePlate("Plate_B", new Vector3(4, 0.35f, 6), new Vector3(1.5f, 0.1f, 1.5f), mechParent.transform);
        var df = CreateEmpty("DoorFrame", new Vector3(4, 0.25f, 12));
        df.transform.SetParent(mechParent.transform, true);
        var d1 = MakeCube("Door", Vector3.zero, new Vector3(3, 8f, 0.3f), df.transform, _matDoor);
        var dc = d1.AddComponent<DoorController>();
        dc.plates = new PressurePlate[] { plateB };

        var exit1 = MakeCube("LevelExit", new Vector3(-4, 0.5f, 18), new Vector3(2, 2, 0.3f), mechParent.transform, _matExit);
        exit1.GetComponent<BoxCollider>().isTrigger = true;
        var exitComp1 = exit1.AddComponent<LevelExit>();
        exitComp1.loadNextBuildIndex = true;

        var exit2 = MakeCube("LevelExit", new Vector3(4, 0.5f, 18), new Vector3(2, 2, 0.3f), mechParent.transform, _matExit);
        exit2.GetComponent<BoxCollider>().isTrigger = true;
        var exitComp2 = exit2.AddComponent<LevelExit>();
        exitComp2.loadNextBuildIndex = true;

        SpawnLevelRuntime(mechParent.transform, "Activa un camino y usa el eco para sostenerlo.", "Observa las dos rutas y elige cual mantiene tu eco.", "Luego pruebas.");
        SpawnPuzzleIntent(mechParent.transform, 2, 2, true, true, false, 6f, "Nivel de lectura: dos rutas visibles, el eco sostiene la opcion elegida.");
        GoalTrigger l2A = CreateGoalTrigger("Goal_Plate_A", mechParent.transform, plateA, null, true, false);
        GoalTrigger l2B = CreateGoalTrigger("Goal_Plate_B", mechParent.transform, plateB, null, true, false);
        SpawnLevelGoal(mechParent.transform, new[] { exitComp1, exitComp2 }, new[] { l2A, l2B }, "Activa una ruta y llega a la salida.", "Ruta sincronizada.", "Luego pruebas.", 1);
        SpawnGuideBeacon("GoalLight_L2A", new Vector3(-4f, 3.4f, 18f), mechParent.transform, new Color(1f, 0.82f, 0.42f, 1f));
        SpawnGuideBeacon("GoalLight_L2B", new Vector3(4f, 3.4f, 18f), mechParent.transform, new Color(1f, 0.82f, 0.42f, 1f));

        SpawnPlayer(new Vector3(0, 1.5f, 0));
        SpawnCamera();
        SpawnHUD();
        SpawnPauseAndTutorial();
        SpawnLight();
        SpawnAtmosphere();
        SpawnParticles();
        SpawnDecorations(envParent.transform, 6, 0f, 20f);
        SaveScene(scene, "Level_02");
    }

    // ================================================================
    //  LEVEL 03 — Refuerzo
    // ================================================================
    static void BuildLevel03()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Level_03";

        // Slight complexity: AND gate with a gap to jump
        var envParent = CreateEmpty("--- ENVIRONMENT ---", Vector3.zero);
        MakeFloor("Floor_Start",  new Vector3(0,0,0),     new Vector3(10,0.5f,6),  envParent.transform);
        MakeFloor("Bridge_L",     new Vector3(-3,0,5),    new Vector3(2,0.5f,4),   envParent.transform);
        MakeFloor("Bridge_R",     new Vector3(3,0,5),     new Vector3(2,0.5f,4),   envParent.transform);
        MakeFloor("Floor_Plates", new Vector3(0,0,9),     new Vector3(10,0.5f,4),  envParent.transform);
        
        // Gap here!
        
        MakeFloor("Floor_Gate",   new Vector3(0,0,16),    new Vector3(6,0.5f,4),   envParent.transform);
        MakeFloor("Floor_End",    new Vector3(0,0,20),    new Vector3(6,0.5f,4),   envParent.transform);

        var mechParent = CreateEmpty("--- MECHANICS ---", Vector3.zero);
        var pL = MakePlate("Plate_L",  new Vector3(-3, 0.35f, 9), new Vector3(1.5f, 0.1f, 1.5f), mechParent.transform);
        var pR = MakePlate("Plate_R",  new Vector3( 3, 0.35f, 9), new Vector3(1.5f, 0.1f, 1.5f), mechParent.transform);

        var df = CreateEmpty("DoorFrame", new Vector3(0, 0.25f, 14.5f));
        df.transform.SetParent(mechParent.transform, true);
        var door = MakeCube("Door", Vector3.zero, new Vector3(6, 8f, 0.3f), df.transform, _matDoor);
        var dc = door.AddComponent<DoorController>();
        dc.plates = new PressurePlate[] { pL, pR };

        var exit = MakeCube("LevelExit", new Vector3(0, 0.5f, 20), new Vector3(2, 2, 0.3f), mechParent.transform, _matExit);
        exit.GetComponent<BoxCollider>().isTrigger = true;
        var exitComp = exit.AddComponent<LevelExit>();
        exitComp.loadNextBuildIndex = true;

        SpawnLevelRuntime(mechParent.transform, "Activa ambas placas y cruza cuando la puerta ceda.", "Necesitas dos acciones coordinadas para abrir el paso.", "Dos decisiones se sostienen.");
        SpawnPuzzleIntent(mechParent.transform, 2, 3, true, true, true, 8f, "AND gate: el eco mantiene una placa mientras avanzas a la otra.");
        GoalTrigger l3L = CreateGoalTrigger("Goal_Plate_Left", mechParent.transform, pL, null, true, false);
        GoalTrigger l3R = CreateGoalTrigger("Goal_Plate_Right", mechParent.transform, pR, null, true, false);
        GoalTrigger l3Door = CreateGoalTrigger("Goal_Door", mechParent.transform, null, dc, false, true);
        SpawnLevelGoal(mechParent.transform, new[] { exitComp }, new[] { l3L, l3R, l3Door }, "Coordina eco y avance para abrir la puerta.", "Paso completo.", "Dos decisiones se sostienen.", 3);
        SpawnGuideBeacon("GoalLight_L3", new Vector3(0f, 3.6f, 20f), mechParent.transform, new Color(1f, 0.82f, 0.42f, 1f));

        SpawnPlayer(new Vector3(0, 1.5f, 0));
        SpawnCamera();
        SpawnHUD();
        SpawnPauseAndTutorial();
        SpawnLight();
        SpawnParticles();
        SpawnDecorations(envParent.transform, 6, 0f, 20f);
        SaveScene(scene, "Level_03");
    }

    // ================================================================
    //  LEVEL 04 — Twist (El orden importa)
    // ================================================================
    static void BuildLevel04()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Level_04";

        var envParent = CreateEmpty("--- ENVIRONMENT ---", Vector3.zero);
        MakeFloor("Floor_Start", new Vector3(0, 0, 0), new Vector3(6, 0.5f, 6), envParent.transform);
        MakeFloor("Floor_End", new Vector3(0, 0, 18), new Vector3(6, 0.5f, 6), envParent.transform);

        // The bridge gap is huge, they must use the moving platform
        var mechParent = CreateEmpty("--- MECHANICS ---", Vector3.zero);

        var plate = MakePlate("PressurePlate_1", new Vector3(0, 0.35f, 2), new Vector3(1.5f, 0.1f, 1.5f), mechParent.transform);

        var platFrame = CreateEmpty("PlatformFrame", Vector3.zero);
        platFrame.transform.SetParent(mechParent.transform, true);
        var plat = MakeCube("MovingPlatform", Vector3.zero, new Vector3(4, 0.5f, 4), platFrame.transform, _matBridge);
        plat.layer = GROUND_LAYER;
        var mp = plat.AddComponent<TimedMovingPlatform>();
        mp.plate = plate;
        mp.inactiveLocal = new Vector3(0, 0, 15);
        mp.activeLocal = new Vector3(0, 0, 6);
        mp.travelSpeed = 6f; // Faster to force the twist

        var exit = MakeCube("LevelExit", new Vector3(0, 0.5f, 20), new Vector3(2, 2, 0.3f), mechParent.transform, _matExit);
        var exitComp = exit.AddComponent<LevelExit>();
        exitComp.loadNextBuildIndex = true;
        exit.GetComponent<BoxCollider>().isTrigger = true;

        SpawnLevelRuntime(mechParent.transform, "Graba el recorrido y deja que el eco mueva el puente.", "El orden correcto es dejar el eco y correr al objetivo.", "El orden cambia el camino.");
        SpawnPuzzleIntent(mechParent.transform, 1, 3, true, true, true, 10f, "Twist: el eco activa el puente mientras el jugador aprovecha el timing.");
        GoalTrigger l4Plate = CreateGoalTrigger("Goal_Plate", mechParent.transform, plate, null, true, false);
        GoalTrigger l4Bridge = CreateGoalTrigger("Goal_BridgeTravel", mechParent.transform, plate, null, true, false);
        SpawnLevelGoal(mechParent.transform, new[] { exitComp }, new[] { l4Plate, l4Bridge }, "Sincroniza el eco con el puente y cruza.", "Momento resuelto.", "El orden cambia el camino.", 1);
        SpawnGuideBeacon("GoalLight_L4", new Vector3(0f, 3.6f, 20f), mechParent.transform, new Color(1f, 0.82f, 0.42f, 1f));

        SpawnPlayer(new Vector3(0, 1.5f, -2));
        SpawnCamera();
        SpawnHUD();
        SpawnPauseAndTutorial();
        SpawnLight();
        
        var tutParent = CreateEmpty("--- TUTORIAL ---", Vector3.zero);
        SpawnTutorialTrigger("Tut_Twist", new Vector3(0, 1, -2), new Vector3(5, 3, 5),
            "CONSEJO AVANZADO", "Graba tu viaje a la meta y haz que el eco active el botón", 5f, tutParent.transform);

        SpawnParticles();
        SpawnDecorations(envParent.transform, 6, 0f, 20f);
        SaveScene(scene, "Level_04");
    }

    // ================================================================
    //  LEVEL 05 — Ejecución Precisa
    // ================================================================
    static void BuildLevel05()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Level_05";

        var envParent = CreateEmpty("--- ENVIRONMENT ---", Vector3.zero);
        MakeFloor("Floor_Start", new Vector3(0, 0, 0), new Vector3(6, 0.5f, 6), envParent.transform);
        MakeFloor("Floor_Mid", new Vector3(0, 0, 12), new Vector3(6, 0.5f, 6), envParent.transform);
        MakeFloor("Floor_End", new Vector3(0, 0, 24), new Vector3(6, 0.5f, 6), envParent.transform);

        var mechParent = CreateEmpty("--- MECHANICS ---", Vector3.zero);

        // Setup:
        // Plate 1 (Start) brings Bridge 1 from Mid to Start
        // Plate 2 (Mid) brings Bridge 2 from End to Mid
        var plate1 = MakePlate("PressurePlate_1", new Vector3(-2, 0.35f, 0), new Vector3(1f, 0.1f, 1f), mechParent.transform);
        var pf1 = CreateEmpty("PlatFrame_1", Vector3.zero);
        pf1.transform.SetParent(mechParent.transform, true);
        var plat1 = MakeCube("MovingPlatform_1", Vector3.zero, new Vector3(3, 0.5f, 3), pf1.transform, _matBridge);
        plat1.layer = GROUND_LAYER;
        var mp1 = plat1.AddComponent<TimedMovingPlatform>();
        mp1.plate = plate1;
        mp1.inactiveLocal = new Vector3(0, 0, 9);
        mp1.activeLocal = new Vector3(0, 0, 4);
        mp1.travelSpeed = 8f;

        var plate2 = MakePlate("PressurePlate_2", new Vector3(2, 0.35f, 12), new Vector3(1f, 0.1f, 1f), mechParent.transform);
        var pf2 = CreateEmpty("PlatFrame_2", Vector3.zero);
        pf2.transform.SetParent(mechParent.transform, true);
        var plat2 = MakeCube("MovingPlatform_2", Vector3.zero, new Vector3(3, 0.5f, 3), pf2.transform, _matBridge);
        plat2.layer = GROUND_LAYER;
        var mp2 = plat2.AddComponent<TimedMovingPlatform>();
        mp2.plate = plate2;
        mp2.inactiveLocal = new Vector3(0, 0, 21);
        mp2.activeLocal = new Vector3(0, 0, 16);
        mp2.travelSpeed = 8f;

        var exit = MakeCube("LevelExit", new Vector3(0, 0.5f, 26), new Vector3(2, 2, 0.3f), mechParent.transform, _matExit);
        var exitComp = exit.AddComponent<LevelExit>();
        exitComp.loadNextBuildIndex = true;

        SpawnLevelRuntime(mechParent.transform, "Usa el eco para encadenar dos puentes hasta la salida.", "Primero deja una accion sostenida, luego corre al segundo puente.", "La precision revela el patron.");
        SpawnPuzzleIntent(mechParent.transform, 2, 4, true, true, true, 12f, "Encadenado: eco en puente 1, avance, activacion de puente 2 y salida.");
        GoalTrigger l5P1 = CreateGoalTrigger("Goal_Plate_1", mechParent.transform, plate1, null, true, false);
        GoalTrigger l5P2 = CreateGoalTrigger("Goal_Plate_2", mechParent.transform, plate2, null, true, false);
        SpawnLevelGoal(mechParent.transform, new[] { exitComp }, new[] { l5P1, l5P2 }, "Activa ambas memorias para abrir el camino final.", "Camino completo.", "La precision revela el patron.", 2);
        SpawnGuideBeacon("GoalLight_L5", new Vector3(0f, 3.8f, 26f), mechParent.transform, new Color(1f, 0.82f, 0.42f, 1f));

        SpawnPlayer(new Vector3(0, 1.5f, -2), 3);
        SpawnCamera();
        SpawnHUD();
        SpawnPauseAndTutorial();
        SpawnLight();
        SpawnParticles();
        SpawnDecorations(envParent.transform, 6, 0f, 25f);
        SaveScene(scene, "Level_05");
    }

    // ================================================================
    //  LEVEL 06 — El Examen Final
    // ================================================================
    static void BuildLevel06()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Level_06";

        var envParent = CreateEmpty("--- ENVIRONMENT ---", Vector3.zero);
        MakeFloor("Floor_Start", new Vector3(0, 0, 0), new Vector3(10, 0.5f, 6), envParent.transform);
        MakeFloor("Floor_Split_L", new Vector3(-8, 0, 8), new Vector3(6, 0.5f, 6), envParent.transform);
        MakeFloor("Floor_Split_R", new Vector3(8, 0, 8), new Vector3(6, 0.5f, 6), envParent.transform);
        MakeFloor("Floor_Gate", new Vector3(0, 0, 16), new Vector3(10, 0.5f, 6), envParent.transform);
        MakeFloor("Floor_End", new Vector3(0, 0, 22), new Vector3(6, 0.5f, 6), envParent.transform);

        MakeFloor("Bridge_L", new Vector3(-4, 0, 4), new Vector3(2, 0.5f, 2), envParent.transform);
        MakeFloor("Bridge_R", new Vector3(4, 0, 4), new Vector3(2, 0.5f, 2), envParent.transform);
        MakeFloor("Bridge_L2", new Vector3(-4, 0, 12), new Vector3(2, 0.5f, 2), envParent.transform);
        MakeFloor("Bridge_R2", new Vector3(4, 0, 12), new Vector3(2, 0.5f, 2), envParent.transform);
        MakeFloor("Bridge_Final", new Vector3(0, 0, 19), new Vector3(2, 0.5f, 2), envParent.transform);

        var mechParent = CreateEmpty("--- MECHANICS ---", Vector3.zero);

        // 3 Plates needed to open the final door
        var p1 = MakePlate("Plate_L", new Vector3(-8, 0.35f, 8), new Vector3(1.5f, 0.1f, 1.5f), mechParent.transform);
        var p2 = MakePlate("Plate_R", new Vector3(8, 0.35f, 8), new Vector3(1.5f, 0.1f, 1.5f), mechParent.transform);
        var p3 = MakePlate("Plate_Center", new Vector3(0, 0.35f, 15), new Vector3(1.5f, 0.1f, 1.5f), mechParent.transform);

        var df = CreateEmpty("DoorFrame_Final", new Vector3(0, 0.25f, 18));
        df.transform.SetParent(mechParent.transform, true);
        var d1 = MakeCube("Door_Final", Vector3.zero, new Vector3(4, 8f, 0.3f), df.transform, _matDoor);
        var dc1 = d1.AddComponent<DoorController>();
        dc1.plates = new PressurePlate[] { p1, p2, p3 };

        var exit = MakeCube("LevelExit", new Vector3(0, 0.5f, 24), new Vector3(2, 2, 0.3f), mechParent.transform, _matExit);
        var exitComp = exit.AddComponent<LevelExit>();
        exitComp.loadNextBuildIndex = false;
        exitComp.nextSceneName = "MainMenu";
        exit.GetComponent<BoxCollider>().isTrigger = true;

        SpawnLevelRuntime(mechParent.transform, "Activa las tres placas y cruza la puerta final.", "Divide el recorrido con tus ecos antes de volver al centro.", "Tu identidad vuelve al centro.");
        SpawnPuzzleIntent(mechParent.transform, 3, 5, true, true, true, 16f, "Final: tres placas separadas y puerta central acumulativa.");
        GoalTrigger l6P1 = CreateGoalTrigger("Goal_Plate_Left", mechParent.transform, p1, null, true, false);
        GoalTrigger l6P2 = CreateGoalTrigger("Goal_Plate_Right", mechParent.transform, p2, null, true, false);
        GoalTrigger l6P3 = CreateGoalTrigger("Goal_Plate_Center", mechParent.transform, p3, null, true, false);
        GoalTrigger l6Door = CreateGoalTrigger("Goal_Door", mechParent.transform, null, dc1, false, true);
        SpawnLevelGoal(mechParent.transform, new[] { exitComp }, new[] { l6P1, l6P2, l6P3, l6Door }, "Completa las tres memorias del nivel.", "Salida restaurada.", "Tu identidad vuelve al centro.", 4);
        SpawnGuideBeacon("GoalLight_L6", new Vector3(0f, 4f, 24f), mechParent.transform, new Color(1f, 0.82f, 0.42f, 1f), 7f, 10f);

        SpawnPlayer(new Vector3(0, 1.5f, -2), 3, 20f);
        SpawnCamera();
        SpawnHUD();
        SpawnPauseAndTutorial();
        SpawnLight();
        SpawnParticles();
        SpawnDecorations(envParent.transform, 10, 0f, 25f);
        SaveScene(scene, "Level_06");
    }

    // ================================================================
    //  LEVEL 07 - Modular Gravity Trials
    // ================================================================
    static void BuildLevel07()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Level_07";

        var envParent = CreateEmpty("--- ENVIRONMENT ---", Vector3.zero);
        var mechParent = CreateEmpty("--- MECHANICS ---", Vector3.zero);

        var rooms = new List<ModularRoomSpec>
        {
            new ModularRoomSpec("Room_StartHub", new Vector3(0f, 0f, 0f), new Vector2(12f, 12f), 6f) { addCeiling = true },
            new ModularRoomSpec("Room_EchoGallery", new Vector3(0f, 0f, 18f), new Vector2(12f, 10f), 6f),
            new ModularRoomSpec("Room_GravityAtrium", new Vector3(18f, 0f, 18f), new Vector2(14f, 14f), 8f) { addCeiling = true },
            new ModularRoomSpec("Room_ArchiveWing", new Vector3(-16f, 0f, 18f), new Vector2(10f, 10f), 6f),
            new ModularRoomSpec("Room_ExitSanctum", new Vector3(18f, 0f, 38f), new Vector2(12f, 10f), 6f)
        };

        foreach (ModularRoomSpec room in rooms)
            BuildModularRoom(room, envParent.transform);

        var corridors = new List<ModularCorridorSpec>
        {
            new ModularCorridorSpec(0, 1, 4f),
            new ModularCorridorSpec(1, 2, 4f),
            new ModularCorridorSpec(1, 3, 4f),
            new ModularCorridorSpec(2, 4, 4f)
        };

        foreach (ModularCorridorSpec corridor in corridors)
            ConnectRooms(rooms[corridor.fromRoomIndex], rooms[corridor.toRoomIndex], corridor.width, envParent.transform);

        var wallWalk = MakeCube("GravityWall_Walkable", new Vector3(24f, 3f, 18f), new Vector3(0.5f, 6f, 10f), envParent.transform, _matBridge);
        wallWalk.layer = GROUND_LAYER;
        wallWalk.isStatic = true;

        var ceilingWalk = MakeCube("GravityCeiling_Walkable", new Vector3(18f, 6f, 26f), new Vector3(10f, 0.5f, 8f), envParent.transform, _matBridge);
        ceilingWalk.layer = GROUND_LAYER;
        ceilingWalk.isStatic = true;

        MakeGravityZone("GravityZone_Wall", new Vector3(19f, 3f, 18f), new Vector3(14f, 8f, 14f), Vector3.right, mechParent.transform, 24f, 10);
        MakeGravityZone("GravityZone_Ceiling", new Vector3(18f, 4.5f, 27f), new Vector3(12f, 5f, 12f), Vector3.up, mechParent.transform, 24f, 11);

        var p1 = MakePlate("Plate_EchoGallery", new Vector3(0f, 0.35f, 18f), new Vector3(1.5f, 0.1f, 1.5f), mechParent.transform);
        var p2 = MakePlate("Plate_ArchiveWing", new Vector3(-16f, 0.35f, 18f), new Vector3(1.5f, 0.1f, 1.5f), mechParent.transform);
        var p3 = MakePlate("Plate_GravityAtrium", new Vector3(18f, 0.35f, 23f), new Vector3(1.5f, 0.1f, 1.5f), mechParent.transform);

        var doorFrame = CreateEmpty("DoorFrame_ModularFinal", new Vector3(18f, 0.25f, 33f));
        doorFrame.transform.SetParent(mechParent.transform, true);
        var finalDoor = MakeCube("Door_ModularFinal", Vector3.zero, new Vector3(4f, 8f, 0.3f), doorFrame.transform, _matDoor);
        var doorController = finalDoor.AddComponent<DoorController>();
        doorController.plates = new[] { p1, p2, p3 };

        var exit = MakeCube("LevelExit", new Vector3(18f, 0.5f, 42f), new Vector3(2f, 2f, 0.3f), mechParent.transform, _matExit);
        exit.GetComponent<BoxCollider>().isTrigger = true;
        var exitComp = exit.AddComponent<LevelExit>();
        exitComp.loadNextBuildIndex = false;
        exitComp.nextSceneName = "MainMenu";

        SpawnLevelRuntime(mechParent.transform, "Activa las tres memorias y usa gravedad + eco para llegar al santuario.", "La pared y el techo son parte del puzzle, no solo del recorrido.", "Tu identidad vuelve al centro.");
        SpawnPuzzleIntent(mechParent.transform, 3, 6, true, true, true, 18f, "Trial modular: eco, timing y cambios de gravedad hasta la salida visible.");
        GoalTrigger l7P1 = CreateGoalTrigger("Goal_EchoGallery", mechParent.transform, p1, null, true, false);
        GoalTrigger l7P2 = CreateGoalTrigger("Goal_ArchiveWing", mechParent.transform, p2, null, true, false);
        GoalTrigger l7P3 = CreateGoalTrigger("Goal_GravityAtrium", mechParent.transform, p3, null, true, false);
        GoalTrigger l7Door = CreateGoalTrigger("Goal_FinalDoor", mechParent.transform, null, doorController, false, true);
        SpawnLevelGoal(mechParent.transform, new[] { exitComp }, new[] { l7P1, l7P2, l7P3, l7Door }, "Restaura las tres memorias y regresa al centro.", "Santuario abierto.", "Tu identidad vuelve al centro.", 4);
        SpawnGuideBeacon("GoalLight_L7", new Vector3(18f, 4.2f, 42f), mechParent.transform, new Color(1f, 0.82f, 0.42f, 1f), 7f, 11f);

        SpawnTutorialTrigger(
            "Tutorial_Gravity",
            new Vector3(18f, 1.5f, 14f),
            new Vector3(6f, 3f, 6f),
            "Las zonas azules cambian tu gravedad.",
            "Usa la pared y el techo para avanzar con el mismo control suave.",
            5f,
            mechParent.transform);

        SpawnPlayer(new Vector3(0f, 1.5f, -4f), 4, 20f);
        SpawnCamera();
        SpawnHUD();
        SpawnPauseAndTutorial();
        SpawnLight();
        SpawnParticles();
        SpawnDecorations(envParent.transform, 14, -2f, 46f);
        SaveScene(scene, "Level_07");
    }

    // ================================================================
    //  BUILD SETTINGS
    // ================================================================
    static void AddScenesToBuild()
    {
        string[] scenePaths = new[]
        {
            SCENE_ROOT + "/MainMenu.unity",
            SCENE_ROOT + "/Level_01.unity",
            SCENE_ROOT + "/Level_02.unity",
            SCENE_ROOT + "/Level_03.unity",
            SCENE_ROOT + "/Level_04.unity",
            SCENE_ROOT + "/Level_05.unity",
            SCENE_ROOT + "/Level_06.unity",
            SCENE_ROOT + "/Level_07.unity"
        };

        var list = new List<EditorBuildSettingsScene>();

        foreach (string sp in scenePaths)
        {
            if (!File.Exists(sp) && !File.Exists(Application.dataPath + "/../" + sp))
            {
                // Try the full path
                string fullPath = Path.GetFullPath(sp);
                if (!File.Exists(fullPath))
                {
                    Debug.LogWarning($"[Echoes] Scene not found, skipping: {sp}");
                    continue;
                }
            }
            list.Add(new EditorBuildSettingsScene(sp, true));
        }

        EditorBuildSettings.scenes = list.ToArray();
        Debug.Log($"[Echoes] Build Settings updated with {list.Count} scenes.");
    }

    // ================================================================
    //  HELPERS — SPAWNERS
    // ================================================================
    static void SpawnPlayer(Vector3 position, int maxEchoes = 3, float maxRecordSeconds = 10f)
    {
        var parent = CreateEmpty("--- PLAYER ---", Vector3.zero);

        GameObject player = new GameObject("Player");
        player.transform.SetParent(parent.transform, false);
        player.transform.position = position;
        player.tag = "Player";

        var rb = player.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        var cc = player.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.3f;
        cc.center = new Vector3(0, 0.9f, 0);
        cc.skinWidth = 0.08f;

        var playerController = player.AddComponent<PlayerController>();
        playerController.gravityStrength = 24f;
        playerController.alignToGroundNormal = true;
        playerController.rotationSharpness = 12f;
        playerController.acceleration = 14f;
        playerController.deceleration = 18f;
        playerController.groundMask = 1 << 6; // Layer 6 is Ground. Using -1 breaks the jump by detecting the player itself.

        var recorder = player.AddComponent<EchoRecorder>();
        GameObject echoPrefab = GetEchoPrefab();
        SetPrivateField(recorder, "echoPrefab", echoPrefab);
        SetPrivateField(recorder, "maxEchoes", maxEchoes);
        SetPrivateField(recorder, "maxRecordSeconds", maxRecordSeconds);

        // Visual child
        var visual = new GameObject("PlayerVisual");
        visual.transform.SetParent(player.transform, false);
        visual.transform.localPosition = Vector3.zero;

        var polyChar = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/3D Models/Character Base/characterBase.fbx");
        if (polyChar != null)
        {
            var instance = PrefabUtility.InstantiatePrefab(polyChar) as GameObject;
            instance.transform.SetParent(visual.transform, false);
            instance.name = "PlayerModel";
            instance.transform.localScale = Vector3.one;
            
            // Add Animator component if it doesn't have one
            var anim = instance.GetComponent<Animator>();
            if (anim == null) anim = instance.AddComponent<Animator>();
            anim.applyRootMotion = false;
            
            var animController = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>("Assets/Prefabs/PlayerAnimController.controller");
            if (animController != null) anim.runtimeAnimatorController = animController;
        }
        else
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "PlayerVisualCube";
            cube.transform.SetParent(visual.transform, false);
            cube.transform.localPosition = new Vector3(0, 0.9f, 0);
            cube.transform.localScale = new Vector3(0.6f, 1.8f, 0.6f);
            Object.DestroyImmediate(cube.GetComponent<BoxCollider>());
            if (_matPlayer != null)
                cube.GetComponent<MeshRenderer>().sharedMaterial = _matPlayer;
        }

        // GroundCheck child
        var gc = new GameObject("GroundCheck");
        gc.transform.SetParent(player.transform, false);
        gc.transform.localPosition = new Vector3(0, -0.78f, 0);

        var focus = new GameObject("CameraFocus");
        focus.transform.SetParent(player.transform, false);
        focus.transform.localPosition = new Vector3(0f, 1.2f, 0.08f);
    }

    static void SpawnCamera()
    {
        var parent = CreateEmpty("--- CAMERA ---", Vector3.zero);

        // Find or create camera
        Camera cam = Object.FindAnyObjectByType<Camera>();
        GameObject camGO;
        if (cam != null)
        {
            camGO = cam.gameObject;
        }
        else
        {
            camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            cam = camGO.AddComponent<Camera>();
            camGO.AddComponent<AudioListener>();
        }
        camGO.transform.SetParent(parent.transform, false);
        camGO.transform.position = new Vector3(0, 4, -6);

        // Estética: Sin Skybox por defecto, color oscuro
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = HexColor("080A12");
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = HexColor("151520");

        // ThirdPersonCamera
        var tpc = camGO.GetComponent<ThirdPersonCamera>();
        if (tpc == null) tpc = camGO.AddComponent<ThirdPersonCamera>();
        tpc.distance = 4f;
        tpc.focusOffset = new Vector3(0f, 1.2f, 0f);
        tpc.baseFov = 54f;
        tpc.minPitch = 8f;
        tpc.maxPitch = 18f;

        if (camGO.GetComponent<CameraShake>() == null)
            camGO.AddComponent<CameraShake>();

        if (camGO.GetComponent<GameFeelController>() == null)
            camGO.AddComponent<GameFeelController>();

        // Find the Player object to assign as target
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Transform focus = player.transform.Find("CameraFocus");
            Transform target = focus != null ? focus : player.transform;
            tpc.target = target;

            var worldUp = new GameObject("CameraWorldUp");
            worldUp.transform.SetParent(parent.transform, false);
            var upOverride = worldUp.AddComponent<CameraWorldUpOverride>();
            upOverride.Target = target;
        }
    }

    static void SpawnHUD()
    {
        var parent = CreateEmpty("--- UI ---", Vector3.zero);
        var hudGO = new GameObject("GameHUD");
        hudGO.transform.SetParent(parent.transform, false);
        hudGO.AddComponent<GameHUD>();
    }

    static void SpawnPauseAndTutorial()
    {
        var uiParent = GameObject.Find("--- UI ---");
        Transform parent = uiParent != null ? uiParent.transform : null;

        var pauseGO = new GameObject("PauseMenu");
        if (parent != null) pauseGO.transform.SetParent(parent, false);
        pauseGO.AddComponent<PauseMenu>();

        var tutGO = new GameObject("TutorialHUD");
        if (parent != null) tutGO.transform.SetParent(parent, false);
        tutGO.AddComponent<TutorialHUD>();
    }

    static void SpawnTutorialTrigger(string name, Vector3 pos, Vector3 size,
        string msg, string hint, float duration, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;

        var col = go.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = size;

        var trig = go.AddComponent<TutorialTrigger>();
        SetPrivateField(trig, "message", msg);
        SetPrivateField(trig, "hint", hint);
        SetPrivateField(trig, "displayDuration", duration);
        SetPrivateField(trig, "oneShot", true);
    }

    static void SpawnLevelRuntime(Transform parent, string objective, string intro, string completion)
    {
        var runtimeGO = new GameObject("LevelRuntimeController");
        runtimeGO.transform.SetParent(parent, false);
        var runtime = runtimeGO.AddComponent<LevelRuntimeController>();
        SetPrivateField(runtime, "objectiveText", objective);
        SetPrivateField(runtime, "introLine", intro);
        SetPrivateField(runtime, "completionLine", completion);

        var stateGO = new GameObject("GameStateController");
        stateGO.transform.SetParent(parent, false);
        stateGO.AddComponent<GameStateController>();
    }

    static void SpawnPuzzleIntent(Transform parent, int buttonCount, int requiredActions, bool requiresMovement, bool requiresTiming, bool multiStep, float echoDistance, string note)
    {
        var intentGO = new GameObject("PuzzleIntent");
        intentGO.transform.SetParent(parent, false);
        var intent = intentGO.AddComponent<PuzzleIntent>();
        intent.buttonCount = buttonCount;
        intent.requiredActions = requiredActions;
        intent.requiresMovement = requiresMovement;
        intent.requiresTiming = requiresTiming;
        intent.isMultiStep = multiStep;
        intent.minimumEchoDistance = echoDistance;
        SetPrivateField(intent, "designNote", note);
    }

    static GoalTrigger CreateGoalTrigger(string name, Transform parent, PressurePlate plate, DoorController door, bool usePlate, bool useDoor)
    {
        var triggerGO = new GameObject(name);
        triggerGO.transform.SetParent(parent, false);
        var trigger = triggerGO.AddComponent<GoalTrigger>();
        SetPrivateField(trigger, "pressurePlate", plate);
        SetPrivateField(trigger, "doorController", door);
        SetPrivateField(trigger, "usePlatePressedState", usePlate);
        SetPrivateField(trigger, "useDoorOpenState", useDoor);
        SetPrivateField(trigger, "displayName", name);
        return trigger;
    }

    static void SpawnLevelGoal(Transform parent, LevelExit[] exits, GoalTrigger[] triggers, string objective, string readyPrompt, string completionToast, int requiredTriggerCount)
    {
        var goalGO = new GameObject("LevelGoal");
        goalGO.transform.SetParent(parent, false);
        var goal = goalGO.AddComponent<LevelGoal>();
        SetPrivateField(goal, "objectiveText", objective);
        SetPrivateField(goal, "readyPrompt", readyPrompt);
        SetPrivateField(goal, "completionToast", completionToast);
        SetPrivateField(goal, "requiredTriggerCount", requiredTriggerCount);

        var so = new SerializedObject(goal);
        var exitsProp = so.FindProperty("linkedExits");
        exitsProp.arraySize = exits.Length;
        for (int i = 0; i < exits.Length; i++)
            exitsProp.GetArrayElementAtIndex(i).objectReferenceValue = exits[i];

        var triggerProp = so.FindProperty("triggers");
        triggerProp.arraySize = triggers.Length;
        for (int i = 0; i < triggers.Length; i++)
            triggerProp.GetArrayElementAtIndex(i).objectReferenceValue = triggers[i];

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SpawnGuideBeacon(string name, Vector3 position, Transform parent, Color color, float intensity = 6f, float range = 9f)
    {
        var beacon = new GameObject(name);
        beacon.transform.SetParent(parent, false);
        beacon.transform.position = position;

        var light = beacon.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.None;
    }

    // ================================================================
    //  MAIN MENU SCENE
    // ================================================================
    static void BuildMainMenu()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "MainMenu";

        // Camera
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        camGO.AddComponent<Camera>();
        camGO.AddComponent<AudioListener>();
        camGO.transform.position = new Vector3(0, 1, -10);

        // Menu Script
        var menuGO = new GameObject("MainMenu");
        menuGO.AddComponent<MainMenu>();

        // Aesthetic background (since skybox is removed)
        camGO.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
        camGO.GetComponent<Camera>().backgroundColor = HexColor("080A12");
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = HexColor("151520");
        SpawnLight();
        SpawnAtmosphere();
        SpawnParticles();

        SaveScene(scene, "MainMenu");
        Debug.Log("[Echoes] MainMenu built.");
    }

    static void SpawnLight()
    {
        var lightGO = new GameObject("DirectionalLight");
        lightGO.transform.position = new Vector3(0, 10, 0);
        lightGO.transform.eulerAngles = new Vector3(50, -30, 0);
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        // Warm sunset look for aesthetics
        light.color = HexColor("FFD5A1");
        light.intensity = 1.3f; // Slightly lowered to reduce blowout
        light.shadows = LightShadows.Soft;
    }

    static void SpawnAtmosphere()
    {
        var go = new GameObject("--- ATMOSPHERE ---");
        go.AddComponent<AtmosphereController>();
    }

    static void SpawnParticles()
    {
        var psGO = new GameObject("EnvironmentParticles");
        psGO.transform.position = new Vector3(0, 2, 8);
        var ps = psGO.AddComponent<ParticleSystem>();

        var main = ps.main;
        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = _matEcho; // Usar material del eco para brillo

        main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
        main.maxParticles = 50;

        var em = ps.emission;
        em.rateOverTime = 8f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(20, 10, 25);

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.y = new ParticleSystem.MinMaxCurve(0.1f, 0.5f);
        vel.x = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
        vel.z = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);

        var color = ps.colorOverLifetime;
        color.enabled = true;

        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) }
        );
        color.color = new ParticleSystem.MinMaxGradient(g);
    }

    // ================================================================
    //  HELPERS — GEOMETRY
    // ================================================================

    static GameObject MakeCube(string name, Vector3 localPos, Vector3 scale, Transform parent, Material mat, string fbxModelPath = null)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = localPos;
        cube.transform.localScale = scale;

        if (!string.IsNullOrEmpty(fbxModelPath))
        {
            var pfb = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(fbxModelPath);
            if (pfb != null)
            {
                cube.GetComponent<MeshRenderer>().enabled = false;
                var vis = UnityEditor.PrefabUtility.InstantiatePrefab(pfb) as GameObject;
                vis.transform.SetParent(cube.transform, false);
                vis.transform.localPosition = Vector3.zero;
                vis.transform.localScale = Vector3.one; // Inherit parent bounds perfectly
            }
            else
            {
                if (mat != null) cube.GetComponent<MeshRenderer>().sharedMaterial = mat;
            }
        }
        else
        {
            if (mat != null) cube.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        return cube;
    }

    static void MakeFloor(string name, Vector3 pos, Vector3 scale, Transform parent)
    {
        var cube = MakeCube(name, pos, scale, parent, _matFloor, "Assets/3D Models/Models/FBX format/block-grass.fbx");
        cube.isStatic = true;
        cube.layer = GROUND_LAYER;
    }

    static void BuildModularRoom(ModularRoomSpec room, Transform parent)
    {
        var roomRoot = CreateEmpty(room.name, room.center);
        roomRoot.transform.SetParent(parent, true);

        MakeFloor(room.name + "_Floor", room.center, new Vector3(room.size.x, 0.5f, room.size.y), roomRoot.transform.parent);

        if (room.addSideWalls)
        {
            float halfX = room.size.x * 0.5f;
            float halfZ = room.size.y * 0.5f;
            float wallHeight = room.ceilingHeight;
            float thickness = room.wallThickness;

            var north = MakeCube(room.name + "_NorthWall", new Vector3(room.center.x, wallHeight * 0.5f, room.center.z + halfZ), new Vector3(room.size.x, wallHeight, thickness), parent, _matBridge);
            var south = MakeCube(room.name + "_SouthWall", new Vector3(room.center.x, wallHeight * 0.5f, room.center.z - halfZ), new Vector3(room.size.x, wallHeight, thickness), parent, _matBridge);
            var east = MakeCube(room.name + "_EastWall", new Vector3(room.center.x + halfX, wallHeight * 0.5f, room.center.z), new Vector3(thickness, wallHeight, room.size.y), parent, _matBridge);
            var west = MakeCube(room.name + "_WestWall", new Vector3(room.center.x - halfX, wallHeight * 0.5f, room.center.z), new Vector3(thickness, wallHeight, room.size.y), parent, _matBridge);

            north.isStatic = south.isStatic = east.isStatic = west.isStatic = true;
            north.layer = south.layer = east.layer = west.layer = GROUND_LAYER;
        }

        if (room.addCeiling)
        {
            var ceiling = MakeCube(room.name + "_Ceiling", new Vector3(room.center.x, room.ceilingHeight, room.center.z), new Vector3(room.size.x, 0.5f, room.size.y), parent, _matBridge);
            ceiling.isStatic = true;
            ceiling.layer = GROUND_LAYER;
        }
    }

    static void ConnectRooms(ModularRoomSpec from, ModularRoomSpec to, float width, Transform parent)
    {
        Vector3 delta = to.center - from.center;
        float floorThickness = 0.5f;

        if (Mathf.Abs(delta.x) > 0.01f && Mathf.Abs(delta.z) > 0.01f)
        {
            Debug.LogWarning($"[Echoes] Corridor '{from.name}' -> '{to.name}' is diagonal. Keep room centers aligned on X or Z.");
            return;
        }

        if (Mathf.Abs(delta.x) > 0.01f)
        {
            float direction = Mathf.Sign(delta.x);
            float start = from.center.x + direction * (from.size.x * 0.5f);
            float end = to.center.x - direction * (to.size.x * 0.5f);
            float length = Mathf.Abs(end - start);
            if (length <= 0.1f) return;

            float centerX = (start + end) * 0.5f;
            MakeFloor($"{from.name}_To_{to.name}_Corridor", new Vector3(centerX, 0f, from.center.z), new Vector3(length, floorThickness, width), parent);
            return;
        }

        float directionZ = Mathf.Sign(delta.z);
        float startZ = from.center.z + directionZ * (from.size.y * 0.5f);
        float endZ = to.center.z - directionZ * (to.size.y * 0.5f);
        float corridorLength = Mathf.Abs(endZ - startZ);
        if (corridorLength <= 0.1f) return;

        float centerZ = (startZ + endZ) * 0.5f;
        MakeFloor($"{from.name}_To_{to.name}_Corridor", new Vector3(from.center.x, 0f, centerZ), new Vector3(width, floorThickness, corridorLength), parent);
    }

    static GravityZone MakeGravityZone(string name, Vector3 pos, Vector3 size, Vector3 gravityDirection, Transform parent, float gravityStrength, int priority)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;

        var col = go.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = size;

        var zone = go.AddComponent<GravityZone>();
        SetPrivateField(zone, "gravityDirection", gravityDirection);
        SetPrivateField(zone, "gravityStrength", gravityStrength);
        SetPrivateField(zone, "priority", priority);
        return zone;
    }



    static GameObject CreateEmpty(string name, Vector3 pos)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        return go;
    }

    static void SpawnDecorations(Transform parent, int amount, float minZ, float maxZ)
    {
        var treePfb = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/3D Models/Models/FBX format/tree-pine.fbx");
        if (treePfb == null) return;

        var decorParent = CreateEmpty("Decorations", Vector3.zero);
        decorParent.transform.SetParent(parent, false);

        for (int i = 0; i < amount; i++)
        {
            float sideX = Random.value > 0.5f ? Random.Range(3f, 8f) : Random.Range(-8f, -3f);
            float z = Random.Range(minZ, maxZ);
            
            var tree = PrefabUtility.InstantiatePrefab(treePfb) as GameObject;
            tree.transform.SetParent(decorParent.transform, false);
            tree.transform.position = new Vector3(sideX, -0.5f, z);
            float scale = Random.Range(0.8f, 1.5f);
            tree.transform.localScale = new Vector3(scale, scale, scale);
            tree.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
        }
    }

    // ================================================================
    //  HELPERS — SCENE
    // ================================================================
    static void SaveScene(Scene scene, string sceneName)
    {
        string path = SCENE_ROOT + "/" + sceneName + ".unity";
        EditorSceneManager.SaveScene(scene, path);
    }

    // ================================================================
    //  HELPERS — REFLECTION (set [SerializeField] private fields)
    // ================================================================
    static void SetPrivateField<T>(Component comp, string fieldName, T value)
    {
        var so = new SerializedObject(comp);
        var prop = so.FindProperty(fieldName);
        if (prop == null)
        {
            Debug.LogWarning($"[Echoes] Field '{fieldName}' not found on {comp.GetType().Name}");
            return;
        }

        switch (value)
        {
            case int i:
                prop.intValue = i;
                break;
            case float f:
                prop.floatValue = f;
                break;
            case bool b:
                prop.boolValue = b;
                break;
            case string s:
                prop.stringValue = s;
                break;
            case Vector3 v:
                prop.vector3Value = v;
                break;
            case GameObject go:
                prop.objectReferenceValue = go;
                break;
            case PressurePlate pp:
                prop.objectReferenceValue = pp;
                break;
            case PressurePlate[] ppArr:
                prop.arraySize = ppArr.Length;
                for (int idx = 0; idx < ppArr.Length; idx++)
                    prop.GetArrayElementAtIndex(idx).objectReferenceValue = ppArr[idx];
                break;
            default:
                prop.objectReferenceValue = value as Object;
                break;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ================================================================
    //  HELPERS — COLOR
    // ================================================================
    static Color HexColor(string hex, float alpha = 1f)
    {
        if (ColorUtility.TryParseHtmlString("#" + hex, out Color c))
        {
            c.a = alpha;
            return c;
        }
        return new Color(1, 0, 1, alpha); // magenta fallback
    }
}
