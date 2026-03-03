#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Cria ou recria todos os GameObjects de UI do EcoPark VR.
/// Menu: EcoPark > Create UI Elements
/// </summary>
public static class UIElementsBuilder
{
    [MenuItem("EcoPark/Create UI Elements")]
    public static void CreateUIElements()
    {
        Camera cam = Camera.main ?? Object.FindObjectOfType<Camera>();
        if (cam == null)
        {
            EditorUtility.DisplayDialog("EcoPark",
                "Nenhuma Camera encontrada!\nCrie a cena primeiro:\nEcoPark > Build Scene Automatically", "OK");
            return;
        }

        int created = 0, skipped = 0;

        // ── SCORE CANVAS ────────────────────────────────────────────────
        var scoreCanvas = EnsureCanvas("ScoreCanvas", cam.transform,
            new Vector3(0, 0.3f, 2f), new Vector2(420, 110));

        Ensure("ScoreText", ref created, ref skipped, () =>
            MakeTMP("ScoreText", scoreCanvas.transform,
                new Vector3(-90, 0, 0), new Vector2(190, 70),
                "Pontos: 0", 28, Color.white, FontStyles.Bold));

        Ensure("ProgressText", ref created, ref skipped, () =>
            MakeTMP("ProgressText", scoreCanvas.transform,
                new Vector3(90, 0, 0), new Vector2(190, 70),
                "Reciclados: 0/5", 20, new Color(0.7f, 1f, 0.7f), FontStyles.Normal));

        // ── FEEDBACK CANVAS ─────────────────────────────────────────────
        var feedCanvas = EnsureCanvas("FeedbackCanvas", cam.transform,
            new Vector3(0, 0f, 2f), new Vector2(520, 110));
        feedCanvas.SetActive(false);

        Ensure("FeedbackBG", ref created, ref skipped, () =>
        {
            var go = new GameObject("FeedbackBG");
            go.transform.SetParent(feedCanvas.transform, false);
            go.AddComponent<Image>().color = new Color(0, 0, 0, 0.72f);
            SetRect(go, new Vector2(510, 100));
            return go;
        });

        Ensure("FeedbackText", ref created, ref skipped, () =>
        {
            var go = MakeTMP("FeedbackText", feedCanvas.transform,
                Vector3.zero, new Vector2(500, 96), "", 26, Color.green, FontStyles.Bold);
            go.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;
            return go;
        });

        // ── HELD ITEM CANVAS ────────────────────────────────────────────
        var heldCanvas = EnsureCanvas("HeldItemCanvas", cam.transform,
            new Vector3(0, -0.22f, 1.8f), new Vector2(360, 88));
        heldCanvas.SetActive(false);

        Ensure("HeldBG", ref created, ref skipped, () =>
        {
            var go = new GameObject("HeldBG");
            go.transform.SetParent(heldCanvas.transform, false);
            go.AddComponent<Image>().color = new Color(0, 0, 0, 0.65f);
            SetRect(go, new Vector2(350, 80));
            return go;
        });

        Ensure("HeldText", ref created, ref skipped, () =>
        {
            var go = MakeTMP("HeldText", heldCanvas.transform,
                Vector3.zero, new Vector2(340, 76),
                "Segurando: Item\nDepositar na lixeira correta",
                18, Color.white, FontStyles.Normal);
            go.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;
            return go;
        });

        // ── WIN CANVAS ──────────────────────────────────────────────────
        var winCanvas = EnsureCanvas("WinCanvas", cam.transform,
            new Vector3(0, 0.1f, 2.5f), new Vector2(620, 320));
        winCanvas.SetActive(false);

        Ensure("WinBG", ref created, ref skipped, () =>
        {
            var go = new GameObject("WinBG");
            go.transform.SetParent(winCanvas.transform, false);
            go.AddComponent<Image>().color = new Color(0.05f, 0.25f, 0.05f, 0.92f);
            SetRect(go, new Vector2(610, 310));
            return go;
        });

        Ensure("FinalScoreText", ref created, ref skipped, () =>
        {
            var go = MakeTMP("FinalScoreText", winCanvas.transform,
                new Vector3(0, 20, 0), new Vector2(590, 240),
                "Parabens! Voce reciclou tudo!", 24, Color.white, FontStyles.Normal);
            go.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;
            return go;
        });

        // ── RE-WIRE ─────────────────────────────────────────────────────
        UIAutoWirer wirer = Object.FindObjectOfType<UIAutoWirer>();
        if (wirer != null) wirer.WireAll();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log($"[UIElementsBuilder] Criados: {created} | Ja existiam: {skipped}");
        EditorUtility.DisplayDialog("EcoPark - UI Elements",
            $"Concluido!\n\nCriados:     {created}\nJa existiam: {skipped}\n\nCanvas em: {cam.name}\n\nPressione Play!", "OK");
    }

    // ─── HELPERS ────────────────────────────────────────────────────────

    static void Ensure(string name, ref int created, ref int skipped,
                        System.Func<GameObject> factory)
    {
        if (FindAnywhere(name) != null) { skipped++; return; }
        factory();
        created++;
        Debug.Log($"[UIElementsBuilder] Criado: {name}");
    }

    static GameObject EnsureCanvas(string name, Transform parent,
                                    Vector3 localPos, Vector2 size)
    {
        GameObject existing = FindAnywhere(name);
        if (existing != null) return existing;

        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale    = Vector3.one * 0.001f;

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        go.GetComponent<RectTransform>().sizeDelta = size;
        go.AddComponent<CanvasScaler>();
        go.AddComponent<GraphicRaycaster>();
        Debug.Log($"[UIElementsBuilder] Canvas criado: {name}");
        return go;
    }

    static GameObject MakeTMP(string name, Transform parent, Vector3 localPos,
                               Vector2 size, string text, float fontSize,
                               Color color, FontStyles style)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.localPosition = localPos;
        rt.sizeDelta     = size;
        rt.localScale    = Vector3.one;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.color     = color;
        tmp.fontStyle = style;
        return go;
    }

    static void SetRect(GameObject go, Vector2 size)
    {
        var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
        rt.sizeDelta = size; rt.localPosition = Vector3.zero; rt.localScale = Vector3.one;
    }

    // Busca em toda a hierarquia (ativos + inativos + qualquer profundidade)
    static GameObject FindAnywhere(string name)
    {
        GameObject go = GameObject.Find(name);
        if (go != null) return go;
        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        foreach (var root in scene.GetRootGameObjects())
        {
            var r = SearchChildren(root.transform, name);
            if (r != null) return r;
        }
        return null;
    }

    static GameObject SearchChildren(Transform t, string name)
    {
        if (t.name == name) return t.gameObject;
        for (int i = 0; i < t.childCount; i++)
        {
            var r = SearchChildren(t.GetChild(i), name);
            if (r != null) return r;
        }
        return null;
    }
}
#endif