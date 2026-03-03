using UnityEngine;

/// <summary>
/// Movimento basico do jogador para teste no Editor sem headset.
/// No Meta Quest, o movimento e feito pelo thumbstick via XR Toolkit.
/// 
/// Controles no Editor:
/// - WASD / Setas: Mover
/// - Mouse: Rotacionar camera
/// - Space: Pular (se CharacterController presente)
/// - E: Depositar item na lixeira mais proxima
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movimento")]
    public float moveSpeed = 4f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.2f;

    [Header("Camera")]
    [Tooltip("Transform da camera para rotacao com mouse.")]
    public Transform cameraTransform;
    public float mouseSensitivity = 2f;
    public float maxPitchAngle = 80f;

    private CharacterController _cc;
    private Vector3 _velocity;
    private float _xRotation = 0f;
    private bool _isGrounded;

    void Start()
    {
        _cc = GetComponent<CharacterController>();

        if (cameraTransform == null)
            cameraTransform = Camera.main?.transform;

#if UNITY_EDITOR
        // Trava o cursor no Editor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
#endif
    }

    void Update()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouseLook();
        HandleMovement();
        HandleEditorShortcuts();
#endif
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        _xRotation -= mouseY;
        _xRotation = Mathf.Clamp(_xRotation, -maxPitchAngle, maxPitchAngle);

        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);

        transform.Rotate(Vector3.up * mouseX);

        // Desbloquear cursor com Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        if (Input.GetMouseButtonDown(0) && Cursor.lockState == CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void HandleMovement()
    {
        _isGrounded = _cc.isGrounded;

        if (_isGrounded && _velocity.y < 0)
            _velocity.y = -2f;

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;
        _cc.Move(move * moveSpeed * Time.deltaTime);

        // Pulo
        if (Input.GetButtonDown("Jump") && _isGrounded)
            _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        _velocity.y += gravity * Time.deltaTime;
        _cc.Move(_velocity * Time.deltaTime);
    }

    void HandleEditorShortcuts()
    {
        // E = depositar na lixeira mais proxima
        if (Input.GetKeyDown(KeyCode.E))
        {
            PlayerInteraction pi = GetComponentInChildren<PlayerInteraction>();
            pi?.TryDepositAtNearestBin();
        }
    }
}
