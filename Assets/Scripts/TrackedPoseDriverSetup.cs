using UnityEngine;

/// <summary>
/// Adiciona automaticamente o TrackedPoseDriver na Main Camera em runtime
/// se ele ainda nao estiver presente.
///
/// Coloque este script no mesmo GameObject do XR Origin ou do GameManager.
/// Ele roda no Awake(), antes do XROrigin reclamar no console.
///
/// Compativel com:
///   - UnityEngine.InputSystem.XR.TrackedPoseDriver  (Input System 1.x)
///   - UnityEngine.SpatialTracking.TrackedPoseDriver (XR Legacy)
/// </summary>
[DefaultExecutionOrder(-50)]
public class TrackedPoseDriverSetup : MonoBehaviour
{
    void Awake()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[TrackedPoseDriverSetup] Main Camera nao encontrada.");
            return;
        }

        // Verifica se ja tem algum TrackedPoseDriver (qualquer versao)
        if (HasTrackedPoseDriver(cam.gameObject)) return;

        // Tenta adicionar via reflection — sem depender de using especifico
        bool added =
            TryAdd(cam.gameObject, "UnityEngine.InputSystem.XR.TrackedPoseDriver, Unity.InputSystem") ||
            TryAdd(cam.gameObject, "UnityEngine.InputSystem.XR.TrackedPoseDriver, UnityEngine.InputSystem") ||
            TryAdd(cam.gameObject, "UnityEngine.SpatialTracking.TrackedPoseDriver, UnityEngine.SpatialTracking");

        if (added)
            Debug.Log("[TrackedPoseDriverSetup] TrackedPoseDriver adicionado na Main Camera.");
        else
            Debug.LogWarning("[TrackedPoseDriverSetup] TrackedPoseDriver nao encontrado. " +
                "Instale o Meta XR SDK ou XR Interaction Toolkit e adicione manualmente na Main Camera.");
    }

    static bool HasTrackedPoseDriver(GameObject go)
    {
        foreach (var c in go.GetComponents<Component>())
        {
            if (c == null) continue;
            string typeName = c.GetType().Name;
            if (typeName == "TrackedPoseDriver") return true;
        }
        return false;
    }

    static bool TryAdd(GameObject go, string fullTypeName)
    {
        System.Type t = System.Type.GetType(fullTypeName);
        if (t == null) return false;

        var component = go.AddComponent(t);

        // Configura trackingType = RotationAndPosition via reflection
        try
        {
            var prop = t.GetProperty("trackingType");
            if (prop != null)
            {
                var val = System.Enum.ToObject(prop.PropertyType, 0); // 0 = RotationAndPosition
                prop.SetValue(component, val);
            }
        }
        catch { /* propriedade pode ter nome diferente em versoes antigas */ }

        return true;
    }
}
