using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 조립 시 "공격 타일"로 지정된 픽셀 한 칸에 부여되는 공격 판정입니다.
/// 적과 접촉하면 PlayerPixelBody.CurrentAttack을 기준으로 데미지를 입힙니다.
/// </summary>
public class PixelAttackHitbox : MonoBehaviour
{
    [Tooltip("같은 적을 다시 때리기까지 걸리는 최소 간격(초)입니다.")]
    [SerializeField] private float hitInterval = 0.5f;

    private PlayerPixelBody pixelBody;
    private readonly Dictionary<Enemy, float> nextAllowedHitTime = new Dictionary<Enemy, float>();

    public void Init(PlayerPixelBody body)
    {
        pixelBody = body;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (pixelBody == null) return;

        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy == null) return;

        if (nextAllowedHitTime.TryGetValue(enemy, out float nextTime) && Time.time < nextTime)
            return;

        nextAllowedHitTime[enemy] = Time.time + hitInterval;
        enemy.TakeDamage(pixelBody.CurrentAttack);
    }
}
