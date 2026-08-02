using System;
using System.Collections.Generic;
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
    public ArbeitManager arbeit;
    public SlideshowView slideshow; // 인트로 컷신 (없으면 그냥 바로 시작)

    public Sprite[] introSlides;      // 새 게임 시작할 때 보여줄 그림들 (인스펙터에서 연결)
    public Sprite[] databaseEndSlides; // DB 접근 엔딩 컷신
    public Sprite[] reportEndSlides;   // 신고 엔딩 컷신
    public Sprite[] gameOverSlides;    // 게임 오버 컷신

    // 게임 상태
    public GamePhase phase = GamePhase.Intro; // 지금 어느 단계인지
    public bool hasSpaceship = false;         // 우주선 샀는지
    public bool shopUnlocked = false;         // 밤 상점이 열렸는지 (이벤트로 해금)

    // 하드모드 (우주선 산 뒤)
    public bool hardMode = false;                             // 하드모드 중인지
    public int hardModeDay = 0;                               // 하드모드 며칠째인지
    public BrokerChoice brokerChoice = BrokerChoice.None;     // 브로커한테 뭐라고 답했는지 (엔딩 분기)

    // 파티용품점 버프 (밤에 사면 다음 날 하루 적용)
    public int pendingPayoutBuff = 0;      // 내일 성과금 몇 % 더 받을지
    public int pendingExtraComplaints = 0; // 내일 민원인 몇 명 더 올지
    public int payoutBuffToday = 0;        // 오늘 적용 중인 성과금 버프
    public int extraComplaintsToday = 0;   // 오늘 적용 중인 민원인 추가

    // 어떻게 끝났는지 기록 (엔딩 화면에서 보여줄 때 씀)
    public GameOverReason lastGameOverReason;
    public EndingType lastEnding = EndingType.None;

    // 단계 바뀌면 알려줌 (나중에 UI 가 이거 듣고 화면 바꿈)
    public event Action<GamePhase> OnPhaseChanged;

    void Awake()
    {
        instance = this; // 게임 켜질 때 instance 에 나를 넣어줌 (싱글톤 패턴)
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

    // 새 게임 시작 (타이틀에서 부름)
    public void GameStart()
    {
        ResetForNewGame();
        save.Save(); // 첫날 아침도 이어할 수 있게 저장

        // 인트로 슬라이드쇼 보고 나서 첫날 시작 (연결 안 했거나 그림 없으면 바로 시작)
        if (slideshow != null && introSlides != null && introSlides.Length > 0)
        {
            slideshow.Play(introSlides, StartNewDay);
            return;
        }

        StartNewDay();
    }

    // 저장한 아침부터 이어하기
    public bool ContinueGame()
    {
        if (save.Load() == false)
        {
            return false;
        }

        StartNewDay();
        return true;
    }

    // 새 게임에 필요한 값들을 처음 상태로 돌림
    void ResetForNewGame()
    {
        phase = GamePhase.Intro;
        hasSpaceship = false;
        shopUnlocked = false;
        hardMode = false;
        hardModeDay = 0;
        brokerChoice = BrokerChoice.None;
        lastEnding = EndingType.None;

        calendar.day = 1;
        economy.credits = config.startingCredits;
        economy.rentMissCount = 0;
        economy.nextRentDay = config.firstRentDay;
        economy.foodPaidToday = 0;
        economy.rentPaidToday = 0;
        economy.rentWasDueToday = false;
        economy.rentMissedToday = false;
        suspicion.current = 0;

        inventory.ownedItems.Clear();
        inventory.permItemIds.Clear();
        inventory.permItemLevels.Clear();
        pendingPayoutBuff = 0;
        pendingExtraComplaints = 0;
        payoutBuffToday = 0;
        extraComplaintsToday = 0;
        arbeit.currentArbeit = null;
        arbeit.arbeitLeftToday = 0;

        for(int i = 0; i < arbeit.arbeitDoneCounts.Count; i++)
        {
            arbeit.arbeitDoneCounts[i] = 0;
        }

        for (int i = 0; i < shop.shopExp.Count; i++)
        {
            shop.shopExp[i] = 0;
        }

        events.seenEventIds.Clear();
        events.currentEvent = null;
        events.lineIndex = 0;
    }

    // 하루 시작 (낮 - 민원 처리)
    void StartNewDay()
    {
        // 어젯밤 산 버프가 오늘부터 적용됨
        payoutBuffToday = pendingPayoutBuff;
        extraComplaintsToday = pendingExtraComplaints;
        pendingPayoutBuff = 0;
        pendingExtraComplaints = 0;
        arbeit.ResetDaily();     // 알바 횟수 충전
        ApplyDailyItemEffects(); // 밀수템 아침 정산

        // 밀수템 의심도 때문에 아침부터 게임오버 났으면 하루 시작 안 함
        if (phase == GamePhase.GameOver)
        {
            return;
        }

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

    // 매일 아침 밀수템 효과 정산
    void ApplyDailyItemEffects()
    {
        List<ItemDefinition> items = content.items;
        for (int i = 0; i < items.Count; i++)
        {
            ItemDefinition item = items[i];
            if (item.kind != ItemKind.Permanent || inventory.GetItemLevel(item.id) <= 0)
            {
                continue; // 안 산 물건
            }

            if (item.effect == ItemEffect.DailyMoneyPercent)
            {
                // 복제된 법인카드: 돈이 붇는 대신 매일 의심받음
                if (economy.credits > 0)
                {
                    economy.credits += Mathf.RoundToInt(economy.credits * item.power / 100f);
                }
                suspicion.AddSuspicion(item.sidePower);
            }
            else if (item.effect == ItemEffect.DailyRandomBox)
            {
                // 밀수 루트 지도: 매일 아침 랜덤박스가 굴러감
                economy.AddCredits(UnityEngine.Random.Range(item.boxRewardMin, item.boxRewardMax + 1));
            }
        }
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
        if (complaint.mistakeCount == 0 && complaint.successCount > 0)
        {
            economy.AddCredits(config.perfectDayBonus);
            complaint.earnedToday += config.perfectDayBonus;
        }

        // 업무가 끝났으니 식비와 월세를 차례대로 정산
        economy.PayDailyBills(calendar.day);

        // 월세가 세 번 밀려 게임오버 됐으면 결산 화면으로 넘어가지 않음
        if (phase == GamePhase.GameOver)
        {
            return;
        }

        ChangePhase(GamePhase.DayResult);
    }

    // 결산 확인 -> 밤 (상점)으로. 상점 해금 전엔 밤 없이 바로 다음날
    public void ConfirmDayResult()
    {
        if (phase != GamePhase.DayResult)
        {
            return; // 두 번 눌려도 하루가 두 번 넘어가지 않게
        }

        if (shopUnlocked == false)
        {
            SleepAndAdvance();
            return;
        }

        ChangePhase(GamePhase.NightShop);
    }

    // 상점 해금 (해금 이벤트가 불러줌) - 오늘 밤부터 상점 열리고 알바도 한 번 생김
    public void UnlockShop()
    {
        if (shopUnlocked == true)
        {
            return;
        }

        shopUnlocked = true;
        arbeit.arbeitLeftToday += 1; // 아침 충전은 이미 끝났으니 오늘 몫은 직접 넣어줌
        Debug.Log("상점 해금됨");
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
            // 브로커 등장 대사는 이벤트 데이터로 넣음 (timing=Trigger, condition=HasSpaceship)
        }

        save.Save(); // 아침마다 자동 저장

        StartNewDay();
    }

    // 우주선 사기 (브로커 상점의 우주선표를 사는 것과 같음)
    public bool TryBuySpaceship()
    {
        ItemDefinition ticket = FindSpaceshipItem();
        if (ticket == null)
        {
            Debug.LogWarning("우주선표 아이템이 데이터에 없음");
            return false;
        }

        return shop.Buy(ticket); // 값이랑 해금 조건은 전부 상점이 봐줌
    }

    // 데이터에서 우주선표 찾기 (없으면 null)
    public ItemDefinition FindSpaceshipItem()
    {
        List<ItemDefinition> items = content.items;
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].isSpaceshipTicket == true)
            {
                return items[i];
            }
        }

        return null;
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

        // 엔딩날 아침이면 지출이나 소문 없이 바로 엔딩 (마지막 날은 엔딩 구간)
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
            return;
        }

        // 하드모드엔 고정 지출이 늘어남
        economy.credits -= config.hardModeExtraCost;

        // 신고 루트면 브로커가 퍼뜨린 소문 때문에 매일 의심받음
        if (brokerChoice == BrokerChoice.ReportBroker)
        {
            suspicion.AddSuspicion(config.reportSuspicionPerDay);
        }
    }

    // 게임 오버
    public void GameOver(GameOverReason reason)
    {
        lastGameOverReason = reason;
        Debug.Log("게임 오버! 이유: " + reason);
        save.Clear(); // 죽었으니 이어하기 못 하게 저장 지움
        ChangePhase(GamePhase.GameOver);
        PlayEndCutscene(gameOverSlides);
    }

    // 엔딩
    public void Ending(EndingType type)
    {
        lastEnding = type;
        Debug.Log("엔딩! " + type);
        save.Clear(); // 엔딩 봤으니 이 회차는 끝 (새로 시작해야 함)
        ChangePhase(GamePhase.Ending);

        if (type == EndingType.DatabaseAccess)
        {
            PlayEndCutscene(databaseEndSlides);
        }
        else
        {
            PlayEndCutscene(reportEndSlides);
        }
    }

    // 끝날 때 컷신 틀기
    // 페이즈는 이미 바꿔놓고 그 위에 덮어서 보여줌 - 안 그러면 하루가 더 진행돼버림
    void PlayEndCutscene(Sprite[] slides)
    {
        if (slideshow == null || slides == null || slides.Length == 0)
        {
            return; // 그림 아직 없으면 그냥 결과 화면
        }

        slideshow.Play(slides, null);
    }

    // 밸런스 시뮬레이션용 - 인트로 없이 바로 1일차부터 다시 시작 (디버그 화면에서만 씀)
    public void RestartForSim()
    {
        ResetForNewGame();
        StartNewDay();
    }

    // 타이틀로 (지금은 그냥 씬 다시 켜서 처리)
    public void RestartToTitle()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }
}
