using UnityEngine;

public class LightEnergy : MonoBehaviour
{
    public float maxEnergy = 500f;
    public float regenRate = 1f;
    public float regenDelay = 1.5f;

    private float currentEnergy;
    private float lastSpendTime;

    public float CurrentEnergy => currentEnergy;
    public float MaxEnergy => maxEnergy;
    public float EnergyPercent => currentEnergy / maxEnergy;

    void Awake()
    {
        currentEnergy = maxEnergy;
    }

    void Update()
    {
        if (Time.time - lastSpendTime >= regenDelay && currentEnergy < maxEnergy)
        {
            currentEnergy = Mathf.Min(currentEnergy + regenRate * Time.deltaTime, maxEnergy);
        }
    }

    public bool CanSpend(float amount)
    {
        return currentEnergy >= amount;
    }

    public bool TrySpend(float amount)
    {
        if (currentEnergy < amount)
            return false;

        Spend(amount);
        return true;
    }

    public void Spend(float amount)
    {
        currentEnergy = Mathf.Max(0f, currentEnergy - amount);
        lastSpendTime = Time.time;
    }
}
