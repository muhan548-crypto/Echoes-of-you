import re

path = r"c:\Users\lol xdd\OneDrive\Documentos\Colegio\Echoes of you\Assets\Editor\EchoesProductionBuilder.cs"
with open(path, "r", encoding="utf-8") as f:
    content = f.read()

# LEVEL 03: EL SACRIFICIO (Jumping into the Abyss)
l3 = '''    static void BuildLevel03()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Level_03";
        Transform env = CreateRoot("--- ENVIRONMENT ---");
        Transform mech = CreateRoot("--- MECHANICS ---");
        Transform ui = CreateRoot("--- UI ---");
        Transform tutorial = CreateRoot("--- TUTORIAL ---");

        SetupAtmosphere(new Color(0.02f, 0.02f, 0.04f, 1f), 0.06f, new Color(0.02f, 0.02f, 0.04f, 1f));
        SpawnDirectionalLight();
        MakeBackdrop("Backdrop", new Vector3(0f, 0f, 8f), 32f, 40f, 40f, env);

        // U-Shape Upper Platforms
        MakePlatform("Floor_Start", new Vector3(-6f, 4f, 6f), new Vector3(6f, 0.5f, 18f), env, _floorMat);
        MakePlatform("Floor_Exit", new Vector3(6f, 4f, 6f), new Vector3(6f, 0.5f, 18f), env, _floorMat);
        
        // Deep Pit in the middle
        MakePlatform("Floor_Pit", new Vector3(0f, -6f, 10f), new Vector3(6f, 0.5f, 8f), env, _floorMat);

        // Button at bottom of pit
        PressurePlate btnPit = CreatePlate("Button_Pit", new Vector3(0f, -5.64f, 10f), mech);
        btnPit.autoReleaseTimer = 2f;

        // Drawbridge connecting the two upper arms across the void
        TimedMovingPlatform bridge = CreateBridge("VoidBridge", new Vector3(0f, 4f, 12f), new Vector3(0f, -8f, 0f), new Vector3(0f, 0f, 0f), new Vector3(6f, 0.5f, 4f), btnPit, mech);

        LevelExit exit = CreateLevelExit(new Vector3(6f, 5.25f, 12f), mech, "Level_04");
        CreateLevelGoal(mech, "A veces, la unica forma de subir es caer.", "El abismo responde.", "El sacrificio de un recuerdo.", exit, btnPit);

        // A jumping ramp pointing into the void
        MakePlatform("Ramp_Jump", new Vector3(-3.5f, 4.2f, 10f), new Vector3(2f, 0.2f, 2f), env, _bridgeMat);

        GameObject player = SpawnPlayer(new Vector3(-6f, 5.1f, -1f), true, 1, 8f);
        SpawnGameplayCamera(player.transform);

        SpawnPointLight("Light_Pit", new Vector3(0f, -4f, 10f), new Color(0.6f, 0.1f, 0.2f, 1f), 4f, 14f, env);
        SpawnPointLight("Light_Bridge", new Vector3(0f, 6f, 12f), new Color(0.1f, 0.6f, 1f, 1f), 3f, 8f, env);

        SpawnGameplayHud(ui);
        SpawnPauseMenu(ui);
        SpawnLevelRuntime(mech, "", "Salta al vacio. Graba tu final.", "");
        SpawnAmbientLights(env, new Vector3(0f, 4f, 6f), 16f, 24f);
        
        // Silent hill dense fog in the pit
        SpawnSmokeVolume("L03_PitFog", new Vector3(0f, -2f, 10f), new Vector3(12f, 8f, 14f), env, 60f);

        SaveScene(scene, "Level_03");
    }'''

# LEVEL 05: LA ORQUESTA (2 Echos, 3 Buttons)
l5 = '''    static void BuildLevel05()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Level_05";
        Transform env = CreateRoot("--- ENVIRONMENT ---");
        Transform mech = CreateRoot("--- MECHANICS ---");
        Transform ui = CreateRoot("--- UI ---");
        Transform tutorial = CreateRoot("--- TUTORIAL ---");

        SetupAtmosphere(new Color(0.01f, 0.02f, 0.03f, 1f), 0.06f, new Color(0.01f, 0.02f, 0.03f, 1f));
        SpawnDirectionalLight();
        MakeBackdrop("Backdrop", new Vector3(0f, 0f, 12f), 36f, 50f, 50f, env);

        // Start platform
        MakePlatform("Floor_Main", new Vector3(0f, 0f, 0f), new Vector3(8f, 0.5f, 8f), env, _floorMat);
        
        // Islands (Jump distance 4.5m - needs sprint)
        MakePlatform("Isle_Left", new Vector3(-8f, 0f, 10f), new Vector3(4f, 0.5f, 4f), env, _floorMat);
        MakePlatform("Isle_Right", new Vector3(8f, 0f, 10f), new Vector3(4f, 0.5f, 4f), env, _floorMat);
        MakePlatform("Isle_Center", new Vector3(0f, 0f, 14f), new Vector3(4f, 0.5f, 4f), env, _floorMat);

        MakePlatform("Floor_Exit", new Vector3(0f, 0f, 24f), new Vector3(8f, 0.5f, 8f), env, _floorMat);

        PressurePlate btnL = CreatePlate("Button_L", new Vector3(-8f, 0.36f, 10f), mech);
        PressurePlate btnR = CreatePlate("Button_R", new Vector3(8f, 0.36f, 10f), mech);
        PressurePlate btnC = CreatePlate("Button_C", new Vector3(0f, 0.36f, 14f), mech);

        // We use PuzzleWire to connect 3 buttons with AND logic to the Door
        DoorController door = CreateDoor("Door_Exit", new Vector3(0f, 1.75f, 19f), new Vector3(6f, 3.5f, 1f), mech, new PressurePlate[0]);
        door.latchOpen = false;

        GameObject wireObj = new GameObject("Wire_3Buttons");
        wireObj.transform.SetParent(mech);
        PuzzleWire wire = wireObj.AddComponent<PuzzleWire>();
        wire.connections = new PuzzleWire.Connection[1];
        wire.connections[0] = new PuzzleWire.Connection {
            door = door,
            plates = new[] { btnL, btnR, btnC },
            logic = PuzzleWire.LogicMode.AND,
            latchOpen = true,
            openMessage = "Armonia alcanzada."
        };

        LevelExit exit = CreateLevelExit(new Vector3(0f, 1.25f, 24f), mech, "Level_06");
        CreateLevelGoal(mech, "Tres voces deben sonar al unisono.", "Convergencia.", "No estas solo en tu mente.", exit, btnL, btnR, btnC);

        // MAX ECHOES = 2
        GameObject player = SpawnPlayer(new Vector3(0f, 1.1f, -2f), true, 2, 12f);
        SpawnGameplayCamera(player.transform);

        SpawnPointLight("Light_L", new Vector3(-8f, 2f, 10f), new Color(0.1f, 0.5f, 1f), 3f, 8f, env);
        SpawnPointLight("Light_R", new Vector3(8f, 2f, 10f), new Color(0.1f, 0.5f, 1f), 3f, 8f, env);
        SpawnPointLight("Light_C", new Vector3(0f, 2f, 14f), new Color(0.1f, 0.5f, 1f), 3f, 8f, env);
        SpawnPointLight("Light_Door", new Vector3(0f, 3f, 19f), new Color(0.6f, 0.1f, 0.1f), 3f, 6f, env);

        SpawnGameplayHud(ui);
        SpawnPauseMenu(ui);
        SpawnLevelRuntime(mech, "", "Tienes DOS ecos. Úsalos sabiamente. Presiona Shift para correr.", "");
        SpawnAmbientLights(env, new Vector3(0f, 0f, 12f), 24f, 36f);
        SpawnSmokeVolume("L05_Fog", new Vector3(0f, -2f, 12f), new Vector3(28f, 4f, 36f), env, 50f);

        SaveScene(scene, "Level_05");
    }'''

# LEVEL 06: LA ESCALERA (3 Echos, Platforming Sequence)
l6 = '''    static void BuildLevel06()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Level_06";
        Transform env = CreateRoot("--- ENVIRONMENT ---");
        Transform mech = CreateRoot("--- MECHANICS ---");
        Transform ui = CreateRoot("--- UI ---");
        Transform tutorial = CreateRoot("--- TUTORIAL ---");

        SetupAtmosphere(new Color(0.01f, 0.01f, 0.02f, 1f), 0.07f, new Color(0.01f, 0.01f, 0.02f, 1f));
        SpawnDirectionalLight();
        MakeBackdrop("Backdrop", new Vector3(0f, 0f, 20f), 28f, 60f, 60f, env);

        // Start platform
        MakePlatform("Floor_Start", new Vector3(0f, 0f, 0f), new Vector3(12f, 0.5f, 10f), env, _floorMat);
        // Exit platform (Way across the void)
        MakePlatform("Floor_Exit", new Vector3(0f, 4f, 34f), new Vector3(8f, 0.5f, 10f), env, _floorMat);

        // 3 Buttons on start platform
        PressurePlate btn1 = CreatePlate("Button_1", new Vector3(-4f, 0.36f, 2f), mech);
        PressurePlate btn2 = CreatePlate("Button_2", new Vector3(0f, 0.36f, 2f), mech);
        PressurePlate btn3 = CreatePlate("Button_3", new Vector3(4f, 0.36f, 2f), mech);
        btn1.autoReleaseTimer = 2.5f;
        btn2.autoReleaseTimer = 2.5f;
        btn3.autoReleaseTimer = 2.5f;

        // 3 Bridges forming a staircase across the void
        CreateBridge("Bridge_1", new Vector3(0f, 0f, 10f), new Vector3(0f, -10f, 0f), new Vector3(0f, 0f, 0f), new Vector3(4f, 0.5f, 4f), btn1, mech);
        CreateBridge("Bridge_2", new Vector3(0f, 2f, 18f), new Vector3(0f, -10f, 0f), new Vector3(0f, 0f, 0f), new Vector3(4f, 0.5f, 4f), btn2, mech);
        CreateBridge("Bridge_3", new Vector3(0f, 4f, 26f), new Vector3(0f, -10f, 0f), new Vector3(0f, 0f, 0f), new Vector3(4f, 0.5f, 4f), btn3, mech);

        LevelExit exit = CreateLevelExit(new Vector3(0f, 5.25f, 36f), mech, "MainMenu");
        CreateLevelGoal(mech, "Sincroniza tus latidos.", "Eres libre.", "Una escalera de tiempo y memoria.", exit, btn3); // Exit logic is just reaching the end

        // MAX ECHOES = 3
        GameObject player = SpawnPlayer(new Vector3(0f, 1.1f, -3f), true, 3, 10f);
        SpawnGameplayCamera(player.transform);

        SpawnPointLight("Light_B1", new Vector3(-4f, 2f, 2f), new Color(0.2f, 0.6f, 1f), 3f, 6f, env);
        SpawnPointLight("Light_B2", new Vector3(0f, 2f, 2f), new Color(0.2f, 0.6f, 1f), 3f, 6f, env);
        SpawnPointLight("Light_B3", new Vector3(4f, 2f, 2f), new Color(0.2f, 0.6f, 1f), 3f, 6f, env);
        
        SpawnPointLight("Light_P1", new Vector3(0f, 2f, 10f), new Color(0.8f, 0.8f, 0.9f), 3f, 8f, env);
        SpawnPointLight("Light_P2", new Vector3(0f, 4f, 18f), new Color(0.8f, 0.8f, 0.9f), 3f, 8f, env);
        SpawnPointLight("Light_P3", new Vector3(0f, 6f, 26f), new Color(0.8f, 0.8f, 0.9f), 3f, 8f, env);

        SpawnGameplayHud(ui);
        SpawnPauseMenu(ui);
        SpawnLevelRuntime(mech, "", "Tienes TRES ecos. Crea un camino en el tiempo.", "");
        SpawnAmbientLights(env, new Vector3(0f, 0f, 18f), 20f, 40f);
        SpawnSmokeVolume("L06_Fog", new Vector3(0f, -4f, 18f), new Vector3(20f, 8f, 40f), env, 60f);

        SaveScene(scene, "Level_06");
    }'''

content = re.sub(r'    static void BuildLevel03\(\)\s*\{[\s\S]*?SaveScene\(scene, "Level_03"\);\s*\}', l3, content)
content = re.sub(r'    static void BuildLevel05\(\)\s*\{[\s\S]*?SaveScene\(scene, "Level_05"\);\s*\}', l5, content)
content = re.sub(r'    static void BuildLevel06\(\)\s*\{[\s\S]*?SaveScene\(scene, "Level_06"\);\s*\}', l6, content)

with open(path, "w", encoding="utf-8") as f:
    f.write(content)

print("Levels 3, 5, and 6 updated for multiple echoes, poetic hints, and deep fog.")
