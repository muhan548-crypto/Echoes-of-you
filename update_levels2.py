import re

path = r"c:\Users\lol xdd\OneDrive\Documentos\Colegio\Echoes of you\Assets\Editor\EchoesProductionBuilder.cs"

with open(path, "r", encoding="utf-8") as f:
    content = f.read()

# Add ambient lights helper before SpawnPuzzleIntent
ambient_helper = '''    static void SpawnAmbientLights(Transform parent, Vector3 center, float width, float depth)
    {
        Color warmDim = new Color(0.85f, 0.75f, 0.6f, 1f);
        Color coolDim = new Color(0.4f, 0.55f, 0.75f, 1f);
        float intensity = 0.6f;
        float range = 10f;
        float halfW = width * 0.4f;
        float halfD = depth * 0.4f;

        SpawnPointLight("Amb_FL", center + new Vector3(-halfW, 3f, -halfD), warmDim, intensity, range, parent);
        SpawnPointLight("Amb_FR", center + new Vector3(halfW, 3f, -halfD), coolDim, intensity, range, parent);
        SpawnPointLight("Amb_BL", center + new Vector3(-halfW, 3f, halfD), coolDim, intensity, range, parent);
        SpawnPointLight("Amb_BR", center + new Vector3(halfW, 3f, halfD), warmDim, intensity, range, parent);
        SpawnPointLight("Amb_Center", center + new Vector3(0f, 5f, 0f), new Color(0.5f, 0.6f, 0.8f, 1f), 0.4f, 14f, parent);
    }

'''
content = content.replace(
    '    // Declara la intención de diseño del puzzle para validación\n',
    ambient_helper + '    // Declara la intención de diseño del puzzle para validación\n'
)
# Also try \r\n variant
content = content.replace(
    '    // Declara la intenci\u00f3n de dise\u00f1o del puzzle para validaci\u00f3n\r\n',
    ambient_helper.replace('\n', '\r\n') + '    // Declara la intenci\u00f3n de dise\u00f1o del puzzle para validaci\u00f3n\r\n'
)

# Now update each level to add ambient lights and tutorial triggers

# Level 01 - add ambient lights before SaveScene
old_l1_end = '''        SpawnLevelRuntime(mech, "", "Usa el boton izquierdo del mouse para grabar.", "");

        SaveScene(scene, "Level_01");'''

new_l1_end = '''        SpawnLevelRuntime(mech, "", "Usa el boton izquierdo del mouse para grabar.", "");

        SpawnAmbientLights(env, new Vector3(0f, 0f, 10f), 8f, 24f);
        SpawnSmokeVolume("L01_Fog", new Vector3(0f, 0.5f, 10f), new Vector3(10f, 3f, 26f), env, 40f);

        CreateTutorialTrigger("Tut_Record", new Vector3(0f, 1f, 12f), new Vector3(4f, 3f, 4f),
            "Manten E o R para grabar tu movimiento.", "Suelta para crear un eco.", 4f, tutorial);
        CreateTutorialTrigger("Tut_Pit", new Vector3(0f, -0.5f, 4f), new Vector3(6f, 3f, 6f),
            "El eco repetira tu camino. Usalo para pisar el boton.", "", 4f, tutorial);

        SaveScene(scene, "Level_01");'''

content = content.replace(old_l1_end, new_l1_end)

# Level 02
old_l2_end = '''        SpawnLevelRuntime(mech, "", "El eco recrea tu recorrido completo.", "");

        SaveScene(scene, "Level_02");'''

new_l2_end = '''        SpawnLevelRuntime(mech, "", "El eco recrea tu recorrido completo.", "");

        SpawnAmbientLights(env, new Vector3(0f, 0f, 10f), 12f, 28f);
        SpawnSmokeVolume("L02_Fog", new Vector3(0f, 0.5f, 10f), new Vector3(14f, 3f, 30f), env, 50f);

        CreateTutorialTrigger("Tut_Route", new Vector3(-3f, 1f, 4f), new Vector3(4f, 3f, 4f),
            "Graba una ruta completa: pisa A, luego B.", "El eco puede hacer multiples acciones.", 4f, tutorial);

        SaveScene(scene, "Level_02");'''

content = content.replace(old_l2_end, new_l2_end)

# Level 03
old_l3_end = '''        SpawnLevelRuntime(mech, "", "A veces debes soltar para poder avanzar.", "");

        SaveScene(scene, "Level_03");'''

new_l3_end = '''        SpawnLevelRuntime(mech, "", "A veces debes soltar para poder avanzar.", "");

        SpawnAmbientLights(env, new Vector3(0f, 0f, 15f), 14f, 40f);
        SpawnSmokeVolume("L03_Fog", new Vector3(0f, 0.5f, 15f), new Vector3(16f, 3f, 42f), env, 60f);

        CreateTutorialTrigger("Tut_Airlock", new Vector3(0f, 1f, 16f), new Vector3(6f, 3f, 4f),
            "Cruza mientras la puerta este abierta.", "No te quedes atras.", 3.5f, tutorial);

        SaveScene(scene, "Level_03");'''

content = content.replace(old_l3_end, new_l3_end)

# Level 04
old_l4_end = '''        SpawnLevelRuntime(mech, "", "Delega las tareas largas al eco.", "");

        SaveScene(scene, "Level_04");'''

new_l4_end = '''        SpawnLevelRuntime(mech, "", "Delega las tareas largas al eco.", "");

        SpawnAmbientLights(env, new Vector3(0f, 0f, 10f), 14f, 30f);
        SpawnPointLight("Lab_Light1", new Vector3(8f, 3f, 16f), new Color(0.4f, 0.3f, 0.6f, 1f), 1.2f, 8f, env);
        SpawnPointLight("Lab_Light2", new Vector3(8f, 3f, 20f), new Color(0.4f, 0.3f, 0.6f, 1f), 1.2f, 8f, env);
        SpawnSmokeVolume("L04_Fog", new Vector3(0f, 0.5f, 10f), new Vector3(20f, 3f, 30f), env, 55f);

        SaveScene(scene, "Level_04");'''

content = content.replace(old_l4_end, new_l4_end)

# Level 05
old_l5_end = '''        SpawnLevelRuntime(mech, "", "A veces es mejor dejar caer a un fantasma.", "");

        SaveScene(scene, "Level_05");'''

new_l5_end = '''        SpawnLevelRuntime(mech, "", "A veces es mejor dejar caer a un fantasma.", "");

        SpawnAmbientLights(env, new Vector3(0f, 2f, 0f), 10f, 24f);
        SpawnPointLight("Pit_Glow", new Vector3(0f, -9f, 12f), new Color(0.8f, 0.2f, 0.2f, 1f), 1.5f, 12f, env);
        SpawnSmokeVolume("L05_FogPit", new Vector3(0f, -8f, 12f), new Vector3(12f, 4f, 12f), env, 45f);
        SpawnSmokeVolume("L05_FogTop", new Vector3(0f, 4.5f, 0f), new Vector3(12f, 2f, 12f), env, 30f);

        SaveScene(scene, "Level_05");'''

content = content.replace(old_l5_end, new_l5_end)

# Level 06
old_l6_end = '''        SpawnLevelRuntime(mech, "", "Solo tienes 1 eco. Hagan una carrera de relevos cruzando puertas.", "");

        SaveScene(scene, "Level_06");'''

new_l6_end = '''        SpawnLevelRuntime(mech, "", "Solo tienes 1 eco. Turnense para abrir puertas.", "");

        SpawnAmbientLights(env, new Vector3(0f, 0f, 16f), 10f, 44f);
        SpawnPointLight("Corridor_1", new Vector3(-3f, 3f, 7f), new Color(0.5f, 0.4f, 0.7f, 1f), 0.8f, 8f, env);
        SpawnPointLight("Corridor_2", new Vector3(3f, 3f, 19f), new Color(0.5f, 0.4f, 0.7f, 1f), 0.8f, 8f, env);
        SpawnPointLight("Corridor_3", new Vector3(-3f, 3f, 31f), new Color(0.5f, 0.4f, 0.7f, 1f), 0.8f, 8f, env);
        SpawnSmokeVolume("L06_Fog", new Vector3(0f, 0.5f, 16f), new Vector3(12f, 3f, 46f), env, 70f);

        SaveScene(scene, "Level_06");'''

content = content.replace(old_l6_end, new_l6_end)

with open(path, "w", encoding="utf-8") as f:
    f.write(content)

print("Done! Ambient lights, fog, and tutorials added to all levels.")
