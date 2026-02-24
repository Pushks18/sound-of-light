using UnityEngine;
using TMPro;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance;

    
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI enemyText;
    public TextMeshProUGUI flashText;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        var player = FindAnyObjectByType<PlayerHealth>();
        UpdateHP(player != null ? player.maxHealth : 3);
        UpdateFlash(0f);
    }

    public void UpdateHP(int currentHP)
    {
        if (hpText != null)
            hpText.text = "HP: " + currentHP;
    }

    public void UpdateEnemyCount(int newCount)
    {
        Debug.Log("Updating UI to: " + newCount);
        if (enemyText != null)
            enemyText.text = "Enemies: " + newCount;
    }

    public void UpdateFlash(float currentFlash)
    {
        if (flashText != null)
            flashText.text = "Flash: " + currentFlash.ToString("F1") + "s / 5s";
    }
}