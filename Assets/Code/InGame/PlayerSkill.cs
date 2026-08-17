using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSkillController : MonoBehaviour
{
    [Header("참조")]
    public PlayerPixelBody pixelBody;

    [Header("Red Rush - 돌진")]
    public int redRushCost = 5;
    public float redRushCooldown = 10f;
    public float redRushDamageRatio = 0.15f;
    public float redRushDashDistanceCells = 3f;
    public float redRushDashDuration = 0.2f;

    [Header("Unyielding - 블루 보강")]
    public int unyieldingCost = 5;
    public float unyieldingCooldown = 30f;

    [Header("Push - 밀쳐내기")]
    public int pushCost = 10;
    public float pushCooldown = 60f;
    public float pushRangeCells = 3f;
    public float pushDistanceCells = 3f;
    public float pushStunDuration = 0.5f;

    [Header("Magnetic Boost - 자석 강화")]
    public int magnetBoostCost = 8;
    public float magnetBoostCooldown = 60f;
    public float magnetBoostMultiplier = 2.5f; // 반경 150% 증가 = 기존의 2.5배
    public float magnetBoostDuration = 8f; // 기획서에 지속시간 명시 없어 임시값, 조정 필요

    private Dictionary<PlayerSkillType, float> cooldownTimers = new Dictionary<PlayerSkillType, float>();
    private PlayerPixelMagnet magnet;

    private void Awake()
    {
        if (pixelBody == null) pixelBody = GetComponent<PlayerPixelBody>();
        magnet = GetComponent<PlayerPixelMagnet>();

        foreach (PlayerSkillType skill in System.Enum.GetValues(typeof(PlayerSkillType)))
        {
            cooldownTimers[skill] = 0f;
        }
    }
    // PlayerSkillController.cs 클래스 내부, 기존 메서드들 사이에 추가

    // UI에서 스킬 타입만으로 발동을 시도할 수 있도록 공개 래퍼를 제공합니다.
    public bool ActivateSkill(PlayerSkillType skill)
    {
        if (IsOnCooldown(skill)) return false;

        bool activated = false;

        switch (skill)
        {
            case PlayerSkillType.RedRush:
                activated = TryActivateRedRush();
                if (activated) StartCooldown(skill, redRushCooldown);
                break;
            case PlayerSkillType.Unyielding:
                activated = TryActivateUnyielding();
                if (activated) StartCooldown(skill, unyieldingCooldown);
                break;
            case PlayerSkillType.Push:
                activated = TryActivatePush();
                if (activated) StartCooldown(skill, pushCooldown);
                break;
            case PlayerSkillType.MagnetBoost:
                activated = TryActivateMagnetBoost();
                if (activated) StartCooldown(skill, magnetBoostCooldown);
                break;
        }

        return activated;
    }

    // UI가 쿨타임 게이지(fillAmount)를 그리기 위해 최대 쿨타임 값을 필요로 합니다.
    public float GetCooldownDuration(PlayerSkillType skill)
    {
        switch (skill)
        {
            case PlayerSkillType.RedRush: return redRushCooldown;
            case PlayerSkillType.Unyielding: return unyieldingCooldown;
            case PlayerSkillType.Push: return pushCooldown;
            case PlayerSkillType.MagnetBoost: return magnetBoostCooldown;
            default: return 0f;
        }
    }

    // UI가 "코스트 부족" 상태를 판단하기 위해 필요한 소모 픽셀 개수입니다.
    public int GetPixelCost(PlayerSkillType skill)
    {
        switch (skill)
        {
            case PlayerSkillType.RedRush: return redRushCost;
            case PlayerSkillType.Unyielding: return unyieldingCost;
            case PlayerSkillType.Push: return pushCost;
            case PlayerSkillType.MagnetBoost: return magnetBoostCost;
            default: return 0;
        }
    }

    // 스킬 타입에 대응하는 픽셀 색상입니다. GameManager.GetOwnedPixelCount 호출 시 필요합니다.
    public PixelColor GetPixelColor(PlayerSkillType skill)
    {
        switch (skill)
        {
            case PlayerSkillType.RedRush: return PixelColor.Red;
            case PlayerSkillType.Unyielding: return PixelColor.Blue;
            case PlayerSkillType.Push: return PixelColor.Yellow;
            case PlayerSkillType.MagnetBoost: return PixelColor.Green;
            default: return PixelColor.None;
        }
    }

    private void Update()
    {
        List<PlayerSkillType> keys = new List<PlayerSkillType>(cooldownTimers.Keys);
        foreach (var skill in keys)
        {
            if (cooldownTimers[skill] > 0f)
                cooldownTimers[skill] -= Time.deltaTime;
        }

        if (SettingsManager.Instance == null) return;

        if (Input.GetKeyDown(SettingsManager.Instance.GetSkillKey(PlayerSkillType.RedRush)))
            TryActivateSkill(PlayerSkillType.RedRush);

        if (Input.GetKeyDown(SettingsManager.Instance.GetSkillKey(PlayerSkillType.Unyielding)))
            TryActivateSkill(PlayerSkillType.Unyielding);

        if (Input.GetKeyDown(SettingsManager.Instance.GetSkillKey(PlayerSkillType.Push)))
            TryActivateSkill(PlayerSkillType.Push);

        if (Input.GetKeyDown(SettingsManager.Instance.GetSkillKey(PlayerSkillType.MagnetBoost)))
            TryActivateSkill(PlayerSkillType.MagnetBoost);
    }

    // UI 버튼 OnClick에 각각 연결
    public void OnClickRedRush() => TryActivateSkill(PlayerSkillType.RedRush);
    public void OnClickUnyielding() => TryActivateSkill(PlayerSkillType.Unyielding);
    public void OnClickPush() => TryActivateSkill(PlayerSkillType.Push);
    public void OnClickMagnetBoost() => TryActivateSkill(PlayerSkillType.MagnetBoost);

    public bool IsOnCooldown(PlayerSkillType skill) => cooldownTimers.TryGetValue(skill, out float t) && t > 0f;
    public float GetRemainingCooldown(PlayerSkillType skill) => cooldownTimers.TryGetValue(skill, out float t) ? Mathf.Max(0f, t) : 0f;

    private void TryActivateSkill(PlayerSkillType skill)
    {
        if (IsOnCooldown(skill)) return;

        switch (skill)
        {
            case PlayerSkillType.RedRush:
                if (TryActivateRedRush()) StartCooldown(skill, redRushCooldown);
                break;
            case PlayerSkillType.Unyielding:
                if (TryActivateUnyielding()) StartCooldown(skill, unyieldingCooldown);
                break;
            case PlayerSkillType.Push:
                if (TryActivatePush()) StartCooldown(skill, pushCooldown);
                break;
            case PlayerSkillType.MagnetBoost:
                if (TryActivateMagnetBoost()) StartCooldown(skill, magnetBoostCooldown);
                break;
        }
    }

    private void StartCooldown(PlayerSkillType skill, float duration)
    {
        cooldownTimers[skill] = duration;
    }

    private bool TryActivateRedRush()
    {
        if (!GameManager.Instance.TrySpendPixels(PixelColor.Red, redRushCost))
            return false;

        float damage = pixelBody.CurrentAttack * redRushDamageRatio;
        Vector2 dashDir = transform.up; // 실제 이동 스크립트의 전진 축에 맞게 수정
        float dashDistance = redRushDashDistanceCells * PixelGameConstants.CellToWorld;

        StartCoroutine(RedRushRoutine(dashDir, dashDistance, damage));
        return true;
    }

    private IEnumerator RedRushRoutine(Vector2 direction, float distance, float damage)
    {
        Vector3 start = transform.position;
        Vector3 end = start + (Vector3)(direction * distance);
        float elapsed = 0f;
        HashSet<Enemy> hitEnemies = new HashSet<Enemy>();

        while (elapsed < redRushDashDuration)
        {
            transform.position = Vector3.Lerp(start, end, elapsed / redRushDashDuration);

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 0.3f);
            foreach (var hit in hits)
            {
                Enemy enemy = hit.GetComponent<Enemy>();
                if (enemy != null && !hitEnemies.Contains(enemy))
                {
                    hitEnemies.Add(enemy);
                    enemy.TakeDamage(damage);
                    enemy.ApplyKnockback(direction, distance, 0.15f);
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = end;
    }

    private bool TryActivateUnyielding()
    {
        if (!GameManager.Instance.TrySpendPixels(PixelColor.Blue, unyieldingCost))
            return false;

        int ownedBlueAfterCost = GameManager.Instance.GetOwnedPixelCount(PixelColor.Blue);
        int bonusHp = ownedBlueAfterCost / 2;

        if (bonusHp > 0)
            pixelBody.ApplyOuterTileBonus(bonusHp);

        return true;
    }

    private bool TryActivatePush()
    {
        if (!GameManager.Instance.TrySpendPixels(PixelColor.Yellow, pushCost))
            return false;

        float range = pushRangeCells * PixelGameConstants.CellToWorld;
        float pushDistance = pushDistanceCells * PixelGameConstants.CellToWorld;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range);
        foreach (var hit in hits)
        {
            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy == null) continue;

            Vector2 dir = (enemy.transform.position - transform.position).normalized;
            enemy.ApplyKnockback(dir, pushDistance, 0.2f);
            enemy.Stun(pushStunDuration);
        }

        return true;
    }

    private bool TryActivateMagnetBoost()
    {
        if (!GameManager.Instance.TrySpendPixels(PixelColor.Green, magnetBoostCost))
            return false;

        if (magnet != null)
            StartCoroutine(MagnetBoostRoutine());

        return true;
    }

    private IEnumerator MagnetBoostRoutine()
    {
        magnet.SetTemporaryMultiplier(magnetBoostMultiplier);
        yield return new WaitForSeconds(magnetBoostDuration);
        magnet.SetTemporaryMultiplier(1f);
    }
}
