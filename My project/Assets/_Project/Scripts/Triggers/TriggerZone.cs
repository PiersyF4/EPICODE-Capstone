using UnityEngine;

public enum ActionType
{
    Death,
    Fall,
    End,
    Swallow
}

[RequireComponent(typeof(Collider2D))]
public class TriggerZone : MonoBehaviour
{
    [Header("Riferimenti")]
    public CameraActions cameraActions;

    [Header("Configurazione")]
    public ActionType action = ActionType.Death;
    [Tooltip("Se vuoto, ricarica la scena corrente")]
    public string sceneName = "";
    [Tooltip("Attiva una sola volta")]
    public bool oneShot = true;
    [Tooltip("Solo oggetti con questo tag attivano il trigger (vuoto = qualsiasi)")]
    public string requiredTag = "Player";

    bool triggered;

    void Awake()
    {
        // Assicura che il collider sia trigger
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        // Prova ad auto-assegnare la reference
        if (!cameraActions)
            cameraActions = FindObjectOfType<CameraActions>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Controlli preliminari
        if (oneShot && triggered) return;
        if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag)) return;

        triggered = true;

        // Esegui l'azione
        switch (action)
        {
            case ActionType.Death:    // Morte
                cameraActions.PlayDeathCamera(BlankToNull(sceneName));
                break;
            case ActionType.Fall:     // Caduta
                cameraActions.PlayFallCamera(BlankToNull(sceneName));
                break;
            case ActionType.End:      // Fine
                cameraActions.PlayEndCamera(BlankToNull(sceneName));
                break;
            case ActionType.Swallow:  // Inghiottimento
                cameraActions.PlaySwallowCamera(BlankToNull(sceneName));
                break;
        }

        // Disabilita il collider se one-shot
        if (oneShot)
            colEnabled(false);
    }

    // Abilita/disabilita il collider 2D
    void colEnabled(bool v)
    {
        var col = GetComponent<Collider2D>();
        if (col) col.enabled = v;
    }

    // Converte stringa vuota o spazi bianchi in null
    static string BlankToNull(string s) => string.IsNullOrWhiteSpace(s) ? null : s;
}
