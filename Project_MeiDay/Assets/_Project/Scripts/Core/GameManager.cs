using System;
using UnityEngine;

// 게임 전체를 관리하는 대장 클래스
// 다른 데서 GameManager.instance 로 불러서 씀
public class GameManager : MonoBehaviour
{
    public static GameManager instance; // 어디서든 쓰는 나 자신

    // 게임 데이터 (인스펙터에서 연결)
    public GameConfig config;
    public ContentDatabase content;

    // 매니저들 (인스펙터에서 연결)
    public EconomyManager economy;
    public SuspicionManager suspicion;
    public CalendarManager calendar;
    public InventorySystem inventory;
    public ShopManager shop;
    public SaveManager save;
    public ComplaintManager complaint;
    public EventManager events;

    // 게임 상태
    public GamePhase phase = GamePhase.Intro; // 지금 어느 단계인지
    public bool hasSpaceship = false;         // 우주선 샀는지

    // 하드모드 (우주선 산 뒤)
    public bool hardMode = false;                             // 하드모드 중인지
    public int hardModeDay = 0;                               // 하드모드 며칠째인지
    public BrokerChoice brokerChoice = BrokerChoice.None;     // 브로커한테 뭐라고 답했는지 (엔딩 분기)

    // 어떻게 끝났는지 기록 (엔딩 화면에서 보여줄 때 씀)
    public GameOverReason lastGameOverReason;
    public EndingType lastEnding = EndingType.None;

    // 단계 바뀌면 알려줌 (나중에 UI 가 이거 듣고 화면 바꿈)
    public event Action<GamePhase> OnPhaseChanged;

    void Awake()
    {
        instance = this; // 게임 켜질 때 instance 에 나를 넣어줌
    }

    // 단계 바꾸기
    public void ChangePhase(GamePhase newPhase)
    {
        phase = newPhase;
        Debug.Log("단계 바뀜: " + newPhase);

        // 구독한 애들한테 알려줌
        if (OnPhaseChanged != null)
        {
            OnPhaseChanged(newPhase);
        }
    }

    // 게임 시작 (타이틀에서 부름)
    public void GameStart()
    {
        // TODO: 나중에 여기서 인트로 슬라이드쇼 재생
        // TODO: 이어하기 버튼 만들면 여기서 save.Load() 분기
        economy.nextRentDay = config.firstRentDay; // 첫 월세 날 맞춰주기 (3일차부터)
        StartNewDay();
    }

    // 하루 시작 (낮 - 민원 처리)
    void StartNewDay()
    {
        ChangePhase(GamePhase.DayWork);

        // 아침 이벤트 있으면 대사부터 (끝나면 EventManager 가 OnMorningEventDone 불러줌)
        EventDefinition todayEvent = events.PickTodayEvent();
        if (todayEvent != null)
        {
            events.StartEvent(todayEvent);
            return;
        }

        complaint.StartDay(null);
    }

    // 아침 이벤트 대사 다 봤을 때 (EventManager 가 불러줌)
    public void OnMorningEventDone(EventDefinition ev)
    {
        // 이벤트에서 의심도 올라 게임오버 났으면 민원 시작 안 함
        if (phase == GamePhase.GameOver)
        {
            return;
        }

        // 이벤트에 민원 붙어있으면 그 민원이 오늘 첫 손님
        complaint.StartDay(ev.linkedComplaint);
    }

    // 낮 일 다 끝남 (민원 다 처리) -> 결산 화면으로
    public void OnDayWorkDone()
    {
        // 하루 다 맞췄으면 무실수 보너스
        if (complaint.failCount == 0 && complaint.successCount > 0)
        {
            economy.AddCredits(config.perfectDayBonus);
            complaint.earnedToday += config.perfectDayBonus;
        }

        ChangePhase(GamePhase.DayResult);
    }

    // 결산 확인 -> 밤 (상점)으로
    public void ConfirmDayResult()
    {
        ChangePhase(GamePhase.NightShop);
    }

    // 자고 다음날로 (밥값/월세/엔딩 체크는 calendar 가 함)
    public void SleepAndAdvance()
    {
        calendar.NextDay();

        // 다음날 넘기다가 엔딩이나 게임오버 됐으면 하루 또 시작 안 함
        if (phase == GamePhase.Ending || phase == GamePhase.GameOver)
        {
            return;
        }

        // 우주선 샀는데 아직 하드모드 아니면 지금부터 하드모드 시작 (브로커 만남)
        if (hasSpaceship == true && hardMode == false)
        {
            hardMode = true;
            hardModeDay = 0;
            brokerChoice = BrokerChoice.None;
            // TODO: 여기서 브로커 만나는 연출 넣기
        }

        save.Save(); // 아침마다 자동 저장

        StartNewDay();
    }

    // 우주선 사기 (돈 되면 사고 true)
    public bool TryBuySpaceship()
    {
        if (hasSpaceship == true)
        {
            return false;
        }
        if (economy.SpendCredits(config.spaceshipPrice) == false)
        {
            return false; // 돈 부족
        }

        hasSpaceship = true;
        return true;
    }

    // 브로커한테 답하기 (하드모드 첫 아침에 UI 가 불러줌)
    public void ChooseBroker(BrokerChoice choice)
    {
        brokerChoice = choice;

        // DB 접근을 골랐으면 브로커가 자금이 부족하다며 돈을 떼감
        if (choice == BrokerChoice.DatabaseAccess && economy.credits > 0)
        {
            int taken = Mathf.RoundToInt(economy.credits * config.brokerTakeRate);
            economy.credits -= taken;
        }
    }

    // 하드모드 하루 진행 (calendar 가 다음날마다 부름)
    public void AdvanceHardMode()
    {
        // 아직 브로커한테 답 안 했으면 카운트 안 함 (답하고 나서부터 7일)
        if (brokerChoice == BrokerChoice.None)
        {
            return;
        }

        hardModeDay++;

        // 하드모드엔 고정 지출이 늘어남
        economy.credits -= config.hardModeExtraCost;

        // 신고 루트면 브로커가 퍼뜨린 소문 때문에 매일 의심받음
        if (brokerChoice == BrokerChoice.ReportBroker)
        {
            suspicion.AddSuspicion(config.reportSuspicionPerDay);
        }

        // 의심도 초과로 게임오버 됐으면 끝
        if (phase == GamePhase.GameOver)
        {
            return;
        }

        // 엔딩일까지 버티면 고른 쪽 엔딩
        if (hardModeDay >= config.hardModeDays)
        {
            if (brokerChoice == BrokerChoice.DatabaseAccess)
            {
                Ending(EndingType.DatabaseAccess);
            }
            else
            {
                Ending(EndingType.ReportBroker);
            }
        }
    }

    // 게임 오버
    public void GameOver(GameOverReason reason)
    {
        phase = GamePhase.GameOver;
        lastGameOverReason = reason;
        Debug.Log("게임 오버! 이유: " + reason);

        if (OnPhaseChanged != null)
        {
            OnPhaseChanged(phase);
        }
    }

    // 엔딩
    public void Ending(EndingType type)
    {
        phase = GamePhase.Ending;
        lastEnding = type;
        Debug.Log("엔딩! " + type);

        if (OnPhaseChanged != null)
        {
            OnPhaseChanged(phase);
        }
    }

    // 타이틀로 (지금은 그냥 씬 다시 켜서 처리)
    public void RestartToTitle()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }
}
