using UnityEngine;
using TMPro; // 必须引用

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance;

    [Header("UI 引用")]
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI enemyText;
    public TextMeshProUGUI flashText;

    void Start()
    {
        if (flashText == null)
        {
            var flashObj = GameObject.Find("Flashlight_Charge_Text");
            if (flashObj != null)
                flashText = flashObj.GetComponent<TextMeshProUGUI>();
        }

        // 假设玩家初始血量是 100，你可以根据你朋友的代码修改这个值
        UpdateHP(3);

        // 运行一次计数逻辑，显示当前关卡里有多少怪
        UpdateEnemyCount(GameObject.FindGameObjectsWithTag("Enemy").Length);
        UpdateFlash(0f);
    }

    void Awake()
    {
        Instance = this;
    }

    // 更新血量的方法
    public void UpdateHP(int currentHP)
    {
        hpText.text = "HP: " + currentHP;
    }

    // 更新剩余敌人的方法
    public void UpdateEnemyCount(int newCount)
    {
        Debug.Log("Updated count");
        enemyText.text = "Enemies: " + newCount;
        if (newCount <= 0)
        {
            Debug.Log("All enemies eliminated!");
        }
    }

    public void UpdateFlash(float currentFlash)
    {
        if (flashText == null) return;
        flashText.text = "Flash: " + currentFlash.ToString("F1") + "s / 5s";
    }
}