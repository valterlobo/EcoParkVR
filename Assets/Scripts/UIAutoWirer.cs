using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DefaultExecutionOrder(-10)]
[RequireComponent(typeof(GameManager))]
public class UIAutoWirer : MonoBehaviour
{
    public bool verbose = true;

    System.Collections.IEnumerator Start()
    {
        yield return null; // frame 1 — hierarquia pronta

        // Tenta vincular o que ja existe
        WireAll();

        GameManager gm = GetComponent<GameManager>();
        if (gm == null) yield break;

        // Verifica quais campos ainda estao vazios
        bool allOk = gm.scoreText != null && gm.progressText   != null
                  && gm.feedbackPanel != null && gm.feedbackText != null
                  && gm.winPanel != null && gm.finalScoreText  != null
                  && gm.heldItemPanel != null && gm.heldItemText != null;

        if (!allOk)
        {
            // Cria os objetos faltantes em runtime e tenta de novo
            CreateMissingUI();
            yield return null; // frame 2 — objetos criados
            WireAll();
        }

        // Log final de status
        if (verbose) LogStatus(gm);
    }

    // ─── WIRE ─────────────────────────────────────────────────────

    public void WireAll()
    {
        GameManager gm = GetComponent<GameManager>();
        if (gm == null) return;

        gm.scoreText      = Get<TMP_Text>    ("ScoreText");
        gm.progressText   = Get<TMP_Text>    ("ProgressText");
        gm.feedbackPanel  = FindAnywhere     ("FeedbackCanvas");
        gm.feedbackText   = Get<TMP_Text>    ("FeedbackText");
        gm.winPanel       = FindAnywhere     ("WinCanvas");
        gm.finalScoreText = Get<TMP_Text>    ("FinalScoreText");
        gm.heldItemPanel  = FindAnywhere     ("HeldItemCanvas");
        gm.heldItemText   = Get<TMP_Text>    ("HeldText");
        gm.feedbackIcon   = Get<Image>       ("FeedbackBG");

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(gm);
#endif
    }

    static T Get<T>(string name) where T : Component
    {
        GameObject go = FindAnywhere(name);
        return go != null ? go.GetComponent<T>() : null;
    }

    // ─── AUTO-CRIACAO ─────────────────────────────────────────────

    void CreateMissingUI()
    {
        Camera cam = Camera.main ?? FindObjectOfType<Camera>();
        Transform anchor = cam != null ? cam.transform : transform;

        var sc = EnsureCanvas("ScoreCanvas",    anchor, new Vector3(0, 0.3f,  2f),   new Vector2(420,110), true);
        var fc = EnsureCanvas("FeedbackCanvas", anchor, new Vector3(0, 0f,    2f),   new Vector2(520,110), false);
        var hc = EnsureCanvas("HeldItemCanvas", anchor, new Vector3(0,-0.22f, 1.8f), new Vector2(360, 88), false);
        var wc = EnsureCanvas("WinCanvas",      anchor, new Vector3(0, 0.1f,  2.5f), new Vector2(620,320), false);

        EnsureTMP ("ScoreText",     sc, new Vector3(-90,0,0), new Vector2(190,70),  "Pontos: 0",               28, Color.white,              FontStyles.Bold,   TextAlignmentOptions.Left);
        EnsureTMP ("ProgressText",  sc, new Vector3( 90,0,0), new Vector2(190,70),  "Reciclados: 0/5",         20, new Color(.7f,1f,.7f),     FontStyles.Normal, TextAlignmentOptions.Left);
        EnsureImg ("FeedbackBG",    fc, Vector3.zero,          new Vector2(510,100), new Color(0,0,0,.72f));
        EnsureTMP ("FeedbackText",  fc, Vector3.zero,          new Vector2(500, 96), "",                        26, Color.green,              FontStyles.Bold,   TextAlignmentOptions.Center);
        EnsureImg ("HeldBG",        hc, Vector3.zero,          new Vector2(350, 80), new Color(0,0,0,.65f));
        EnsureTMP ("HeldText",      hc, Vector3.zero,          new Vector2(340, 76), "Segurando: Item",         18, Color.white,              FontStyles.Normal, TextAlignmentOptions.Center);
        EnsureImg ("WinBG",         wc, Vector3.zero,          new Vector2(610,310), new Color(.05f,.25f,.05f,.92f));
        EnsureTMP ("FinalScoreText",wc, new Vector3(0,20,0),   new Vector2(590,240), "Parabens! Voce reciclou tudo!", 28, Color.white,        FontStyles.Bold,   TextAlignmentOptions.Center);
    }

    static GameObject EnsureCanvas(string name, Transform parent,
                                    Vector3 pos, Vector2 size, bool active)
    {
        GameObject go = FindAnywhere(name);
        if (go != null) { go.SetActive(active); return go; }

        go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale    = Vector3.one * 0.001f;
        var cv = go.AddComponent<Canvas>();
        cv.renderMode = RenderMode.WorldSpace;
        go.GetComponent<RectTransform>().sizeDelta = size;
        go.AddComponent<CanvasScaler>();
        go.AddComponent<GraphicRaycaster>();
        go.SetActive(active);
        Debug.Log("[UIAutoWirer] Canvas criado: " + name);
        return go;
    }

    static void EnsureTMP(string name, GameObject parent, Vector3 lpos, Vector2 size,
                           string text, float fs, Color col, FontStyles style,
                           TextAlignmentOptions align)
    {
        if (FindAnywhere(name) != null) return;
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.localPosition = lpos; rt.sizeDelta = size; rt.localScale = Vector3.one;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = fs; t.color = col; t.fontStyle = style; t.alignment = align;
        Debug.Log("[UIAutoWirer] TMP criado: " + name);
    }

    static void EnsureImg(string name, GameObject parent, Vector3 lpos, Vector2 size, Color col)
    {
        if (FindAnywhere(name) != null) return;
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<Image>().color = col;
        var rt = go.GetComponent<RectTransform>();
        rt.localPosition = lpos; rt.sizeDelta = size; rt.localScale = Vector3.one;
        Debug.Log("[UIAutoWirer] Image criada: " + name);
    }

    // ─── LOG DE STATUS ────────────────────────────────────────────

    void LogStatus(GameManager gm)
    {
        string s =
            "\n[UIAutoWirer] === STATUS FINAL ===\n" +
            "  scoreText:      " + Stat(gm.scoreText)      + "\n" +
            "  progressText:   " + Stat(gm.progressText)   + "\n" +
            "  feedbackPanel:  " + Stat(gm.feedbackPanel)  + "\n" +
            "  feedbackText:   " + Stat(gm.feedbackText)   + "\n" +
            "  winPanel:       " + Stat(gm.winPanel)       + "\n" +
            "  finalScoreText: " + Stat(gm.finalScoreText) + "\n" +
            "  heldItemPanel:  " + Stat(gm.heldItemPanel)  + "\n" +
            "  heldItemText:   " + Stat(gm.heldItemText)   + "\n" +
            "  feedbackIcon:   " + Stat(gm.feedbackIcon)   + " (opcional)";
        Debug.Log(s);
    }

    static string Stat(Object o) => o != null ? "OK  [" + o.name + "]" : "FALTA";

    // ─── BUSCA UNIVERSAL ──────────────────────────────────────────

    public static GameObject FindAnywhere(string name)
    {
        GameObject go = GameObject.Find(name);
        if (go != null) return go;
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
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