using System.Collections;
using UnityEngine;
using TMPro;

public class RoundTimer : MonoBehaviour
{
    [Header("Round Settings")]
    public int currentRound = 1;
    public float roundTime = 60f;

    [Header("UI References")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI roundAnnounceText;

    [Header("Announce Animation Settings")]
    public float animationDuration = 1.5f;
    public float startScale = 1.0f;
    public float endScale = 1.8f;

    private float currentTime;
    private bool isTimerRunning = false;

    private void Start()
    {
        StartNewRound(currentRound);
    }

    private void Update()
    {
        if (!isTimerRunning) return;

        if (currentTime > 0)
        {
            currentTime -= Time.deltaTime;
            UpdateTimerText(currentTime);
        }
        else
        {
            currentTime = 0;
            UpdateTimerText(currentTime);
            OnRoundEnd();
        }
    }

    public void StartNewRound(int roundNumber)
    {
        currentRound = roundNumber;
        currentTime = roundTime;
        isTimerRunning = true;

        if (roundAnnounceText != null)
        {
            StartCoroutine(ShowRoundAnnounceRoutine());
        }
    }

    private IEnumerator ShowRoundAnnounceRoutine()
    {
        roundAnnounceText.gameObject.SetActive(true);
        roundAnnounceText.text = $"{currentRound} WAVE";

        Vector3 initialScale = Vector3.one * startScale;
        Vector3 targetScale = Vector3.one * endScale;

        Color baseColor = roundAnnounceText.color;

        float elapsedTime = 0f;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / animationDuration;

            roundAnnounceText.transform.localScale = Vector3.Lerp(initialScale, targetScale, progress);

            float alpha = Mathf.Lerp(1f, 0f, progress);
            roundAnnounceText.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);

            yield return null;
        }

        roundAnnounceText.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f);
        roundAnnounceText.transform.localScale = initialScale;
        roundAnnounceText.gameObject.SetActive(false);
    }

    private void UpdateTimerText(float timeToDisplay)
    {
        if (timerText == null) return;

        if (timeToDisplay < 0) timeToDisplay = 0;

        int minutes = Mathf.FloorToInt(timeToDisplay / 60);
        int seconds = Mathf.FloorToInt(timeToDisplay % 60);

        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    private void OnRoundEnd()
    {
        isTimerRunning = false;
    }
}