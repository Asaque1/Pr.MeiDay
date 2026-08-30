using UnityEngine;
using UnityEngine.SceneManagement;

// 로비(타이틀) 화면 버튼들
// 시작을 누르면 프롤로그부터, 이어하기가 있으면 저장한 데서 바로 시작
public class LobbyView : MonoBehaviour
{
    [SerializeField] string storySceneName = "Story";      // 프롤로그 첫 씬
    [SerializeField] string gameSceneName = "Ingame_base"; // 프롤로그 건너뛸 때 갈 곳
    [SerializeField] GameObject settingsWindow;            // 설정창 (없으면 그냥 안 뜸)

    // 시작 버튼
    public void OnClickStart()
    {
        Click();

        // 프롤로그를 다 보고 나면 Cutscene 이 알아서 게임 씬으로 넘겨줌
        string next = storySceneName;
        if (string.IsNullOrEmpty(next) == true)
        {
            next = gameSceneName;
        }

        SceneManager.LoadScene(next);
    }

    // 설정 버튼
    public void OnClickSetting()
    {
        Click();

        if (settingsWindow != null)
        {
            settingsWindow.SetActive(settingsWindow.activeSelf == false);
        }
    }

    // 나가기 버튼
    public void OnClickQuit()
    {
        Click();
        PlayerPrefs.Save(); // 맞춰둔 소리 설정은 남기고 끄기

#if UNITY_EDITOR
        // 에디터에선 Application.Quit 이 안 먹으니 플레이만 멈춤
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // 누르는 소리 (SoundManager 가 아직 없는 씬이어도 안 터지게)
    void Click()
    {
        if (SoundManager.instance != null)
        {
            SoundManager.instance.PlaySfx(SoundManager.instance.uiClick);
        }
    }
}
