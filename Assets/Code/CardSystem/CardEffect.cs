using UnityEngine;

/// <summary>
/// 카드가 실제 캐릭터에 적용할 효과의 기본 ScriptableObject입니다.
/// 각 효과는 이 클래스를 상속하여 Apply와 Remove를 구현해 주시면 됩니다.
/// ScriptableObject는 여러 대상이 공유하므로 캐릭터별 런타임 상태는 target의 컴포넌트에 보관해 주세요.
/// </summary>
public abstract class CardEffect : ScriptableObject
{
    /// <summary>
    /// 카드가 장착될 때 대상 캐릭터에 효과를 적용합니다.
    /// </summary>
    public abstract void Apply(GameObject target);

    /// <summary>
    /// 카드가 해제되거나 다른 카드로 교체될 때 기존 효과를 제거합니다.
    /// 제거 처리가 필요하지 않은 효과라면 구현하지 않으셔도 됩니다.
    /// </summary>
    public virtual void Remove(GameObject target)
    {
    }
}
