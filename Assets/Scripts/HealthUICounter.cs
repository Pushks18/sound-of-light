using UnityEngine;
using TMPro;

public class HealthUICounter : MonoBehaviour
{
    public PlayerHealth player;
    public EnemyHealth enemy;
    public TMP_Text playerText;
    public TMP_Text enemyText;

    void Start()
    {
        if (player == null)
            player = FindObjectOfType<PlayerHealth>();
        if (enemy == null)
            enemy = FindObjectOfType<EnemyHealth>();
    }

    void Update()
    {
        if (player != null && playerText != null)
            playerText.text = $"Player HP: {player.currentHealth} / {player.maxHealth}";

        if (enemy != null && enemyText != null)
            enemyText.text = $"Enemy HP: {enemy.currentHealth} / {enemy.maxHealth}";
    }
}