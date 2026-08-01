using System;

/// <summary>
/// 저장 파일에는 ScriptableObject 참조 대신 선택한 카드의 고유 ID만 보관합니다.
/// </summary>
[Serializable]
public sealed class CharacterSaveData
{
    public string equippedCardId;
}
