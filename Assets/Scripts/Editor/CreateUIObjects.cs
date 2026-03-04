#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Menu: EcoPark > Create UI Objects
/// Cria exatamente os 8 GameObjects de UI necessarios para o EcoPark VR.
/// Nao duplica — verifica se ja existe antes de criar.
/// </summary>
public static class CreateUIObjects
{
    [MenuItem("EcoPark/Create UI Objects")]
    public static void Create()
    {
        Camera cam = Camera.main ?? Object.FindObjectOfType<Camera>();
        if (cam == null)
        {
            EditorUtility.DisplayDialog("EcoPark",
                "Nenhuma Camera encontrada na cena!\n\nCrie a cena primeiro:\nEcoPark > Build Scene Automatically", "OK");
            return;
        }

        int created = 0;

        // ── ScoreCanvas (pai de ScoreText e ProgressText) ──────────────
        var scoreCanvas = Canvas("ScoreCanvas", cam.transform,
            pos: new Vector3(0, 0.3f, 2f),
            size: new Vector2(420, 110));

        TMP(ref created, "ScoreText", scoreCanvas,
            localPos : new Vector3(-90, 0, 0),
            size     : new Vector2(190, 70),
            text     : "Pontos: 0",
            fontSize : 28,
            color    : Color.white,
            bold     : true);

        TMP(ref created, "ProgressText", scoreCanvas,
            localPos : new Vector3(90, 0, 0),
            size     : new Vector2(190, 70),
            text     : "Reciclados: 0/5",
            fontSize : 20,
            color    : new Color(0.7f, 1f, 0.7f),
            bold     : false);

        // ── FeedbackCanvas ─────────────────────────────────────────────
        var feedCanvas = Canvas("FeedbackCanvas", cam.transform,
            pos: new Vector3(0, 0f, 2f),
            size: new Vector2(520, 110));
        feedCanvas.SetActive(false);

        Img("FeedbackBG", feedCanvas, new Vector2(510, 100), new Color(0, 0, 0, 0.72f));

        var feedText = TMP(ref created, "FeedbackText", feedCanvas,
            localPos : Vector3.zero,
            size     : new Vector2(500, 96),
            text     : "",
            fontSize : 26,
            color    : Color.green,
            bold     : true);
        if (feedText != null)
            feedText.alignment = TextAlignmentOptions.Center;

        // ── HeldItemCanvas ─────────────────────────────────────────────
        var heldCanvas = Canvas("HeldItemCanvas", cam.transform,
            pos: new Vector3(0, -0.22f, 1.8f),
            size: new Vector2(360, 88));
        heldCanvas.SetActive(false);

        Img("HeldBG", heldCanvas, new Vector2(350, 80), new Color(0, 0, 0, 0.65f));

        var heldText = TMP(ref created, "HeldText", heldCanvas,
            localPos : Vector3.zero,
            size     : new Vector2(340, 76),
            text     : "Segurando: Item\nDepositar na lixeira correta",
            fontSize : 18,
            color    : Color.white,
            bold     : false);
        if (heldText != null)
            heldText.alignment = TextAlignmentOptions.Center;

        // ── WinCanvas ──────────────────────────────────────────────────
        var winCanvas = Canvas("WinCanvas", cam.transform,
            pos: new Vector3(0, 0.1f, 2.5f),
            size: new Vector2(620, 320));
        winCanvas.SetActive(false);

        Img("WinBG", winCanvas, new Vector2(610, 310), new Color(0.05f, 0.25f, 0.05f, 0.92f));

        var finalText = TMP(ref created, "FinalScoreText", winCanvas,
            localPos : new Vector3(0, 20, 0),
            size     : new Vector2(590, 240),
            text     : "Parabens!\nVoce reciclou tudo!",
            fontSize : 28,
            color    : Color.white,
            bold     : true);
        if (finalText != null)
            finalText.alignment = TextAlignmentOptions.Center;

        // ── Re-wire GameManager ────────────────────────────────────────
        UIAutoWirer wirer = Object.FindObjectOfType<UIAutoWirer>();
        if (wirer != null) wirer.WireAll();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        string msg = created > 0
            ? $"{created} objeto(s) criado(s) com sucesso!"
            : "Todos os objetos ja existiam.";

        Debug.Log("[CreateUIObjects] " + msg);
        EditorUtility.DisplayDialog("EcoPark - UI Objects", msg +
            "\n\nHierarquia:\n" +
            $"  {cam.name}\n" +
            "    ScoreCanvas\n      ScoreText\n      ProgressText\n" +
            "    FeedbackCanvas\n      FeedbackBG\n      FeedbackText\n" +
            "    HeldItemCanvas\n      HeldBG\n      HeldText\n" +
            "    WinCanvas\n      WinBG\n      FinalScoreText", "OK");
    }

    // ─── FACTORIES ────────────────────────────────────────────────────

    static GameObject Canvas(string name, Transform parent, Vector3 pos, Vector2 size)
    {
        GameObject existing = Find(name);
        if (existing != null) return existing;

        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale    = Vector3.one * 0.001f;

        var c = go.AddComponent<UnityEngine.Canvas>();
        c.renderMode = RenderMode.WorldSpace;
        go.GetComponent<RectTransform>().sizeDelta = size;
        go.AddComponent<CanvasScaler>();
        go.AddComponent<GraphicRaycaster>();

        Debug.Log("[CreateUIObjects] Canvas criado: " + name);
        return go;
    }

    static TMP_Text TMP(ref int count, string name, GameObject parent,
                         Vector3 localPos, Vector2 size, string text,
                         float fontSize, Color color, bool bold)
    {
        if (Find(name) != null)
        {
            Debug.Log("[CreateUIObjects] Ja existe: " + name);
            return Find(name).GetComponent<TMP_Text>();
        }

        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);

        var rt = go.AddComponent<RectTransform>();
        rt.localPosition = localPos;
        rt.sizeDelta     = size;
        rt.localScale    = Vector3.one;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.color     = color;
        tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;

        count++;
        Debug.Log("[CreateUIObjects] Criado: " + name);
        return tmp;
    }

    static void Img(string name, GameObject parent, Vector2 size, Color color)
    {
        if (Find(name) != null) return;

        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<Image>().color = color;
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta     = size;
        rt.localPosition = Vector3.zero;
        rt.localScale    = Vector3.one;
    }

    static GameObject Find(string name)
    {
        GameObject go = GameObject.Find(name);
        if (go != null) return go;

        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        foreach (var root in scene.GetRootGameObjects())
        {
            var r = Deep(root.transform, name);
            if (r != null) return r;
        }
        return null;
    }

    static GameObject Deep(Transform t, string name)
    {
        if (t.name == name) return t.gameObject;
        for (int i = 0; i < t.childCount; i++)
        {
            var r = Deep(t.GetChild(i), name);
            if (r != null) return r;
        }
        return null;
    }
}
#endif
