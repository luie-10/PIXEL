using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    // 인스펙터 창에서 이동할 씬 이름을 입력받습니다.
    [SerializeField] private string targetSceneName;

    // 버튼 클릭 이벤트에 연결할 함수입니다.
    public void LoadTargetScene()
    {
        LoadingSceneManager.LoadScene(targetSceneName);
    }
}