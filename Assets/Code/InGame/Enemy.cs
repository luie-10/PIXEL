using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    private float size;
    public float HP = 10f;
    [SerializeField] private float speed = 2.0f;
    public PixelColor colorBlock;

    [Header("Death Fly Effect")]
    [SerializeField] private GameObject pixelEffectPrefab;

    [Header("Player Contact Damage")]
    [Tooltip("실제 밸런스에 맞게 임시로 5로 설정했습니다. 밸런스에 맞게 수정해주세요.")]
    [SerializeField] private int damageToPlayer = 5;

    [SerializeField] private float changeDirectionInterval = 2.0f;
    private Vector2 moveDirection;
    private float timer;

    private bool isStunned = false;
    private bool isKnockedBack = false;

    private void Start()
    {
        SetRandomDirection();
    }

    private void Update()
    {
        if (isStunned || isKnockedBack) return;

        timer += Time.deltaTime;

        if (timer >= changeDirectionInterval)
        {
            SetRandomDirection();
            timer = 0f;
        }

        transform.Translate(moveDirection * speed * Time.deltaTime, Space.World);
    }

    private void SetRandomDirection()
    {
        float randomAngle = Random.Range(0f, 360f);
        moveDirection = new Vector2(Mathf.Cos(randomAngle * Mathf.Deg2Rad), Mathf.Sin(randomAngle * Mathf.Deg2Rad)).normalized;
    }

    // 기존: 플레이어 근접 공격 등, UI로 날아가는 연출이 필요한 경우 사용
    public void TakeDamage(float damage, RectTransform targetUIButton)
    {
        HP -= damage;
        if (HP <= 0)
        {
            Die(targetUIButton);
        }
    }

    // 신규: 스킬 데미지 등, UI 타겟이 없는 경우 사용. 연출 없이 즉시 픽셀 지급 후 파괴.
    public void TakeDamage(float damage)
    {
        HP -= damage;
        if (HP <= 0)
        {
            DieInstant();
        }
    }

    private void Die(RectTransform targetUIButton)
    {
        if (pixelEffectPrefab != null && targetUIButton != null)
        {
            GameObject effectObj = Instantiate(pixelEffectPrefab, transform.position, Quaternion.identity);
            PixelFlyEffect flyScript = effectObj.GetComponent<PixelFlyEffect>();

            if (flyScript != null)
            {
                Color visualColor = GetColorFromType(colorBlock);
                flyScript.Setup(targetUIButton, colorBlock, visualColor);
            }
        }

        Destroy(gameObject);
    }

    // 스킬로 처치했을 때: 날아가는 연출 없이 바로 GameManager에 픽셀 적립
    private void DieInstant()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddBlock(colorBlock);
        }

        Destroy(gameObject);
    }

    private Color GetColorFromType(PixelColor color)
    {
        switch (color)
        {
            case PixelColor.Red: return Color.red;
            case PixelColor.Blue: return Color.blue;
            case PixelColor.Yellow: return Color.yellow;
            case PixelColor.Green: return Color.green;
            default: return Color.white;
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryDamagePlayer(collision.collider);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDamagePlayer(other);
    }

    private void TryDamagePlayer(Collider2D playerCollider)
    {
        if (playerCollider == null || !playerCollider.CompareTag("Player")) return;

        PlayerHealthController health = playerCollider.GetComponent<PlayerHealthController>();

        if (health == null)
        {
            health = playerCollider.GetComponentInParent<PlayerHealthController>();
        }

        if (health != null)
        {
            health.TryTakeDamage(damageToPlayer);
        }
    }

    public void Stun(float duration)
    {
        if (isStunned) return;
        StartCoroutine(StunRoutine(duration));
    }

    private IEnumerator StunRoutine(float duration)
    {
        isStunned = true;
        yield return new WaitForSeconds(duration);
        isStunned = false;
    }

    public void ApplyKnockback(Vector2 direction, float distance, float duration)
    {
        if (isKnockedBack) return;
        StartCoroutine(KnockbackRoutine(direction.normalized, distance, duration));
    }

    private IEnumerator KnockbackRoutine(Vector2 direction, float distance, float duration)
    {
        isKnockedBack = true;
        Vector3 start = transform.position;
        Vector3 end = start + (Vector3)(direction * distance);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(start, end, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = end;
        isKnockedBack = false;
    }
}
