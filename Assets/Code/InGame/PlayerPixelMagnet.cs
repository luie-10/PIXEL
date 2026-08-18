using System.Collections;
using UnityEngine;

/// <summary>
/// 초록 픽셀 패시브(수집 범위 증가)와 Magnetic Boost 스킬을 반영해
/// 현재 픽셀 수집 반경을 월드 좌표 기준으로 계산합니다.
/// </summary>
public class PlayerPixelMagnet : MonoBehaviour
{
    [Header("기본 수집 범위 (칸 단위)")]
    public float baseRangeCells = 2f;

    [Header("References")]
    [SerializeField] private PlayerPixelBody pixelBody;

    private float temporaryMultiplier = 1f;
    private Coroutine temporaryMultiplierCoroutine;
    private float temporaryMultiplierValue = 1f;
    private void Awake()
    {
        if (pixelBody == null) pixelBody = GetComponent<PlayerPixelBody>();
    }

    /// <summary>
    /// 현재 수집 반경(월드 유닛)입니다. PixelFlyEffect 쪽 감지 반경 등으로 사용하면 됩니다.
    /// </summary>
    public float CurrentRangeWorld
    {
        get
        {
            float greenBonusCells = pixelBody != null ? pixelBody.GreenPickupRangeBonus : 0f;
            float totalCells = baseRangeCells + greenBonusCells;
            return totalCells * PixelGameConstants.CellToWorld * temporaryMultiplier;
        }
    }

   

    /// <summary>
    /// 일정 시간(duration) 동안 픽셀 수집 범위에 배율(multiplier)을 곱해 적용합니다.
    /// 이미 다른 임시 배율이 적용 중이면 취소하고 새로 시작합니다(중첩 방지).
    /// PlayerSkill.cs의 Magnetic Boost 스킬에서 호출합니다.
    /// </summary>
    public void SetTemporaryMultiplier(float multiplier, float duration)
    {
        if (temporaryMultiplierCoroutine != null)
        {
            StopCoroutine(temporaryMultiplierCoroutine);
        }

        temporaryMultiplierCoroutine = StartCoroutine(CoTemporaryMultiplier(multiplier, duration));
    }

    private IEnumerator CoTemporaryMultiplier(float multiplier, float duration)
    {
        temporaryMultiplierValue = multiplier;
        yield return new WaitForSeconds(duration);
        temporaryMultiplierValue = 1f;
        temporaryMultiplierCoroutine = null;
    }
}
