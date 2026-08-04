using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement; // 씬 전환을 위해 필수 작성

public class TitleScreenManager : MonoBehaviour
{
    [Header("초기 선택 버튼")]
    [SerializeField] private Button firstSelectedButton;

    [Header("씬 이름 설정")]
    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private string creditSceneName = "CreditScene"; // 이동할 크레딧 씬 이름

    private void Start()
    {
        // 게임 시작 시 첫 번째 버튼에 키보드/패드 포커스 지정
        SetDefaultFocus();
    }

    private void Update()
    {
        // 키보드 방향키/WASD 입력이 발생했는데 현재 아무 버튼도 선택되어 있지 않을 때 재포커스
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

    // --- 버튼 클릭 이벤트 연결 메서드 ---

    // 1. 게임 시작 버튼
    public void OnClickStartGame()
    {
        Debug.Log("게임 시작!");
        LoadingSceneManager.LoadScene(gameSceneName);
    }

    // 2. 크레딧 버튼 (추가됨)
    public void OnClickCredits()
    {
        Debug.Log("크레딧 씬으로 이동");
        LoadingSceneManager.LoadScene(creditSceneName);
    }

    // 3. 설정 버튼
    public void OnClickOptions()
    {
        Debug.Log("설정 창 열기");
    }

    // 4. 게임 종료 버튼
    public void OnClickExit()
    {
        Debug.Log("게임 종료");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
}