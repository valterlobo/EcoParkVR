using UnityEngine;

/// <summary>
/// Faz o canvas da lixeira sempre ficar de frente para a camera.
/// Adicione nos labels flutuantes das lixeiras.
/// </summary>
public class BillboardCanvas : MonoBehaviour
{
    private Camera _mainCamera;

    void Start()
    {
        _mainCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (_mainCamera == null) return;

        // Rotaciona para olhar para a camera (apenas eixo Y)
        Vector3 dir = _mainCamera.transform.position - transform.position;
        dir.y = 0;

        if (dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(-dir);
    }
}
