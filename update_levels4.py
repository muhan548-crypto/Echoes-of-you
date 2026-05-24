import re

path = r"c:\Users\lol xdd\OneDrive\Documentos\Colegio\Echoes of you\Assets\Editor\EchoesProductionBuilder.cs"
with open(path, "r", encoding="utf-8") as f:
    content = f.read()

# LEVEL 02: THE ELEVATOR DECOY
# Uses a moving platform (elevator) and requires the Echo to do 2 sequential tasks.
l2 = '''    static void BuildLevel02()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Level_02";
        Transform env = CreateRoot("--- ENVIRONMENT ---");
        Transform mech = CreateRoot("--- MECHANICS ---");
        Transform ui = CreateRoot("--- UI ---");
        Transform tutorial = CreateRoot("--- TUTORIAL ---");

        SetupAtmosphere(new Color(0.05f, 0.05f, 0.08f, 1f), 0.045f, new Color(0.05f, 0.05f, 0.08f, 1f));
        SpawnDirectionalLight();
        MakeBackdrop("Backdrop", new Vector3(0f, 0f, 15f), 32f, 40f, 40f, env);

        // Ground Floor
        MakePlatform("Floor_Ground", new Vector3(0f, 0f, 6f), new Vector3(16f, 0.5f, 16f), env, _floorMat);
        // High Ledge
        MakePlatform("Floor_High", new Vector3(0f, 4f, 22f), new Vector3(8f, 0.5f, 8f), env, _floorMat);
        
        // Buttons on Ground Floor
        PressurePlate btnDecoy = CreatePlate("Button_DECOY", new Vector3(-5f, 0.36f, 6f), mech);
        PressurePlate btnReal = CreatePlate("Button_REAL", new Vector3(5f, 0.36f, 6f), mech);

        // Elevator (Bridge moving vertically)
        TimedMovingPlatform elevator = CreateBridge("Elevator", new Vector3(0f, 0f, 16f), new Vector3(0f, 4f, 0f), new Vector3(0f, 0f, 0f), new Vector3(4f, 0.5f, 4f), btnDecoy, mech);
        
        // Door on High Ledge blocking Exit
        DoorController exitDoor = CreateDoor("Door_Exit", new Vector3(0f, 5.75f, 19.5f), new Vector3(8f, 3.5f, 1f), mech, new[] { btnReal });
        exitDoor.latchOpen = false;

        LevelExit exit = CreateLevelExit(new Vector3(0f, 5.25f, 24f), mech, "Level_03");
        CreateLevelGoal(mech, "Aprovecha el ascensor.", "Salida abierta.", "Un buen eco hace dos favores.", exit, btnReal);

        GameObject player = SpawnPlayer(new Vector3(0f, 1.1f, 0f), true, 1, 10f);
        SpawnGameplayCamera(player.transform);

        SpawnPointLight("Light_Decoy", new Vector3(-5f, 2f, 6f), new Color(1f, 0.8f, 0.2f, 1f), 3f, 8f, env);
        SpawnPointLight("Light_Real", new Vector3(5f, 2f, 6f), new Color(0.1f, 0.5f, 1f), 3f, 8f, env);
        SpawnPointLight("Light_Elevator", new Vector3(0f, 6f, 16f), new Color(0.8f, 0.8f, 0.9f, 1f), 4f, 12f, env);
        SpawnPointLight("Light_Door", new Vector3(0f, 7f, 19.5f), new Color(0.6f, 0.1f, 0.1f), 2f, 5f, env);

        SpawnGameplayHud(ui);
        SpawnPauseMenu(ui);
        SpawnLevelRuntime(mech, "", "El eco puede realizar multiples tareas antes de desaparecer.", "");
        SpawnAmbientLights(env, new Vector3(0f, 2f, 12f), 16f, 32f);
        SpawnSmokeVolume("L02_Fog", new Vector3(0f, 0.5f, 12f), new Vector3(18f, 3f, 32f), env, 30f);

        CreateTutorialTrigger("Tut_Elevator", new Vector3(-5f, 1f, 6f), new Vector3(4f, 3f, 4f),
            "Este boton baja el ascensor.", "Pero necesitas estar arriba para cruzar la puerta.", 4f, tutorial);

        SaveScene(scene, "Level_02");
    }'''

# LEVEL 03: THE SACRIFICE (JUMPING INTO THE PIT)
l3 = '''    static void BuildLevel03()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Level_03";
        Transform env = CreateRoot("--- ENVIRONMENT ---");
        Transform mech = CreateRoot("--- MECHANICS ---");
        Transform ui = CreateRoot("--- UI ---");
        Transform tutorial = CreateRoot("--- TUTORIAL ---");

        SetupAtmosphere(new Color(0.04f, 0.04f, 0.07f, 1f), 0.05f, new Color(0.04f, 0.04f, 0.07f, 1f));
        SpawnDirectionalLight();
        MakeBackdrop("Backdrop", new Vector3(0f, 0f, 8f), 32f, 40f, 40f, env);

        // U-Shape Upper Platforms
        MakePlatform("Floor_Start", new Vector3(-6f, 4f, 6f), new Vector3(6f, 0.5f, 18f), env, _floorMat);
        MakePlatform("Floor_Exit", new Vector3(6f, 4f, 6f), new Vector3(6f, 0.5f, 18f), env, _floorMat);
        
        // Deep Pit in the middle
        MakePlatform("Floor_Pit", new Vector3(0f, -6f, 10f), new Vector3(6f, 0.5f, 8f), env, _floorMat);

        // Button at bottom of pit
        PressurePlate btnPit = CreatePlate("Button_Pit", new Vector3(0f, -5.64f, 10f), mech);
        btnPit.autoReleaseTimer = 6f;

        // Drawbridge connecting the two upper arms across the void
        TimedMovingPlatform bridge = CreateBridge("VoidBridge", new Vector3(0f, 4f, 12f), new Vector3(0f, -8f, 0f), new Vector3(0f, 0f, 0f), new Vector3(6f, 0.5f, 4f), btnPit, mech);

        LevelExit exit = CreateLevelExit(new Vector3(6f, 5.25f, 12f), mech, "Level_04");
        CreateLevelGoal(mech, "Alguien debe caer.", "Puente extendido.", "El eco absorbe tu caida.", exit, btnPit);

        // A jumping ramp pointing into the void
        MakePlatform("Ramp_Jump", new Vector3(-3.5f, 4.2f, 10f), new Vector3(2f, 0.2f, 2f), env, _bridgeMat);

        GameObject player = SpawnPlayer(new Vector3(-6f, 5.1f, -1f), true, 1, 8f);
        SpawnGameplayCamera(player.transform);

        SpawnPointLight("Light_Pit", new Vector3(0f, -4f, 10f), new Color(0.8f, 0.15f, 0.15f, 1f), 4f, 12f, env);
        SpawnPointLight("Light_Bridge", new Vector3(0f, 6f, 12f), new Color(0.1f, 0.6f, 1f, 1f), 3f, 8f, env);

        SpawnGameplayHud(ui);
        SpawnPauseMenu(ui);
        SpawnLevelRuntime(mech, "", "No puedes cruzar volando, pero puedes caer con estilo.", "");
        SpawnAmbientLights(env, new Vector3(0f, 4f, 6f), 16f, 24f);
        SpawnSmokeVolume("L03_PitFog", new Vector3(0f, -3f, 10f), new Vector3(8f, 8f, 12f), env, 30f);

        CreateTutorialTrigger("Tut_Jump", new Vector3(-6f, 5f, 4f), new Vector3(6f, 3f, 6f),
            "El boton esta muy abajo.", "Si saltas moriras... pero el eco sobrevivira lo suficiente.", 4f, tutorial);

        SaveScene(scene, "Level_03");
    }'''

# LEVEL 04: THE PARADOX (PRECISION JUMPING & TIMING)
l4 = '''    static void BuildLevel04()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Level_04";
        Transform env = CreateRoot("--- ENVIRONMENT ---");
        Transform mech = CreateRoot("--- MECHANICS ---");
        Transform ui = CreateRoot("--- UI ---");
        Transform tutorial = CreateRoot("--- TUTORIAL ---");

        SetupAtmosphere(new Color(0.04f, 0.04f, 0.07f, 1f), 0.05f, new Color(0.04f, 0.04f, 0.07f, 1f));
        SpawnDirectionalLight();
        MakeBackdrop("Backdrop", new Vector3(0f, 0f, 16f), 36f, 50f, 50f, env);

        // Center hub
        MakePlatform("Floor_Center", new Vector3(0f, 0f, 0f), new Vector3(6f, 0.5f, 6f), env, _floorMat);
        
        // Left Island (Requires 3.5m jump)
        MakePlatform("Floor_Left", new Vector3(-8f, 0f, 8f), new Vector3(4f, 0.5f, 4f), env, _floorMat);
        // Right Island (Requires 3.5m jump)
        MakePlatform("Floor_Right", new Vector3(8f, 0f, 8f), new Vector3(4f, 0.5f, 4f), env, _floorMat);
        // Exit Island (Requires 3.5m jump from center)
        MakePlatform("Floor_Exit", new Vector3(0f, 0f, 16f), new Vector3(6f, 0.5f, 8f), env, _floorMat);

        // Connecting jump pillars to make it slightly easier but risky
        MakePlatform("Pillar_L", new Vector3(-4f, -0.5f, 4f), new Vector3(2f, 1.5f, 2f), env, _bridgeMat);
        MakePlatform("Pillar_R", new Vector3(4f, -0.5f, 4f), new Vector3(2f, 1.5f, 2f), env, _bridgeMat);
        MakePlatform("Pillar_F", new Vector3(0f, -0.5f, 8f), new Vector3(2f, 1.5f, 2f), env, _bridgeMat);

        PressurePlate btnL = CreatePlate("Button_L", new Vector3(-8f, 0.36f, 8f), mech);
        PressurePlate btnR = CreatePlate("Button_R", new Vector3(8f, 0.36f, 8f), mech);

        DoorController door = CreateDoor("Door_Exit", new Vector3(0f, 1.75f, 13f), new Vector3(6f, 3.5f, 1f), mech, new[] { btnL, btnR });
        door.latchOpen = false;

        LevelExit exit = CreateLevelExit(new Vector3(0f, 1.25f, 17f), mech, "Level_05");
        CreateLevelGoal(mech, "Ambos lados al mismo tiempo.", "Sincronizado.", "Coordina el salto.", exit, btnL, btnR);

        GameObject player = SpawnPlayer(new Vector3(0f, 1.1f, -1f), true, 1, 12f);
        SpawnGameplayCamera(player.transform);

        SpawnPointLight("Light_BtnL", new Vector3(-8f, 2f, 8f), new Color(0.1f, 0.5f, 1f), 4f, 8f, env);
        SpawnPointLight("Light_BtnR", new Vector3(8f, 2f, 8f), new Color(0.1f, 0.5f, 1f), 4f, 8f, env);
        SpawnPointLight("Light_Door", new Vector3(0f, 3f, 13f), new Color(0.6f, 0.1f, 0.1f), 3f, 6f, env);
        SpawnPointLight("Light_Center", new Vector3(0f, 4f, 0f), new Color(0.8f, 0.8f, 0.8f), 2f, 10f, env);

        SpawnGameplayHud(ui);
        SpawnPauseMenu(ui);
        SpawnLevelRuntime(mech, "", "Calcula bien tus saltos y los del eco.", "");
        SpawnAmbientLights(env, new Vector3(0f, 0f, 8f), 20f, 24f);
        SpawnSmokeVolume("L04_Fog", new Vector3(0f, -2f, 8f), new Vector3(24f, 4f, 24f), env, 45f);

        SaveScene(scene, "Level_04");
    }'''

content = re.sub(r'    static void BuildLevel02\(\)\s*\{[\s\S]*?SaveScene\(scene, "Level_02"\);\s*\}', l2, content)
content = re.sub(r'    static void BuildLevel03\(\)\s*\{[\s\S]*?SaveScene\(scene, "Level_03"\);\s*\}', l3, content)
content = re.sub(r'    static void BuildLevel04\(\)\s*\{[\s\S]*?SaveScene\(scene, "Level_04"\);\s*\}', l4, content)

with open(path, "w", encoding="utf-8") as f:
    f.write(content)

print("Levels 2, 3, and 4 updated with jumping and true aha mechanics.")
