using UnityEngine;
using TMPro; // 必须引用

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance;

    [Header("UI 引用")]
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI enemyText;

    void Start()
    {
        // 假设玩家初始血量是 100，你可以根据你朋友的代码修改这个值
        UpdateHP(3);

        // 运行一次计数逻辑，显示当前关卡里有多少怪
        UpdateEnemyCount();
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
    public void UpdateEnemyCount()
    {
        // 实时查找场景中带 Enemy 标签的物体数量
        int count = GameObject.FindGameObjectsWithTag("Enemy").Length;
        enemyText.text = "Enemies: " + count;

        if (count <= 0)
        {
            // 这里可以触发胜利逻辑
            Debug.Log("All enemies eliminated!");
        }
    }
}