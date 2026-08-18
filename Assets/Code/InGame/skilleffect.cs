using UnityEngine;

/// <summary>
/// 스킬 발동 시 소환되는 애니메이션 이펙트 오브젝트에 붙입니다.
/// Animator가 있으면 현재 재생 중인 애니메이션 길이를 자동으로 읽어 그 시간만큼 유지 후 파괴하고,
/// Animator가 없거나 길이를 읽지 못하면 lifeTimeFallback 값을 그대로 사용합니다.
/// </summary>
public class SkillEffectAutoDestroy : MonoBehaviour
{
    [Tooltip("Animator가 없거나 클립 길이를 읽지 못했을 때 사용할 기본 유지 시간(초)")]
    [SerializeField] private float lifeTimeFallback = 1f;

    private void Start()
    {
        float lifeTime = lifeTimeFallback;

        Animator animator = GetComponent<Animator>();
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            AnimatorClipInfo[] clipInfos = animator.GetCurrentAnimatorClipInfo(0);
            if (clipInfos.Length > 0 && clipInfos[0].clip != null)
            {
                lifeTime = clipInfos[0].clip.length;
            }
        }

        Destroy(gameObject, lifeTime);
    }
}
