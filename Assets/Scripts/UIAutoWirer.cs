using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Vincula automaticamente todos os campos de UI do GameManager
/// percorrendo TODA a hierarquia da cena, incluindo objetos inativos
/// e filhos de qualquer profundidade.
///
/// Estratégia de busca (em ordem de tentativa):
///   1. GameObject.Find()                  — objetos ativos, raiz
///   2. FindInScene() via GetComponentsInChildren — toda hierarquia, inativos
///   3. Busca por tipo direto (TMP_Text, Image) filtrando por nome
/// </summary>
[DefaultExecutionOrder(-10)]
[RequireComponent(typeof(GameManager))]
public class UIAutoWirer : MonoBehaviour
{
    [Header("Configuração")]
    public bool autoWireOnAwake = true;
    public bool verbose = true;

    void Awake()
    {
        if (autoWireOnAwake)
            WireAll();
    }

    public void WireAll()
    {
        GameManager gm = GetComponent<GameManager>();
        if (gm == null) { Debug.LogError("[UIAutoWirer] GameManager não encontrado!"); return; }

        int ok = 0, fail = 0;

        gm.scoreText      = FindTMP("ScoreText",      ref ok, ref fail);
        gm.progressText   = FindTMP("ProgressText",   ref ok, ref fail);
        gm.feedbackPanel  = FindGO ("FeedbackCanvas", ref ok, ref fail);
        gm.feedbackText   = FindTMP("FeedbackText",   ref ok, ref fail);
        gm.winPanel       = FindGO ("WinCanvas",      ref ok, ref fail);
        gm.finalScoreText = FindTMP("FinalScoreText", ref ok, ref fail);
        gm.heldItemPanel  = FindGO ("HeldItemCanvas", ref ok, ref fail);
        gm.heldItemText   = FindTMP("HeldText",       ref ok, ref fail);

        // feedbackIcon é opcional — não conta como falha
        gm.feedbackIcon = FindImageOptional("FeedbackBG", "FeedbackBackground", "FeedbackPanel");
        if (gm.feedbackIcon != null) ok++;
        else if (verbose) Debug.Log("[UIAutoWirer] ℹ️  feedbackIcon não encontrado (opcional).");

        if (verbose)
        {
            string msg = fail == 0
                ? $"<color=green>✅ {ok} campos vinculados!</color>"
                : $"<color=orange>⚠️ {ok} ok, {fail} campo(s) crítico(s) não encontrado(s).</color>";
            Debug.Log($"[UIAutoWirer] {msg}");
        }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(gm);
#endif
    }

    // ─── FINDERS ──────────────────────────────────────────────────

    TMP_Text FindTMP(string name, ref int ok, ref int fail)
    {
        GameObject go = FindAnywhere(name);
        if (go == null) { Warn("TMP_Text", name); fail++; return null; }

        TMP_Text tmp = go.GetComponent<TMP_Text>();
        if (tmp == null) { Warn("TMP_Text (componente)", name); fail++; return null; }

        if (verbose) Debug.Log($"[UIAutoWirer] ✔  {name} → TMP_Text");
        ok++;
        return tmp;
    }

    GameObject FindGO(string name, ref int ok, ref int fail)
    {
        GameObject go = FindAnywhere(name);
        if (go == null) { Warn("GameObject", name); fail++; return null; }

        if (verbose) Debug.Log($"[UIAutoWirer] ✔  {name} → GameObject");
        ok++;
        return go;
    }

    Image FindImageOptional(params string[] names)
    {
        foreach (string name in names)
        {
            GameObject go = FindAnywhere(name);
            if (go != null)
            {
                Image img = go.GetComponent<Image>();
                if (img != null) return img;
            }
        }
        return null;
    }

    void Warn(string type, string name) =>
        Debug.LogWarning($"[UIAutoWirer] ⚠️  {type} não encontrado: '{name}'");

    // ─── BUSCA UNIVERSAL ──────────────────────────────────────────

    /// <summary>
    /// Encontra qualquer GameObject por nome, incluindo:
    /// - Objetos inativos (SetActive false)
    /// - Objetos filhos em qualquer profundidade
    /// - Objetos dentro de Canvases filho de câmeras
    /// </summary>
    static GameObject FindAnywhere(string name)
    {
        // Tentativa 1: Find padrão (rápido, só ativos)
        GameObject go = GameObject.Find(name);
        if (go != null) return go;

        // Tentativa 2: Percorre todos os root objects da cena (ativos e inativos)
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            GameObject found = SearchChildren(root.transform, name);
            if (found != null) return found;
        }

        return null;
    }

    static GameObject SearchChildren(Transform parent, string name)
    {
        // Verifica o próprio objeto
        if (parent.name == name) return parent.gameObject;

        // Percorre todos os filhos recursivamente (inclui inativos)
        for (int i = 0; i < parent.childCount; i++)
        {
            GameObject found = SearchChildren(parent.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }
}
