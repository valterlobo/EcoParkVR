#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Adiciona botão no Inspector e item de menu para executar
/// a vinculação de UI sem precisar dar Play.
/// </summary>
[CustomEditor(typeof(UIAutoWirer))]
public class UIAutoWirerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        UIAutoWirer wirer = (UIAutoWirer)target;
        GameManager gm = wirer.GetComponent<GameManager>();

        EditorGUILayout.Space(10);

        // ── Botão principal ──────────────────────────────────────
        GUI.backgroundColor = new Color(0.3f, 0.85f, 0.4f);
        if (GUILayout.Button("🔗  Vincular UI Automaticamente Agora", GUILayout.Height(40)))
        {
            wirer.WireAll();
            EditorUtility.SetDirty(gm);
            Debug.Log("[UIAutoWirer] Vinculação executada via botão do Inspector.");
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(4);

        // ── Status visual dos campos ─────────────────────────────
        if (gm != null)
        {
            EditorGUILayout.LabelField("Status dos Campos", EditorStyles.boldLabel);

            DrawFieldStatus("scoreText",     gm.scoreText     != null);
            DrawFieldStatus("progressText",  gm.progressText  != null);
            DrawFieldStatus("feedbackPanel", gm.feedbackPanel != null);
            DrawFieldStatus("feedbackText",  gm.feedbackText  != null);
            DrawFieldStatus("feedbackIcon",  gm.feedbackIcon  != null);
            DrawFieldStatus("winPanel",      gm.winPanel      != null);
            DrawFieldStatus("finalScoreText",gm.finalScoreText!= null);
            DrawFieldStatus("heldItemPanel", gm.heldItemPanel != null);
            DrawFieldStatus("heldItemText",  gm.heldItemText  != null);
        }
    }

    void DrawFieldStatus(string fieldName, bool linked)
    {
        GUIStyle style = new GUIStyle(EditorStyles.label);
        style.normal.textColor = linked
            ? new Color(0.2f, 0.75f, 0.3f)
            : new Color(0.9f, 0.35f, 0.2f);

        string icon = linked ? "✔" : "✘";
        EditorGUILayout.LabelField($"  {icon}  {fieldName}", style);
    }
}

/// <summary>
/// Menu item acessível via EcoPark → Auto-Wire UI
/// (Funciona mesmo sem selecionar nenhum objeto)
/// </summary>
public static class UIAutoWirerMenu
{
    [MenuItem("EcoPark/Auto-Wire UI References")]
    public static void AutoWireFromMenu()
    {
        UIAutoWirer wirer = Object.FindObjectOfType<UIAutoWirer>();

        if (wirer == null)
        {
            // Tenta adicionar automaticamente ao GameManager
            GameManager gm = Object.FindObjectOfType<GameManager>();

            if (gm == null)
            {
                EditorUtility.DisplayDialog("EcoPark Auto-Wire",
                    "Nenhum GameManager encontrado na cena!\n\n" +
                    "Execute primeiro:\nEcoPark → Build Scene Automatically",
                    "OK");
                return;
            }

            wirer = gm.gameObject.GetComponent<UIAutoWirer>();
            if (wirer == null)
                wirer = gm.gameObject.AddComponent<UIAutoWirer>();

            Debug.Log("[UIAutoWirer] UIAutoWirer adicionado ao GameManager automaticamente.");
        }

        wirer.WireAll();

        EditorUtility.SetDirty(wirer.GetComponent<GameManager>());
        AssetDatabase.SaveAssets();

        int missing = CountMissingFields(wirer.GetComponent<GameManager>());

        if (missing == 0)
        {
            EditorUtility.DisplayDialog("EcoPark Auto-Wire",
                "✅ Todos os campos de UI vinculados com sucesso!\n\n" +
                "Você pode dar Play agora.", "Ótimo!");
        }
        else
        {
            EditorUtility.DisplayDialog("EcoPark Auto-Wire",
                $"⚠️  {missing} campo(s) não foram encontrados.\n\n" +
                "Verifique o Console para detalhes.\n\n" +
                "Possível causa: a cena não foi gerada com\n" +
                "EcoPark → Build Scene Automatically",
                "Entendido");
        }
    }

    [MenuItem("EcoPark/Add UIAutoWirer to GameManager")]
    public static void AddWirerComponent()
    {
        GameManager gm = Object.FindObjectOfType<GameManager>();

        if (gm == null)
        {
            Debug.LogWarning("[UIAutoWirer] Nenhum GameManager na cena!");
            return;
        }

        if (gm.GetComponent<UIAutoWirer>() == null)
        {
            gm.gameObject.AddComponent<UIAutoWirer>();
            EditorUtility.SetDirty(gm.gameObject);
            Debug.Log("[UIAutoWirer] UIAutoWirer adicionado ao GameManager.");
        }
        else
        {
            Debug.Log("[UIAutoWirer] UIAutoWirer já existe no GameManager.");
        }

        Selection.activeGameObject = gm.gameObject;
        EditorGUIUtility.PingObject(gm.gameObject);
    }

    static int CountMissingFields(GameManager gm)
    {
        if (gm == null) return 9;
        int count = 0;
        if (gm.scoreText     == null) count++;
        if (gm.progressText  == null) count++;
        if (gm.feedbackPanel == null) count++;
        if (gm.feedbackText  == null) count++;
        if (gm.feedbackIcon  == null) count++;
        if (gm.winPanel      == null) count++;
        if (gm.finalScoreText== null) count++;
        if (gm.heldItemPanel == null) count++;
        if (gm.heldItemText  == null) count++;
        return count;
    }
}
#endif
