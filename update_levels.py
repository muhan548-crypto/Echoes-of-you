import re

path = r"c:\Users\lol xdd\OneDrive\Documentos\Colegio\Echoes of you\Assets\Editor\EchoesProductionBuilder.cs"

with open(path, "r", encoding="utf-8") as f:
    content = f.read()

# Replace BuildLevel01
l1_new = """    static void BuildLevel01()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Level_01";
        Transform env = CreateRoot("--- ENVIRONMENT ---");
        Transform mech = CreateRoot("--- MECHANICS ---");
        Transform ui = CreateRoot("--- UI ---");
        Transform tutorial = CreateRoot("--- TUTORIAL ---");

        SetupAtmosphere(new Color(0.05f, 0.05f, 0.08f, 1f), 0.045f, new Color(0.05f, 0.05f, 0.08f, 1f));
        SpawnDirectionalLight();
        MakeBackdrop("Backdrop", new Vector3(0f, 0f, 10f), 24f, 36f, 30f, env);

        MakePlatform("Floor_Main", new Vector3(0f, 0f, 14f), new Vector3(8f, 0.5f, 12f), env, _floorMat);
        MakePlatform("Floor_Pit", new Vector3(0f, -2f, 4f), new Vector3(8f, 0.5f, 8f), env, _floorMat);

        PressurePlate btn1 = CreatePlate("Button_1", new Vector3(0f, -1.64f, 4f), mech);
        
        DoorController door = CreateDoor("Door", new Vector3(0f, 1.75f, 18f), new Vector3(4f, 3.5f, 1f), mech, new[] { btn1 });
        door.latchOpen = false;
        
        LevelExit exit = CreateLevelExit(new Vector3(0f, 1.25f, 22f), mech, "Level_02");
        CreateLevelGoal(mech, "Activa la memoria.", "Salida revelada.", "Primero recuerdas.", exit, btn1);

        GameObject player = SpawnPlayer(new Vector3(0f, 1.1f, 14f), true, 1, 5f);
        SpawnGameplayCamera(player.transform);

        SpawnEchoPathHint(mech, new Vector3[] {
            new Vector3(0f, 0.5f, 14f),
            new Vector3(0f, -1.5f, 4f)
        });
        SpawnPuzzleIntent(mech, 1, 1, true, true, false, 8f, "Reverse Role: Player at door, echo runs to pit");

        SpawnPointLight("Light_Button", new Vector3(0f, 0f, 4f), new Color(0.1f, 0.5f, 1f), 4f, 6f, env);
        SpawnPointLight("Light_Door", new Vector3(0f, 3f, 18f), new Color(0.6f, 0.1f, 0.1f), 3f, 5f, env);
        SpawnPointLight("Light_Exit", new Vector3(0f, 3f, 22f), new Color(1f, 1f, 1f), 4f, 8f, env);

        SpawnGameplayHud(ui);
        SpawnPauseMenu(ui);
        SpawnLevelRuntime(mech, "", "Usa el boton izquierdo del mouse para grabar.", "");

        SaveScene(scene, "Level_01");
    }"""
content = re.sub(r'    static void BuildLevel01\(\)[\s\S]*?SaveScene\(scene, "Level_01"\);\s*?}', l1_new, content)


# Replace BuildLevel02
l2_new = """    static void BuildLevel02()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Level_02";
        Transform env = CreateRoot("--- ENVIRONMENT ---");
        Transform mech = CreateRoot("--- MECHANICS ---");
        Transform ui = CreateRoot("--- UI ---");
        Transform tutorial = CreateRoot("--- TUTORIAL ---");

        SetupAtmosphere(new Color(0.05f, 0.05f, 0.08f, 1f), 0.045f, new Color(0.05f, 0.05f, 0.08f, 1f));
        SpawnDirectionalLight();
        MakeBackdrop("Backdrop", new Vector3(0f, 0f, 15f), 24f, 36f, 40f, env);

        MakePlatform("Floor_Main", new Vector3(0f, 0f, 10f), new Vector3(12f, 0.5f, 28f), env, _floorMat);

        PressurePlate btnA = CreatePlate("Button_A", new Vector3(-3f, 0.36f, 4f), mech);
        PressurePlate btnB = CreatePlate("Button_B", new Vector3(3f, 0.36f, 16f), mech);
        
        DoorController barrier = CreateDoor("Barrier", new Vector3(3f, 1.75f, 10f), new Vector3(6f, 3.5f, 1f), mech, new[] { btnA });
        barrier.latchOpen = false;

        DoorController door = CreateDoor("Door", new Vector3(0f, 1.75f, 22f), new Vector3(10f, 3.5f, 1f), mech, new[] { btnB });
        door.latchOpen = false;
        
        LevelExit exit = CreateLevelExit(new Vector3(0f, 1.25f, 26f), mech, "Level_03");
        CreateLevelGoal(mech, "Abre ambas puertas.", "Salida despejada.", "Luego pruebas.", exit, btnB);

        GameObject player = SpawnPlayer(new Vector3(0f, 1.1f, 0f), true, 1, 10f);
        SpawnGameplayCamera(player.transform);

        SpawnEchoPathHint(mech, new Vector3[] {
            new Vector3(-3f, 0.5f, 4f),
            new Vector3(3f, 0.5f, 16f)
        });
        SpawnPuzzleIntent(mech, 2, 2, true, true, false, 8f, "Route recording: Echo goes A to B, player crosses barrier to exit");

        SpawnPointLight("Light_ButtonA", new Vector3(-3f, 2f, 4f), new Color(0.1f, 0.5f, 1f), 4f, 6f, env);
        SpawnPointLight("Light_ButtonB", new Vector3(3f, 2f, 16f), new Color(0.1f, 0.5f, 1f), 4f, 6f, env);
        SpawnPointLight("Light_Door", new Vector3(0f, 3f, 22f), new Color(0.6f, 0.1f, 0.1f), 3f, 5f, env);
        SpawnPointLight("Light_Exit", new Vector3(0f, 3f, 26f), new Color(1f, 1f, 1f), 4f, 8f, env);

        SpawnGameplayHud(ui);
        SpawnPauseMenu(ui);
        SpawnLevelRuntime(mech, "", "El eco recrea tu recorrido completo.", "");

        SaveScene(scene, "Level_02");
    }"""
content = re.sub(r'    static void BuildLevel02\(\)[\s\S]*?SaveScene\(scene, "Level_02"\);\s*?}', l2_new, content)


# Replace BuildLevel03
l3_new = """    static void BuildLevel03()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Level_03";
        Transform env = CreateRoot("--- ENVIRONMENT ---");
        Transform mech = CreateRoot("--- MECHANICS ---");
        Transform ui = CreateRoot("--- UI ---");
        Transform tutorial = CreateRoot("--- TUTORIAL ---");

        SetupAtmosphere(new Color(0.05f, 0.05f, 0.08f, 1f), 0.045f, new Color(0.05f, 0.05f, 0.08f, 1f));
        SpawnDirectionalLight();
        MakeBackdrop("Backdrop", new Vector3(0f, 0f, 15f), 24f, 50f, 50f, env);

        MakePlatform("Floor_Main", new Vector3(0f, 0f, 4f), new Vector3(14f, 0.5f, 16f), env, _floorMat);
        MakePlatform("Floor_Airlock", new Vector3(0f, 0f, 20f), new Vector3(8f, 0.5f, 16f), env, _floorMat);
        MakePlatform("Floor_Exit", new Vector3(0f, 0f, 32f), new Vector3(8f, 0.5f, 8f), env, _floorMat);

        PressurePlate btnA = CreatePlate("Button_A", new Vector3(0f, 0.36f, 4f), mech);
        PressurePlate btnB = CreatePlate("Button_B", new Vector3(0f, 0.36f, 20f), mech);

        DoorController doorA = CreateDoor("Door_A", new Vector3(0f, 1.75f, 12f), new Vector3(8f, 3.5f, 1f), mech, new[] { btnA });
        doorA.latchOpen = false;
        
        DoorController doorB = CreateDoor("Door_B", new Vector3(0f, 1.75f, 28f), new Vector3(8f, 3.5f, 1f), mech, new[] { btnB });
        doorB.latchOpen = false;
        
        LevelExit exit = CreateLevelExit(new Vector3(0f, 1.25f, 32f), mech, "Level_04");
        CreateLevelGoal(mech, "Atraviesa la esclusa.", "Secuencia completada.", "Sincronizacion asincrona.", exit, btnB);

        GameObject player = SpawnPlayer(new Vector3(0f, 1.1f, -2f), true, 1, 10f);
        SpawnGameplayCamera(player.transform);

        SpawnEchoPathHint(mech, new Vector3[] {
            new Vector3(0f, 0.5f, 4f)
        });
        SpawnPuzzleIntent(mech, 2, 2, true, true, true, 15f, "Echo presses A then releases it. Player gets locked in airlock and presses B");

        SpawnPointLight("Light_ButtonA", new Vector3(0f, 2f, 4f), new Color(0.1f, 0.5f, 1f), 4f, 6f, env);
        SpawnPointLight("Light_ButtonB", new Vector3(0f, 2f, 20f), new Color(0.1f, 0.5f, 1f), 4f, 6f, env);
        SpawnPointLight("Light_DoorA", new Vector3(0f, 3f, 12f), new Color(0.6f, 0.1f, 0.1f), 3f, 5f, env);
        SpawnPointLight("Light_DoorB", new Vector3(0f, 3f, 28f), new Color(0.6f, 0.1f, 0.1f), 3f, 5f, env);
        SpawnPointLight("Light_Exit", new Vector3(0f, 3f, 32f), new Color(1f, 1f, 1f), 4f, 8f, env);

        SpawnGameplayHud(ui);
        SpawnPauseMenu(ui);
        SpawnLevelRuntime(mech, "", "A veces debes soltar para poder avanzar.", "");

        SaveScene(scene, "Level_03");
    }"""
content = re.sub(r'    static void BuildLevel03\(\)[\s\S]*?SaveScene\(scene, "Level_03"\);\s*?}', l3_new, content)

# Replace BuildLevel04
l4_new = """    static void BuildLevel04()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Level_04";
        Transform env = CreateRoot("--- ENVIRONMENT ---");
        Transform mech = CreateRoot("--- MECHANICS ---");
        Transform ui = CreateRoot("--- UI ---");
        Transform tutorial = CreateRoot("--- TUTORIAL ---");

        SetupAtmosphere(new Color(0.05f, 0.05f, 0.08f, 1f), 0.045f, new Color(0.05f, 0.05f, 0.08f, 1f));
        SpawnDirectionalLight();
        MakeBackdrop("Backdrop", new Vector3(0f, 0f, 10f), 32f, 40f, 40f, env);

        MakePlatform("Floor_Main", new Vector3(0f, 0f, 4f), new Vector3(14f, 0.5f, 16f), env, _floorMat);
        MakePlatform("Floor_Labyrinth", new Vector3(8f, 0f, 18f), new Vector3(8f, 0.5f, 14f), env, _floorMat);
        MakePlatform("Floor_Exit", new Vector3(-6f, 0f, 18f), new Vector3(8f, 0.5f, 14f), env, _floorMat);

        MakePlatform("Labyrinth_Wall", new Vector3(8f, 1.5f, 14f), new Vector3(8f, 3f, 1f), env, _bridgeMat);

        PressurePlate btnA = CreatePlate("Button_A", new Vector3(-4f, 0.36f, 4f), mech);
        PressurePlate btnB = CreatePlate("Button_B", new Vector3(8f, 0.36f, 22f), mech);
        
        DoorController doorExit = CreateDoor("Door_Exit", new Vector3(-4f, 1.75f, 12f), new Vector3(4f, 3.5f, 1f), mech, new[] { btnA, btnB });
        doorExit.latchOpen = false;
        
        LevelExit exit = CreateLevelExit(new Vector3(-4f, 1.25f, 20f), mech, "Level_05");
        CreateLevelGoal(mech, "Sincroniza ambas presiones.", "Salida final abierta.", "Intercambio maestro.", exit, btnA, btnB);

        GameObject player = SpawnPlayer(new Vector3(0f, 1.1f, -2f), true, 1, 15f);
        SpawnGameplayCamera(player.transform);

        SpawnEchoPathHint(mech, new Vector3[] {
            new Vector3(8f, 0.5f, 22f)
        });
        SpawnPuzzleIntent(mech, 2, 3, true, true, true, 12f, "Delegation: Echo goes to labyrinth B, player holds A at exit");

        SpawnPointLight("Light_BtnA", new Vector3(-4f, 2f, 4f), new Color(0.1f, 0.5f, 1f), 4f, 6f, env);
        SpawnPointLight("Light_BtnB", new Vector3(8f, 2f, 22f), new Color(0.1f, 0.5f, 1f), 4f, 6f, env);
        SpawnPointLight("Light_DoorE", new Vector3(-4f, 3f, 12f), new Color(0.6f, 0.1f, 0.1f), 3f, 5f, env);
        SpawnPointLight("Light_Exit", new Vector3(-4f, 3f, 20f), new Color(1f, 1f, 1f), 4f, 8f, env);

        SpawnGameplayHud(ui);
        SpawnPauseMenu(ui);
        SpawnLevelRuntime(mech, "", "Delega las tareas largas al eco.", "");

        SaveScene(scene, "Level_04");
    }"""
content = re.sub(r'    static void BuildLevel04\(\)[\s\S]*?SaveScene\(scene, "Level_04"\);\s*?}', l4_new, content)

# Replace BuildLevel05
l5_new = """    static void BuildLevel05()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Level_05";
        Transform env = CreateRoot("--- ENVIRONMENT ---");
        Transform mech = CreateRoot("--- MECHANICS ---");
        Transform ui = CreateRoot("--- UI ---");
        Transform tutorial = CreateRoot("--- TUTORIAL ---");

        SetupAtmosphere(new Color(0.05f, 0.05f, 0.08f, 1f), 0.045f, new Color(0.05f, 0.05f, 0.08f, 1f));
        SpawnDirectionalLight();
        MakeBackdrop("Backdrop", new Vector3(0f, 0f, 0f), 32f, 40f, 40f, env);

        MakePlatform("Floor_Top", new Vector3(0f, 4f, 0f), new Vector3(10f, 0.5f, 10f), env, _floorMat);
        MakePlatform("Floor_Pit", new Vector3(0f, -10f, 12f), new Vector3(10f, 0.5f, 10f), env, _floorMat);
        MakePlatform("Floor_Exit", new Vector3(0f, 4f, -12f), new Vector3(10f, 0.5f, 10f), env, _floorMat);

        PressurePlate btnPit = CreatePlate("Button_Pit", new Vector3(0f, -9.64f, 12f), mech);
        btnPit.autoReleaseTimer = 2.5f;

        DoorController door = CreateDoor("Door", new Vector3(0f, 5.75f, -6f), new Vector3(4f, 3.5f, 1f), mech, new[] { btnPit });
        door.latchOpen = false;

        LevelExit exit = CreateLevelExit(new Vector3(0f, 5.25f, -12f), mech, "Level_06");
        CreateLevelGoal(mech, "El puente fantasma.", "Atravesado.", "Sacrificio de eco.", exit, btnPit);

        GameObject player = SpawnPlayer(new Vector3(0f, 5.1f, 0f), true, 1, 15f);
        SpawnGameplayCamera(player.transform);

        SpawnEchoPathHint(mech, new Vector3[] {
            new Vector3(0f, -9f, 12f)
        });
        SpawnPuzzleIntent(mech, 1, 1, true, true, true, 15f, "Suicide fall. Record, jump, soft reset, echo does it.");

        SpawnPointLight("Light_Pit", new Vector3(0f, -8f, 12f), new Color(0.1f, 0.5f, 1f), 4f, 6f, env);
        SpawnPointLight("Light_Door", new Vector3(0f, 7f, -6f), new Color(0.6f, 0.1f, 0.1f), 3f, 5f, env);
        SpawnPointLight("Light_Exit", new Vector3(0f, 7f, -12f), new Color(1f, 1f, 1f), 4f, 8f, env);

        SpawnGameplayHud(ui);
        SpawnPauseMenu(ui);
        SpawnLevelRuntime(mech, "", "A veces es mejor dejar caer a un fantasma.", "");

        SaveScene(scene, "Level_05");
    }"""
content = re.sub(r'    static void BuildLevel05\(\)[\s\S]*?SaveScene\(scene, "Level_05"\);\s*?}', l5_new, content)

# Replace BuildLevel06
l6_new = """    static void BuildLevel06()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Level_06";

        Transform env = CreateRoot("--- ENVIRONMENT ---");
        Transform mech = CreateRoot("--- MECHANICS ---");
        Transform ui = CreateRoot("--- UI ---");
        Transform tutorial = CreateRoot("--- TUTORIAL ---");

        SetupAtmosphere(new Color(0.05f, 0.05f, 0.08f, 1f), 0.045f, new Color(0.05f, 0.05f, 0.08f, 1f));
        SpawnDirectionalLight();
        MakeBackdrop("Backdrop", new Vector3(0f, 0f, 18f), 32f, 60f, 60f, env);

        MakePlatform("Floor_Main", new Vector3(0f, 0f, 16f), new Vector3(10f, 0.5f, 44f), env, _floorMat);

        PressurePlate btn1 = CreatePlate("Button_1", new Vector3(0f, 0.36f, 4f), mech);
        PressurePlate btn2 = CreatePlate("Button_2", new Vector3(0f, 0.36f, 16f), mech);
        PressurePlate btn3 = CreatePlate("Button_3", new Vector3(0f, 0.36f, 28f), mech);

        DoorController door1 = CreateDoor("Door_1", new Vector3(0f, 1.75f, 10f), new Vector3(8f, 3.5f, 1f), mech, new[] { btn1 });
        door1.latchOpen = false;
        
        DoorController door2 = CreateDoor("Door_2", new Vector3(0f, 1.75f, 22f), new Vector3(8f, 3.5f, 1f), mech, new[] { btn2 });
        door2.latchOpen = false;
        
        DoorController door3 = CreateDoor("Door_3", new Vector3(0f, 1.75f, 34f), new Vector3(8f, 3.5f, 1f), mech, new[] { btn3 });
        door3.latchOpen = false;

        LevelExit exit = CreateLevelExit(new Vector3(0f, 1.25f, 38f), mech, "MainMenu");
        CreateLevelGoal(mech, "Armoniza con tu pasado.", "Eres libre.", "La suma de tus ecos.", exit, btn3);

        GameObject player = SpawnPlayer(new Vector3(0f, 1.1f, -2f), true, 1, 20f);
        SpawnGameplayCamera(player.transform);

        SpawnEchoPathHint(mech, new Vector3[] {
            new Vector3(0f, 0.5f, 4f),
            new Vector3(0f, 0.5f, 28f)
        });
        SpawnPuzzleIntent(mech, 3, 3, true, true, true, 20f, "Relay Race: MaxEchoes=1. Player and Echo take turns opening doors.");

        SpawnPointLight("Light_Btn1", new Vector3(0f, 2f, 4f), new Color(0.1f, 0.5f, 1f), 4f, 6f, env);
        SpawnPointLight("Light_Btn2", new Vector3(0f, 2f, 16f), new Color(0.1f, 0.5f, 1f), 4f, 6f, env);
        SpawnPointLight("Light_Btn3", new Vector3(0f, 2f, 28f), new Color(0.1f, 0.5f, 1f), 4f, 6f, env);
        
        SpawnPointLight("Light_Door1", new Vector3(0f, 3f, 10f), new Color(0.6f, 0.1f, 0.1f), 3f, 5f, env);
        SpawnPointLight("Light_Door2", new Vector3(0f, 3f, 22f), new Color(0.6f, 0.1f, 0.1f), 3f, 5f, env);
        SpawnPointLight("Light_Door3", new Vector3(0f, 3f, 34f), new Color(0.6f, 0.1f, 0.1f), 3f, 5f, env);

        SpawnGameplayHud(ui);
        SpawnPauseMenu(ui);
        SpawnLevelRuntime(mech, "", "Solo tienes 1 eco. Hagan una carrera de relevos cruzando puertas.", "");

        SaveScene(scene, "Level_06");
    }"""
content = re.sub(r'    static void BuildLevel06\(\)[\s\S]*?SaveScene\(scene, "Level_06"\);\s*?}', l6_new, content)

with open(path, "w", encoding="utf-8") as f:
    f.write(content)
