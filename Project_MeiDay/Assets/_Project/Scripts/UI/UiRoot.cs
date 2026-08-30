using UnityEngine;
using UnityEngine.UI;

// 화면(패널) 전환을 담당하는 UI 대장
// 페이즈가 바뀌면 그 페이즈에 맞는 패널만 켜줌
public class UiRoot : MonoBehaviour
{
    [SerializeField] GameObject titlePanel;  // 타이틀 (시작 버튼)
    [SerializeField] GameObject dayPanel;    // 낮 (민원 처리)
    [SerializeField] GameObject resultPanel; // 결산
    [SerializeField] GameObject nightPanel;  // 밤 (상점 + 알바)
    [SerializeField] GameObject shopPanel;   // 밤 안에서 상점 쪽
    [SerializeField] GameObject arbeitPanel; // 밤 안에서 알바 쪽
    [SerializeField] GameObject endingPanel; // 엔딩 / 게임오버
    [SerializeField] GameObject hardChoicePanel; // 하드모드 첫 아침에 뜨는 브로커 선택창
    [SerializeField] Button continueButton; // 저장 없으면 못 누르는 이어하기 버튼
    [SerializeField] GameObject cutscenePopup; // 새 게임 누르면 뜨는 "컷신 볼래?" 창

    // 컷신을 보고 돌아왔는지
    // 씬을 갈아끼우고 오면 여기 값들이 다 초기화돼서, 정적으로 들고 있어야 살아남음
    public static bool startAfterCutscene = false;

    void Start()
    {
        GameManager.instance.OnPhaseChanged += ShowPanelFor;

        // 대사 끝나면 브로커 선택창 뜨게 (매니저 연결 안 해뒀어도 안 터지게)
        if (GameManager.instance.events != null)
        {
            GameManager.instance.events.OnDialogueUpdated += RefreshHardChoice;
        }

        ShowPanelFor(GameManager.instance.phase); // 시작할 때 한 번 맞춰줌
        RefreshContinueButton();
        SetPanel(cutscenePopup, false);

        // 컷신 보고 돌아온 참이면 타이틀 안 거치고 바로 시작
        if (startAfterCutscene == true)
        {
            startAfterCutscene = false;
            GameManager.instance.GameStart();
        }
    }

    // 저장 데이터가 있을 때만 이어하기를 누를 수 있게 함
    void RefreshContinueButton()
    {
        if (continueButton != null)
        {
            continueButton.interactable = GameManager.instance.save.HasSave();
        }
    }

    void OnDestroy()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.OnPhaseChanged -= ShowPanelFor;

            if (GameManager.instance.events != null)
            {
                GameManager.instance.events.OnDialogueUpdated -= RefreshHardChoice;
            }
        }
    }

    // 페이즈에 맞는 패널만 켜기
    void ShowPanelFor(GamePhase phase)
    {
        SetPanel(titlePanel, phase == GamePhase.Intro);
        SetPanel(dayPanel, phase == GamePhase.DayWork);
        SetPanel(resultPanel, phase == GamePhase.DayResult);
        SetPanel(nightPanel, phase == GamePhase.NightShop);

        // 밤에 들어올 때는 상점 쪽부터 보여줌
        if (phase == GamePhase.NightShop)
        {
            ShowShop();
        }
        SetPanel(endingPanel, phase == GamePhase.Ending || phase == GamePhase.GameOver);

        // 컷신 물어보는 창은 타이틀에서만 뜸
        // 창 뒤의 이어하기가 눌려서 게임이 시작돼도 창만 덩그러니 남아 있었음
        if (phase != GamePhase.Intro)
        {
            SetPanel(cutscenePopup, false);
        }

        RefreshHardChoice();
    }

    // 패널 켜고 끄기
    // 아직 안 만든 패널이 있어도 게임이 죽으면 안 됨 - 여기서 터지면 ChangePhase 를 부른
    // 쪽(하루 시작, 결산 등)까지 통째로 끊겨서 루프가 멈춤
    void SetPanel(GameObject panel, bool on)
    {
        if (panel == null)
        {
            return;
        }

        panel.SetActive(on);
    }

    // 하드모드 시작한 아침엔 브로커 선택창 (대사 다 보고 나서, 아직 답 안 했을 때만)
    void RefreshHardChoice()
    {
        GameManager gm = GameManager.instance;
        bool inDialogue = (gm.events != null && gm.events.currentEvent != null);

        SetPanel(hardChoicePanel, gm.phase == GamePhase.DayWork
            && gm.hardMode == true
            && gm.brokerChoice == BrokerChoice.None
            && inDialogue == false);
    }

    // ---- 아래는 버튼 OnClick 에 연결해서 씀 ----

    // 타이틀: 새 게임 - 바로 시작하지 않고 컷신을 볼지 먼저 물어봄
    public void OnClickStart()
    {
        if (cutscenePopup == null)
        {
            GameManager.instance.GameStart(); // 창을 아직 안 만들어뒀으면 예전처럼 바로 시작
            return;
        }

        SetPanel(cutscenePopup, true);
    }

    // 컷신 창: 본다 -> 스토리 씬으로
    public void OnClickCutsceneYes()
    {
        SetPanel(cutscenePopup, false);
        startAfterCutscene = true; // 컷신 끝나고 돌아오면 바로 1일차부터
        UnityEngine.SceneManagement.SceneManager.LoadScene("Story");
    }

    // 컷신 창: 안 본다 -> 그냥 시작
    public void OnClickCutsceneNo()
    {
        SetPanel(cutscenePopup, false);
        GameManager.instance.GameStart();
    }

    // 타이틀: 이어하기
    public void OnClickContinue()
    {
        GameManager.instance.ContinueGame();
    }

    // 결산: 확인 -> 밤으로
    public void OnClickResultConfirm()
    {
        GameManager.instance.ConfirmDayResult();
    }

    // 밤: 자기 -> 다음날
    public void OnClickSleep()
    {
        GameManager.instance.SleepAndAdvance();
    }

    // 밤 탭: 상점 보기
    public void OnClickShopTab()
    {
        ShowShop();
    }

    // 밤 탭: 알바 보기
    public void OnClickArbeitTab()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }
        if (arbeitPanel != null)
        {
            arbeitPanel.SetActive(true);
        }
    }

    // 밤 화면에서 상점 쪽만 켜기
    void ShowShop()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
        }
        if (arbeitPanel != null)
        {
            arbeitPanel.SetActive(false);
        }
    }

    // 브로커 선택: DB 접근시키기 (돈 뜯기고 편한 엔딩 루트)
    public void OnClickBrokerDatabase()
    {
        GameManager.instance.ChooseBroker(BrokerChoice.DatabaseAccess);
        SetPanel(hardChoicePanel, false);
    }

    // 브로커 선택: 신고하기 (매일 의심받는 엔딩 루트)
    public void OnClickBrokerReport()
    {
        GameManager.instance.ChooseBroker(BrokerChoice.ReportBroker);
        SetPanel(hardChoicePanel, false);
    }

    // 엔딩/게임오버: 타이틀로
    public void OnClickToTitle()
    {
        GameManager.instance.RestartToTitle();
    }
}
