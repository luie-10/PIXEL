using System.Collections;
using UnityEngine;
using TMPro;

[System.Serializable]
public class RoundData
{
    public int roundNumber = 1;      // 라운드 번호
    public float roundTime = 60f;    // 해당 라운드의 제한시간(초)
}

public class RoundTimer : MonoBehaviour
{
    [Header("Round Settings (Array)")]
    public RoundData[] rounds;
    private int currentRoundIndex = 0;

    [Header("UI References")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI roundAnnounceText;

    [Header("Announce Animation Settings")]
    public float animationDuration = 1.5f;
    public float startScale = 1.0f;
    public float endScale = 1.8f;

    [Header("Result UI & Hide Settings")]
    [Tooltip("라운드 종료 시 숨길 인게임 UI 오브젝트 목록")]
    public GameObject[] uiToHide;
    [Tooltip("라운드 종료 시 활성화할 결과 창 패널")]
    public GameObject resultUIPanel;
    [Tooltip("결과 합산 및 요약을 한 줄씩 출력할 텍스트 UI")]
    public TextMeshProUGUI resultSummaryText;

    [Header("Enemy Management Settings")]
    [Tooltip("필드의 적 태그 이름 (기본값: Enemy)")]
    public string enemyTag = "Enemy";

    private float currentTime;
    private bool isTimerRunning = false;

    private void Start()
    {
        if (resultUIPanel != null)
        {
            resultUIPanel.SetActive(false);
        }

        // SettingsManager에 기존 라운드 기록이 있다면 해당 라운드부터 시작
        int startIndex = 0;
        if (SettingsManager.Instance != null)
        {
            startIndex = SettingsManager.Instance.CurrentRoundIndex;
        }

        StartRoundByIndex(startIndex);
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

    public void StartRoundByIndex(int index)
    {
        if (rounds == null || rounds.Length == 0)
        {
            Debug.LogWarning("RoundTimer: 라운드 데이터가 설정되지 않았습니다.");
            return;
        }

        if (index < 0 || index >= rounds.Length)
        {
            Debug.Log("모든 라운드가 완료되었거나 잘못된 라운드 인덱스입니다.");
            return;
        }

        currentRoundIndex = index;
        currentTime = rounds[currentRoundIndex].roundTime;
        isTimerRunning = true;

        // SettingsManager와 라운드 정보 동기화
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SetRoundIndex(currentRoundIndex);
        }

        if (roundAnnounceText != null)
        {
            StartCoroutine(ShowRoundAnnounceRoutine());
        }
    }

    private IEnumerator ShowRoundAnnounceRoutine()
    {
        roundAnnounceText.gameObject.SetActive(true);
        roundAnnounceText.text = $"{rounds[currentRoundIndex].roundNumber} WAVE";

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

        UpdateTimerText(0f);

        StopSpawningAndClearEnemies();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SaveCurrentRoundData();
        }

        HideInGameUI();

        ShowResultUI();
    }

    private void StopSpawningAndClearEnemies()
    {
        SpawnManager spawnManager = FindObjectOfType<SpawnManager>();
        if (spawnManager != null)
        {
            spawnManager.enabled = false;
        }

        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        for (int i = 0; i < enemies.Length; i++)
        {
            Destroy(enemies[i]);
        }
    }

    private void HideInGameUI()
    {
        if (uiToHide == null) return;

        for (int i = 0; i < uiToHide.Length; i++)
        {
            if (uiToHide[i] != null)
            {
                uiToHide[i].SetActive(false);
            }
        }
    }

    private void ShowResultUI()
    {
        if (resultUIPanel != null)
        {
            resultUIPanel.SetActive(true);
        }

        if (resultSummaryText != null && GameManager.Instance != null)
        {
            StartCoroutine(GameManager.Instance.ShowSummaryTextLineByLine(resultSummaryText));
        }
    }

    /// <summary>
    /// 다음 라운드로 진행할 때 호출할 함수 (버튼 등에 연결)
    /// </summary>
    public void GoToNextRound()
    {
        if (currentRoundIndex + 1 < rounds.Length)
        {
            StartRoundByIndex(currentRoundIndex + 1);
        }
        else
        {
            Debug.Log("모든 라운드가 끝났습니다!");
        }
    }
}