using UnityEngine;

/// <summary>
/// Faz o item de lixo coletado seguir visualmente a mao/controlador do jogador.
/// Adicione este componente ao objeto vazio "ItemHolder" que fica
/// como filho do XR Controller ou da Main Camera.
/// </summary>
public class HeldItemFollower : MonoBehaviour
{
    [Header("Configuracao")]
    [Tooltip("Offset de posicao em relacao ao ponto de ancoragem (mao/camera).")]
    public Vector3 holdOffset = new Vector3(0.3f, -0.2f, 0.5f);

    [Tooltip("Velocidade de seguimento suave do item.")]
    public float followSpeed = 15f;

    [Tooltip("Velocidade de rotacao suave do item.")]
    public float rotateSpeed = 10f;

    [Tooltip("Escala do item enquanto segurado (menor para nao obstruir visao).")]
    public float heldScale = 0.5f;

    private TrashItem _currentItem;
    private Transform _anchorTransform;

    void Start()
    {
        _anchorTransform = transform;
    }

    void Update()
    {
        TrashItem held = GameManager.Instance?.GetHeldItem();

        if (held != _currentItem)
        {
            // Novo item coletado
            if (held != null)
                AttachItem(held);
            else
                DetachItem();

            _currentItem = held;
        }

        // Segue suavemente a posicao de ancoragem
        if (_currentItem != null && _currentItem.isCollected)
        {
            Vector3 targetPos = _anchorTransform.TransformPoint(holdOffset);
            _currentItem.transform.position = Vector3.Lerp(
                _currentItem.transform.position,
                targetPos,
                Time.deltaTime * followSpeed);

            _currentItem.transform.rotation = Quaternion.Lerp(
                _currentItem.transform.rotation,
                _anchorTransform.rotation,
                Time.deltaTime * rotateSpeed);
        }
    }

    void AttachItem(TrashItem item)
    {
        item.gameObject.SetActive(true);
        item.transform.localScale = Vector3.one * heldScale;

        var rb = item.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        var col = item.GetComponent<Collider>();
        if (col != null) col.isTrigger = true; // Vira trigger para colidir com lixeiras
    }

    void DetachItem()
    {
        // Nada a fazer — o item ja foi destruido ou resetado pelo RecycleBin
    }
}
