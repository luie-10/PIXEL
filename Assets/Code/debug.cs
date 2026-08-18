using System.Collections;
using UnityEngine;

/// <summary>
/// 개발/테스트 전용: 게임 시작 시 각 색상 픽셀의 누적 보유량(totalSavedBlocks)을
/// targetAmount로 채워줍니다. GameManager.Instance가 아직 준비되지 않았을 경우를 대비해
/// 몇 프레임 대기 후 재시도하며, 적용 전/후 수량을 콘솔에 명확히 출력합니다.
/// </summary>
public class PixelCheatStartup : MonoBehaviour
{
    [SerializeField] private bool enableCheat = true;
    [SerializeField] private int targetAmount = 100;

    private void Start()
    {
        if (!enableCheat) return;
        StartCoroutine(CoApplyCheat());
    }

    private IEnumerator CoApplyCheat()
    {
        float timeout = 3f;
        float elapsed = 0f;

        while (GameManager.Instance == null && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogError("[PixelCheatStartup] GameManager.Instance를 끝내 찾지 못했습니다. " +
                "이 씬에 GameManager가 존재하는지, 혹은 인트로/로딩 씬을 거치지 않고 이 씬에서 바로 Play하지 않았는지 확인하세요.");
            yield break;
        }

        PixelColor[] colors = { PixelColor.Red, PixelColor.Blue, PixelColor.Yellow, PixelColor.Green };

        foreach (PixelColor color in colors)
        {
            int before = GameManager.Instance.GetTotalSavedBlockCount(color);
            int shortage = targetAmount - before;

            if (shortage > 0)
            {
                GameManager.Instance.UsePixel(color, -shortage);
            }

            int after = GameManager.Instance.GetTotalSavedBlockCount(color);
            Debug.Log($"[PixelCheatStartup] {color} : {before} → {after} (목표 {targetAmount})");
        }
    }
}
