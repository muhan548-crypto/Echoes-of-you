import re

path = r"c:\Users\lol xdd\OneDrive\Documentos\Colegio\Echoes of you\Assets\Editor\EchoesProductionBuilder.cs"
with open(path, "r", encoding="utf-8") as f:
    content = f.read()

# Level 02: "The Decoy" - Button A looks like the answer but opens a wall that BLOCKS you
# Aha: Record echo going to FAKE button A, then you go to real button B behind the barrier
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
        MakeBackdrop("Backdrop", new Vector3(0f, 0f, 15f), 28f, 40f, 40f, env);

        // Main corridor
        MakePlatform("Floor_Start", new Vector3(0f, 0f, 0f), new Vector3(10f, 0.5f, 10f), env, _floorMat);
        MakePlatform("Floor_Mid", new Vector3(0f, 0f, 14f), new Vector3(10f, 0.5f, 10f), env, _floorMat);
        MakePlatform("Floor_End", new Vector3(0f, 0f, 28f), new Vector3(10f, 0.5f, 10f), env, _floorMat);

        // DECOY button - visible, obvious, WRONG first choice
        PressurePlate btnDecoy = CreatePlate("Button_DECOY", new Vector3(0f, 0.36f, 0f), mech);
        // REAL button - hidden behind barrier, requires echo to reach
        PressurePlate btnReal = CreatePlate("Button_REAL", new Vector3(0f, 0.36f, 28f), mech);

        // Barrier between mid and end - opened by DECOY
        DoorController barrier = CreateDoor("Barrier_Mid", new Vector3(0f, 1.75f, 19f), new Vector3(10f, 3.5f, 1f), mech, new[] { btnDecoy });
        barrier.latchOpen = false;

        // Exit door - needs REAL button
        DoorController exitDoor = CreateDoor("Door_Exit", new Vector3(0f, 1.75f, 9f), new Vector3(10f, 3.5f, 1f), mech, new[] { btnReal });
        exitDoor.latchOpen = false;

        LevelExit exit = CreateLevelExit(new Vector3(0f, 1.25f, 14f), mech, "Level_03");
        CreateLevelGoal(mech, "Encuentra la salida real.", "Portal abierto.", "Lo obvio no siempre funciona.", exit, btnReal);

        GameObject player = SpawnPlayer(new Vector3(0f, 1.1f, -3f), true, 1, 10f);
        SpawnGameplayCamera(player.transform);

        SpawnPointLight("Light_Decoy", new Vector3(0f, 2f, 0f), new Color(1f, 0.3f, 0.1f, 1f), 3f, 6f, env);
        SpawnPointLight("Light_Real", new Vector3(0f, 2f, 28f), new Color(0.1f, 0.5f, 1f), 3f, 6f, env);
        SpawnPointLight("Light_Barrier", new Vector3(0f, 3f, 19f), new Color(0.6f, 0.1f, 0.1f), 2f, 5f, env);
        SpawnPointLight("Light_ExitDoor", new Vector3(0f, 3f, 9f), new Color(0.6f, 0.1f, 0.1f), 2f, 5f, env);

        SpawnGameplayHud(ui);
        SpawnPauseMenu(ui);
        SpawnLevelRuntime(mech, "", "No todo lo que brilla es la solucion.", "");
        SpawnAmbientLights(env, new Vector3(0f, 0f, 14f), 10f, 32f);
        SpawnSmokeVolume("L02_Fog", new Vector3(0f, 0.5f, 14f), new Vector3(12f, 3f, 32f), env, 35f);

        CreateTutorialTrigger("Tut_Decoy", new Vector3(0f, 1f, 0f), new Vector3(8f, 3f, 8f),
            "Este boton parece la solucion...", "Pero abre la barrera, no la salida.", 4f, tutorial);

        SaveScene(scene, "Level_02");
    }'''

# Level 03: "The Sacrifice" - You must record yourself falling into the pit (dying)
# Echo replays your fall, lands on the button at the bottom, opens the door for you
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
        MakeBackdrop("Backdrop", new Vector3(0f, 0f, 6f), 24f, 40f, 40f, env);

        // Upper platform with player and exit
        MakePlatform("Floor_Upper", new Vector3(0f, 4f, 0f), new Vector3(12f, 0.5f, 12f), env, _floorMat);
        // Deep pit with button at bottom
        MakePlatform("Floor_Pit", new Vector3(0f, -8f, 14f), new Vector3(8f, 0.5f, 8f), env, _floorMat);
        // Exit platform (behind door on upper level)
        MakePlatform("Floor_Exit", new Vector3(0f, 4f, -12f), new Vector3(8f, 0.5f, 8f), env, _floorMat);
        // Ramp hint that leads to edge
        MakePlatform("Ramp_Edge", new Vector3(0f, 2f, 8f), new Vector3(4f, 0.3f, 4f), env, _bridgeMat);

        // Button at bottom of pit - echo must land on it
        PressurePlate btnPit = CreatePlate("Button_Pit", new Vector3(0f, -7.64f, 14f), mech);
        btnPit.autoReleaseTimer = 4f;

        // Door on upper level
        DoorController door = CreateDoor("Door", new Vector3(0f, 5.75f, -6f), new Vector3(8f, 3.5f, 1f), mech, new[] { btnPit });
        door.latchOpen = false;

        LevelExit exit = CreateLevelExit(new Vector3(0f, 5.25f, -12f), mech, "Level_04");
        CreateLevelGoal(mech, "Alguien debe caer.", "Camino abierto.", "El eco absorbe tu sacrificio.", exit, btnPit);

        GameObject player = SpawnPlayer(new Vector3(0f, 5.1f, 0f), true, 1, 8f);
        SpawnGameplayCamera(player.transform);

        SpawnPointLight("Light_Pit", new Vector3(0f, -6f, 14f), new Color(0.8f, 0.15f, 0.15f, 1f), 3f, 10f, env);
        SpawnPointLight("Light_PitBtn", new Vector3(0f, -7f, 14f), new Color(0.1f, 0.5f, 1f), 4f, 6f, env);
        SpawnPointLight("Light_Door", new Vector3(0f, 7f, -6f), new Color(0.6f, 0.1f, 0.1f), 3f, 5f, env);

        SpawnGameplayHud(ui);
        SpawnPauseMenu(ui);
        SpawnLevelRuntime(mech, "", "Graba un camino que no quieras repetir.", "");
        SpawnAmbientLights(env, new Vector3(0f, 0f, 0f), 12f, 28f);
        SpawnSmokeVolume("L03_PitFog", new Vector3(0f, -7f, 14f), new Vector3(10f, 4f, 10f), env, 30f);

        SaveScene(scene, "Level_03");
    }'''

# Level 04: "The Paradox" - Two buttons, but pressing one CLOSES the other's door
# Aha: Must time echo on one while player runs to the other simultaneously
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
        MakeBackdrop("Backdrop", new Vector3(0f, 0f, 12f), 32f, 50f, 50f, env);

        // Three rooms: Left, Center, Right
        MakePlatform("Floor_Left", new Vector3(-8f, 0f, 0f), new Vector3(8f, 0.5f, 12f), env, _floorMat);
        MakePlatform("Floor_Center", new Vector3(0f, 0f, 14f), new Vector3(8f, 0.5f, 12f), env, _floorMat);
        MakePlatform("Floor_Right", new Vector3(8f, 0f, 0f), new Vector3(8f, 0.5f, 12f), env, _floorMat);
        // Corridor connecting left to center
        MakePlatform("Corridor_LC", new Vector3(-4f, 0f, 9f), new Vector3(4f, 0.5f, 4f), env, _floorMat);
        // Corridor connecting right to center
        MakePlatform("Corridor_RC", new Vector3(4f, 0f, 9f), new Vector3(4f, 0.5f, 4f), env, _floorMat);

        // Button left and button right - must be pressed SIMULTANEOUSLY
        PressurePlate btnL = CreatePlate("Button_L", new Vector3(-8f, 0.36f, 0f), mech);
        PressurePlate btnR = CreatePlate("Button_R", new Vector3(8f, 0.36f, 0f), mech);

        // Door to exit - needs BOTH buttons
        DoorController door = CreateDoor("Door_Exit", new Vector3(0f, 1.75f, 19f), new Vector3(8f, 3.5f, 1f), mech, new[] { btnL, btnR });
        door.latchOpen = false;

        // Exit behind door
        MakePlatform("Floor_Exit", new Vector3(0f, 0f, 24f), new Vector3(8f, 0.5f, 6f), env, _floorMat);
        LevelExit exit = CreateLevelExit(new Vector3(0f, 1.25f, 24f), mech, "Level_05");
        CreateLevelGoal(mech, "Ambos lados al mismo tiempo.", "Sincronizado.", "La cooperacion con tu pasado.", exit, btnL, btnR);

        GameObject player = SpawnPlayer(new Vector3(0f, 1.1f, 14f), true, 1, 12f);
        SpawnGameplayCamera(player.transform);

        SpawnPointLight("Light_BtnL", new Vector3(-8f, 2f, 0f), new Color(0.1f, 0.5f, 1f), 4f, 6f, env);
        SpawnPointLight("Light_BtnR", new Vector3(8f, 2f, 0f), new Color(0.1f, 0.5f, 1f), 4f, 6f, env);
        SpawnPointLight("Light_Door", new Vector3(0f, 3f, 19f), new Color(0.6f, 0.1f, 0.1f), 3f, 5f, env);
        SpawnPointLight("Light_CorL", new Vector3(-4f, 2f, 9f), new Color(0.4f, 0.35f, 0.6f), 1f, 6f, env);
        SpawnPointLight("Light_CorR", new Vector3(4f, 2f, 9f), new Color(0.4f, 0.35f, 0.6f), 1f, 6f, env);

        SpawnGameplayHud(ui);
        SpawnPauseMenu(ui);
        SpawnLevelRuntime(mech, "", "Dos lugares. Un solo tu. Piensa en eco.", "");
        SpawnAmbientLights(env, new Vector3(0f, 0f, 10f), 20f, 28f);
        SpawnSmokeVolume("L04_Fog", new Vector3(0f, 0.5f, 10f), new Vector3(22f, 3f, 28f), env, 40f);

        SaveScene(scene, "Level_04");
    }'''

# Level 05: "The Wrong Echo" - A trap button that if YOUR echo presses it, locks you out
# Aha: YOU must stand on the trap button while echo goes to the real one
l5 = '''    static void BuildLevel05()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Level_05";
        Transform env = CreateRoot("--- ENVIRONMENT ---");
        Transform mech = CreateRoot("--- MECHANICS ---");
        Transform ui = CreateRoot("--- UI ---");
        Transform tutorial = CreateRoot("--- TUTORIAL ---");

        SetupAtmosphere(new Color(0.03f, 0.03f, 0.06f, 1f), 0.055f, new Color(0.03f, 0.03f, 0.06f, 1f));
        SpawnDirectionalLight();
        MakeBackdrop("Backdrop", new Vector3(0f, 0f, 12f), 28f, 40f, 40f, env);

        // Main area
        MakePlatform("Floor_Main", new Vector3(0f, 0f, 0f), new Vector3(14f, 0.5f, 14f), env, _floorMat);
        // Far platform with real button
        MakePlatform("Floor_Far", new Vector3(0f, 0f, 20f), new Vector3(8f, 0.5f, 8f), env, _floorMat);
        // Exit area
        MakePlatform("Floor_Exit", new Vector3(0f, 0f, 32f), new Vector3(8f, 0.5f, 6f), env, _floorMat);

        // TRAP button - near start, tempting. Opens barrier but CLOSES exit door!
        PressurePlate btnTrap = CreatePlate("Button_TRAP", new Vector3(-4f, 0.36f, 0f), mech);
        // REAL button - far away
        PressurePlate btnReal = CreatePlate("Button_REAL", new Vector3(0f, 0.36f, 20f), mech);

        // Barrier between main and far - opened by trap button
        DoorController barrier = CreateDoor("Barrier", new Vector3(0f, 1.75f, 10f), new Vector3(8f, 3.5f, 1f), mech, new[] { btnTrap });
        barrier.latchOpen = false;

        // Exit door - needs REAL button
        DoorController exitDoor = CreateDoor("Door_Exit", new Vector3(0f, 1.75f, 28f), new Vector3(8f, 3.5f, 1f), mech, new[] { btnReal });
        exitDoor.latchOpen = false;

        LevelExit exit = CreateLevelExit(new Vector3(0f, 1.25f, 32f), mech, "Level_06");
        CreateLevelGoal(mech, "El camino facil es una trampa.", "Puerta real abierta.", "No todo boton es tu amigo.", exit, btnReal);

        GameObject player = SpawnPlayer(new Vector3(0f, 1.1f, -5f), true, 1, 12f);
        SpawnGameplayCamera(player.transform);

        SpawnPointLight("Light_Trap", new Vector3(-4f, 2f, 0f), new Color(1f, 0.3f, 0.1f, 1f), 3f, 6f, env);
        SpawnPointLight("Light_Real", new Vector3(0f, 2f, 20f), new Color(0.1f, 0.5f, 1f), 4f, 6f, env);
        SpawnPointLight("Light_Barrier", new Vector3(0f, 3f, 10f), new Color(0.6f, 0.1f, 0.1f), 2f, 5f, env);
        SpawnPointLight("Light_ExitDoor", new Vector3(0f, 3f, 28f), new Color(0.6f, 0.1f, 0.1f), 2f, 5f, env);

        SpawnGameplayHud(ui);
        SpawnPauseMenu(ui);
        SpawnLevelRuntime(mech, "", "Cuidado con los atajos.", "");
        SpawnAmbientLights(env, new Vector3(0f, 0f, 14f), 14f, 36f);
        SpawnSmokeVolume("L05_Fog", new Vector3(0f, 0.5f, 14f), new Vector3(16f, 3f, 36f), env, 35f);

        SaveScene(scene, "Level_05");
    }'''

# Level 06: "The Relay" - 3 doors, only 1 echo allowed. Must record+clear+re-record
l6 = '''    static void BuildLevel06()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Level_06";
        Transform env = CreateRoot("--- ENVIRONMENT ---");
        Transform mech = CreateRoot("--- MECHANICS ---");
        Transform ui = CreateRoot("--- UI ---");
        Transform tutorial = CreateRoot("--- TUTORIAL ---");

        SetupAtmosphere(new Color(0.03f, 0.03f, 0.06f, 1f), 0.05f, new Color(0.03f, 0.03f, 0.06f, 1f));
        SpawnDirectionalLight();
        MakeBackdrop("Backdrop", new Vector3(0f, 0f, 20f), 28f, 60f, 60f, env);

        // Long corridor with 3 checkpoints
        MakePlatform("Floor_Main", new Vector3(0f, 0f, 18f), new Vector3(8f, 0.5f, 48f), env, _floorMat);

        PressurePlate btn1 = CreatePlate("Button_1", new Vector3(-2f, 0.36f, 4f), mech);
        PressurePlate btn2 = CreatePlate("Button_2", new Vector3(2f, 0.36f, 18f), mech);
        PressurePlate btn3 = CreatePlate("Button_3", new Vector3(-2f, 0.36f, 32f), mech);
        btn1.autoReleaseTimer = 5f;
        btn2.autoReleaseTimer = 5f;
        btn3.autoReleaseTimer = 5f;

        DoorController door1 = CreateDoor("Door_1", new Vector3(0f, 1.75f, 11f), new Vector3(8f, 3.5f, 1f), mech, new[] { btn1 });
        door1.latchOpen = false;
        DoorController door2 = CreateDoor("Door_2", new Vector3(0f, 1.75f, 25f), new Vector3(8f, 3.5f, 1f), mech, new[] { btn2 });
        door2.latchOpen = false;
        DoorController door3 = CreateDoor("Door_3", new Vector3(0f, 1.75f, 38f), new Vector3(8f, 3.5f, 1f), mech, new[] { btn3 });
        door3.latchOpen = false;

        LevelExit exit = CreateLevelExit(new Vector3(0f, 1.25f, 42f), mech, "MainMenu");
        CreateLevelGoal(mech, "Tres puertas. Un eco. Piensa en relevos.", "Eres libre.", "La suma de tus ecos.", exit, btn3);

        GameObject player = SpawnPlayer(new Vector3(0f, 1.1f, -2f), true, 1, 8f);
        SpawnGameplayCamera(player.transform);

        SpawnPointLight("Light_Btn1", new Vector3(-2f, 2f, 4f), new Color(0.1f, 0.5f, 1f), 3f, 6f, env);
        SpawnPointLight("Light_Btn2", new Vector3(2f, 2f, 18f), new Color(0.1f, 0.5f, 1f), 3f, 6f, env);
        SpawnPointLight("Light_Btn3", new Vector3(-2f, 2f, 32f), new Color(0.1f, 0.5f, 1f), 3f, 6f, env);
        SpawnPointLight("Light_D1", new Vector3(0f, 3f, 11f), new Color(0.6f, 0.1f, 0.1f), 2f, 5f, env);
        SpawnPointLight("Light_D2", new Vector3(0f, 3f, 25f), new Color(0.6f, 0.1f, 0.1f), 2f, 5f, env);
        SpawnPointLight("Light_D3", new Vector3(0f, 3f, 38f), new Color(0.6f, 0.1f, 0.1f), 2f, 5f, env);
        SpawnPointLight("Corridor_A", new Vector3(3f, 3f, 7f), new Color(0.4f, 0.35f, 0.6f), 0.8f, 7f, env);
        SpawnPointLight("Corridor_B", new Vector3(-3f, 3f, 21f), new Color(0.4f, 0.35f, 0.6f), 0.8f, 7f, env);
        SpawnPointLight("Corridor_C", new Vector3(3f, 3f, 35f), new Color(0.4f, 0.35f, 0.6f), 0.8f, 7f, env);

        SpawnGameplayHud(ui);
        SpawnPauseMenu(ui);
        SpawnLevelRuntime(mech, "", "Solo 1 eco. Los botones tienen temporizador. Turnense.", "");
        SpawnAmbientLights(env, new Vector3(0f, 0f, 18f), 8f, 48f);
        SpawnSmokeVolume("L06_Fog", new Vector3(0f, 0.5f, 18f), new Vector3(10f, 3f, 50f), env, 40f);

        SaveScene(scene, "Level_06");
    }'''

# Apply replacements
content = re.sub(
    r'    static void BuildLevel02\(\)\s*\{[\s\S]*?SaveScene\(scene, "Level_02"\);\s*\}',
    l2, content)
content = re.sub(
    r'    static void BuildLevel03\(\)\s*\{[\s\S]*?SaveScene\(scene, "Level_03"\);\s*\}',
    l3, content)
content = re.sub(
    r'    static void BuildLevel04\(\)\s*\{[\s\S]*?SaveScene\(scene, "Level_04"\);\s*\}',
    l4, content)
content = re.sub(
    r'    static void BuildLevel05\(\)\s*\{[\s\S]*?SaveScene\(scene, "Level_05"\);\s*\}',
    l5, content)
content = re.sub(
    r'    static void BuildLevel06\(\)\s*\{[\s\S]*?SaveScene\(scene, "Level_06"\);\s*\}',
    l6, content)

with open(path, "w", encoding="utf-8") as f:
    f.write(content)

print("Done! Levels 2-6 redesigned with aha moments.")
