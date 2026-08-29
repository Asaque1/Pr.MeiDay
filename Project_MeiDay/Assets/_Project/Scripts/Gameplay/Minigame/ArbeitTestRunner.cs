using UnityEngine;
using UnityEngine.InputSystem;

// 알바 테스트 씬(ArbeitTest) 전용 실행기
// 플레이 누르면 밤 알바 화면이 바로 뜸. 숫자키로 종목을 골라서 몇 번이고 돌려볼 수 있음
// 본편 씬엔 안 들어감 - 알바 판만 따로 확인하는 용도
public class ArbeitTestRunner : MonoBehaviour
{
    [Header("바로 시작할 알바")]
    [Tooltip("비워두면 목록 화면부터 나옴")]
    public MinigameDefinition startWith;

    [Header("설정")]
    public bool playOnStart = true;
    public int arbeitCount = 99;      // 몇 번이고 돌려볼 수 있게 넉넉히
    public int startCredits = 500;    // 보수가 붙는 게 보이게 얼마간 들고 시작

    [Tooltip("0보다 크면 제한 시간을 이 값으로 덮어씀 (천천히 살펴볼 때 크게)")]
    public float overrideSeconds = 0f;

    [Header("도움말")]
    public bool showHelp = true; // 화면 구석에 조작법 띄우기

    ArbeitView _view;

    void Start()
    {
        if (playOnStart == true)
        {
            // 매니저들 Awake 가 먼저 돌아야 해서 한 프레임 기다림
            StartCoroutine(SetUpNextFrame());
        }
    }

    System.Collections.IEnumerator SetUpNextFrame()
    {
        yield return null;
        SetUp();

        if (startWith != null)
        {
            Play(startWith);
        }
    }

    // 알바를 할 수 있는 상태로 만들어둠 (상점 해금 + 횟수 + 밤 단계)
    void SetUp()
    {
        GameManager gm = GameManager.instance;
        if (gm == null)
        {
            Debug.LogWarning("씬에 GameManager 가 없음 - 본편 씬에서 GameManager 오브젝트를 가져와야 함");
            return;
        }

        gm.shopUnlocked = true;
        gm.economy.AddCredits(startCredits - gm.economy.credits);
        gm.arbeit.arbeitLeftToday = arbeitCount;
        gm.ChangePhase(GamePhase.NightShop);

        _view = FindView();
        if (_view == null)
        {
            Debug.LogWarning("씬에 ArbeitView 가 없음 - 본편 씬의 ArbeitPanel 을 가져와야 함");
            return;
        }

        _view.gameObject.SetActive(true);
        _view.ShowList();
    }

    void Update()
    {
        Keyboard keys = Keyboard.current;
        if (keys == null)
        {
            return;
        }

        if (keys.digit1Key.wasPressedThisFrame)
        {
            Play(FindByKind(MinigameKind.BookSort));
        }
        if (keys.digit2Key.wasPressedThisFrame)
        {
            Play(FindByKind(MinigameKind.Spotlight));
        }
        if (keys.digit3Key.wasPressedThisFrame)
        {
            Play(FindByKind(MinigameKind.Promo));
        }
        if (keys.escapeKey.wasPressedThisFrame)
        {
            BackToList();
        }
    }

    // 알바 한 판 시작 (하던 게 있으면 물리고 새로)
    [ContextMenu("다시 시작")]
    public void PlayAgain()
    {
        Play(startWith);
    }

    void Play(MinigameDefinition arbeit)
    {
        if (arbeit == null)
        {
            Debug.LogWarning("그 종목의 알바 데이터가 없음");
            return;
        }

        GameManager gm = GameManager.instance;
        if (gm == null)
        {
            return;
        }

        if (_view == null)
        {
            _view = FindView();
        }
        if (_view == null)
        {
            return;
        }

        startWith = arbeit;

        _view.ShowList(); // 하던 판이 있으면 물리고 목록으로
        gm.arbeit.arbeitLeftToday = arbeitCount;
        _view.OnClickArbeit(arbeit);

        StretchTime(gm);
    }

    // 제한 시간 덮어쓰기 (0이면 데이터에 있는 대로)
    void StretchTime(GameManager gm)
    {
        if (overrideSeconds <= 0f)
        {
            return;
        }

        if (gm.arbeit.bookSort != null)
        {
            gm.arbeit.bookSort.duration = overrideSeconds;
            gm.arbeit.bookSort.timeLeft = overrideSeconds;
        }
        if (gm.arbeit.spotlight != null)
        {
            gm.arbeit.spotlight.duration = overrideSeconds;
            gm.arbeit.spotlight.timeLeft = overrideSeconds;
        }
        if (gm.arbeit.promo != null)
        {
            gm.arbeit.promo.duration = overrideSeconds;
            gm.arbeit.promo.timeLeft = overrideSeconds;
        }
    }

    public void BackToList()
    {
        if (_view == null)
        {
            _view = FindView();
        }
        if (_view != null)
        {
            _view.ShowList();
        }
    }

    ArbeitView FindView()
    {
        return FindFirstObjectByType<ArbeitView>(FindObjectsInactive.Include);
    }

    MinigameDefinition FindByKind(MinigameKind kind)
    {
        GameManager gm = GameManager.instance;
        if (gm == null || gm.content == null)
        {
            return null;
        }

        for (int i = 0; i < gm.content.minigames.Count; i++)
        {
            MinigameDefinition m = gm.content.minigames[i];
            if (m != null && m.kind == kind)
            {
                return m;
            }
        }
        return null;
    }

    void OnGUI()
    {
        if (showHelp == false)
        {
            return;
        }

        GUILayout.BeginArea(new Rect(10f, 10f, 300f, 130f), GUI.skin.box);
        GUILayout.Label("알바 테스트 씬");
        GUILayout.Label("1 = 책 정리   2 = 조명 연출   3 = 판촉");
        GUILayout.Label("Esc = 목록으로");

        GameManager gm = GameManager.instance;
        if (gm != null)
        {
            GUILayout.Label("크레딧 " + gm.economy.credits + "   남은 알바 " + gm.arbeit.arbeitLeftToday + "회");
        }
        GUILayout.EndArea();
    }
}
