using UnityEngine;

/// <summary>
/// Gerencia a interacao do jogador com os itens de lixo.
///
/// Nao usa com.unity.inputsystem — usa apenas Input classico do Unity,
/// evitando o NullReferenceException na inicializacao do InputSystem.
///
/// Suporte de input:
/// - Editor / PC   : mouse + teclado (Input classico)
/// - Meta Quest VR : polling de eixos XR (sem dependencia de SDK especifico)
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    [Header("Configuracao de Interacao")]
    [Tooltip("Distancia maxima de interacao em metros.")]
    public float interactionDistance = 4f;

    [Tooltip("Layer dos objetos interagiveis (TrashItem).")]
    public LayerMask interactableLayer;

    [Header("Feedback Visual - Crosshair")]
    [Tooltip("Crosshair/reticle que aparece ao mirar em um item.")]
    public GameObject crosshair;

    [Tooltip("Cor do crosshair ao mirar em item interagivel.")]
    public Color crosshairHighlightColor = Color.yellow;

    [Tooltip("Cor padrao do crosshair.")]
    public Color crosshairNormalColor = Color.white;

    [Header("Audio")]
    [Tooltip("Som ao coletar um item de lixo.")]
    public AudioClip collectSound;

    // ── Privado ──────────────────────────────────────────────────
    private Camera _mainCamera;
    private AudioSource _audioSource;
    private TrashItem _currentHighlighted;
    private UnityEngine.UI.Image _crosshairImage;

    // Debounce do trigger VR
    private bool  _triggerWasDown   = false;
    private float _triggerThreshold = 0.7f;

    // ─── INICIALIZACAO ────────────────────────────────────────────
    void Start()
    {
        _mainCamera = Camera.main;

        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();

        _audioSource.spatialBlend = 0f;
        _audioSource.playOnAwake  = false;

        if (crosshair != null)
            _crosshairImage = crosshair.GetComponent<UnityEngine.UI.Image>();
    }

    // ─── UPDATE ───────────────────────────────────────────────────
    void Update()
    {
        HandleHighlight();
        HandleInput();
    }

    void HandleInput()
    {
        // Clique de mouse (Editor / PC standalone)
        if (Input.GetMouseButtonDown(0))
        {
            TryInteract();
            return;
        }

        // Trigger VR via Input classico — sem InputSystem
        float trigger  = GetVRTrigger();
        bool  isDown   = trigger >= _triggerThreshold;
        if (isDown && !_triggerWasDown) TryInteract();
        _triggerWasDown = isDown;

        // Tecla E: depositar na lixeira mais proxima
        if (Input.GetKeyDown(KeyCode.E))
            TryDepositAtNearestBin();
    }

    /// <summary>
    /// Le o trigger direito usando apenas Input classico do Unity.
    /// Compativel com Meta Quest, OpenXR, SteamVR legacy e PC.
    /// </summary>
    float GetVRTrigger()
    {
        // Oculus / Meta SDK (funciona sem pacote InputSystem)
        float v = TryAxis("Oculus_CrossPlatform_SecondaryIndexTrigger");
        if (v > 0f) return v;

        // OpenXR / XR Toolkit axis name
        v = TryAxis("XRI_Right_TriggerButton");
        if (v > 0f) return v;

        // SteamVR legacy
        v = TryAxis("RightTrigger");
        if (v > 0f) return v;

        // Fallback: Fire1 (espaco / ctrl / trigger)
        return Input.GetAxis("Fire1");
    }

    float TryAxis(string name)
    {
        try   { return Input.GetAxis(name); }
        catch { return 0f; }
    }

    // ─── HIGHLIGHT ────────────────────────────────────────────────
    void HandleHighlight()
    {
        Ray ray = GetInteractionRay();
        TrashItem newTarget = null;

        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactableLayer))
            newTarget = hit.collider.GetComponent<TrashItem>();

        if (newTarget != _currentHighlighted)
        {
            if (_currentHighlighted != null) _currentHighlighted.SetHighlight(false);
            _currentHighlighted = newTarget;
            if (_currentHighlighted != null) _currentHighlighted.SetHighlight(true);
        }

        bool canInteract = newTarget != null
                        && GameManager.Instance != null
                        && !GameManager.Instance.HasHeldItem();
        UpdateCrosshair(canInteract);
    }

    void UpdateCrosshair(bool canInteract)
    {
        if (_crosshairImage == null) return;
        _crosshairImage.color = canInteract ? crosshairHighlightColor : crosshairNormalColor;
    }

    // ─── INTERACAO ────────────────────────────────────────────────
    void TryInteract()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.HasHeldItem()) return;

        Ray ray = GetInteractionRay();
        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactableLayer))
        {
            TrashItem trash = hit.collider.GetComponent<TrashItem>();
            if (trash != null && !trash.isCollected) CollectItem(trash);
        }
    }

    void CollectItem(TrashItem trash)
    {
        trash.Collect();
        if (collectSound != null) _audioSource.PlayOneShot(collectSound);
        _currentHighlighted = null;
        Debug.Log($"[EcoPark] Coletou: {trash.name}");
    }

    // ─── DEPOSITO MANUAL ─────────────────────────────────────────
    public void TryDepositAtNearestBin()
    {
        if (GameManager.Instance == null || !GameManager.Instance.HasHeldItem()) return;

        RecycleBin nearest     = null;
        float      nearestDist = float.MaxValue;

        foreach (var bin in FindObjectsOfType<RecycleBin>())
        {
            float dist = Vector3.Distance(transform.position, bin.transform.position);
            if (dist < nearestDist) { nearestDist = dist; nearest = bin; }
        }

        if (nearest != null && nearestDist <= 2.5f)
            nearest.SendMessage("ProcessDeposit",
                GameManager.Instance.GetHeldItem(),
                SendMessageOptions.DontRequireReceiver);
        else
            GameManager.Instance.ShowFeedback("Chegue mais perto de uma lixeira!", false);
    }

    // ─── RAY ─────────────────────────────────────────────────────
    Ray GetInteractionRay()
    {
        if (_mainCamera == null) _mainCamera = Camera.main;
#if UNITY_EDITOR
        if (!UnityEngine.XR.XRSettings.isDeviceActive)
            return _mainCamera.ScreenPointToRay(Input.mousePosition);
#endif
        return new Ray(_mainCamera.transform.position, _mainCamera.transform.forward);
    }
}
