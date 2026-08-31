using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

// 프롤로그 컷신 - 빈칸이 깔려 있고 컷이 하나씩 채워짐 (앞 컷은 그대로 남음)
// 컷 그림은 cutParent 밑에 자식으로 순서대로 넣어두면 됨 (자식 순서 = 채워지는 순서)
// 화면 아무 데나 누르면 다음 컷, 스킵하기 누르면 남은 걸 한 번에 채우고 끝, 자동재생은 알아서 채움
public class Cutscene : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] Transform cutParent; // 컷 그림들이 자식으로 들어있는 자리
    [SerializeField] TMP_Text autoLabel;  // 자동재생 버튼 글씨 (켜짐/꺼짐 표시)
    [SerializeField] TMP_Text pageLabel;  // "3 / 6" 처럼 몇 번째 컷인지

    [SerializeField] float autoDelay = 3f;      // 자동재생일 때 컷 하나를 보여주는 시간
    [SerializeField] bool autoAtStart = true;   // 처음부터 자동재생으로 시작할지

    // 컷을 자식 순서 거꾸로 보여줌
    // 마지막 컷이 앞 컷 위로 삐져나오게 그려진 장이 있는데, 유니티는 자식 순서대로 겹쳐 그려서
    // 그냥 두면 마지막 컷이 앞 컷들을 덮어버림
    // 자식을 거꾸로 넣어두고 이걸 켜면, 보여주는 순서는 그대로면서 마지막 컷이 맨 뒤에 깔림
    [SerializeField] bool reverseOrder = false;

    // 다 보고 나서 갈 씬 (비워두면 씬은 안 옮기고 이 오브젝트만 끔)
    [SerializeField] string nextSceneName = "Ingame_base";

    // 스킵 누르면 갈 씬
    // 프롤로그가 여러 씬으로 쪼개져 있어서, 스킵은 다음 컷신이 아니라 게임으로 바로 보내야 함
    [SerializeField] string gameSceneName = "Ingame_base";

    int _cut = -1;       // 지금 보여주는 컷 번호 (-1 이면 아직 첫 컷도 안 띄움)
    bool _auto = false;  // 자동재생 중인지
    float _autoLeft = 0f;
    bool _done = false;  // 이미 끝냈는지 - 스킵이랑 마지막 컷 넘김이 겹쳐서 두 번 나가는 걸 막음

    void Start()
    {
        // 컷은 전부 꺼둔 채로 시작 (씬에 켜둔 채 저장해뒀어도 첫 컷부터 나오게)
        HideAllCuts();
        _auto = autoAtStart;
        ShowAutoLabel();
        Next();
    }

    void Update()
    {
        if (_auto == false || _done == true)
        {
            return;
        }

        // 컷신은 시간이 멈춘 데서도 돌 수 있어서 unscaled 로 셈
        _autoLeft -= Time.unscaledDeltaTime;
        if (_autoLeft <= 0f)
        {
            Next();
        }
    }

    // 화면 아무 데나 클릭 (버튼 위를 누르면 버튼이 먼저 먹으니까 여기까진 안 옴)
    void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
    {
        Next();
    }

    // 다음 컷 채우기 (마지막이었으면 끝냄)
    // 앞에 채운 컷은 그대로 둠 - 한 화면에 쭉 쌓이는 게 이 연출의 핵심
    public void Next()
    {
        if (_done == true || cutParent == null)
        {
            return;
        }

        _cut++;

        if (_cut >= cutParent.childCount)
        {
            Finish();
            return;
        }

        CutAt(_cut).gameObject.SetActive(true);
        _autoLeft = autoDelay;
        ShowPageLabel();
    }

    // 스킵하기 버튼
    // 프롤로그를 통째로 건너뛰고 바로 게임으로 감
    // 이 씬 하나만 넘기면 다음 프롤로그 씬이 또 나와서, 스킵 누른 사람 입장에선 안 끝나는 것처럼 보임
    public void OnClickSkip()
    {
        Go(GameScene());
    }

    // 스킵으로 갈 씬 (인스펙터에 안 채워져 있어도 인게임으로)
    string GameScene()
    {
        if (string.IsNullOrEmpty(gameSceneName) == true)
        {
            return "Ingame_base";
        }

        return gameSceneName;
    }

    // 자동재생 버튼 (누를 때마다 켜고 끔)
    public void OnClickAuto()
    {
        _auto = (_auto == false);
        _autoLeft = autoDelay; // 켠 직후에 바로 넘어가버리면 놀람
        ShowAutoLabel();
    }

    // 몇 번째로 보여줄 컷인지 (거꾸로면 뒤에서부터)
    Transform CutAt(int index)
    {
        if (reverseOrder == true)
        {
            return cutParent.GetChild(cutParent.childCount - 1 - index);
        }

        return cutParent.GetChild(index);
    }

    // 컷 전부 끄기
    void HideAllCuts()
    {
        if (cutParent == null)
        {
            return;
        }

        foreach (Transform child in cutParent)
        {
            child.gameObject.SetActive(false);
        }
    }

    // 자동재생 버튼 글씨 갱신
    void ShowAutoLabel()
    {
        if (autoLabel == null)
        {
            return;
        }

        if (_auto == true)
        {
            autoLabel.text = "자동재생 끄기";
        }
        else
        {
            autoLabel.text = "자동재생";
        }
    }

    // 몇 번째 컷인지 갱신
    void ShowPageLabel()
    {
        if (pageLabel == null || cutParent == null)
        {
            return;
        }

        pageLabel.text = (_cut + 1) + " / " + cutParent.childCount;
    }

    // 컷신 끝 - 다음 차례로 넘어감 (마지막 프롤로그 씬이면 게임)
    void Finish()
    {
        Go(nextSceneName);
    }

    // 컷신 접고 씬 옮기기
    void Go(string sceneName)
    {
        if (_done == true)
        {
            return;
        }

        _done = true;

        if (string.IsNullOrEmpty(sceneName) == true)
        {
            gameObject.SetActive(false); // 씬을 안 옮기는 쓰임새면 그냥 접음
            return;
        }

        // 씬이 실제로 바뀌는 건 이 프레임 끝이라, 그동안 컷신 화면이 한 번 더 그려짐
        // 컷이 없는 씬을 지나갈 때 빈 배경에 남은 페이지 숫자만 번쩍여서 먼저 접어둠
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas.gameObject.SetActive(false);
        }

        // 돌아가서 타이틀을 또 보여주면 컷신 본 보람이 없음 - 바로 1일차부터 시작
        UiRoot.startAfterCutscene = true;
        SceneManager.LoadScene(sceneName);
    }
}
