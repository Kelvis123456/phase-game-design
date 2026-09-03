using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

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
        Sprite groundSprite = CreateSolidSprite("Assets/Art/tile_ground.png", new Color32(58, 66, 84, 255), 16, 16);
        Sprite playerSprite = CreateSolidSprite("Assets/Art/player.png", new Color32(232, 238, 248, 255), 16, 32);
        Sprite hazardSprite = CreateSolidSprite("Assets/Art/hazard.png", new Color32(200, 60, 60, 255), 16, 16);

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

        var roomControllerGO = new GameObject("VSRoomController");
        var roomController = roomControllerGO.AddComponent<VSRoomController>();
        SetPrivate(roomController, "_playerSpawnPoint", spawnGO.transform);
        SetPrivate(roomController, "_echoManager", echoManager);
        SetPrivate(roomController, "_recorder", recorder);
        SetPrivate(roomController, "_loopTimer", loopTimer);
        SetPrivate(roomController, "_deathFlash", flashImg);

        // ---- Save scene, register in build settings ----
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[VSSceneBuilder] Scene build complete: " + ScenePath);
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
}
