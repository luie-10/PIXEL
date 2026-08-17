using System.Collections;
using UnityEngine;

/// <summary>
/// 플레이어가 적과 닿아 데미지를 입었을 때 픽셀 내구도 시스템(PlayerPixelBody)에 데미지를 전달하고,
/// 피격 직후 일정 시간 무적 + 스프라이트 깜빡임 효과를 처리합니다.
/// 계속 맞닿아 있어도 무적 시간 동안은 추가 데미지가 들어가지 않습니다.
/// </summary>
[RequireComponent(typeof(PlayerPixelBody))]
public class PlayerHealthController : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private PixelColorConfig config;

    [Header("References")]
    [SerializeField] private PlayerPixelBody pixelBody;
    [SerializeField] private SpriteRenderer spriteRenderer;

    public bool IsInvincible { get; private set; }

    private Coroutine invincibilityCoroutine;

    private void Awake()
    {
        if (pixelBody == null) pixelBody = GetComponent<PlayerPixelBody>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    /// <summary>
    /// 적과 접촉해 데미지를 입었을 때 호출합니다.
    /// 무적 상태이거나 참조가 없으면 데미지가 무시되고 false를 반환합니다.
    /// </summary>
    public bool TryTakeDamage(int damage)
    {
        if (IsInvincible || pixelBody == null || config == null) return false;

        bool applied = pixelBody.TryDamageRandomAliveTile(damage);

        if (applied)
        {
            if (invincibilityCoroutine != null) StopCoroutine(invincibilityCoroutine);
            invincibilityCoroutine = StartCoroutine(CoInvincibility());
        }

        return applied;
    }

    private IEnumerator CoInvincibility()
    {
        IsInvincible = true;

        float elapsed = 0f;
        bool visible = true;
        float flashInterval = Mathf.Max(config.hitFlashInterval, 0.01f);

        while (elapsed < config.hitInvincibleDuration)
        {
            visible = !visible;
            if (spriteRenderer != null) spriteRenderer.enabled = visible;

            yield return new WaitForSeconds(flashInterval);
            elapsed += flashInterval;
        }

        if (spriteRenderer != null) spriteRenderer.enabled = true;

        IsInvincible = false;
        invincibilityCoroutine = null;
    }
}
