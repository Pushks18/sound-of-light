using UnityEngine;
using TMPro;

public class HealthUICounter : MonoBehaviour
{
    public PlayerHealth player;
    public EnemyHealth enemy;
    public TMP_Text playerText;
    public TMP_Text enemyText;
    public TMP_Text flashlightText;

    private LightEnergy lightEnergy;

    void Start()
    {
        if (player == null)
            player = FindAnyObjectByType<PlayerHealth>();
        if (enemy == null)
            enemy = FindAnyObjectByType<EnemyHealth>();
        lightEnergy = FindAnyObjectByType<LightEnergy>();
    }

    void Update()
    {
        if (player != null && playerText != null)
            playerText.text = $"Player HP: {player.currentHealth} / {player.maxHealth}";

        if (enemy != null && enemyText != null)
            enemyText.text = $"Enemy HP: {enemy.currentHealth} / {enemy.maxHealth}";

        if (lightEnergy != null && flashlightText != null)
            flashlightText.text = $"Energy: {lightEnergy.CurrentEnergy:F1} / {lightEnergy.MaxEnergy}";
    }
}
