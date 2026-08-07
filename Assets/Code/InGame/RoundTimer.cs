using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RoundTimer : MonoBehaviour
{
    [Header("시간 설정")]
    [Tooltip("각 라운드별 제한 시간(초)을 설정합니다.")]
    [SerializeField] private float[] roundDurations = new float[] { 60f, 75f, 90f, 120f };
    [SerializeField] private float defaultDuration = 60f;

    private float roundDuration;
    private float currentTimer;
    private bool isTimerRunning = false;

    [Header("UI 연결")]
    [SerializeField] private TextMeshProUGUI timerText;       // 남은 시간 텍스트 (예: 01:00)
    [SerializeField] private Slider timerSlider;            // 남은 시간 슬라이더

    // ✨ 라운드 숫자만 표시할 텍스트 컴포넌트 추가
    [Tooltip("현재 라운드 숫자만 표시되는 텍스트 (예: 1, 2, 3...)")]
    [SerializeField] private TextMeshProUGUI roundNumberText;

    [SerializeField] private TextMeshProUGUI waveText;        // WAVE 애니메이션 연출용 텍스트
    [SerializeField] private TextMeshProUGUI summaryText;     // 결과 창 텍스트
    [SerializeField] private GameObject resultPanel;         // 결과 패널

    [Header("스폰 및 적 관리")]
    [SerializeField] private SpawnManager spawnManager;

    private void Start()
    {
        InitializeRound();
    }

    /// <summary>
    /// 라운드 시작 및 UI/타이머 초기화
    /// </summary>
    public void InitializeRound()
    {
        isTimerRunning = false;

        // 1. 이번 라운드 획득 픽셀 데이터 초기화
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetCurrentRoundBlocks();
        }

        // 2. SettingsManager에서 최신 라운드 정보 읽기
        int currentRound = 1;
        int roundIndex = 0;

        if (SettingsManager.Instance != null)
        {
            currentRound = SettingsManager.Instance.CurrentRound;          // 1부터 시작 (1, 2, 3...)
            roundIndex = SettingsManager.Instance.CurrentRoundIndex;    // 0부터 시작 (0, 1, 2...)
        }

        // ✨ 3. 라운드 숫자 UI 갱신 (숫자만 출력)
        if (roundNumberText != null)
        {
            roundNumberText.text = currentRound.ToString();
        }

        // 4. 라운드 제한시간 설정
        if (roundDurations != null && roundDurations.Length > 0)
        {
            if (roundIndex < roundDurations.Length)
            {
                roundDuration = roundDurations[roundIndex];
            }
            else
            {
                roundDuration = roundDurations[roundDurations.Length - 1];
            }
        }
        else
        {
            roundDuration = defaultDuration;
        }

        // 5. 타이머 UI 갱신
        currentTimer = roundDuration;
        UpdateTimerUI();

        // 6. WAVE 커지는 애니메이션 연출 텍스트 설정 (선택 사항)
        if (waveText != null)
        {
            waveText.text = $"WAVE {currentRound}";
            StartCoroutine(CoPlayWaveAnimation());
        }

        // 7. UI 및 스폰 매니저 초기화
        if (resultPanel != null) resultPanel.SetActive(false);
        if (spawnManager != null) spawnManager.enabled = true;

        isTimerRunning = true;
    }

    private void Update()
    {
        if (!isTimerRunning) return;

        currentTimer -= Time.deltaTime;

        if (currentTimer <= 0f)
        {
            currentTimer = 0f;
            UpdateTimerUI();
            OnRoundEnd();
        }
        else
        {
            UpdateTimerUI();
        }
    }

    private void UpdateTimerUI()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(currentTimer / 60f);
            int seconds = Mathf.FloorToInt(currentTimer % 60f);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }

        if (timerSlider != null)
        {
            timerSlider.value = currentTimer / roundDuration;
        }
    }

    private IEnumerator CoPlayWaveAnimation()
    {
        if (waveText == null) yield break;

        waveText.gameObject.SetActive(true);
        Transform textTransform = waveText.transform;

        float duration = 1.5f;
        float elapsed = 0f;

        Vector3 startScale = Vector3.one * 0.5f;
        Vector3 targetScale = Vector3.one * 1.2f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            textTransform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }

        waveText.gameObject.SetActive(false);
    }

    private void OnRoundEnd()
    {
        isTimerRunning = false;

        if (spawnManager != null) spawnManager.enabled = false;

        Enemy[] remainingEnemies = FindObjectsOfType<Enemy>();
        foreach (Enemy enemy in remainingEnemies)
        {
            Destroy(enemy.gameObject);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SaveCurrentRoundData();
        }

        if (resultPanel != null)
        {
            resultPanel.SetActive(true);

            if (summaryText != null)
            {
                summaryText.gameObject.SetActive(true);

                if (GameManager.Instance != null)
                {
                    summaryText.text = "결과 계산 중...";
                    StartCoroutine(GameManager.Instance.ShowSummaryTextLineByLine(summaryText));
                }
            }
        }
    }

    public void OnClickNextRoundButton()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.NextRound();
        }

        LoadingSceneManager.LoadScene("2_CARD SLECT");
    }

    public void OnClickMainMenuButton()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.ResetRound();
        }

        LoadingSceneManager.LoadScene("0_Title");
    }
}