using UnityEngine;

/// <summary>
/// Define o tipo de lixo que o item representa.
/// Cada tipo deve ser descartado na lixeira correspondente.
/// </summary>
public enum TrashType
{
    Plastic,  // Lixeira Vermelha
    Paper,    // Lixeira Azul
    Glass     // Lixeira Verde
}

/// <summary>
/// Componente aplicado a cada objeto de lixo no parque.
/// Gerencia coleta, estado e reset do item.
/// </summary>
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class TrashItem : MonoBehaviour
{
    [Header("Tipo do Lixo")]
    [Tooltip("Defina o tipo correto para este item de lixo.")]
    public TrashType trashType;

    [Header("Configuracao Visual")]
    [Tooltip("Highlight quando o jogador mira no lixo.")]
    public Material highlightMaterial;

    [Header("Estado Interno")]
    [HideInInspector] public bool isCollected = false;

    // Guardar estado inicial para reset
    private Vector3 _originalPosition;
    private Quaternion _originalRotation;
    private Renderer _renderer;
    private Material _originalMaterial;
    private Rigidbody _rb;
    private Collider _col;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _col = GetComponent<Collider>();
        _renderer = GetComponent<Renderer>();

        if (_renderer != null)
            _originalMaterial = _renderer.sharedMaterial;
    }

    void Start()
    {
        _originalPosition = transform.position;
        _originalRotation = transform.rotation;

        // Garante que o collider NAO seja trigger (jogador interage fisicamente)
        _col.isTrigger = false;
        gameObject.tag = "TrashItem";
        gameObject.layer = LayerMask.NameToLayer("Interactable");
    }

    /// <summary>
    /// Chamado quando o jogador interage com o item.
    /// Oculta o objeto e notifica o GameManager.
    /// </summary>
    public void Collect()
    {
        if (isCollected) return;

        isCollected = true;
        _rb.isKinematic = true;
        _col.enabled = false;
        gameObject.SetActive(false);

        GameManager.Instance.SetHeldItem(this);
        Debug.Log($"[EcoPark] Lixo coletado: {gameObject.name} ({trashType})");
    }

    /// <summary>
    /// Retorna o item ao estado e posicao original (erro de descarte).
    /// </summary>
    public void ResetItem()
    {
        isCollected = false;
        _rb.isKinematic = false;
        _col.enabled = true;

        transform.position = _originalPosition;
        transform.rotation = _originalRotation;
        gameObject.SetActive(true);

        SetHighlight(false);
        Debug.Log($"[EcoPark] Lixo resetado: {gameObject.name}");
    }

    /// <summary>
    /// Ativa/desativa o highlight visual ao mirar no item.
    /// </summary>
    public void SetHighlight(bool active)
    {
        if (_renderer == null) return;

        if (active && highlightMaterial != null)
            _renderer.material = highlightMaterial;
        else if (_originalMaterial != null)
            _renderer.material = _originalMaterial;
    }
}
