using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TitleScreenManager : MonoBehaviour
{
    [Header("초기 선택 버튼")]
    [SerializeField] private Button firstSelectedButton;

    [Header("씬 이름 설정")]
    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private string creditSceneName = "CreditScene";

    [Header("UI 연동 참조")]
    [SerializeField] private SettingsUI settingsUI;

    private void Start()
    {
        SetDefaultFocus();

        // 씬 시작 시 SettingsUI 참조 상태 미리 점검
        if (settingsUI == null)
        {
            Debug.LogError("[TitleScreenManager] 경고: SettingsUI 스크립트 연결이 누락되었습니다! Inspector를 확인해주세요.");
        }
    }

    private void Update()
    {
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
        {
            if (Input.GetAxisRaw("Vertical") != 0 || Input.GetAxisRaw("Horizontal") != 0)
            {
                SetDefaultFocus();
            }
        }
    }

    public void SetDefaultFocus()
    {
        if (firstSelectedButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(firstSelectedButton.gameObject);
        }
    }

    public void OnClickStartGame()
    {
        Debug.Log("[TitleScreenManager] '게임 시작' 버튼 클릭됨");
        PixelSaveSystem.DeleteSaveData();
        // 게임을 처음 시작할 때는 라운드를 1라운드로 초기화
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.ResetRound();
        }

        LoadingSceneManager.LoadScene(gameSceneName);
    }
    public void OnClickCredits()
    {
        Debug.Log("[TitleScreenManager] '크레딧' 버튼 클릭됨");
        LoadingSceneManager.LoadScene(creditSceneName);
    }

    // ✨ 설정 버튼 클릭 이벤트 (디버그 로그 포함)
    public void OnClickOptions()
    {
        Debug.Log("[TitleScreenManager] '설정' 버튼 이벤트 진입 완료");

        if (settingsUI != null)
        {
            Debug.Log("[TitleScreenManager] settingsUI 참조 확인됨. OpenPanel() 호출 중...");
            settingsUI.OpenPanel();
        }
        else
        {
            Debug.LogError("[TitleScreenManager] 오류: settingsUI가 null 상태입니다. TitleScreenManager 인스펙터의 Settings UI 슬롯에 오브젝트를 연결해주세요!");
        }
    }

    public void OnClickExit()
    {
        Debug.Log("[TitleScreenManager] '게임 종료' 버튼 클릭됨");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}