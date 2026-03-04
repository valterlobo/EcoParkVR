#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.XR;

/// <summary>
/// Detecta automaticamente qual SDK XR está instalado e substitui
/// o XR Origin placeholder pelo prefab correto, já configurado.
///
/// Menu: EcoPark → Setup XR Origin
///
/// Suporte:
/// - Meta XR SDK (OVRCameraRig)      → detectado via tipo OVRCameraRig
/// - XR Interaction Toolkit (Action-based) → detectado via XROrigin
/// - Fallback manual                 → câmera com PlayerInteraction
/// </summary>
public static class XROriginSetup
{
    // ─── MENU ENTRIES ─────────────────────────────────────────────
    [MenuItem("EcoPark/Setup XR Origin")]
    public static void RunSetup()
    {
        XRSDKType sdk = DetectInstalledSDK();
        ShowSetupDialog(sdk);
    }

    [MenuItem("EcoPark/Diagnostics/Detect Installed SDK")]
    public static void DiagnoseSDK()
    {
        XRSDKType sdk = DetectInstalledSDK();
        string msg = sdk switch
        {
            XRSDKType.MetaXR   => "✅ Meta XR SDK detectado!\nOVRCameraRig disponível.",
            XRSDKType.XRToolkit => "✅ XR Interaction Toolkit detectado!\nXROrigin (Action-based) disponível.",
            _                  => "⚠️ Nenhum SDK XR completo encontrado.\nSerá criado um XR Origin manual."
        };
        EditorUtility.DisplayDialog("EcoPark — SDK Detector", msg + 
            "\n\nExecute: EcoPark → Setup XR Origin", "OK");
        Debug.Log($"[EcoPark] SDK detectado: {sdk}");
    }

    // ─── DETECÇÃO DE SDK ──────────────────────────────────────────
    enum XRSDKType { MetaXR, XRToolkit, Manual }

    static XRSDKType DetectInstalledSDK()
    {
        // Verifica Meta XR SDK (OVRCameraRig)
        if (TypeExists("OVRCameraRig") || TypeExists("OVRManager"))
            return XRSDKType.MetaXR;

        // Verifica XR Interaction Toolkit
        if (TypeExists("UnityEngine.XR.Interaction.Toolkit.XROrigin") ||
            TypeExists("Unity.XR.CoreUtils.XROrigin"))
            return XRSDKType.XRToolkit;

        return XRSDKType.Manual;
    }

    static bool TypeExists(string typeName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type.Name == typeName || type.FullName == typeName)
                    return true;
            }
        }
        return false;
    }

    // ─── DIALOG ───────────────────────────────────────────────────
    static void ShowSetupDialog(XRSDKType sdk)
    {
        string sdkName = sdk switch
        {
            XRSDKType.MetaXR    => "Meta XR SDK (OVRCameraRig)",
            XRSDKType.XRToolkit => "XR Interaction Toolkit (XROrigin Action-based)",
            _                   => "Modo Manual (sem SDK completo)"
        };

        bool confirm = EditorUtility.DisplayDialog(
            "EcoPark — Setup XR Origin",
            $"SDK detectado: {sdkName}\n\n" +
            "Este processo irá:\n" +
            "  1. Remover o XR Origin placeholder\n" +
            "  2. Criar o XR Origin correto para seu SDK\n" +
            "  3. Adicionar PlayerInteraction na câmera\n" +
            "  4. Adicionar PlayerMovement no root\n" +
            "  5. Adicionar HeldItemFollower na mão direita\n" +
            "  6. Re-vincular a UI automaticamente\n\n" +
            "Continuar?",
            "Sim, configurar!", "Cancelar");

        if (!confirm) return;

        switch (sdk)
        {
            case XRSDKType.MetaXR:    SetupMetaXROrigin();    break;
            case XRSDKType.XRToolkit: SetupXRToolkitOrigin(); break;
            default:                  SetupManualOrigin();    break;
        }
    }

    // ─── SETUP META XR ────────────────────────────────────────────
    static void SetupMetaXROrigin()
    {
        Debug.Log("[EcoPark] Configurando com Meta XR SDK...");

        // Remove origin placeholder
        GameObject old = FindXROriginPlaceholder();
        Vector3 spawnPos = old != null ? old.transform.position : Vector3.zero;
        if (old != null)
        {
            Undo.DestroyObjectImmediate(old);
            Debug.Log("[EcoPark] XR Origin placeholder removido.");
        }

        // Tenta instanciar o OVRCameraRig via prefab da Meta
        GameObject ovrRig = TryInstantiateMetaPrefab(spawnPos);

        if (ovrRig == null)
        {
            // Fallback: cria manualmente com componentes da Meta
            ovrRig = CreateMetaRigManually(spawnPos);
        }

        if (ovrRig == null)
        {
            Debug.LogWarning("[EcoPark] Não foi possível criar OVRCameraRig. Usando fallback manual.");
            SetupManualOrigin();
            return;
        }

        // Adiciona scripts EcoPark
        AttachEcoParkScripts(ovrRig, "CenterEyeAnchor", "RightHandAnchor");
        MoveUnderSceneRoot(ovrRig);

        FinalizeSetup("Meta XR SDK (OVRCameraRig)");
    }

    static GameObject TryInstantiateMetaPrefab(Vector3 pos)
    {
        // Procura o prefab OVRCameraRig nos assets
        string[] guids = AssetDatabase.FindAssets("OVRCameraRig t:Prefab");
        if (guids.Length == 0) return null;

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) return null;

        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.position = pos;
        Undo.RegisterCreatedObjectUndo(go, "Create OVRCameraRig");
        Debug.Log($"[EcoPark] OVRCameraRig instanciado de: {path}");
        return go;
    }

    static GameObject CreateMetaRigManually(Vector3 pos)
    {
        // Tenta criar via tipo OVRCameraRig diretamente
        Type ovrType = FindType("OVRCameraRig");
        if (ovrType == null) return null;

        GameObject go = new GameObject("OVRCameraRig");
        go.transform.position = pos;
        go.AddComponent(ovrType);

        // OVRManager é necessário na cena
        Type mgrType = FindType("OVRManager");
        if (mgrType != null)
        {
            // Adiciona no mesmo GO se não existir na cena
            if (UnityEngine.Object.FindObjectOfType(mgrType) == null)
                go.AddComponent(mgrType);
        }

        // Cria a hierarquia padrão do OVRCameraRig
        GameObject trackingSpace = CreateChild(go, "TrackingSpace");
        GameObject leftEye       = CreateChild(trackingSpace, "LeftEyeAnchor");
        GameObject centerEye     = CreateChild(trackingSpace, "CenterEyeAnchor");
        GameObject rightEye      = CreateChild(trackingSpace, "RightEyeAnchor");
        GameObject leftHand      = CreateChild(trackingSpace, "LeftHandAnchor");
        GameObject rightHand     = CreateChild(trackingSpace, "RightHandAnchor");
        GameObject leftController  = CreateChild(leftHand,  "LeftControllerAnchor");
        GameObject rightController = CreateChild(rightHand, "RightControllerAnchor");

        // Câmera no CenterEyeAnchor
        Camera cam = centerEye.AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.nearClipPlane = 0.01f;
        centerEye.AddComponent<AudioListener>();

        Undo.RegisterCreatedObjectUndo(go, "Create OVRCameraRig");
        Debug.Log("[EcoPark] OVRCameraRig criado manualmente.");
        return go;
    }

    // ─── SETUP XR TOOLKIT ─────────────────────────────────────────
    static void SetupXRToolkitOrigin()
    {
        Debug.Log("[EcoPark] Configurando com XR Interaction Toolkit...");

        GameObject old = FindXROriginPlaceholder();
        Vector3 spawnPos = old != null ? old.transform.position : Vector3.zero;
        if (old != null)
        {
            Undo.DestroyObjectImmediate(old);
            Debug.Log("[EcoPark] Placeholder removido.");
        }

        // Procura prefab XR Origin nos assets
        GameObject xrOrigin = TryInstantiateXRToolkitPrefab(spawnPos);

        if (xrOrigin == null)
            xrOrigin = CreateXRToolkitOriginManually(spawnPos);

        AttachEcoParkScripts(xrOrigin, "Main Camera", "Right Controller");
        MoveUnderSceneRoot(xrOrigin);

        FinalizeSetup("XR Interaction Toolkit (Action-based)");
    }

    static GameObject TryInstantiateXRToolkitPrefab(Vector3 pos)
    {
        // Tenta encontrar o prefab XR Origin (Action-based)
        string[] names = { "XR Origin (Action-based)", "XR Origin", "XROrigin" };
        foreach (string name in names)
        {
            string[] guids = AssetDatabase.FindAssets($"{name} t:Prefab");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    go.transform.position = pos;
                    Undo.RegisterCreatedObjectUndo(go, "Create XR Origin");
                    Debug.Log($"[EcoPark] XR Origin instanciado de: {path}");
                    return go;
                }
            }
        }
        return null;
    }

    static GameObject CreateXRToolkitOriginManually(Vector3 pos)
    {
        // Cria hierarquia XR Origin (Action-based) do zero
        GameObject xrOrigin = new GameObject("XR Origin (Action-based)");
        xrOrigin.transform.position = pos;

        // Tenta adicionar o componente XROrigin se disponível
        Type xrOriginType = FindType("Unity.XR.CoreUtils.XROrigin") 
                         ?? FindType("UnityEngine.XR.Interaction.Toolkit.XROrigin");
        if (xrOriginType != null)
            xrOrigin.AddComponent(xrOriginType);

        // CharacterController para colisão com o chão
        var cc = xrOrigin.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.center = new Vector3(0, 0.9f, 0);
        cc.radius = 0.3f;

        // Camera Offset
        GameObject camOffset = CreateChild(xrOrigin, "Camera Floor Offset");
        camOffset.transform.localPosition = new Vector3(0, 1.65f, 0);

        // Main Camera
        GameObject camGO = CreateChild(camOffset, "Main Camera");
        Camera cam = camGO.AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.nearClipPlane = 0.01f;
        cam.fieldOfView = 90f;
        camGO.AddComponent<AudioListener>();

        // Controladores
        GameObject leftController  = CreateChild(camOffset, "Left Controller");
        GameObject rightController = CreateChild(camOffset, "Right Controller");
        leftController.transform.localPosition  = new Vector3(-0.2f, -0.2f, 0.4f);
        rightController.transform.localPosition = new Vector3( 0.2f, -0.2f, 0.4f);

        // Tenta adicionar ActionBasedController se disponível
        Type controllerType = FindType("UnityEngine.XR.Interaction.Toolkit.ActionBasedController");
        if (controllerType != null)
        {
            leftController.AddComponent(controllerType);
            rightController.AddComponent(controllerType);
        }

        Undo.RegisterCreatedObjectUndo(xrOrigin, "Create XR Origin Manual");
        Debug.Log("[EcoPark] XR Origin (Action-based) criado manualmente.");
        return xrOrigin;
    }

    // ─── SETUP MANUAL (sem SDK) ───────────────────────────────────
    static void SetupManualOrigin()
    {
        Debug.Log("[EcoPark] Configurando XR Origin manual (sem SDK completo)...");

        GameObject old = FindXROriginPlaceholder();
        Vector3 spawnPos = old != null ? old.transform.position : Vector3.zero;
        if (old != null)
        {
            Undo.DestroyObjectImmediate(old);
        }

        GameObject player = new GameObject("XR Origin [Manual]");
        player.transform.position = spawnPos;

        var cc = player.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.center = new Vector3(0, 0.9f, 0);
        cc.radius = 0.3f;

        GameObject camOffset = CreateChild(player, "Camera Floor Offset");
        camOffset.transform.localPosition = new Vector3(0, 1.65f, 0);

        GameObject camGO = CreateChild(camOffset, "Main Camera");
        Camera cam = camGO.AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.nearClipPlane = 0.01f;
        camGO.AddComponent<AudioListener>();

        GameObject rightHand = CreateChild(camOffset, "Right Controller");
        rightHand.transform.localPosition = new Vector3(0.2f, -0.2f, 0.4f);

        AttachEcoParkScripts(player, "Main Camera", "Right Controller");
        MoveUnderSceneRoot(player);

        FinalizeSetup("Manual (sem SDK XR completo)");

        EditorUtility.DisplayDialog("EcoPark — Aviso",
            "XR Origin manual criado.\n\n" +
            "Para rodar no Meta Quest você ainda precisará instalar:\n" +
            "  • Meta XR All-in-One SDK  (recomendado)\n" +
            "  • OU XR Interaction Toolkit\n\n" +
            "Mas o jogo já funciona no Editor com mouse + WASD!",
            "Entendido");
    }

    // ─── ATTACH ECOPARK SCRIPTS ───────────────────────────────────
    /// <summary>
    /// Adiciona PlayerInteraction, PlayerMovement e HeldItemFollower
    /// nos GameObjects corretos dentro do novo XR Origin.
    /// </summary>
    static void AttachEcoParkScripts(GameObject xrRoot,
                                      string cameraChildName,
                                      string rightHandChildName)
    {
        // ── PlayerMovement no root ──
        if (xrRoot.GetComponent<PlayerMovement>() == null)
        {
            PlayerMovement pm = xrRoot.AddComponent<PlayerMovement>();
            // Câmera será vinculada abaixo
            Debug.Log("[EcoPark] PlayerMovement adicionado ao XR Origin.");
        }

        // ── Encontra a Main Camera nos filhos ──
        Camera cam = xrRoot.GetComponentInChildren<Camera>();
        GameObject camGO = cam != null ? cam.gameObject : FindChildByName(xrRoot, cameraChildName);

        if (camGO != null)
        {
            // PlayerInteraction na câmera
            if (camGO.GetComponent<PlayerInteraction>() == null)
            {
                PlayerInteraction pi = camGO.AddComponent<PlayerInteraction>();
                pi.interactionDistance = 4f;
                pi.interactableLayer = LayerMask.GetMask("Interactable");
                Debug.Log($"[EcoPark] PlayerInteraction adicionado em: {camGO.name}");
            }

            // Vincula câmera ao PlayerMovement
            PlayerMovement pmRef = xrRoot.GetComponent<PlayerMovement>();
            if (pmRef != null)
            {
                pmRef.cameraTransform = camGO.transform;
                EditorUtility.SetDirty(pmRef);
            }
        }
        else
        {
            Debug.LogWarning($"[EcoPark] Câmera '{cameraChildName}' não encontrada nos filhos do XR Origin!");
        }

        // ── HeldItemFollower na mão direita ou câmera ──
        GameObject handGO = FindChildByName(xrRoot, rightHandChildName)
                         ?? FindChildByName(xrRoot, "RightHandAnchor")
                         ?? FindChildByName(xrRoot, "RightControllerAnchor")
                         ?? camGO;

        if (handGO != null)
        {
            // Cria ItemHolder como filho da mão
            GameObject holder = new GameObject("ItemHolder");
            holder.transform.parent = handGO.transform;
            holder.transform.localPosition = Vector3.zero;

            HeldItemFollower hif = holder.AddComponent<HeldItemFollower>();
            hif.holdOffset = new Vector3(0f, -0.05f, 0.35f);
            Debug.Log($"[EcoPark] HeldItemFollower adicionado em: {handGO.name}/ItemHolder");
        }

        EditorUtility.SetDirty(xrRoot);
    }

    // ─── FINALIZAR ────────────────────────────────────────────────
    static void FinalizeSetup(string sdkUsed)
    {
        // Re-executa o UIAutoWirer para garantir que UI está vinculada
        UIAutoWirer wirer = UnityEngine.Object.FindObjectOfType<UIAutoWirer>();
        if (wirer != null)
        {
            wirer.WireAll();
            Debug.Log("[EcoPark] UIAutoWirer re-executado após XR Origin setup.");
        }

        // Salva a cena
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log($"[EcoPark] ✅ XR Origin configurado com sucesso! SDK: {sdkUsed}");

        EditorUtility.DisplayDialog("EcoPark — XR Origin Configurado!",
            $"✅ XR Origin pronto!\n" +
            $"SDK: {sdkUsed}\n\n" +
            $"Componentes adicionados:\n" +
            $"  • PlayerMovement → XR Origin (raiz)\n" +
            $"  • PlayerInteraction → Main Camera\n" +
            $"  • HeldItemFollower → ItemHolder (mão direita)\n\n" +
            $"Próximo passo: Pressione ▶ Play para testar!",
            "Jogar agora!");
    }

    // ─── UTILITÁRIOS ──────────────────────────────────────────────
    static GameObject FindXROriginPlaceholder()
    {
        // Tenta pelos nomes possíveis criados pelo Builder
        string[] candidates = {
            "XR Origin", "XR Origin (Action-based)", "XR Origin [Manual]",
            "OVRCameraRig", "XROrigin"
        };

        foreach (string name in candidates)
        {
            GameObject go = GameObject.Find(name);
            if (go != null)
            {
                Debug.Log($"[EcoPark] Placeholder encontrado: {go.name}");
                return go;
            }
        }

        // Busca por CharacterController (heurística do placeholder)
        CharacterController[] ccs = UnityEngine.Object.FindObjectsOfType<CharacterController>();
        foreach (var cc in ccs)
        {
            // O placeholder criado pelo builder tem CharacterController
            if (cc.gameObject.GetComponent<Camera>() == null)
                return cc.gameObject;
        }

        Debug.LogWarning("[EcoPark] Nenhum XR Origin placeholder encontrado na cena.");
        return null;
    }

    static void MoveUnderSceneRoot(GameObject go)
    {
        // Move para ficar como filho da raiz da cena (= == EcoParkVR Scene ===)
        GameObject sceneRoot = GameObject.Find("=== EcoParkVR Scene ===");
        if (sceneRoot != null)
            go.transform.parent = sceneRoot.transform;
    }

    static GameObject CreateChild(GameObject parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.parent = parent.transform;
        child.transform.localPosition = Vector3.zero;
        child.transform.localRotation = Quaternion.identity;
        child.transform.localScale = Vector3.one;
        return child;
    }

    static GameObject FindChildByName(GameObject root, string name)
    {
        if (root == null || string.IsNullOrEmpty(name)) return null;

        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == name) return t.gameObject;
        }
        return null;
    }

    static Type FindType(string typeName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.FullName == typeName || type.Name == typeName)
                        return type;
                }
            }
            catch { /* Alguns assemblies lançam exceção no GetTypes() — ignorar */ }
        }
        return null;
    }
}
#endif
