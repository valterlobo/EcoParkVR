#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Script de Editor que monta automaticamente toda a cena EcoPark VR.
/// 
/// USO: No menu do Unity, clique em:
/// EcoPark > Build Scene Automatically
/// 
/// Isso ira criar todos os GameObjects, configurar componentes,
/// materiais, UI e hierarquia completa da cena.
/// </summary>
public class EcoParkSceneBuilder : EditorWindow
{
    [MenuItem("EcoPark/Build Scene Automatically")]
    public static void BuildScene()
    {
        if (!EditorUtility.DisplayDialog("EcoPark Scene Builder",
            "Isso vai criar toda a cena EcoPark VR automaticamente.\n\n" +
            "Recomendado em uma cena NOVA (File > New Scene).\n\n" +
            "Continuar?", "Sim, Criar!", "Cancelar"))
            return;

        EcoParkSceneBuilder builder = new EcoParkSceneBuilder();
        builder.Build();
        Debug.Log("[EcoPark] Cena criada com sucesso! Leia o README para os proximos passos.");
        EditorUtility.DisplayDialog("Sucesso!",
            "Cena EcoPark VR criada!\n" +
            "UI vinculada automaticamente!\n\n" +
            "Proximos passos:\n" +
            "1. Instale o Meta XR SDK (Package Manager)\n" +
            "2. Substitua XR Origin pelo OVRCameraRig\n" +
            "3. Adicione sons nas lixeiras\n" +
            "4. Pressione Play para testar!\n\n" +
            "Para re-vincular UI: EcoPark > Auto-Wire UI", "Jogar!");
    }

    [MenuItem("EcoPark/Create Materials")]
    public static void CreateMaterialsMenu()
    {
        EcoParkSceneBuilder builder = new EcoParkSceneBuilder();
        builder.CreateAllMaterials();
        AssetDatabase.SaveAssets();
        Debug.Log("[EcoPark] Materiais criados em Assets/Materials/");
    }

    // ─── MATERIAIS ───────────────────────────────────────────
    Material _groundMat, _pathMat, _trunkMat, _leavesMat, _benchMat;
    Material _blueBinMat, _redBinMat, _greenBinMat;
    Material _plasticMat, _paperMat, _glassMat;
    Material _highlightMat;

    void CreateAllMaterials()
    {
        System.IO.Directory.CreateDirectory("Assets/Materials");

        _groundMat   = CreateMat("Ground",     new Color(0.24f, 0.55f, 0.18f));
        _pathMat     = CreateMat("Path",       new Color(0.65f, 0.60f, 0.52f));
        _trunkMat    = CreateMat("Trunk",      new Color(0.40f, 0.25f, 0.12f));
        _leavesMat   = CreateMat("Leaves",     new Color(0.15f, 0.48f, 0.10f));
        _benchMat    = CreateMat("Bench",      new Color(0.55f, 0.35f, 0.15f));
        _blueBinMat  = CreateMat("BlueBin",    new Color(0.10f, 0.35f, 0.85f));
        _redBinMat   = CreateMat("RedBin",     new Color(0.85f, 0.10f, 0.10f));
        _greenBinMat = CreateMat("GreenBin",   new Color(0.10f, 0.65f, 0.20f));
        _plasticMat  = CreateMat("Plastic",    new Color(0.20f, 0.75f, 0.95f));
        _paperMat    = CreateMat("Paper",      new Color(0.92f, 0.88f, 0.75f));
        _glassMat    = CreateMat("Glass",      new Color(0.60f, 0.90f, 0.65f));
        _highlightMat = CreateMat("Highlight", new Color(1.0f,  0.90f, 0.10f));

        AssetDatabase.SaveAssets();
    }

    Material CreateMat(string name, Color color)
    {
        string path = $"Assets/Materials/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.color = color;
        return mat;
    }

    // ─── BUILD PRINCIPAL ─────────────────────────────────────
    public void Build()
    {
        CreateAllMaterials();
        AddLayer("Interactable");

        // ── Raiz da cena ──
        GameObject root = new GameObject("=== EcoParkVR Scene ===");

        // ── Luz direcional ──
        SetupLighting();

        // ── XR Origin (simulado) ──
        GameObject player = BuildPlayer(root);

        // ── Ambiente ──
        GameObject env = new GameObject("Environment");
        env.transform.parent = root.transform;
        BuildGround(env);
        BuildPath(env);
        BuildTrees(env);
        BuildBench(env);

        // ── Sistema de reciclagem ──
        GameObject recycling = new GameObject("RecyclingSystem");
        recycling.transform.parent = root.transform;

        GameObject trashItems = new GameObject("TrashItems");
        trashItems.transform.parent = recycling.transform;
        BuildTrashItems(trashItems);

        GameObject bins = new GameObject("Bins");
        bins.transform.parent = recycling.transform;
        BuildRecycleBins(bins);

        // ── UI ──
        GameObject ui = new GameObject("UI");
        ui.transform.parent = root.transform;
        BuildUI(ui, player);

        // ── Game Manager ──
        BuildGameManager(root);

        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);
    }

    // ─── ILUMINACAO ──────────────────────────────────────────
    void SetupLighting()
    {
        // Remove luzes existentes
        Light[] lights = FindObjectsOfType<Light>();
        foreach (var l in lights)
        {
            if (l.type == LightType.Directional)
                Object.DestroyImmediate(l.gameObject);
        }

        GameObject sunGO = new GameObject("Sun_Directional");
        Light sun = sunGO.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.2f;
        sun.color = new Color(1f, 0.95f, 0.85f);
        sun.shadows = LightShadows.Soft;
        sunGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor    = new Color(0.5f, 0.7f, 1.0f);
        RenderSettings.ambientEquatorColor = new Color(0.4f, 0.5f, 0.4f);
        RenderSettings.ambientGroundColor  = new Color(0.2f, 0.3f, 0.1f);
    }

    // ─── PLAYER ──────────────────────────────────────────────
    GameObject BuildPlayer(GameObject parent)
    {
        // Cria um XR Origin basico — sera substituido pelo Meta XR Origin
        GameObject xrOrigin = new GameObject("XR Origin");
        xrOrigin.transform.parent = parent.transform;
        xrOrigin.transform.position = new Vector3(0, 0, 0);

        // Camera Offset
        GameObject camOffset = new GameObject("Camera Floor Offset");
        camOffset.transform.parent = xrOrigin.transform;
        camOffset.transform.localPosition = new Vector3(0, 1.6f, 0);

        // Main Camera
        GameObject camGO = new GameObject("Main Camera");
        camGO.transform.parent = camOffset.transform;
        camGO.transform.localPosition = Vector3.zero;
        Camera cam = camGO.AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.nearClipPlane = 0.01f;

        // Audio Listener na camera
        camGO.AddComponent<AudioListener>();

        // TrackedPoseDriver — atualiza posicao/rotacao da camera pelo headset XR
        // Tenta adicionar via reflection para nao exigir using direto do InputSystem
        TryAddTrackedPoseDriver(camGO);

        // PlayerInteraction na camera
        PlayerInteraction pi = camGO.AddComponent<PlayerInteraction>();
        pi.interactionDistance = 4f;
        pi.interactableLayer = LayerMask.GetMask("Interactable");

        // HeldItemFollower
        GameObject holderGO = new GameObject("ItemHolder");
        holderGO.transform.parent = camGO.transform;
        holderGO.AddComponent<HeldItemFollower>();

        // CharacterController para movimento basico (opcional)
        CharacterController cc = xrOrigin.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.center = new Vector3(0, 0.9f, 0);
        cc.radius = 0.3f;

        Debug.Log("[EcoPark] XR Origin criado. Substitua pelo Meta XR Origin (Action-based) apos instalar o SDK.");
        return xrOrigin;
    }

    // ─── AMBIENTE ─────────────────────────────────────────────
    void BuildGround(GameObject parent)
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground_Grass";
        ground.transform.parent = parent.transform;
        ground.transform.localScale = new Vector3(5f, 1f, 5f);
        ground.transform.position = Vector3.zero;
        ApplyMat(ground, _groundMat);
    }

    void BuildPath(GameObject parent)
    {
        // Caminho central
        GameObject path = GameObject.CreatePrimitive(PrimitiveType.Cube);
        path.name = "Path_Main";
        path.transform.parent = parent.transform;
        path.transform.position = new Vector3(0, 0.01f, 0);
        path.transform.localScale = new Vector3(2f, 0.02f, 40f);
        ApplyMat(path, _pathMat);

        // Caminho transversal
        GameObject path2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        path2.name = "Path_Cross";
        path2.transform.parent = parent.transform;
        path2.transform.position = new Vector3(0, 0.01f, -5f);
        path2.transform.localScale = new Vector3(20f, 0.02f, 2f);
        ApplyMat(path2, _pathMat);
    }

    void BuildTrees(GameObject parent)
    {
        Vector3[] positions = {
            new Vector3(-8f, 0, 8f),
            new Vector3(8f, 0, 8f),
            new Vector3(-8f, 0, -8f),
            new Vector3(8f, 0, -8f),
            new Vector3(-12f, 0, 0f),
            new Vector3(12f, 0, 0f),
        };

        for (int i = 0; i < positions.Length; i++)
        {
            GameObject tree = new GameObject($"Tree_{i + 1:00}");
            tree.transform.parent = parent.transform;
            tree.transform.position = positions[i];

            // Tronco
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.parent = tree.transform;
            trunk.transform.localPosition = new Vector3(0, 1.2f, 0);
            trunk.transform.localScale = new Vector3(0.3f, 1.2f, 0.3f);
            ApplyMat(trunk, _trunkMat);

            // Copa
            GameObject leaves = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leaves.name = "Leaves";
            leaves.transform.parent = tree.transform;
            leaves.transform.localPosition = new Vector3(0, 3.2f, 0);
            leaves.transform.localScale = new Vector3(2.5f, 2.5f, 2.5f);
            ApplyMat(leaves, _leavesMat);
        }
    }

    void BuildBench(GameObject parent)
    {
        // Banco de praca simples
        GameObject bench = new GameObject("Bench_01");
        bench.transform.parent = parent.transform;
        bench.transform.position = new Vector3(-5f, 0, 3f);
        bench.transform.rotation = Quaternion.Euler(0, 45f, 0);

        // Assento
        GameObject seat = GameObject.CreatePrimitive(PrimitiveType.Cube);
        seat.name = "Seat";
        seat.transform.parent = bench.transform;
        seat.transform.localPosition = new Vector3(0, 0.5f, 0);
        seat.transform.localScale = new Vector3(2f, 0.1f, 0.6f);
        ApplyMat(seat, _benchMat);

        // Encosto
        GameObject back = GameObject.CreatePrimitive(PrimitiveType.Cube);
        back.name = "Back";
        back.transform.parent = bench.transform;
        back.transform.localPosition = new Vector3(0, 0.85f, -0.25f);
        back.transform.localScale = new Vector3(2f, 0.6f, 0.08f);
        ApplyMat(back, _benchMat);

        // Pes
        Vector3[] legPos = { new Vector3(-0.8f, 0.25f, 0), new Vector3(0.8f, 0.25f, 0) };
        foreach (var lp in legPos)
        {
            GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leg.name = "Leg";
            leg.transform.parent = bench.transform;
            leg.transform.localPosition = lp;
            leg.transform.localScale = new Vector3(0.08f, 0.5f, 0.6f);
            ApplyMat(leg, _benchMat);
        }
    }

    // ─── LIXO ─────────────────────────────────────────────────
    void BuildTrashItems(GameObject parent)
    {
        // Garrafa Plastica
        BuildTrash(parent, "PlasticBottle",  TrashType.Plastic,
                   PrimitiveType.Capsule, new Vector3(-3f, 0.4f, -2f),
                   new Vector3(0.2f, 0.4f, 0.2f), _plasticMat);

        // Garrafa Plastica 2
        BuildTrash(parent, "PlasticBottle_02", TrashType.Plastic,
                   PrimitiveType.Capsule, new Vector3(4f, 0.4f, 3f),
                   new Vector3(0.2f, 0.4f, 0.2f), _plasticMat);

        // Jornal
        BuildTrash(parent, "Newspaper",  TrashType.Paper,
                   PrimitiveType.Cube, new Vector3(2f, 0.05f, -4f),
                   new Vector3(0.4f, 0.05f, 0.3f), _paperMat);

        // Caixa de Papel
        BuildTrash(parent, "PaperBox",  TrashType.Paper,
                   PrimitiveType.Cube, new Vector3(-5f, 0.15f, -6f),
                   new Vector3(0.3f, 0.3f, 0.25f), _paperMat);

        // Garrafa de Vidro
        BuildTrash(parent, "GlassBottle",  TrashType.Glass,
                   PrimitiveType.Cylinder, new Vector3(5f, 0.35f, -3f),
                   new Vector3(0.15f, 0.35f, 0.15f), _glassMat);
    }

    void BuildTrash(GameObject parent, string itemName, TrashType type,
                    PrimitiveType shape, Vector3 pos, Vector3 scale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(shape);
        go.name = itemName;
        go.transform.parent = parent.transform;
        go.transform.position = pos;
        go.transform.localScale = scale;
        go.transform.rotation = Quaternion.Euler(
            Random.Range(-15f, 15f), Random.Range(0f, 360f), Random.Range(-15f, 15f));

        ApplyMat(go, mat);

        // Rigidbody
        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.mass = 0.3f;

        // Layer interagivel
        go.layer = LayerMask.NameToLayer("Interactable");
        go.tag = "TrashItem";

        // Componente TrashItem
        TrashItem ti = go.AddComponent<TrashItem>();
        ti.trashType = type;
        ti.highlightMaterial = _highlightMat;
    }

    // ─── LIXEIRAS ─────────────────────────────────────────────
    void BuildRecycleBins(GameObject parent)
    {
        BuildBin(parent, "BlueBin_Paper",   TrashType.Paper,
                 new Vector3(-6f, 0, -8f),  _blueBinMat,  "Papel");

        BuildBin(parent, "RedBin_Plastic",  TrashType.Plastic,
                 new Vector3(0f, 0, -10f),  _redBinMat,   "Plastico");

        BuildBin(parent, "GreenBin_Glass",  TrashType.Glass,
                 new Vector3(6f, 0, -8f),   _greenBinMat, "Vidro");
    }

    void BuildBin(GameObject parent, string binName, TrashType type,
                  Vector3 pos, Material mat, string label)
    {
        GameObject bin = new GameObject(binName);
        bin.transform.parent = parent.transform;
        bin.transform.position = pos;
        bin.tag = "RecycleBin";

        // Corpo da lixeira
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.name = "Body";
        body.transform.parent = bin.transform;
        body.transform.localPosition = new Vector3(0, 0.6f, 0);
        body.transform.localScale = new Vector3(0.7f, 0.6f, 0.7f);
        ApplyMat(body, mat);

        // Tampa da lixeira
        GameObject lid = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        lid.name = "Lid";
        lid.transform.parent = bin.transform;
        lid.transform.localPosition = new Vector3(0, 1.22f, 0);
        lid.transform.localScale = new Vector3(0.75f, 0.05f, 0.75f);
        ApplyMat(lid, mat);

        // Adesivo de identificacao
        GameObject sticker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sticker.name = "TypeSticker";
        sticker.transform.parent = bin.transform;
        sticker.transform.localPosition = new Vector3(0, 0.65f, 0.36f);
        sticker.transform.localScale = new Vector3(0.45f, 0.25f, 0.01f);

        Material stickerMat = new Material(Shader.Find("Standard"));
        stickerMat.color = Color.white;
        sticker.GetComponent<Renderer>().sharedMaterial = stickerMat;

        // ─── TRIGGER ZONE ──
        // Box trigger maior que a lixeira — detecta quando item se aproxima
        BoxCollider trigger = bin.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(1.5f, 2f, 1.5f);
        trigger.center = new Vector3(0, 0.6f, 0);

        // Componente RecycleBin
        RecycleBin rb = bin.AddComponent<RecycleBin>();
        rb.acceptedType = type;
        rb.binLabel = label;

        // Label flutuante (3D Text via Canvas)
        BuildBinLabel(bin, label, mat.color);
    }

    void BuildBinLabel(GameObject parent, string labelText, Color color)
    {
        GameObject labelGO = new GameObject("BinLabel_Canvas");
        labelGO.transform.parent = parent.transform;
        labelGO.transform.localPosition = new Vector3(0, 1.6f, 0);
        labelGO.transform.localScale = new Vector3(0.005f, 0.005f, 0.005f);

        Canvas canvas = labelGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        RectTransform rt = labelGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200, 60);

        // Billboard — sempre olha para a camera
        labelGO.AddComponent<BillboardCanvas>();
    }

    // ─── UI ───────────────────────────────────────────────────
    void BuildUI(GameObject parent, GameObject player)
    {
        Camera cam = player.GetComponentInChildren<Camera>();

        // ── Score Canvas ──
        GameObject scoreCanvasGO = CreateWorldCanvas("ScoreCanvas",
            cam.transform, new Vector3(0, 0.3f, 2f), new Vector2(400, 100));

        // Score Text
        GameObject scoreTextGO = new GameObject("ScoreText");
        scoreTextGO.transform.parent = scoreCanvasGO.transform;
        scoreTextGO.transform.localPosition = new Vector3(-80, 0, 0);
        var scoreTMP = scoreTextGO.AddComponent<TextMeshProUGUI>();
        scoreTMP.text = "Pontos: 0";
        scoreTMP.fontSize = 28;
        scoreTMP.color = Color.white;
        scoreTMP.fontStyle = FontStyles.Bold;
        SetRectTransform(scoreTextGO, new Vector2(180, 60));

        // Progress Text
        GameObject progTextGO = new GameObject("ProgressText");
        progTextGO.transform.parent = scoreCanvasGO.transform;
        progTextGO.transform.localPosition = new Vector3(80, 0, 0);
        var progTMP = progTextGO.AddComponent<TextMeshProUGUI>();
        progTMP.text = "Reciclados: 0/5";
        progTMP.fontSize = 22;
        progTMP.color = new Color(0.7f, 1f, 0.7f);
        SetRectTransform(progTextGO, new Vector2(180, 60));

        // ── Held Item Canvas ──
        GameObject heldCanvasGO = CreateWorldCanvas("HeldItemCanvas",
            cam.transform, new Vector3(0, -0.2f, 1.8f), new Vector2(350, 80));
        heldCanvasGO.SetActive(false);

        GameObject heldBg = new GameObject("HeldBG");
        heldBg.transform.parent = heldCanvasGO.transform;
        Image heldImg = heldBg.AddComponent<Image>();
        heldImg.color = new Color(0, 0, 0, 0.65f);
        SetRectTransform(heldBg, new Vector2(340, 72));

        GameObject heldTextGO = new GameObject("HeldText");
        heldTextGO.transform.parent = heldCanvasGO.transform;
        heldTextGO.transform.localPosition = Vector3.zero;
        var heldTMP = heldTextGO.AddComponent<TextMeshProUGUI>();
        heldTMP.text = "Segurando: Plastico\nDepositar na Lixeira VERMELHA";
        heldTMP.fontSize = 18;
        heldTMP.color = Color.white;
        heldTMP.alignment = TextAlignmentOptions.Center;
        SetRectTransform(heldTextGO, new Vector2(330, 68));

        // ── Feedback Canvas ──
        GameObject feedbackCanvasGO = CreateWorldCanvas("FeedbackCanvas",
            cam.transform, new Vector3(0, 0.0f, 2f), new Vector2(500, 100));
        feedbackCanvasGO.SetActive(false);

        GameObject feedbackBG = new GameObject("FeedbackBG");
        feedbackBG.transform.parent = feedbackCanvasGO.transform;
        Image bgImg = feedbackBG.AddComponent<Image>();
        bgImg.color = new Color(0, 0, 0, 0.7f);
        SetRectTransform(feedbackBG, new Vector2(490, 90));

        GameObject feedbackTextGO = new GameObject("FeedbackText");
        feedbackTextGO.transform.parent = feedbackCanvasGO.transform;
        feedbackTextGO.transform.localPosition = Vector3.zero;
        var feedbackTMP = feedbackTextGO.AddComponent<TextMeshProUGUI>();
        feedbackTMP.text = "";
        feedbackTMP.fontSize = 26;
        feedbackTMP.color = Color.green;
        feedbackTMP.fontStyle = FontStyles.Bold;
        feedbackTMP.alignment = TextAlignmentOptions.Center;
        SetRectTransform(feedbackTextGO, new Vector2(480, 80));

        // ── Win Canvas ──
        GameObject winCanvasGO = CreateWorldCanvas("WinCanvas",
            cam.transform, new Vector3(0, 0.1f, 2.5f), new Vector2(600, 300));
        winCanvasGO.SetActive(false);

        GameObject winBG = new GameObject("WinBG");
        winBG.transform.parent = winCanvasGO.transform;
        Image winImg = winBG.AddComponent<Image>();
        winImg.color = new Color(0.05f, 0.25f, 0.05f, 0.92f);
        SetRectTransform(winBG, new Vector2(590, 290));

        GameObject winTextGO = new GameObject("FinalScoreText");
        winTextGO.transform.parent = winCanvasGO.transform;
        winTextGO.transform.localPosition = new Vector3(0, 20, 0);
        var winTMP = winTextGO.AddComponent<TextMeshProUGUI>();
        winTMP.text = "Parabens!\nVoce reciclou tudo!";
        winTMP.fontSize = 24;
        winTMP.color = Color.white;
        winTMP.alignment = TextAlignmentOptions.Center;
        SetRectTransform(winTextGO, new Vector2(570, 220));

        // Botao Reiniciar
        GameObject btnGO = new GameObject("RestartButton");
        btnGO.transform.parent = winCanvasGO.transform;
        btnGO.transform.localPosition = new Vector3(0, -100, 0);
        Image btnImg = btnGO.AddComponent<Image>();
        btnImg.color = new Color(0.2f, 0.6f, 0.2f);
        SetRectTransform(btnGO, new Vector2(200, 50));

        Button btn = btnGO.AddComponent<Button>();

        GameObject btnTextGO = new GameObject("BtnText");
        btnTextGO.transform.parent = btnGO.transform;
        var btnTMP = btnTextGO.AddComponent<TextMeshProUGUI>();
        btnTMP.text = "Jogar Novamente";
        btnTMP.fontSize = 20;
        btnTMP.color = Color.white;
        btnTMP.alignment = TextAlignmentOptions.Center;
        SetRectTransform(btnTextGO, new Vector2(190, 44));

        parent.transform.parent = cam.transform;

        // Todos os canvas como filhos da camera
        scoreCanvasGO.transform.parent = cam.transform;
        heldCanvasGO.transform.parent = cam.transform;
        feedbackCanvasGO.transform.parent = cam.transform;
        winCanvasGO.transform.parent = cam.transform;

        // Guarda referencias para o GameManager
        EditorPrefs.SetString("ecopark_score",    GetPath(scoreTextGO));
        EditorPrefs.SetString("ecopark_progress", GetPath(progTextGO));
        EditorPrefs.SetString("ecopark_held",     GetPath(heldTextGO));
        EditorPrefs.SetString("ecopark_feedback_panel", GetPath(feedbackCanvasGO));
        EditorPrefs.SetString("ecopark_feedback_text",  GetPath(feedbackTextGO));
        EditorPrefs.SetString("ecopark_win_panel",      GetPath(winCanvasGO));
        EditorPrefs.SetString("ecopark_win_text",       GetPath(winTextGO));
        EditorPrefs.SetString("ecopark_held_panel",     GetPath(heldCanvasGO));
    }

    string GetPath(GameObject go)
    {
        return go.name; // Simplified — use FindObjectOfType in GameManager
    }

    GameObject CreateWorldCanvas(string name, Transform parent, Vector3 localPos, Vector2 size)
    {
        GameObject canvasGO = new GameObject(name);
        canvasGO.transform.parent = parent;
        canvasGO.transform.localPosition = localPos;
        canvasGO.transform.localRotation = Quaternion.identity;
        canvasGO.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        RectTransform rt = canvasGO.GetComponent<RectTransform>();
        rt.sizeDelta = size;

        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        return canvasGO;
    }

    void SetRectTransform(GameObject go, Vector2 size)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.localPosition = rt.localPosition; // force update
        rt.localScale = Vector3.one;
    }

    // ─── GAME MANAGER ─────────────────────────────────────────
    void BuildGameManager(GameObject parent)
    {
        // TrackedPoseDriverSetup — garante que a camera e rastreada pelo headset
        // Adicionado no root para rodar antes do XROrigin.Awake()
        parent.AddComponent<TrackedPoseDriverSetup>();

        GameObject gmGO = new GameObject("GameManager");
        gmGO.transform.parent = parent.transform;
        gmGO.AddComponent<GameManager>();

        // Adiciona o UIAutoWirer — vincula tudo automaticamente
        UIAutoWirer wirer = gmGO.AddComponent<UIAutoWirer>();
        wirer.verbose = true;

        // Executa a vinculação imediatamente no Editor
        // (um frame de delay para os objetos de UI já existirem)
        EditorApplication.delayCall += () =>
        {
            if (wirer != null)
            {
                wirer.WireAll();
                EditorUtility.SetDirty(gmGO);
                Debug.Log("[EcoPark] UIAutoWirer executado — todos os campos de UI vinculados!");
            }
        };
    }

    // ─── UTILITARIOS ─────────────────────────────────────────
    void ApplyMat(GameObject go, Material mat)
    {
        Renderer r = go.GetComponent<Renderer>();
        if (r != null && mat != null)
            r.sharedMaterial = mat;
    }

    static void AddLayer(string layerName)
    {
        var tagManager = new SerializedObject(
            AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/TagManager.asset"));
        var layers = tagManager.FindProperty("layers");

        bool exists = false;
        for (int i = 8; i < layers.arraySize; i++)
        {
            var layer = layers.GetArrayElementAtIndex(i);
            if (layer.stringValue == layerName) { exists = true; break; }
        }

        if (!exists)
        {
            for (int i = 8; i < layers.arraySize; i++)
            {
                var layer = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(layer.stringValue))
                {
                    layer.stringValue = layerName;
                    tagManager.ApplyModifiedProperties();
                    Debug.Log($"[EcoPark] Layer '{layerName}' adicionada (indice {i}).");
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Adiciona TrackedPoseDriver na camera usando reflection.
    /// Funciona com InputSystem instalado (via dependencia do OpenXR)
    /// sem precisar de using direto no arquivo.
    /// </summary>
    static void TryAddTrackedPoseDriver(GameObject camGO)
    {
        // Tenta UnityEngine.InputSystem.XR.TrackedPoseDriver (Input System 1.x)
        System.Type tpdType =
            System.Type.GetType("UnityEngine.InputSystem.XR.TrackedPoseDriver, Unity.InputSystem") ??
            System.Type.GetType("UnityEngine.InputSystem.XR.TrackedPoseDriver, UnityEngine.InputSystem") ??
            System.Type.GetType("UnityEngine.SpatialTracking.TrackedPoseDriver, UnityEngine.SpatialTracking");

        if (tpdType != null)
        {
            var tpd = camGO.AddComponent(tpdType);

            // Tenta configurar trackingType = RotationAndPosition (0)
            var trackingProp = tpdType.GetProperty("trackingType");
            if (trackingProp != null)
            {
                var enumVal = System.Enum.ToObject(trackingProp.PropertyType, 0);
                trackingProp.SetValue(tpd, enumVal);
            }

            Debug.Log("[EcoPark] TrackedPoseDriver adicionado: " + tpdType.FullName);
        }
        else
        {
            Debug.LogWarning("[EcoPark] TrackedPoseDriver nao encontrado. " +
                "Adicione manualmente na Main Camera apos instalar o Meta XR SDK.");
        }
    }

}
#endif
