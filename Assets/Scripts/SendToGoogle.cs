using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class SendToGoogle : MonoBehaviour
{
    [SerializeField] private string URL = "https://docs.google.com/forms/d/e/1FAIpQLSf0_8dBe39T0VX5Nl88u8J2lZ5QN4jDWqYWvHUnY3StHePOsA/formResponse";
    [SerializeField] private string deathPositionEntry = "entry.1421613746";

    private long _sessionID;

    private void Awake()
    {
        // Assign sessionID to identify playtests
        _sessionID = DateTime.Now.Ticks;
    }

    public void SendDeathPosition(Vector3 deathPosition)
    {
        string deathPositionString = $"{deathPosition.x:F3}, {deathPosition.y:F3}, {deathPosition.z:F3}";
        StartCoroutine(PostDeathPosition(deathPositionString));
    }

    private IEnumerator PostDeathPosition(string deathPosition)
    {
        // Create the form and enter response
        WWWForm form = new WWWForm();
        form.AddField(deathPositionEntry, deathPosition);

        // Send response and verify result
        using (UnityWebRequest www = UnityWebRequest.Post(URL, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Google Form upload failed: {www.error}");
            }
            else
            {
                Debug.Log($"Death position uploaded (session {_sessionID}): {deathPosition}");
            }
        }
    }
}
