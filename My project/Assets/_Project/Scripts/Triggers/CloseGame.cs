using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class CloseGame : MonoBehaviour
{
    [Tooltip("Se impostato, il gioco si chiude solo se l'oggetto che entra nel trigger ha questo tag (es. \"Player\"). Lascia vuoto per chiudere sempre.")]
    [SerializeField] private string requiredTag = "Player";

    // Puoi chiamare questo metodo da un altro script/UnityEvent del tuo trigger.
    public void Close()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Supporto per trigger 3D
    private void OnTriggerEnter(Collider other)
    {
        if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag))
            return;

        Close();
    }

    // Supporto per trigger 2D
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag))
            return;

        Close();
    }
}