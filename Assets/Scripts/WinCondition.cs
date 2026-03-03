using UnityEngine;

/// <summary>
/// Helper opcional — verifica periodicamente se todos os itens
/// foram reciclados e aciona a vitoria via GameManager.
/// Util se algum item foi destruido fora do fluxo normal.
/// </summary>
public class WinCondition : MonoBehaviour
{
    [Tooltip("Intervalo de verificacao em segundos.")]
    public float checkInterval = 3f;

    void Start()
    {
        InvokeRepeating(nameof(CheckWin), checkInterval, checkInterval);
    }

    void CheckWin()
    {
        int remaining = FindObjectsOfType<TrashItem>().Length;
        if (remaining == 0)
        {
            Debug.Log("[EcoPark] WinCondition: Nenhum lixo restante na cena!");
            CancelInvoke(nameof(CheckWin));
            // GameManager cuida da vitoria via RegisterRecycled
        }
    }
}
