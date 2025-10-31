using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class Damageable : MonoBehaviour
{
    public int maxHealth = 2;
    public UnityEvent onDeath;

    [SerializeField] float timeUponDeath = 0.3f;

    int current;
    bool isDead;

    void Awake() { current = maxHealth; }

    public int CurrentHealth => current;

    public void SetHealth(int value)
    {
        // se è già morto non fare nulla
        if (isDead) return;

        current = Mathf.Clamp(value, 0, maxHealth);
        if (current <= 0) Die();
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        current -= amount;
        if (current <= 0) Die();
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        onDeath?.Invoke();

        // aspetta 1 secondo e poi cambia scena
        Invoke(nameof(ReloadCurrentScene), timeUponDeath);
    }

    void ReloadCurrentScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
