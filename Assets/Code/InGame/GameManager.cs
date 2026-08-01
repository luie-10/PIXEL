using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UI 연출 및 버튼 설정")]
    [Tooltip("결과 텍스트가 한 줄씩 출력되는 시간 간격(초)")]
    public float lineDelay = 0.35f;

    [Tooltip("결과 텍스트 출력이 완료된 후 활성화할 버튼 오브젝트")]
    public GameObject resultButton;

    private Dictionary<PixelColor, int> collectedBlocks = new Dictionary<PixelColor, int>();
    private Dictionary<PixelColor, int> totalSavedBlocks = new Dictionary<PixelColor, int>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeBlockData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeBlockData()
    {
        collectedBlocks[PixelColor.Red] = 0;
        collectedBlocks[PixelColor.Blue] = 0;
        collectedBlocks[PixelColor.Yellow] = 0;
        collectedBlocks[PixelColor.Green] = 0;

        totalSavedBlocks[PixelColor.Red] = 0;
        totalSavedBlocks[PixelColor.Blue] = 0;
        totalSavedBlocks[PixelColor.Yellow] = 0;
        totalSavedBlocks[PixelColor.Green] = 0;
    }

    public void AddBlock(PixelColor color, int amount = 1)
    {
        if (color == PixelColor.None) return;

        if (collectedBlocks.ContainsKey(color))
        {
            collectedBlocks[color] += amount;
            Debug.Log($"[{color}] 픽셀 획득! 현재 라운드 수량: {collectedBlocks[color]}");
        }
    }

    public void SaveCurrentRoundData()
    {
        foreach (var pair in collectedBlocks)
        {
            if (totalSavedBlocks.ContainsKey(pair.Key))
            {
                totalSavedBlocks[pair.Key] += pair.Value;
            }
            else
            {
                totalSavedBlocks[pair.Key] = pair.Value;
            }
        }
    }

    public int GetBlockCount(PixelColor color)
    {
        if (collectedBlocks.TryGetValue(color, out int count))
        {
            return count;
        }
        return 0;
    }

    public int GetTotalSavedBlockCount(PixelColor color)
    {
        if (totalSavedBlocks.TryGetValue(color, out int count))
        {
            return count;
        }
        return 0;
    }

    public IEnumerator ShowSummaryTextLineByLine(TextMeshProUGUI targetText)
    {
        if (targetText == null) yield break;

        // 결과 버튼이 지정되어 있다면 연출 시작 시 비활성화
        if (resultButton != null)
        {
            resultButton.SetActive(false);
        }

        targetText.text = "";
        targetText.gameObject.SetActive(true);
        int roundTotal = 0;
        foreach (var val in collectedBlocks.Values)
        {
            roundTotal += val;
        }

        int grandTotal = 0;
        foreach (var val in totalSavedBlocks.Values)
        {
            grandTotal += val;
        }

        string[] lines = new string[]
        {
            "[ 이번 라운드 획득 ]",
            $"RED : {GetBlockCount(PixelColor.Red)}",
            $"BLUE : {GetBlockCount(PixelColor.Blue)}",
            $"YELLOW : {GetBlockCount(PixelColor.Yellow)}",
            $"GREEN : {GetBlockCount(PixelColor.Green)}",
            $"라운드 합계 : {roundTotal}개",
            "",
            "-----------------------",
            "[ 총 보유 누적 픽셀 ]",
            $"총합 : {grandTotal}개"
        };

        for (int i = 0; i < lines.Length; i++)
        {
            targetText.text += lines[i] + "\n";
            yield return new WaitForSeconds(lineDelay);
        }

        // 텍스트 연출이 끝난 후 버튼 활성화
        if (resultButton != null)
        {
            resultButton.SetActive(true);
        }
    }
}