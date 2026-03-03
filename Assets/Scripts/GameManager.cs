using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Gerenciador central do EcoPark VR.
/// Singleton que controla pontuacao, feedback, estado do item segurado
/// e condicao de vitoria.
/// </summary>
public class GameManager : MonoBehaviour
{
    // ─── SINGLETON ───────────────────────────────────────────
    public static GameManager Instance { get; private set; }

    // ─── REFERENCIAS DE UI ────────────────────────────────────
    [Header("UI - Pontuacao")]
    [Tooltip("Texto que exibe a pontuacao atual.")]
    public TMP_Text scoreText;

    [Tooltip("Texto que exibe o progresso de itens reciclados.")]
    public TMP_Text progressText;

    [Header("UI - Feedback")]
    [Tooltip("Painel de feedback (mensagens de acerto/erro).")]
    public GameObject feedbackPanel;

    [Tooltip("Texto da mensagem de feedback.")]
    public TMP_Text feedbackText;

    [Tooltip("Icone de feedback (verde = acerto, vermelho = erro).")]
    public UnityEngine.UI.Image feedbackIcon;

    [Header("UI - Vitoria")]
    [Tooltip("Painel exibido ao reciclar todos os itens.")]
    public GameObject winPanel;

    [Tooltip("Texto da pontuacao final na tela de vitoria.")]
    public TMP_Text finalScoreText;

    [Header("UI - Item Segurado")]
    [Tooltip("Texto que indica qual lixo o jogador esta segurando.")]
    public TMP_Text heldItemText;

    [Tooltip("Painel do item segurado.")]
    public GameObject heldItemPanel;

    // ─── CONFIGURACAO DO JOGO ─────────────────────────────────
    [Header("Configuracao")]
    [Tooltip("Pontos por descarte correto.")]
    public int pointsPerCorrect = 10;

    [Tooltip("Bonus por completar sem erros.")]
    public int perfectBonus = 50;

    [Tooltip("Duracao do feedback em segundos.")]
    public float feedbackDuration = 2.5f;

    // ─── ESTADO INTERNO ───────────────────────────────────────
    private int _score = 0;
    private int _totalTrashItems = 0;
    private int _recycledCount = 0;
    private int _errorCount = 0;
    private TrashItem _heldItem = null;

    // Registro de quantos de cada tipo foram reciclados
    private Dictionary<TrashType, int> _recycledByType = new Dictionary<TrashType, int>
    {
        { TrashType.Plastic, 0 },
        { TrashType.Paper,   0 },
        { TrashType.Glass,   0 }
    };

    // ─── INICIALIZACAO ────────────────────────────────────────
    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        // Conta o total de itens de lixo na cena
        _totalTrashItems = FindObjectsOfType<TrashItem>().Length;

        UpdateScoreUI();
        UpdateProgressUI();

        // Esconde paineis inicialmente
        if (feedbackPanel != null) feedbackPanel.SetActive(false);
        if (winPanel != null)      winPanel.SetActive(false);
        if (heldItemPanel != null) heldItemPanel.SetActive(false);

        Debug.Log($"[EcoPark] Jogo iniciado! Total de itens: {_totalTrashItems}");
    }

    // ─── ITEM SEGURADO ────────────────────────────────────────

    public void SetHeldItem(TrashItem item)
    {
        _heldItem = item;

        if (heldItemPanel != null) heldItemPanel.SetActive(item != null);
        if (heldItemText != null && item != null)
        {
            string tipoBin = item.trashType switch
            {
                TrashType.Plastic => "Lixeira VERMELHA",
                TrashType.Paper   => "Lixeira AZUL",
                TrashType.Glass   => "Lixeira VERDE",
                _                 => "?"
            };
            heldItemText.text = $"Segurando: {item.trashType}\nDepositar na {tipoBin}";
        }
    }

    public void ClearHeldItem()
    {
        _heldItem = null;
        if (heldItemPanel != null) heldItemPanel.SetActive(false);
    }

    public TrashItem GetHeldItem() => _heldItem;

    public bool HasHeldItem() => _heldItem != null;

    // ─── PONTUACAO ────────────────────────────────────────────

    public void AddScore(int points)
    {
        _score += points;
        UpdateScoreUI();
        Debug.Log($"[EcoPark] Score: {_score}");
    }

    void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = $"Pontos: {_score}";
    }

    // ─── PROGRESSO ────────────────────────────────────────────

    public void RegisterRecycled(TrashType type)
    {
        _recycledCount++;
        _recycledByType[type]++;
        UpdateProgressUI();

        if (_recycledCount >= _totalTrashItems)
        {
            StartCoroutine(TriggerWin());
        }
    }

    void UpdateProgressUI()
    {
        if (progressText != null)
            progressText.text = $"Reciclados: {_recycledCount}/{_totalTrashItems}";
    }

    // ─── FEEDBACK ─────────────────────────────────────────────

    public void ShowFeedback(string message, bool isCorrect)
    {
        if (!isCorrect) _errorCount++;

        if (feedbackPanel == null) return;

        feedbackPanel.SetActive(true);

        if (feedbackText != null)
        {
            feedbackText.text = message;
            feedbackText.color = isCorrect
                ? new Color(0.1f, 0.8f, 0.2f)
                : new Color(0.9f, 0.2f, 0.1f);
        }

        if (feedbackIcon != null)
        {
            feedbackIcon.color = isCorrect
                ? new Color(0.1f, 0.8f, 0.2f, 0.3f)
                : new Color(0.9f, 0.2f, 0.1f, 0.3f);
        }

        CancelInvoke(nameof(HideFeedback));
        Invoke(nameof(HideFeedback), feedbackDuration);
    }

    void HideFeedback()
    {
        if (feedbackPanel != null)
            feedbackPanel.SetActive(false);
    }

    // ─── VITORIA ─────────────────────────────────────────────

    IEnumerator TriggerWin()
    {
        yield return new WaitForSeconds(1f);

        HideFeedback();

        // Bonus por jogo perfeito
        if (_errorCount == 0)
        {
            _score += perfectBonus;
            UpdateScoreUI();
        }

        if (winPanel != null) winPanel.SetActive(true);

        if (finalScoreText != null)
        {
            string perfectMsg = _errorCount == 0 ? "\n🌟 Jogo Perfeito! +50 bonus!" : "";
            finalScoreText.text =
                $"Parabens! Voce reciclou tudo!\n\n" +
                $"Pontuacao Final: {_score}{perfectMsg}\n\n" +
                $"Plastico: {_recycledByType[TrashType.Plastic]} itens\n" +
                $"Papel:    {_recycledByType[TrashType.Paper]} itens\n" +
                $"Vidro:    {_recycledByType[TrashType.Glass]} itens";
        }

        Debug.Log($"[EcoPark] VITORIA! Score: {_score} | Erros: {_errorCount}");
    }

    // ─── REINICIAR ────────────────────────────────────────────

    public void RestartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }
}
