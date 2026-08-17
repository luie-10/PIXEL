using System.Collections.Generic;
using UnityEngine;

// 조작 방식 Enum
public enum ControlType
{
    RotateAndMove = 0, // 회전하며 전진 (기본)
    FlipAnd8Way = 1    // 8방향 이동 + 좌우 반전
}

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("Current Settings")]
    public ControlType currentControlType = ControlType.RotateAndMove;

    [Header("Round Management")]
    [SerializeField] private int currentRoundIndex = 0; // 0부터 시작하는 인덱스
    public int CurrentRoundIndex => currentRoundIndex;      // 현재 라운드 인덱스 (0, 1, 2...)
    public int CurrentRound => currentRoundIndex + 1;        // 실제 표시용 라운드 번호 (1, 2, 3...)

    private const string ControlTypeKey = "SavedControlType";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬 변경 시 파괴되지 않음
            LoadSettings(); // 기존 설정 불러오기
            LoadSkillKeyBindings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 라운드가 끝났을 때 다음 라운드로 진행시킵니다.
    /// </summary>
    public void NextRound()
    {
        currentRoundIndex++;
        Debug.Log($"[SettingsManager] 다음 라운드로 진행: {CurrentRound}라운드 (Index: {currentRoundIndex})");
    }

    /// <summary>
    /// 특정 라운드 인덱스로 직접 설정합니다.
    /// </summary>
    public void SetRoundIndex(int index)
    {
        currentRoundIndex = Mathf.Max(0, index);
        Debug.Log($"[SettingsManager] 현재 라운드 설정됨: {CurrentRound}라운드 (Index: {currentRoundIndex})");
    }
    // SettingsManager.cs 클래스 내부에 추가

    /// <summary>
    /// 스킬 키 중 하나라도 KeyCode.None(미설정) 상태인 것이 있는지 확인합니다.
    /// </summary>
    public bool HasAnyUnboundSkillKey()
    {
        foreach (var pair in skillKeyBindings)
        {
            if (pair.Value == KeyCode.None) return true;
        }

        return false;
    }

    /// <summary>
    /// 새로운 게임을 시작할 때 라운드를 1라운드(인덱스 0)로 초기화합니다.
    /// </summary>
    public void ResetRound()
    {
        currentRoundIndex = 0;
        Debug.Log("[SettingsManager] 라운드가 1라운드로 초기화되었습니다.");
    }

    // 설정 저장 (UI에서 호출)
    // SettingsManager.cs 클래스 내부에 추가

    public event System.Action ControlTypeChanged;

    // 기존 SaveSettings를 아래 내용으로 교체
    public void SaveSettings(ControlType newType)
    {
        currentControlType = newType;
        PlayerPrefs.SetInt(ControlTypeKey, (int)currentControlType);
        PlayerPrefs.Save();
        Debug.Log($"[SettingsManager] 조작 방식 저장 완료: {currentControlType}");
        ControlTypeChanged?.Invoke();
    }

    // SettingsManager.cs 클래스 내부에 추가

    private const string SkillKeyPrefPrefix = "SkillKeyBinding_";

    private Dictionary<PlayerSkillType, KeyCode> skillKeyBindings = new Dictionary<PlayerSkillType, KeyCode>
{
    { PlayerSkillType.RedRush,     KeyCode.Q },
    { PlayerSkillType.Unyielding,  KeyCode.E },
    { PlayerSkillType.Push,        KeyCode.R },
    { PlayerSkillType.MagnetBoost, KeyCode.F }
};

    public KeyCode GetSkillKey(PlayerSkillType skill)
    {
        return skillKeyBindings.TryGetValue(skill, out KeyCode key) ? key : KeyCode.None;
    }

    // SettingsManager.cs 클래스 내부에 추가

    // 키가 새로 지정될 때마다 알려주는 이벤트입니다. UI가 이 이벤트를 구독해서 텍스트를 갱신합니다.
    public event System.Action SkillKeyBindingsChanged;

    // 기존 SetSkillKey를 아래 내용으로 교체해주세요.
    public void SetSkillKey(PlayerSkillType skill, KeyCode key)
    {
        // 이미 다른 스킬이 같은 키를 쓰고 있다면, 그 스킬의 키는 해제합니다. (중복 방지)
        List<PlayerSkillType> conflictingSkills = new List<PlayerSkillType>();

        foreach (var pair in skillKeyBindings)
        {
            if (pair.Key != skill && pair.Value == key && key != KeyCode.None)
            {
                conflictingSkills.Add(pair.Key);
            }
        }

        foreach (PlayerSkillType conflictSkill in conflictingSkills)
        {
            skillKeyBindings[conflictSkill] = KeyCode.None;
            PlayerPrefs.SetInt(SkillKeyPrefPrefix + conflictSkill, (int)KeyCode.None);
        }

        skillKeyBindings[skill] = key;
        PlayerPrefs.SetInt(SkillKeyPrefPrefix + skill, (int)key);
        PlayerPrefs.Save();

        SkillKeyBindingsChanged?.Invoke();
    }

    public void LoadSkillKeyBindings()
    {
        foreach (PlayerSkillType skill in System.Enum.GetValues(typeof(PlayerSkillType)))
        {
            string prefKey = SkillKeyPrefPrefix + skill;
            if (PlayerPrefs.HasKey(prefKey))
            {
                skillKeyBindings[skill] = (KeyCode)PlayerPrefs.GetInt(prefKey);
            }
        }
    }

    // 설정 불러오기
    private void LoadSettings()
    {
        if (PlayerPrefs.HasKey(ControlTypeKey))
        {
            currentControlType = (ControlType)PlayerPrefs.GetInt(ControlTypeKey);
        }
        else
        {
            currentControlType = ControlType.RotateAndMove; // 기본값
        }
    }
}