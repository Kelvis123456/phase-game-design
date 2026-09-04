using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Editor-only scene assembly for the PHASE vertical slice.
// Builds the whole test room + wiring programmatically (Sprints 1-4 of vertical-slice.md)
// so it can run headless via -executeMethod, then builds a Windows standalone player.
public static class VSSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/VSTestRoom.unity";
    private const int PPU = 16;

    private static readonly string[] Tags = { "Player", "Echo", "Hazard", "Ground", "Pickup" };
    // index 6-10 are the first free user layers (0-5 are Unity built-ins)
    private static readonly string[] Layers = { "Ground", "Player", "Echo", "Hazard", "Platform" };
    private static readonly string[] SortingLayers =
    {
        "Background_Far", "Background_Mid", "Background_Near", "Terrain", "Hazard",
        "Echo_3", "Echo_2", "Echo_1", "Player", "VFX", "UI_World", "UI_HUD", "UI_Modal"
    };

    // Set una vez en BuildScene() e leídas por BuildSyncRoom() — evita pasar 4 sprites
    // más por parámetro en los 16+ call sites existentes.
    private static Sprite _leverOffSprite, _leverOnSprite, _doorClosedSprite, _doorOpenSprite;

    public static void BuildAll()
    {
        SetupProjectSettings();
        BuildScene();
        BuildPlayerExe();
    }

    // Adds tags/layers/sorting layers via SerializedObject against the real TagManager
    // asset instead of hand-editing its YAML — Unity's asset YAML has parser quirks
    // (confirmed: a hand-written version failed with "Parser Failure ... Expect ':'
    // between key and value", which silently dropped ALL custom tags/layers/sorting
    // layers with no build error). This is the reliable way to do it.
    public static void SetupProjectSettings()
    {
        Debug.Log("[VSSceneBuilder] Configuring tags/layers/sorting layers...");

        var tagManagerAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        var so = new SerializedObject(tagManagerAssets[0]);

        var tagsProp = so.FindProperty("tags");
        foreach (var tag in Tags)
        {
            bool exists = false;
            for (int i = 0; i < tagsProp.arraySize; i++)
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag) { exists = true; break; }
            if (exists) continue;
            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
        }

        // Layers: fixed slots starting at index 6 (0-5 are Unity built-ins: Default,
        // TransparentFX, Ignore Raycast, [reserved], Water, UI).
        var layersProp = so.FindProperty("layers");
        for (int i = 0; i < Layers.Length; i++)
            layersProp.GetArrayElementAtIndex(6 + i).stringValue = Layers[i];

        var sortingLayersProp = so.FindProperty("m_SortingLayers");
        sortingLayersProp.ClearArray();
        for (int i = 0; i < SortingLayers.Length; i++)
        {
            sortingLayersProp.InsertArrayElementAtIndex(i);
            var element = sortingLayersProp.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("name").stringValue = SortingLayers[i];
            element.FindPropertyRelative("uniqueID").intValue = i;
            element.FindPropertyRelative("locked").boolValue = false;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();

        Debug.Log("[VSSceneBuilder] Tags/layers/sorting layers configured.");
    }

    public static void BuildScene()
    {
        Debug.Log("[VSSceneBuilder] Building scene...");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Directory.CreateDirectory("Assets/Scenes");
        Directory.CreateDirectory("Assets/Art");
        Directory.CreateDirectory("Assets/Materials");
        Directory.CreateDirectory("Assets/Tiles");

        // ---- Placeholder art ----
        Sprite groundSprite = ImportRealSprite("tile_ground.png", "Assets/Art/tile_ground.png", PPU);
        Sprite playerSprite = ImportRealSprite("player.png", "Assets/Art/player.png", PPU);
        Sprite hazardSprite = ImportRealSprite("hazard.png", "Assets/Art/hazard.png", PPU);
        _leverOffSprite = ImportRealSprite("lever_off.png", "Assets/Art/lever_off.png", PPU);
        _leverOnSprite = ImportRealSprite("lever_on.png", "Assets/Art/lever_on.png", PPU);
        _doorClosedSprite = ImportRealSprite("door_closed.png", "Assets/Art/door_closed.png", PPU);
        _doorOpenSprite = ImportRealSprite("door_open.png", "Assets/Art/door_open.png", PPU);
        Sprite keyartSprite = ImportUISprite("keyart.png", "Assets/Art/keyart.png");

        TileBase groundTile = CreateTile("Assets/Tiles/GroundTile.asset", groundSprite);

        // ---- Camera ----
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        // z=-10: sprites live at z=0 — without depth separation the camera sits exactly
        // on the near clip plane and renders nothing but its own clear color.
        camGO.transform.position = new Vector3(3f, 1.5f, -10f);
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f, 0.05f, 0.08f);
        camGO.AddComponent<AudioListener>();

        var ppc = camGO.AddComponent<PixelPerfectCamera>();
        ppc.assetsPPU = PPU;
        ppc.refResolutionX = 480;
        ppc.refResolutionY = 270;
        ppc.cropFrame = PixelPerfectCamera.CropFrame.Windowbox;
        ppc.gridSnapping = PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;

        var camData = cam.GetUniversalAdditionalCameraData();
        camData.renderPostProcessing = true;

        // ---- Global Volume (bullet-time post-processing feedback) ----
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, "Assets/Materials/VSVolumeProfile.asset");
        var vignette = profile.Add<Vignette>(true);
        vignette.intensity.overrideState = true;
        vignette.intensity.value = 0.25f;
        var chromatic = profile.Add<ChromaticAberration>(true);
        chromatic.intensity.overrideState = true;
        chromatic.intensity.value = 0f;
        var bloom = profile.Add<Bloom>(true);
        bloom.threshold.overrideState = true;
        bloom.threshold.value = 0.8f;
        bloom.intensity.overrideState = true;
        bloom.intensity.value = 1.2f;

        var volumeGO = new GameObject("Global Volume");
        var volume = volumeGO.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.profile = profile;

        // ---- Ground tilemap: floor with a gap (Zone B), staggered platforms (Zone C) ----
        var gridGO = new GameObject("Grid");
        var grid = gridGO.AddComponent<Grid>();
        grid.cellSize = new Vector3(1f, 1f, 0f);

        var groundGO = new GameObject("Ground");
        groundGO.transform.SetParent(gridGO.transform);
        groundGO.layer = LayerMask.NameToLayer("Ground");
        groundGO.tag = "Ground";
        var tilemap = groundGO.AddComponent<Tilemap>();
        var tmRenderer = groundGO.AddComponent<TilemapRenderer>();
        tmRenderer.sortingLayerName = "Terrain";
        // Visual only — no TilemapCollider2D/CompositeCollider2D here. Four different fixes
        // (bake-after-tiles ordering, runtime Awake() bake, runtime Start() bake, explicit
        // Tile.ColliderType.Grid) all produced a CompositeCollider2D with bounds (0,0,0) and
        // zero OverlapPoint hits in the actual standalone build — confirmed empirically via
        // an on-screen debug HUD across 4 rebuild+relaunch cycles. Root cause not isolated;
        // explicit BoxCollider2D geometry below sidesteps the whole tilemap-collider pipeline.

        // Floor: x=0..9 and x=13..29 at y=0..-1 (two rows thick). Gap = pit at x=10..12.
        for (int x = 0; x <= 9; x++) { tilemap.SetTile(new Vector3Int(x, 0, 0), groundTile); tilemap.SetTile(new Vector3Int(x, -1, 0), groundTile); }
        for (int x = 13; x <= 29; x++) { tilemap.SetTile(new Vector3Int(x, 0, 0), groundTile); tilemap.SetTile(new Vector3Int(x, -1, 0), groundTile); }

        // Zone A - simple platform
        for (int x = 3; x <= 5; x++) tilemap.SetTile(new Vector3Int(x, 3, 0), groundTile);

        // Zone C - staggered platforms
        for (int x = 17; x <= 18; x++) tilemap.SetTile(new Vector3Int(x, 3, 0), groundTile);
        for (int x = 20; x <= 21; x++) tilemap.SetTile(new Vector3Int(x, 5, 0), groundTile);
        for (int x = 23; x <= 24; x++) tilemap.SetTile(new Vector3Int(x, 7, 0), groundTile);

        // Explicit physics geometry matching the tiles above exactly (cell (x,y) spans
        // world [x, x+1) x [y, y+1) with the default 1x1 Grid cell size used here).
        var floorLeftCollider = AddGroundBox(groundGO.transform, "Floor_Left", 5f, 0f, 10f, 2f);      // x=0..9,  y=-1..1
        AddGroundBox(groundGO.transform, "Floor_Right", 21.5f, 0f, 17f, 2f);  // x=13..29, y=-1..1
        AddGroundBox(groundGO.transform, "Platform_ZoneA", 4.5f, 3.5f, 3f, 1f);   // x=3..5,   y=3..4
        AddGroundBox(groundGO.transform, "Platform_ZoneC1", 18f, 3.5f, 2f, 1f);   // x=17..18, y=3..4
        AddGroundBox(groundGO.transform, "Platform_ZoneC2", 21f, 5.5f, 2f, 1f);   // x=20..21, y=5..6
        AddGroundBox(groundGO.transform, "Platform_ZoneC3", 24f, 7.5f, 2f, 1f);   // x=23..24, y=7..8

        // ---- Hazard: pit floor spikes (fall in the gap = death) ----
        var hazardGO = new GameObject("Hazard_Pit");
        hazardGO.layer = LayerMask.NameToLayer("Hazard");
        hazardGO.transform.position = new Vector3(11f, -3f, 0f);
        var hazardSr = hazardGO.AddComponent<SpriteRenderer>();
        hazardSr.sprite = hazardSprite;
        hazardSr.sortingLayerName = "Hazard";
        hazardGO.transform.localScale = new Vector3(3f, 1f, 1f);
        var hazardCol = hazardGO.AddComponent<BoxCollider2D>();
        hazardCol.size = new Vector2(1f, 1f);
        hazardGO.AddComponent<HazardSpike>();

        // ---- VFXPool (empty entries — harmless no-op VFX for the VS) ----
        var vfxGO = new GameObject("VFXPool");
        var vfxPool = vfxGO.AddComponent<VFXPool>();

        // ---- TimeManager ----
        var timeGO = new GameObject("TimeManager");
        var timeManager = timeGO.AddComponent<TimeManager>();
        SetPrivate(timeManager, "_globalVolume", volume);

        // ---- InputReader ----
        var inputGO = new GameObject("InputReader");
        var inputReader = inputGO.AddComponent<InputReader>();

        // ---- Bootstrap (execution order -100, wires the three above) ----
        var bootstrapGO = new GameObject("Bootstrap");
        var bootstrap = bootstrapGO.AddComponent<VSBootstrap>();
        SetPrivate(bootstrap, "timeManager", timeManager);
        SetPrivate(bootstrap, "inputReader", inputReader);
        SetPrivate(bootstrap, "vfxPool", vfxPool);

        // ---- Fase 10 M1: producción — save, progresión, run FSM ----
        var saveGO = new GameObject("SaveSystem");
        saveGO.AddComponent<SaveSystem>();

        var progressionGO = new GameObject("ProgressionSystem");
        progressionGO.AddComponent<ProgressionSystem>();

        var runManagerGO = new GameObject("RunManager");
        runManagerGO.AddComponent<RunManager>();

        // ---- Player ----
        var spawnGO = new GameObject("PlayerSpawnPoint");
        spawnGO.transform.position = new Vector3(1f, 2f, 0f); // floor top surface is at y=1; clear headroom for the capsule collider

        var playerGO = new GameObject("Player");
        playerGO.tag = "Player";
        playerGO.layer = LayerMask.NameToLayer("Player");
        playerGO.transform.position = spawnGO.transform.position;
        var playerSr = playerGO.AddComponent<SpriteRenderer>();
        playerSr.sprite = playerSprite;
        playerSr.sortingLayerName = "Player";
        var playerRb = playerGO.AddComponent<Rigidbody2D>();
        var playerCol = playerGO.AddComponent<CapsuleCollider2D>();
        playerCol.size = new Vector2(0.7f, 1.8f);
        playerCol.direction = CapsuleDirection2D.Vertical;
        playerCol.offset = new Vector2(0f, 0f);
        var playerController = playerGO.AddComponent<PlayerController>();
        int groundMask = (1 << LayerMask.NameToLayer("Ground")) | (1 << LayerMask.NameToLayer("Platform"));
        SetPrivate(playerController, "_groundMask", (LayerMask)groundMask);
        SetPrivate(playerController, "_sprite", playerSr);
        var playerStats = playerGO.AddComponent<PlayerStats>();
        var recorder = playerGO.AddComponent<InputRecorder>();

        // ---- Temporary diagnostic HUD (remove once gameplay is confirmed working) ----
        var debugHudGO = new GameObject("DebugHUD");
        var debugHud = debugHudGO.AddComponent<DebugHUD>();
        debugHud.player = playerGO.transform;
        debugHud.controller = playerController;
        debugHud.playerRb = playerRb;
        debugHud.groundCollider = floorLeftCollider;
        debugHud.groundMask = (LayerMask)groundMask;

        // ---- Echo template (inactive; EchoManager.Instantiate()'s from this) ----
        var echoShader = Shader.Find("PHASE/Echo");
        if (echoShader == null) Debug.LogError("[VSSceneBuilder] PHASE/Echo shader not found — check Assets/Shaders/EchoShader.shader imported correctly.");
        var echoMat = new Material(echoShader);
        AssetDatabase.CreateAsset(echoMat, "Assets/Materials/EchoMaterial.mat");

        var echoTemplateGO = new GameObject("EchoPlayerTemplate");
        echoTemplateGO.layer = LayerMask.NameToLayer("Echo");
        var echoSr = echoTemplateGO.AddComponent<SpriteRenderer>();
        echoSr.sprite = playerSprite;
        echoSr.sharedMaterial = echoMat;
        echoSr.sortingLayerName = "Echo_1";
        var echoPlayer = echoTemplateGO.AddComponent<EchoPlayer>();
        SetPrivate(echoPlayer, "_sprite", echoSr);
        echoTemplateGO.SetActive(false);

        // ---- LoopTimer / EchoManager / VSRoomController ----
        var loopGO = new GameObject("LoopTimer");
        var loopTimer = loopGO.AddComponent<LoopTimer>();

        var echoManagerGO = new GameObject("EchoManager");
        var echoManager = echoManagerGO.AddComponent<EchoManager>();
        SetPrivate(echoManager, "_echoPrefab", echoPlayer);
        SetPrivate(echoManager, "_recorder", recorder);
        SetPrivate(echoManager, "_loopTimer", loopTimer);

        // ---- Minimal UI: death flash overlay ----
        var canvasGO = new GameObject("Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        var flashGO = new GameObject("DeathFlash");
        flashGO.transform.SetParent(canvasGO.transform, false);
        var flashImg = flashGO.AddComponent<Image>();
        flashImg.color = new Color(1f, 0.2f, 0.2f, 0f);
        var flashRt = flashGO.GetComponent<RectTransform>();
        flashRt.anchorMin = Vector2.zero;
        flashRt.anchorMax = Vector2.one;
        flashRt.offsetMin = Vector2.zero;
        flashRt.offsetMax = Vector2.zero;

        // ---- Fase 10 M3.1: EventSystem (requerido para que los Button de UI respondan a
        // clicks) + la UI real del árbol de progresión (Tab para abrir/cerrar) ----
        var eventSystemGO = new GameObject("EventSystem");
        eventSystemGO.AddComponent<EventSystem>();
        eventSystemGO.AddComponent<StandaloneInputModule>();

        var progressionUIGO = new GameObject("ProgressionTreeUI");
        progressionUIGO.AddComponent<ProgressionTreeUI>();

        var upgradeSelectorGO = new GameObject("UpgradeSelectorUI");
        upgradeSelectorGO.AddComponent<UpgradeSelectorUI>();

        var mainMenuGO = new GameObject("MainMenuUI");
        var mainMenu = mainMenuGO.AddComponent<MainMenuUI>();
        SetPrivate(mainMenu, "_background", keyartSprite);

        var cinematicGO = new GameObject("TercerEspejoCinematic");
        var cinematic = cinematicGO.AddComponent<TercerEspejoCinematic>();
        SetPrivate(cinematic, "_echoSprite", playerSprite);

        var roomControllerGO = new GameObject("VSRoomController");
        var roomController = roomControllerGO.AddComponent<VSRoomController>();
        SetPrivate(roomController, "_playerSpawnPoint", spawnGO.transform);
        SetPrivate(roomController, "_echoManager", echoManager);
        SetPrivate(roomController, "_recorder", recorder);
        SetPrivate(roomController, "_loopTimer", loopTimer);
        SetPrivate(roomController, "_deathFlash", flashImg);

        // ---- Fase 10 M2: pool de salas real ----
        BuildRoomPool(cam, playerController, echoManager, loopTimer, spawnGO, gridGO, hazardGO, groundTile);

        // ---- Save scene, register in build settings ----
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[VSSceneBuilder] Scene build complete: " + ScenePath);
    }

    // Fase 10 M2: envuelve la sala original como "Room 0" (SOLO, sin palancas) y construye
    // 2 salas SYNC nuevas — cada una resoluble con el eco de 1 solo slot (el default del VS),
    // parándose sobre una palanca casi los 8s completos del loop para que el eco la sostenga
    // mientras el jugador cruza la puerta. No son las 50 salas del plan — son un pool real
    // y jugable que prueba el sistema de ensamblaje end-to-end.
    private static void BuildRoomPool(Camera cam, PlayerController playerController, EchoManager echoManager,
        LoopTimer loopTimer, GameObject spawnGO, GameObject gridGO, GameObject hazardGO, TileBase groundTile)
    {
        Directory.CreateDirectory("Assets/Rooms");

        var assemblerGO = new GameObject("RoomAssembler");
        var assembler = assemblerGO.AddComponent<RoomAssembler>();
        SetPrivate(assembler, "_camera", cam);
        SetPrivate(assembler, "_player", playerController);
        SetPrivate(assembler, "_echoManager", echoManager);
        SetPrivate(assembler, "_loopTimer", loopTimer);

        // ---- Room 0: la sala original del VS, envuelta como sala SOLO del pool ----
        var room0Container = new GameObject("Room_Z1_SOLO_Original");
        gridGO.transform.SetParent(room0Container.transform);
        hazardGO.transform.SetParent(room0Container.transform);

        var room0CamAnchor = new GameObject("CameraAnchor").transform;
        room0CamAnchor.SetParent(room0Container.transform);
        room0CamAnchor.position = new Vector3(3f, 1.5f, 0f);

        var room0Exit = new GameObject("RoomExit");
        room0Exit.transform.SetParent(room0Container.transform);
        room0Exit.transform.position = new Vector3(28f, 1f, 0f);
        var room0ExitCol = room0Exit.AddComponent<BoxCollider2D>();
        room0ExitCol.size = new Vector2(1f, 3f);
        room0Exit.AddComponent<RoomExit>();

        var room0Data = ScriptableObject.CreateInstance<RoomData>();
        room0Data.roomId = "Z1_SOLO_ORIGINAL";
        room0Data.zoneId = 1;
        room0Data.difficultyTier = 1;
        room0Data.mechanic = PrimaryMechanic.SOLO;
        room0Data.hasAltSolution = true;
        room0Data.introRunMin = 1;
        AssetDatabase.CreateAsset(room0Data, "Assets/Rooms/Z1_SOLO_ORIGINAL.asset");

        assembler.RegisterRoom(new RoomInstance
        {
            data = room0Data,
            container = room0Container,
            spawnPoint = spawnGO.transform,
            cameraAnchor = room0CamAnchor,
        });
        // RegisterRoom desactiva el contenedor por default (las salas del pool solo se activan
        // vía AssembleRun). Room 0 debe quedar visible/jugable de entrada — igual que el VS ya
        // verificado — para no romper el flujo de prueba directo sin pasar por F4/StartRun().
        room0Container.SetActive(true);

        // ---- 2 salas SYNC nuevas: palanca + puerta, resoluble con 1 eco ----
        BuildSyncRoom(assembler, groundTile, "Z1_SYNC_01", xOffset: 100f, leverX: 5f, doorX: 14f, exitX: 17f);
        BuildSyncRoom(assembler, groundTile, "Z1_SYNC_02", xOffset: 200f, leverX: 4f, doorX: 11f, exitX: 14f);

        // ---- Salas SOLO/TIMING adicionales: más rápidas de producir en volumen que las
        // SYNC (sin palanca/puerta/eco que verificar), crecen la variedad real del pool. ----
        BuildTraversalRoom(assembler, "Z1_SOLO_02", xOffset: 300f, length: 16f, hasGap: false, gapStart: 0f, gapWidth: 0f);
        BuildTraversalRoom(assembler, "Z1_SOLO_03", xOffset: 350f, length: 20f, hasGap: false, gapStart: 0f, gapWidth: 0f);
        BuildTraversalRoom(assembler, "Z1_TIMING_01", xOffset: 400f, length: 18f, hasGap: true, gapStart: 8f, gapWidth: 3f);
        BuildTraversalRoom(assembler, "Z1_TIMING_02", xOffset: 450f, length: 22f, hasGap: true, gapStart: 6f, gapWidth: 4f);
        BuildTraversalRoom(assembler, "Z1_TIMING_03", xOffset: 500f, length: 24f, hasGap: true, gapStart: 12f, gapWidth: 3f);
        BuildTraversalRoom(assembler, "Z1_SOLO_04", xOffset: 550f, length: 14f, hasGap: false, gapStart: 0f, gapWidth: 0f);

        // ---- Segundo lote: más SYNC (variedad de layout de palanca/puerta) + más traversal ----
        BuildSyncRoom(assembler, groundTile, "Z1_SYNC_03", xOffset: 600f, leverX: 6f, doorX: 16f, exitX: 19f);
        BuildSyncRoom(assembler, groundTile, "Z1_SYNC_04", xOffset: 700f, leverX: 3f, doorX: 9f, exitX: 12f);
        BuildTraversalRoom(assembler, "Z1_SOLO_05", xOffset: 800f, length: 18f, hasGap: false, gapStart: 0f, gapWidth: 0f);
        BuildTraversalRoom(assembler, "Z1_TIMING_04", xOffset: 850f, length: 20f, hasGap: true, gapStart: 9f, gapWidth: 3.5f);
        BuildTraversalRoom(assembler, "Z1_TIMING_05", xOffset: 900f, length: 26f, hasGap: true, gapStart: 14f, gapWidth: 4f);

        // ---- Tercer lote ----
        BuildSyncRoom(assembler, groundTile, "Z1_SYNC_05", xOffset: 950f, leverX: 5f, doorX: 13f, exitX: 16f);
        BuildSyncRoom(assembler, groundTile, "Z1_SYNC_06", xOffset: 1000f, leverX: 7f, doorX: 17f, exitX: 20f);
        BuildTraversalRoom(assembler, "Z1_SOLO_06", xOffset: 1050f, length: 12f, hasGap: false, gapStart: 0f, gapWidth: 0f);
        BuildTraversalRoom(assembler, "Z1_SOLO_07", xOffset: 1100f, length: 24f, hasGap: false, gapStart: 0f, gapWidth: 0f);
        BuildTraversalRoom(assembler, "Z1_TIMING_06", xOffset: 1150f, length: 16f, hasGap: true, gapStart: 7f, gapWidth: 3f);
        BuildTraversalRoom(assembler, "Z1_TIMING_07", xOffset: 1200f, length: 28f, hasGap: true, gapStart: 16f, gapWidth: 4.5f);
        BuildTraversalRoom(assembler, "Z1_SOLO_08", xOffset: 1250f, length: 15f, hasGap: false, gapStart: 0f, gapWidth: 0f);

        // ---- Lote final: llega a 50 salas reales del pool ----
        BuildSyncRoom(assembler, groundTile, "Z1_SYNC_07", xOffset: 1300f, leverX: 4f, doorX: 10f, exitX: 13f);
        BuildSyncRoom(assembler, groundTile, "Z1_SYNC_08", xOffset: 1350f, leverX: 8f, doorX: 18f, exitX: 21f);
        BuildSyncRoom(assembler, groundTile, "Z1_SYNC_09", xOffset: 1400f, leverX: 5f, doorX: 12f, exitX: 15f);
        BuildSyncRoom(assembler, groundTile, "Z1_SYNC_10", xOffset: 1450f, leverX: 6f, doorX: 15f, exitX: 18f);
        BuildSyncRoom(assembler, groundTile, "Z1_SYNC_11", xOffset: 1500f, leverX: 3f, doorX: 8f, exitX: 11f);
        BuildSyncRoom(assembler, groundTile, "Z1_SYNC_12", xOffset: 1550f, leverX: 9f, doorX: 19f, exitX: 22f);
        BuildSyncRoom(assembler, groundTile, "Z1_SYNC_13", xOffset: 1600f, leverX: 4f, doorX: 11f, exitX: 14f);
        BuildSyncRoom(assembler, groundTile, "Z1_SYNC_14", xOffset: 1650f, leverX: 7f, doorX: 16f, exitX: 19f);
        BuildSyncRoom(assembler, groundTile, "Z1_SYNC_15", xOffset: 1700f, leverX: 5f, doorX: 13f, exitX: 16f);
        BuildSyncRoom(assembler, groundTile, "Z1_SYNC_16", xOffset: 1750f, leverX: 6f, doorX: 14f, exitX: 17f);

        BuildTraversalRoom(assembler, "Z1_SOLO_09", xOffset: 1800f, length: 13f, hasGap: false, gapStart: 0f, gapWidth: 0f);
        BuildTraversalRoom(assembler, "Z1_SOLO_10", xOffset: 1850f, length: 17f, hasGap: false, gapStart: 0f, gapWidth: 0f);
        BuildTraversalRoom(assembler, "Z1_SOLO_11", xOffset: 1900f, length: 21f, hasGap: false, gapStart: 0f, gapWidth: 0f);
        BuildTraversalRoom(assembler, "Z1_SOLO_12", xOffset: 1950f, length: 19f, hasGap: false, gapStart: 0f, gapWidth: 0f);
        BuildTraversalRoom(assembler, "Z1_SOLO_13", xOffset: 2000f, length: 16f, hasGap: false, gapStart: 0f, gapWidth: 0f);
        BuildTraversalRoom(assembler, "Z1_SOLO_14", xOffset: 2050f, length: 22f, hasGap: false, gapStart: 0f, gapWidth: 0f);
        BuildTraversalRoom(assembler, "Z1_SOLO_15", xOffset: 2100f, length: 14f, hasGap: false, gapStart: 0f, gapWidth: 0f);
        BuildTraversalRoom(assembler, "Z1_SOLO_16", xOffset: 2150f, length: 18f, hasGap: false, gapStart: 0f, gapWidth: 0f);
        BuildTraversalRoom(assembler, "Z1_SOLO_17", xOffset: 2200f, length: 20f, hasGap: false, gapStart: 0f, gapWidth: 0f);
        BuildTraversalRoom(assembler, "Z1_SOLO_18", xOffset: 2250f, length: 15f, hasGap: false, gapStart: 0f, gapWidth: 0f);

        BuildTraversalRoom(assembler, "Z1_TIMING_08", xOffset: 2300f, length: 18f, hasGap: true, gapStart: 8f, gapWidth: 3f);
        BuildTraversalRoom(assembler, "Z1_TIMING_09", xOffset: 2350f, length: 22f, hasGap: true, gapStart: 10f, gapWidth: 3.5f);
        BuildTraversalRoom(assembler, "Z1_TIMING_10", xOffset: 2400f, length: 20f, hasGap: true, gapStart: 9f, gapWidth: 4f);
        BuildTraversalRoom(assembler, "Z1_TIMING_11", xOffset: 2450f, length: 24f, hasGap: true, gapStart: 13f, gapWidth: 3f);
        BuildTraversalRoom(assembler, "Z1_TIMING_12", xOffset: 2500f, length: 26f, hasGap: true, gapStart: 15f, gapWidth: 4.5f);
        BuildTraversalRoom(assembler, "Z1_TIMING_13", xOffset: 2550f, length: 19f, hasGap: true, gapStart: 8f, gapWidth: 3f);
        BuildTraversalRoom(assembler, "Z1_TIMING_14", xOffset: 2600f, length: 23f, hasGap: true, gapStart: 12f, gapWidth: 3.5f);
        BuildTraversalRoom(assembler, "Z1_TIMING_15", xOffset: 2650f, length: 21f, hasGap: true, gapStart: 10f, gapWidth: 4f);
        BuildTraversalRoom(assembler, "Z1_TIMING_16", xOffset: 2700f, length: 25f, hasGap: true, gapStart: 14f, gapWidth: 3f);

        // ---- Zona 3 "Abismo": primeras salas reales de DEPENDENCY y FRUSTRATION
        // (GDD §6.2) — antes ausentes del pool por completo. ----
        BuildDependencyRoom(assembler, "Z3_DEPENDENCY_01", xOffset: 2750f, lever1X: 4f, door1X: 9f, lever2X: 13f, door2X: 18f, exitX: 21f);
        BuildDependencyRoom(assembler, "Z3_DEPENDENCY_02", xOffset: 2800f, lever1X: 3f, door1X: 8f, lever2X: 12f, door2X: 17f, exitX: 20f);
        BuildFrustrationRoom(assembler, "Z3_FRUSTRATION_01", xOffset: 2850f, hazardStartX: 5f, hazardWidth: 3f, leverX: 10f, doorX: 15f, exitX: 18f);
        BuildFrustrationRoom(assembler, "Z3_FRUSTRATION_02", xOffset: 2900f, hazardStartX: 4f, hazardWidth: 3.5f, leverX: 9f, doorX: 14f, exitX: 17f);

        // ---- Boss 1 "El Espejo Fragmentado" (GDD §8.2, Fase 1) — slot fijo al final de
        // cada run, no sale del sorteo aleatorio del pool. ----
        BuildBossRoom(assembler, "Z1_BOSS_ESPEJO_FRAGMENTADO", xOffset: 2950f);
    }

    // Sala de traversal simple: piso plano, opcionalmente con un foso que exige saltar
    // (TIMING) o sin él (SOLO). Sin palanca/puerta — el checklist de verificación es solo
    // "el jugador puede caminar/saltar de un extremo al otro", mucho más rápido de producir
    // en volumen que las salas SYNC con eco.
    private static void BuildTraversalRoom(RoomAssembler assembler, string roomId, float xOffset,
        float length, bool hasGap, float gapStart, float gapWidth)
    {
        var container = new GameObject($"Room_{roomId}");
        container.transform.position = new Vector3(xOffset, 0f, 0f);

        void AddFloorSegment(float startX, float endX)
        {
            float w = endX - startX;
            if (w <= 0f) return;
            var segGO = new GameObject($"Floor_{startX:F0}_{endX:F0}");
            segGO.transform.SetParent(container.transform, false);
            segGO.transform.localPosition = new Vector3(startX + w * 0.5f, 0f, 0f);
            segGO.layer = LayerMask.NameToLayer("Ground");
            var sr = segGO.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/tile_ground.png");
            sr.sortingLayerName = "Terrain";
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(w, 2f);
            var col = segGO.AddComponent<BoxCollider2D>();
            col.size = new Vector2(w, 2f);
        }

        if (hasGap)
        {
            AddFloorSegment(0f, gapStart);
            AddFloorSegment(gapStart + gapWidth, length);

            // Red de seguridad: sin esto, fallar el salto = caer para siempre sin morir/resetear
            // (softlock real — lo encontré probando esta sala: el jugador cayó hasta y=-207
            // y se quedó ahí, RunState nunca avanzó). HazardSpike ya maneja muerte+reset;
            // solo hace falta un trigger ancho bien abajo del foso.
            var voidGO = new GameObject("VoidKillZone");
            voidGO.transform.SetParent(container.transform, false);
            voidGO.transform.localPosition = new Vector3(gapStart + gapWidth * 0.5f, -15f, 0f);
            voidGO.layer = LayerMask.NameToLayer("Hazard");
            var voidCol = voidGO.AddComponent<BoxCollider2D>();
            voidCol.size = new Vector2(gapWidth + 6f, 4f);
            voidGO.AddComponent<HazardSpike>();
        }
        else
        {
            AddFloorSegment(0f, length);
        }

        var exitGO = new GameObject("RoomExit");
        exitGO.transform.SetParent(container.transform, false);
        exitGO.transform.localPosition = new Vector3(length - 1f, 1f, 0f);
        var exitCol = exitGO.AddComponent<BoxCollider2D>();
        exitCol.size = new Vector2(1.5f, 3f);
        exitGO.AddComponent<RoomExit>();

        var spawnPoint = new GameObject("SpawnPoint").transform;
        spawnPoint.SetParent(container.transform, false);
        spawnPoint.localPosition = new Vector3(1f, 2f, 0f);

        var camAnchor = new GameObject("CameraAnchor").transform;
        camAnchor.SetParent(container.transform, false);
        camAnchor.localPosition = new Vector3(length * 0.4f, 1.5f, 0f);

        var data = ScriptableObject.CreateInstance<RoomData>();
        data.roomId = roomId;
        data.zoneId = 1;
        data.difficultyTier = hasGap ? 2 : 1;
        data.mechanic = hasGap ? PrimaryMechanic.TIMING : PrimaryMechanic.SOLO;
        data.ecoCountRequired = 0;
        data.hasAltSolution = true;
        data.introRunMin = 1;
        AssetDatabase.CreateAsset(data, $"Assets/Rooms/{roomId}.asset");

        assembler.RegisterRoom(new RoomInstance
        {
            data = data,
            container = container,
            spawnPoint = spawnPoint,
            cameraAnchor = camAnchor,
        });
    }

    private static void BuildSyncRoom(RoomAssembler assembler, TileBase groundTile, string roomId,
        float xOffset, float leverX, float doorX, float exitX)
    {
        var container = new GameObject($"Room_{roomId}");
        container.transform.position = new Vector3(xOffset, 0f, 0f);

        // Floor: una sola franja sólida, suficientemente larga para cubrir palanca+puerta+salida.
        float floorWidth = exitX + 4f;
        var floorGO = new GameObject("Floor");
        floorGO.transform.SetParent(container.transform, false);
        floorGO.transform.localPosition = new Vector3(floorWidth * 0.5f, 0f, 0f);
        floorGO.layer = LayerMask.NameToLayer("Ground");
        var floorSr = floorGO.AddComponent<SpriteRenderer>();
        floorSr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/tile_ground.png");
        floorSr.sortingLayerName = "Terrain";
        floorSr.drawMode = SpriteDrawMode.Tiled;
        floorSr.size = new Vector2(floorWidth, 2f);
        var floorCol = floorGO.AddComponent<BoxCollider2D>();
        floorCol.size = new Vector2(floorWidth, 2f);

        // Palanca — el jugador (o el eco) debe estar parado sobre ella para sostener la puerta.
        var leverGO = new GameObject("Lever");
        leverGO.transform.SetParent(container.transform, false);
        leverGO.transform.localPosition = new Vector3(leverX, 1.5f, 0f);
        var leverSr = leverGO.AddComponent<SpriteRenderer>();
        leverSr.sprite = _leverOffSprite;
        leverSr.sortingLayerName = "Hazard";
        var leverCol = leverGO.AddComponent<BoxCollider2D>();
        var lever = leverGO.AddComponent<TriggerLever>();
        SetPrivate(lever, "_spriteOff", _leverOffSprite);
        SetPrivate(lever, "_spriteOn", _leverOnSprite);

        // Puerta — bloquea el paso hasta que la palanca esté sostenida.
        var doorGO = new GameObject("Door");
        doorGO.transform.SetParent(container.transform, false);
        doorGO.transform.localPosition = new Vector3(doorX, 1.5f, 0f);
        // Sin esto la puerta quedaba en layer Default (0), fuera de _groundMask (Ground+Platform) —
        // el jugador la atravesaba sin importar su estado abierto/cerrado. Solo el RoomExit
        // (el trigger lógico) respetaba _requiredDoor.IsOpen; la puerta en sí no bloqueaba nada.
        doorGO.layer = LayerMask.NameToLayer("Ground");
        var doorSr = doorGO.AddComponent<SpriteRenderer>();
        doorSr.sprite = _doorClosedSprite;
        doorSr.sortingLayerName = "Hazard";
        doorGO.transform.localScale = new Vector3(1f, 1.5f, 1f);
        var doorCol = doorGO.AddComponent<BoxCollider2D>();
        var door = doorGO.AddComponent<DoorGate>();
        SetPrivate(door, "_spriteClosed", _doorClosedSprite);
        SetPrivate(door, "_spriteOpen", _doorOpenSprite);
        // _requiredCount ya es 1 por default en DoorGate — exactamente lo que necesita
        // una sala resoluble con el eco de 1 solo slot.
        SetPrivate(lever, "_linkedDoor", door);

        // Salida — zona ancha para que el jugador pueda cruzar sin soltar el trigger antes
        // de tiempo si camina justo detrás de la puerta.
        var exitGO = new GameObject("RoomExit");
        exitGO.transform.SetParent(container.transform, false);
        exitGO.transform.localPosition = new Vector3(exitX, 1f, 0f);
        var exitCol = exitGO.AddComponent<BoxCollider2D>();
        exitCol.size = new Vector2(1.5f, 3f);
        var exit = exitGO.AddComponent<RoomExit>();
        SetPrivate(exit, "_requiredDoor", door);

        var spawnPoint = new GameObject("SpawnPoint").transform;
        spawnPoint.SetParent(container.transform, false);
        spawnPoint.localPosition = new Vector3(1f, 2f, 0f);

        var camAnchor = new GameObject("CameraAnchor").transform;
        camAnchor.SetParent(container.transform, false);
        camAnchor.localPosition = new Vector3(floorWidth * 0.4f, 1.5f, 0f);

        var data = ScriptableObject.CreateInstance<RoomData>();
        data.roomId = roomId;
        data.zoneId = 1;
        data.difficultyTier = 2;
        data.mechanic = PrimaryMechanic.SYNC;
        data.ecoCountRequired = 1;
        data.hasAltSolution = false;
        data.introRunMin = 1;
        AssetDatabase.CreateAsset(data, $"Assets/Rooms/{roomId}.asset");

        assembler.RegisterRoom(new RoomInstance
        {
            data = data,
            container = container,
            spawnPoint = spawnPoint,
            cameraAnchor = camAnchor,
        });
    }

    private static readonly Color Z3BackgroundColor = new Color(0.07f, 0.02f, 0.13f);

    // GDD §6.2 Zona 3 "Abismo" + §6.1: dos puertas en serie. La primera (D1) es
    // "latching" — una vez abierta por la palanca queda abierta el resto del intento,
    // así que resolverla no exige un eco, solo haberla cruzado antes. La segunda (D2)
    // es momentánea (igual que SYNC): exige que alguien siga parado en L2 mientras
    // el jugador cruza D2, lo cual solo es posible con el eco de un loop anterior
    // sosteniéndola. La cadena real: L1 tuvo que resolverse ANTES de que el intento
    // de sync en L2/D2 tenga sentido — de ahí "DEPENDENCY" en vez de un SYNC más.
    private static void BuildDependencyRoom(RoomAssembler assembler, string roomId, float xOffset,
        float lever1X, float door1X, float lever2X, float door2X, float exitX)
    {
        var container = new GameObject($"Room_{roomId}");
        container.transform.position = new Vector3(xOffset, 0f, 0f);
        container.AddComponent<RoomVisualTheme>().backgroundColor = Z3BackgroundColor;

        float floorWidth = exitX + 4f;
        var floorGO = new GameObject("Floor");
        floorGO.transform.SetParent(container.transform, false);
        floorGO.transform.localPosition = new Vector3(floorWidth * 0.5f, 0f, 0f);
        floorGO.layer = LayerMask.NameToLayer("Ground");
        var floorSr = floorGO.AddComponent<SpriteRenderer>();
        floorSr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/tile_ground.png");
        floorSr.sortingLayerName = "Terrain";
        floorSr.drawMode = SpriteDrawMode.Tiled;
        floorSr.size = new Vector2(floorWidth, 2f);
        var floorCol = floorGO.AddComponent<BoxCollider2D>();
        floorCol.size = new Vector2(floorWidth, 2f);

        DoorGate BuildGate(string label, float leverX, float doorX, bool latching)
        {
            var leverGO = new GameObject($"Lever_{label}");
            leverGO.transform.SetParent(container.transform, false);
            leverGO.transform.localPosition = new Vector3(leverX, 1.5f, 0f);
            var leverSr = leverGO.AddComponent<SpriteRenderer>();
            leverSr.sprite = _leverOffSprite;
            leverSr.sortingLayerName = "Hazard";
            leverGO.AddComponent<BoxCollider2D>();
            var lever = leverGO.AddComponent<TriggerLever>();
            SetPrivate(lever, "_spriteOff", _leverOffSprite);
            SetPrivate(lever, "_spriteOn", _leverOnSprite);

            var doorGO = new GameObject($"Door_{label}");
            doorGO.transform.SetParent(container.transform, false);
            doorGO.transform.localPosition = new Vector3(doorX, 1.5f, 0f);
            doorGO.layer = LayerMask.NameToLayer("Ground");
            var doorSr = doorGO.AddComponent<SpriteRenderer>();
            doorSr.sprite = _doorClosedSprite;
            doorSr.sortingLayerName = "Hazard";
            doorGO.transform.localScale = new Vector3(1f, 1.5f, 1f);
            doorGO.AddComponent<BoxCollider2D>();
            var door = doorGO.AddComponent<DoorGate>();
            SetPrivate(door, "_spriteClosed", _doorClosedSprite);
            SetPrivate(door, "_spriteOpen", _doorOpenSprite);
            SetPrivate(door, "_latching", latching);
            SetPrivate(lever, "_linkedDoor", door);
            return door;
        }

        BuildGate("A", lever1X, door1X, latching: true);
        var door2 = BuildGate("B", lever2X, door2X, latching: false);

        var exitGO = new GameObject("RoomExit");
        exitGO.transform.SetParent(container.transform, false);
        exitGO.transform.localPosition = new Vector3(exitX, 1f, 0f);
        var exitCol = exitGO.AddComponent<BoxCollider2D>();
        exitCol.size = new Vector2(1.5f, 3f);
        var exit = exitGO.AddComponent<RoomExit>();
        SetPrivate(exit, "_requiredDoor", door2);

        var spawnPoint = new GameObject("SpawnPoint").transform;
        spawnPoint.SetParent(container.transform, false);
        spawnPoint.localPosition = new Vector3(1f, 2f, 0f);

        var camAnchor = new GameObject("CameraAnchor").transform;
        camAnchor.SetParent(container.transform, false);
        camAnchor.localPosition = new Vector3(floorWidth * 0.4f, 1.5f, 0f);

        var data = ScriptableObject.CreateInstance<RoomData>();
        data.roomId = roomId;
        data.zoneId = 3;
        data.difficultyTier = 6;
        data.mechanic = PrimaryMechanic.DEPENDENCY;
        data.ecoCountRequired = 1;
        data.hasAltSolution = false;
        data.introRunMin = 3;
        AssetDatabase.CreateAsset(data, $"Assets/Rooms/{roomId}.asset");

        assembler.RegisterRoom(new RoomInstance
        {
            data = data,
            container = container,
            spawnPoint = spawnPoint,
            cameraAnchor = camAnchor,
        });
    }

    // GDD §6.2 Zona 3 "Abismo" — FRUSTRATION ("Eco Frustrado intencional"): el motor
    // actual reproduce ecos como posiciones puras sin colisión contra hazards (un eco
    // nunca muere), así que la sala usa eso a propósito en vez de simularlo: el
    // jugador aprende el timing seguro del TimedHazard arriesgando su propio cuerpo
    // en el loop 1, y en el loop 2 debe CONFIAR en que su eco (grabado cruzando en el
    // momento correcto) va a sostener la palanca cuando él llegue a la puerta — anticipar
    // el comportamiento de tu eco en vez de solo compartir espacio con él (SYNC).
    private static void BuildFrustrationRoom(RoomAssembler assembler, string roomId, float xOffset,
        float hazardStartX, float hazardWidth, float leverX, float doorX, float exitX)
    {
        var container = new GameObject($"Room_{roomId}");
        container.transform.position = new Vector3(xOffset, 0f, 0f);
        container.AddComponent<RoomVisualTheme>().backgroundColor = Z3BackgroundColor;

        float floorWidth = exitX + 4f;
        var floorGO = new GameObject("Floor");
        floorGO.transform.SetParent(container.transform, false);
        floorGO.transform.localPosition = new Vector3(floorWidth * 0.5f, 0f, 0f);
        floorGO.layer = LayerMask.NameToLayer("Ground");
        var floorSr = floorGO.AddComponent<SpriteRenderer>();
        floorSr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/tile_ground.png");
        floorSr.sortingLayerName = "Terrain";
        floorSr.drawMode = SpriteDrawMode.Tiled;
        floorSr.size = new Vector2(floorWidth, 2f);
        var floorCol = floorGO.AddComponent<BoxCollider2D>();
        floorCol.size = new Vector2(floorWidth, 2f);

        var hazardGO = new GameObject("TimedHazard");
        hazardGO.transform.SetParent(container.transform, false);
        hazardGO.transform.localPosition = new Vector3(hazardStartX + hazardWidth * 0.5f, 1f, 0f);
        hazardGO.layer = LayerMask.NameToLayer("Hazard");
        var hazardSr = hazardGO.AddComponent<SpriteRenderer>();
        hazardSr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/hazard.png");
        hazardSr.sortingLayerName = "Hazard";
        hazardSr.drawMode = SpriteDrawMode.Tiled;
        hazardSr.size = new Vector2(hazardWidth, 1f);
        var hazardCol = hazardGO.AddComponent<BoxCollider2D>();
        hazardCol.size = new Vector2(hazardWidth, 1f);
        hazardGO.AddComponent<TimedHazard>();

        var leverGO = new GameObject("Lever");
        leverGO.transform.SetParent(container.transform, false);
        leverGO.transform.localPosition = new Vector3(leverX, 1.5f, 0f);
        var leverSr = leverGO.AddComponent<SpriteRenderer>();
        leverSr.sprite = _leverOffSprite;
        leverSr.sortingLayerName = "Hazard";
        leverGO.AddComponent<BoxCollider2D>();
        var lever = leverGO.AddComponent<TriggerLever>();
        SetPrivate(lever, "_spriteOff", _leverOffSprite);
        SetPrivate(lever, "_spriteOn", _leverOnSprite);

        var doorGO = new GameObject("Door");
        doorGO.transform.SetParent(container.transform, false);
        doorGO.transform.localPosition = new Vector3(doorX, 1.5f, 0f);
        doorGO.layer = LayerMask.NameToLayer("Ground");
        var doorSr = doorGO.AddComponent<SpriteRenderer>();
        doorSr.sprite = _doorClosedSprite;
        doorSr.sortingLayerName = "Hazard";
        doorGO.transform.localScale = new Vector3(1f, 1.5f, 1f);
        doorGO.AddComponent<BoxCollider2D>();
        var door = doorGO.AddComponent<DoorGate>();
        SetPrivate(door, "_spriteClosed", _doorClosedSprite);
        SetPrivate(door, "_spriteOpen", _doorOpenSprite);
        SetPrivate(lever, "_linkedDoor", door);

        var exitGO = new GameObject("RoomExit");
        exitGO.transform.SetParent(container.transform, false);
        exitGO.transform.localPosition = new Vector3(exitX, 1f, 0f);
        var exitCol = exitGO.AddComponent<BoxCollider2D>();
        exitCol.size = new Vector2(1.5f, 3f);
        var exit = exitGO.AddComponent<RoomExit>();
        SetPrivate(exit, "_requiredDoor", door);

        var spawnPoint = new GameObject("SpawnPoint").transform;
        spawnPoint.SetParent(container.transform, false);
        spawnPoint.localPosition = new Vector3(1f, 2f, 0f);

        var camAnchor = new GameObject("CameraAnchor").transform;
        camAnchor.SetParent(container.transform, false);
        camAnchor.localPosition = new Vector3(floorWidth * 0.4f, 1.5f, 0f);

        var data = ScriptableObject.CreateInstance<RoomData>();
        data.roomId = roomId;
        data.zoneId = 3;
        data.difficultyTier = 7;
        data.mechanic = PrimaryMechanic.FRUSTRATION;
        data.ecoCountRequired = 1;
        data.hasAltSolution = false;
        data.introRunMin = 3;
        AssetDatabase.CreateAsset(data, $"Assets/Rooms/{roomId}.asset");

        assembler.RegisterRoom(new RoomInstance
        {
            data = data,
            container = container,
            spawnPoint = spawnPoint,
            cameraAnchor = camAnchor,
        });
    }

    // GDD §8.2 Boss 1 "El Espejo Fragmentado" (Zona 1), Fase 1 ("Primeros Reflejos"):
    // 3 paneles de espejo (E1/E2/E3), cada uno con su palanca — el jugador debe leer el
    // oscilador de cada panel (8s [VS], mitad alineado/mitad no) y coordinar 2 ecos +
    // su propio cuerpo para tener los 3 activos a la vez, luego pararse en el centro
    // 1s continuo. Fases 2-3 (contrapeso E4/E2, 5 paneles) quedan fuera de este pase.
    private static void BuildBossRoom(RoomAssembler assembler, string roomId, float xOffset)
    {
        var container = new GameObject($"Room_{roomId}");
        container.transform.position = new Vector3(xOffset, 0f, 0f);
        container.AddComponent<RoomVisualTheme>().backgroundColor = new Color(0.10f, 0.06f, 0.03f);

        const float e1X = 4f, e2X = 10f, centerX = 16f, e3X = 22f, floorWidth = 28f;

        var floorGO = new GameObject("Floor");
        floorGO.transform.SetParent(container.transform, false);
        floorGO.transform.localPosition = new Vector3(floorWidth * 0.5f, 0f, 0f);
        floorGO.layer = LayerMask.NameToLayer("Ground");
        var floorSr = floorGO.AddComponent<SpriteRenderer>();
        floorSr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/tile_ground.png");
        floorSr.sortingLayerName = "Terrain";
        floorSr.drawMode = SpriteDrawMode.Tiled;
        floorSr.size = new Vector2(floorWidth, 2f);
        var floorCol = floorGO.AddComponent<BoxCollider2D>();
        floorCol.size = new Vector2(floorWidth, 2f);

        var panels = new System.Collections.Generic.List<MirrorPanel>();
        void BuildPanelLever(string label, float x)
        {
            var leverGO = new GameObject($"Lever_{label}");
            leverGO.transform.SetParent(container.transform, false);
            leverGO.transform.localPosition = new Vector3(x, 1.5f, 0f);
            var leverSr = leverGO.AddComponent<SpriteRenderer>();
            leverSr.sprite = _leverOffSprite;
            leverSr.sortingLayerName = "Hazard";
            leverGO.AddComponent<BoxCollider2D>();
            var lever = leverGO.AddComponent<TriggerLever>();
            SetPrivate(lever, "_spriteOff", _leverOffSprite);
            SetPrivate(lever, "_spriteOn", _leverOnSprite);

            var panelGO = new GameObject($"Panel_{label}");
            panelGO.transform.SetParent(container.transform, false);
            panelGO.transform.localPosition = new Vector3(x, 3.2f, 0f);
            var panelSr = panelGO.AddComponent<SpriteRenderer>();
            panelSr.sprite = _doorClosedSprite;
            panelSr.sortingLayerName = "Hazard";
            var panel = panelGO.AddComponent<MirrorPanel>();
            SetPrivate(panel, "_lever", lever);
            panels.Add(panel);
        }

        BuildPanelLever("E1", e1X);
        BuildPanelLever("E2", e2X);
        BuildPanelLever("E3", e3X);

        var centerGO = new GameObject("CenterTrigger");
        centerGO.transform.SetParent(container.transform, false);
        centerGO.transform.localPosition = new Vector3(centerX, 1f, 0f);
        var centerCol = centerGO.AddComponent<BoxCollider2D>();
        centerCol.size = new Vector2(1f, 3f);
        var centerTrigger = centerGO.AddComponent<BossCenterTrigger>();
        var centerSr = centerGO.AddComponent<SpriteRenderer>();
        centerSr.sprite = _leverOnSprite;
        centerSr.sortingLayerName = "Hazard";
        centerSr.color = new Color(1f, 0.9f, 0.4f, 0.6f);

        var bossGO = new GameObject("BossController");
        bossGO.transform.SetParent(container.transform, false);
        var boss = bossGO.AddComponent<BossController>();
        SetPrivateField(boss, "_panels", panels.ToArray());
        SetPrivate(boss, "_centerTrigger", centerTrigger);

        var spawnPoint = new GameObject("SpawnPoint").transform;
        spawnPoint.SetParent(container.transform, false);
        spawnPoint.localPosition = new Vector3(1f, 2f, 0f);

        var camAnchor = new GameObject("CameraAnchor").transform;
        camAnchor.SetParent(container.transform, false);
        camAnchor.localPosition = new Vector3(floorWidth * 0.4f, 1.5f, 0f);

        var data = ScriptableObject.CreateInstance<RoomData>();
        data.roomId = roomId;
        data.zoneId = 1;
        data.difficultyTier = 8;
        data.mechanic = PrimaryMechanic.SYNC;
        data.ecoCountRequired = 2;
        data.hasAltSolution = false;
        data.introRunMin = 1;
        AssetDatabase.CreateAsset(data, $"Assets/Rooms/{roomId}.asset");

        assembler.RegisterBossRoom(new RoomInstance
        {
            data = data,
            container = container,
            spawnPoint = spawnPoint,
            cameraAnchor = camAnchor,
        });
    }

    public static void BuildPlayerExe()
    {
        Debug.Log("[VSSceneBuilder] Building Windows standalone player...");

        PlayerSettings.productName = "PHASE — Vertical Slice";
        PlayerSettings.companyName = "KelvisStudio";
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        // Active Input Handling ("Both") is already set directly in ProjectSettings.asset
        // (activeInputHandler: 2) — no reliable public scripting API for this across versions.

        Directory.CreateDirectory("Build");

        var options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = "Build/PhaseVS.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);
        Debug.Log("[VSSceneBuilder] Build result: " + report.summary.result + " | Errors: " + report.summary.totalErrors + " | Size: " + report.summary.totalSize);

        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.LogError("[VSSceneBuilder] BUILD FAILED");
        }
    }

    // Fase 10: arte pixel real (Assets/ArtSource/, hecho a mano pixel por pixel siguiendo
    // la paleta de la Fase 6 — jugador #D8E4F0, sombra #8AA0BC, acento #4FFFCE, peligro
    // #8B2030) en vez de los rectángulos de color placeholder de CreateSolidSprite.
    private static Sprite ImportRealSprite(string sourceName, string destPath, int ppu)
    {
        string sourcePath = $"Assets/ArtSource/{sourceName}";
        File.Copy(sourcePath, destPath, true);
        AssetDatabase.ImportAsset(destPath);

        var importer = (TextureImporter)AssetImporter.GetAtPath(destPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = ppu;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(destPath);
    }

    // Arte de UI de alta resolución (keyart), no pixel art: filtrado bilinear y
    // compresión normal, a diferencia de ImportRealSprite (sprites de gameplay).
    private static Sprite ImportUISprite(string sourceName, string destPath)
    {
        string sourcePath = $"Assets/ArtSource/{sourceName}";
        File.Copy(sourcePath, destPath, true);
        AssetDatabase.ImportAsset(destPath);

        var importer = (TextureImporter)AssetImporter.GetAtPath(destPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 100;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Compressed;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(destPath);
    }

    private static Sprite CreateSolidSprite(string path, Color32 color, int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var pixels = new Color32[w * h];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
        tex.SetPixels32(pixels);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(path);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = PPU;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static BoxCollider2D AddGroundBox(Transform parent, string name, float centerX, float centerY, float width, float height)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(centerX, centerY, 0f);
        go.layer = LayerMask.NameToLayer("Ground");
        var box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(width, height);
        return box;
    }

    private static TileBase CreateTile(string path, Sprite sprite)
    {
        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        tile.color = Color.white;
        // Explicit Grid collider shape: a full-cell square, independent of whatever physics
        // outline (or lack of one) the sprite importer generated for a solid placeholder
        // texture. Sprite-based collider type depends on that outline existing; Grid doesn't.
        tile.colliderType = Tile.ColliderType.Grid;
        AssetDatabase.CreateAsset(tile, path);
        return tile;
    }

    private static void SetPrivate(Object target, string fieldName, Object value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        if (prop == null) { Debug.LogError($"[VSSceneBuilder] Field '{fieldName}' not found on {target.GetType().Name}"); return; }
        prop.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetPrivate(Object target, string fieldName, LayerMask value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        if (prop == null) { Debug.LogError($"[VSSceneBuilder] Field '{fieldName}' not found on {target.GetType().Name}"); return; }
        prop.intValue = value.value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetPrivate(Object target, string fieldName, bool value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        if (prop == null) { Debug.LogError($"[VSSceneBuilder] Field '{fieldName}' not found on {target.GetType().Name}"); return; }
        prop.boolValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // Para tipos que SerializedProperty no cubre bien acá (arrays de componentes) —
    // reflexión directa en vez de un overload de SerializedObject por cada caso.
    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field == null) { Debug.LogError($"[VSSceneBuilder] Field '{fieldName}' not found on {target.GetType().Name}"); return; }
        field.SetValue(target, value);
    }
}
