using UnityEngine;

// 화면(패널) 전환을 담당하는 UI 대장
// 페이즈가 바뀌면 그 페이즈에 맞는 패널만 켜줌
public class UiRoot : MonoBehaviour
{
    [SerializeField] GameObject titlePanel;  // 타이틀 (시작 버튼)
    [SerializeField] GameObject dayPanel;    // 낮 (민원 처리)
    [SerializeField] GameObject resultPanel; // 결산
    [SerializeField] GameObject nightPanel;  // 밤 (상점)
    [SerializeField] GameObject endingPanel; // 엔딩 / 게임오버
    [SerializeField] GameObject hardChoicePanel; // 하드모드 첫 아침에 뜨는 브로커 선택창

    void Start()
    {
        GameManager.instance.OnPhaseChanged += ShowPanelFor;
        ShowPanelFor(GameManager.instance.phase); // 시작할 때 한 번 맞춰줌
    }

    void OnDestroy()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.OnPhaseChanged -= ShowPanelFor;
        }
    }

    // 페이즈에 맞는 패널만 켜기
    void ShowPanelFor(GamePhase phase)
    {
        titlePanel.SetActive(phase == GamePhase.Intro);
        dayPanel.SetActive(phase == GamePhase.DayWork);
        resultPanel.SetActive(phase == GamePhase.DayResult);
        nightPanel.SetActive(phase == GamePhase.NightShop);
        endingPanel.SetActive(phase == GamePhase.Ending || phase == GamePhase.GameOver);

        // 하드모드 시작한 아침엔 브로커 선택창부터 (아직 답 안 했을 때만)
        GameManager gm = GameManager.instance;
        hardChoicePanel.SetActive(phase == GamePhase.DayWork
            && gm.hardMode == true
            && gm.brokerChoice == BrokerChoice.None);
    }

    // ---- 아래는 버튼 OnClick 에 연결해서 씀 ----

    // 타이틀: 시작
    public void OnClickStart()
    {
        GameManager.instance.GameStart();
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

    // 브로커 선택: DB 접근시키기 (돈 뜯기고 편한 엔딩 루트)
    public void OnClickBrokerDatabase()
    {
        GameManager.instance.ChooseBroker(BrokerChoice.DatabaseAccess);
        hardChoicePanel.SetActive(false);
    }

    // 브로커 선택: 신고하기 (매일 의심받는 엔딩 루트)
    public void OnClickBrokerReport()
    {
        GameManager.instance.ChooseBroker(BrokerChoice.ReportBroker);
        hardChoicePanel.SetActive(false);
    }

    // 엔딩/게임오버: 타이틀로
    public void OnClickToTitle()
    {
        GameManager.instance.RestartToTitle();
    }
}
