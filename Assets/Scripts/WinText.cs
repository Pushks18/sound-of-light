using UnityEngine;
using TMPro;

public class WinText : MonoBehaviour
{
    private static WinText instance;

    void Awake()
    {
        instance = this;
        gameObject.SetActive(false);
    }

    public static void Show()
    {
        if (instance != null)
            instance.gameObject.SetActive(true);
    }
}