using UnityEngine;

/// <summary>
/// 스킬 발동 시 소환되는 연출 오브젝트에 붙어, 매 프레임 플레이어를 따라다니며
/// 방향(회전)과 크기(스케일)를 플레이어 기준으로 실시간 맞춥니다.
/// followOffset을 0으로 두면 플레이어 위치에 딱 붙어서 따라오고,
/// 값을 주면 플레이어가 바라보는 방향의 반대쪽(뒤쪽)에서 잔상처럼 따라옵니다.
/// Red Rush처럼 "플레이어를 뒤따라오며 방향을 바라보는" 연출에 followOffset을 사용합니다.
/// </summary>
public class PlayerEffectFollower : MonoBehaviour
{
    private Transform target;
    private float followOffset;
    private bool matchRotation;
    private float extraScaleMultiplier;

    /// <summary>
    /// 생성 즉시 한 번 호출해서 추적 대상과 옵션을 설정합니다.
    /// </summary>
    public void Setup(Transform playerTransform, float offsetWorld, float scaleMultiplier, bool followRotation = true)
    {
        target = playerTransform;
        followOffset = offsetWorld;
        extraScaleMultiplier = scaleMultiplier;
        matchRotation = followRotation;

        ApplyTransform();
    }

    private void LateUpdate()
    {
        ApplyTransform();
    }

    private void ApplyTransform()
    {
        if (target == null) return;

        // transform.up 기준으로 이동/회전하는 PlayerController2D 구조와 맞춰,
        // "뒤쪽"은 플레이어가 바라보는 방향(up)의 반대 방향입니다.
        Vector3 behindDirection = -target.up;
        transform.position = target.position + behindDirection * followOffset;

        if (matchRotation)
        {
            transform.rotation = target.rotation;
        }

        // 플레이어의 현재 크기(localScale)에 비례해서 연출 크기도 함께 커지거나 작아집니다.
        float playerSize = Mathf.Max(Mathf.Abs(target.localScale.x), 0.0001f);
        transform.localScale = Vector3.one * playerSize * extraScaleMultiplier;
    }
}
