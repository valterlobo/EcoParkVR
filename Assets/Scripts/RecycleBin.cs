using System.Collections;
using UnityEngine;

/// <summary>
/// Componente aplicado a cada lixeira colorida do parque.
/// Detecta quando um item de lixo e depositado e valida o tipo correto.
/// </summary>
[RequireComponent(typeof(Collider))]
public class RecycleBin : MonoBehaviour
{
    [Header("Configuracao da Lixeira")]
    [Tooltip("Tipo de lixo que esta lixeira aceita.")]
    public TrashType acceptedType;

    [Tooltip("Nome visivel da lixeira (ex: Papel, Plastico, Vidro).")]
    public string binLabel = "Lixeira";

    [Header("Sons")]
    [Tooltip("Som tocado ao descartar corretamente.")]
    public AudioClip correctSound;

    [Tooltip("Som tocado ao descartar erroneamente.")]
    public AudioClip wrongSound;

    [Header("Efeitos Visuais")]
    [Tooltip("Cor de highlight quando o jogador se aproxima com item compativel.")]
    public Color glowColor = Color.green;

    [Tooltip("Intensidade do efeito de escala ao acertar (1.0 = sem efeito).")]
    public float successPunchScale = 1.3f;

    private AudioSource _audioSource;
    private Renderer _renderer;
    private Color _originalEmission;
    private Collider _triggerZone;

    void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();

        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 1f; // Audio 3D no VR

        _renderer = GetComponent<Renderer>();
    }

    void Start()
    {
        // O trigger zone eh o collider da lixeira - deve ser isTrigger = true
        _triggerZone = GetComponent<Collider>();
        _triggerZone.isTrigger = true;

        gameObject.tag = "RecycleBin";
    }

    /// <summary>
    /// Detecta o item de lixo quando entra na zona de colisao da lixeira.
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        // Verifica se o objeto que entrou e um TrashItem
        TrashItem trash = other.GetComponent<TrashItem>();
        if (trash == null) return;

        // Verifica se este item foi coletado pelo jogador
        TrashItem held = GameManager.Instance.GetHeldItem();
        if (held == null || held != trash) return;

        ProcessDeposit(trash);
    }

    /// <summary>
    /// Processa o deposito do lixo: correto ou incorreto.
    /// </summary>
    void ProcessDeposit(TrashItem trash)
    {
        bool isCorrect = (trash.trashType == acceptedType);

        if (isCorrect)
        {
            OnCorrectDeposit(trash);
        }
        else
        {
            OnWrongDeposit(trash);
        }
    }

    void OnCorrectDeposit(TrashItem trash)
    {
        Debug.Log($"[EcoPark] CORRETO! {trash.trashType} na {binLabel}");

        // Som de acerto
        if (correctSound != null)
            _audioSource.PlayOneShot(correctSound);

        // Efeito de escala (punch) ao acertar
        StartCoroutine(PunchScale());

        // Pontuacao e feedback
        GameManager.Instance.AddScore(10);
        GameManager.Instance.ShowFeedback($"Correto! {trash.trashType} vai na {binLabel}!", true);
        GameManager.Instance.ClearHeldItem();
        GameManager.Instance.RegisterRecycled(trash.trashType);

        // Destroi o item com um pequeno delay para o som tocar
        GameObject trashObj = trash.gameObject;
        trashObj.SetActive(false);
        Destroy(trashObj, 0.5f);
    }

    void OnWrongDeposit(TrashItem trash)
    {
        Debug.Log($"[EcoPark] ERRADO! {trash.trashType} nao vai na {binLabel}");

        // Som de erro
        if (wrongSound != null)
            _audioSource.PlayOneShot(wrongSound);

        // Feedback de erro
        GameManager.Instance.ShowFeedback(
            $"Ops! {trash.trashType} nao vai na {binLabel}. Tente novamente!", false);

        // Reset do item para a posicao original
        trash.ResetItem();
        GameManager.Instance.ClearHeldItem();

        // Efeito visual de erro na lixeira
        StartCoroutine(FlashError());
    }

    IEnumerator FlashError()
    {
        if (_renderer == null) yield break;

        Color originalColor = _renderer.material.color;
        _renderer.material.color = Color.red;
        yield return new WaitForSeconds(0.3f);
        _renderer.material.color = originalColor;
        yield return new WaitForSeconds(0.1f);
        _renderer.material.color = Color.red;
        yield return new WaitForSeconds(0.3f);
        _renderer.material.color = originalColor;
    }

    IEnumerator PunchScale()
    {
        Vector3 original = transform.localScale;
        Vector3 target   = original * successPunchScale;
        float duration   = 0.12f;

        // Escala para cima
        float t = 0f;
        while (t < duration)
        {
            transform.localScale = Vector3.Lerp(original, target, t / duration);
            t += Time.deltaTime;
            yield return null;
        }
        // Volta ao tamanho original
        t = 0f;
        while (t < duration)
        {
            transform.localScale = Vector3.Lerp(target, original, t / duration);
            t += Time.deltaTime;
            yield return null;
        }
        transform.localScale = original;
    }

    /// <summary>
    /// Retorna a cor associada ao tipo de lixo desta lixeira.
    /// </summary>
    public Color GetBinColor()
    {
        return acceptedType switch
        {
            TrashType.Plastic => new Color(0.9f, 0.1f, 0.1f), // Vermelho
            TrashType.Paper   => new Color(0.1f, 0.3f, 0.9f), // Azul
            TrashType.Glass   => new Color(0.1f, 0.7f, 0.2f), // Verde
            _                 => Color.gray
        };
    }
}
