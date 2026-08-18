using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// "공격 타일"로 지정된 칸에만 생성되는 콜라이더에 붙습니다.
/// 이 트리거가 Enemy와 겹치면 PlayerPixelBody의 현재 공격력만큼 데미지를 줍니다.
/// TakeDamage(float) 오버로드를 사용해 UI 이펙트 없이 즉시 DieInstant() 경로를 타도록 해서,
/// 몸통 공격으로 적을 죽여도 GameManager.Instance.AddBlock(colorBlock)을 통해
/// 픽셀 보상이 정상적으로 지급되도록 했습니다.
/// 같은 적을 매 프레임 연속으로 때리지 않도록 hitCooldown으로 타격 간격을 둡니다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class PixelAttackHitbox : MonoBehaviour
{
    [SerializeField] private float hitCooldown = 0.5f;

    private PlayerPixelBody pixelBody;
    private readonly Dictionary<Enemy, float> lastHitTime = new Dictionary<Enemy, float>();

    public void Init(PlayerPixelBody body)
    {
        pixelBody = body;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHitEnemy(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryHitEnemy(other);
    }

    private void TryHitEnemy(Collider2D other)
    {
        if (pixelBody == null) return;

        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy == null) enemy = other.GetComponentInParent<Enemy>();
        if (enemy == null) return;

        if (lastHitTime.TryGetValue(enemy, out float last) && Time.time - last < hitCooldown)
            return;

        lastHitTime[enemy] = Time.time;

        // TakeDamage(float damage) 오버로드 → HP 0 이하 시 DieInstant() 호출 →
        // GameManager.Instance.AddBlock(colorBlock)으로 픽셀 보상 지급
        enemy.TakeDamage(pixelBody.CurrentAttack);
    }
}
