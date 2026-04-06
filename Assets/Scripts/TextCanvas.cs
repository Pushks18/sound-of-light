using UnityEngine;
using TMPro;
using System.Collections;

public class TextCanvas : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textTitle;
    private void Start()
    {
        StartCoroutine(HideTitleAfterDelay());
    }
    private IEnumerator HideTitleAfterDelay()
    {
        yield return new WaitForSeconds(3f);

        if (textTitle != null)
        {
            textTitle.gameObject.SetActive(false);
        }
    }
}
