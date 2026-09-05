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
    public AchievementManager achievements;
    public SlideshowView slideshow; // 인트로 컷신 (없으면 그냥 바로 시작)

    public Sprite[] introSlides;      // 새 게임 시작할 때 보여줄 그림들 (인스펙터에서 연결)
    public Sprite[] databaseEndSlides; // DB 접근 엔딩 컷신
    public Sprite[] reportEndSlides;   // 신고 엔딩 컷신
    public Sprite[] gameOverSlides;    // 들킨(의심도) 게임 오버 컷신
    public Sprite[] rentOverSlides;    // 월세 밀려 쫓겨난 게임 오버 컷신 (안 꽂으면 위 걸로 나감)

    // 게임 상태
    public GamePhase phase = GamePhase.Intro; // 지금 어느 단계인지
    public bool hasSpaceship = false;         // 우주선 샀는지
    public bool shopUnlocked = false;         // 밤 상점이 열렸는지 (이벤트로 해금)

    // 하드모드 (우주선 산 뒤)
    public bool hardMode = false;                             // 하드모드 중인지
    public int hardModeDay = 0;                               // 하드모드 며칠째인지
    public BrokerChoice brokerChoice = BrokerChoice.None;     // 브로커한테 뭐라고 답했는지 (엔딩 분기)

    // 브로커 첫 대사를 다 봤는지 (다 봐야 선택창이 뜸)
    public bool brokerOfferDone = false;

    bool _nextDayStoryDone = false;   // 우주선 산 다음날 대사를 이미 봤는지
    EndingType _pendingEnding;        // 엔딩 대사 끝나고 갈 엔딩

    // 브로커 상태가 바뀌면 알려줌 (선택창이 듣고 켜고 끔)
    public event System.Action OnBrokerStateChanged;

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
        brokerOfferDone = false;
        _nextDayStoryDone = false;
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
        arbeit.arbeitGoodCount = 0;

        for(int i = 0; i < arbeit.arbeitDoneCounts.Count; i++)
        {
            arbeit.arbeitDoneCounts[i] = 0;
        }

        for (int i = 0; i < shop.shopExp.Count; i++)
        {
            shop.shopExp[i] = 0;
        }
        shop.consumableBuyCount = 0;
        shop.buyLockedShops.Clear(); // 이벤트로 잠가둔 상점 구매도 초기화

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
        shop.DecayConsumableBuys(); // 1회성 물건 값 올려놓은 게 조금 내려감
        ApplyDailyItemEffects(); // 밀수템 아침 정산

        // 밀수템 의심도 때문에 아침부터 게임오버 났으면 하루 시작 안 함
        if (phase == GamePhase.GameOver)
        {
            return;
        }

        ChangePhase(GamePhase.DayWork);

        // 우주선 산 다음날 아침엔 브로커 뒷이야기부터 (하루에 한 번만)
        if (hardMode == true
            && hardModeDay == 1
            && _nextDayStoryDone == false
            && content.brokerNextDayDialogue != null
            && DialogueSystem.Instance != null)
        {
            _nextDayStoryDone = true;

            // 어제 민원 숫자가 상단바에 남아있지 않게 비워둠
            complaint.todayComplaints.Clear();
            complaint.currentIndex = 0;

            DialogueSystem.Instance.StartDialogue(content.brokerNextDayDialogue, ContinueDayStart);
            return;
        }

        ContinueDayStart();
    }

    // 아침 대사가 끝난 뒤부터의 하루 시작
    void ContinueDayStart()
    {
        // 아침 이벤트 있으면 대사부터 (끝나면 EventManager 가 OnMorningEventDone 불러줌)
        EventDefinition todayEvent = events.PickTodayEvent();
        if (todayEvent != null)
        {
            // 어제 민원 숫자가 상단바에 그대로 남아서, 오늘 목록이 잡히기 전엔 비워둠
            complaint.todayComplaints.Clear();
            complaint.currentIndex = 0;
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
        // 하루 마무리 이벤트가 있으면 결산 전에 먼저 보여줌 (게임오버면 건너뜀)
        EventDefinition endEvent = null;
        if (phase != GamePhase.GameOver)
        {
            endEvent = events.PickEvent(EventSlot.DayEnd);
        }
        if (endEvent != null)
        {
            events.StartEvent(endEvent);
            return; // 대사 끝나면 ContinueDayEnd 가 이어감
        }

        ContinueDayEnd();
    }

    // 하루 마무리 이벤트까지 끝난 뒤 실제 정산 (EventManager 도 여기로 이어줌)
    public void ContinueDayEnd()
    {
        // 하루 다 맞췄으면 무실수 보너스
        if (complaint.mistakeCount == 0 && complaint.successCount > 0)
        {
            economy.AddCredits(config.perfectDayBonus);
            complaint.earnedToday += config.perfectDayBonus;

            if (achievements != null)
            {
                achievements.AddPerfectDay();
            }
        }

        // 오늘 처리한 민원을 누적에 더하고 업적 확인
        // 저장은 여기서 한 번만 함 (업적을 다 따고 나면 Unlock 이 안 불려서 누적이 안 남았음)
        if (achievements != null)
        {
            achievements.AddComplaints(complaint.successCount);
            achievements.Check();
            achievements.Save();
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
        arbeit.arbeitMaxToday += 1;
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

        // 하드모드는 우주선을 산 그 자리에서 시작함 (OnSpaceshipBought)
        // 예전엔 다음 날 아침에 켰는데, 브로커가 하루 뒤에 찾아오는 게 어색해서 옮김

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

    // 브로커한테 답하기 (선택창 버튼이 불러줌)
    public void ChooseBroker(BrokerChoice choice)
    {
        brokerChoice = choice;

        // DB 접근을 골랐으면 브로커가 자금이 부족하다며 돈을 떼감
        if (choice == BrokerChoice.DatabaseAccess && economy.credits > 0)
        {
            int taken = Mathf.RoundToInt(economy.credits * config.brokerTakeRate);
            economy.credits -= taken;
        }

        NotifyBrokerChanged(); // 선택창부터 닫고

        // 고른 대로 다른 대사 - 그게 끝나면 그날은 거기서 끝남
        if (choice == BrokerChoice.DatabaseAccess)
        {
            PlayBrokerDialogue(content.brokerDatabaseDialogue, EndDayAfterBroker);
        }
        else
        {
            PlayBrokerDialogue(content.brokerReportDialogue, EndDayAfterBroker);
        }
    }

    // 우주선을 산 그 순간 (상점이 불러줌)
    // 브로커가 바로 찾아와서 대사를 하고, 그게 끝나면 선택창이 뜸
    public void OnSpaceshipBought()
    {
        if (hardMode == true)
        {
            return; // 이미 시작함
        }

        hardMode = true;
        hardModeDay = 0;
        brokerChoice = BrokerChoice.None;
        brokerOfferDone = false;

        NotifyBrokerChanged();

        // 대사를 안 꽂아뒀으면 바로 선택창으로 넘어감 (데이터 없다고 멈추면 안 됨)
        if (content.brokerOfferDialogue == null || DialogueSystem.Instance == null)
        {
            brokerOfferDone = true;
            NotifyBrokerChanged();
            return;
        }

        DialogueSystem.Instance.StartDialogue(content.brokerOfferDialogue, OnBrokerOfferDone);
    }

    // 브로커 첫 대사가 끝났을 때 - 이제 선택창을 띄워도 됨
    void OnBrokerOfferDone()
    {
        brokerOfferDone = true;
        NotifyBrokerChanged();
    }

    // 브로커 대사 틀기 (대사가 없으면 끝난 셈 치고 바로 다음으로)
    void PlayBrokerDialogue(DialogueData dialogue, System.Action onEnd)
    {
        if (dialogue == null || DialogueSystem.Instance == null)
        {
            if (onEnd != null)
            {
                onEnd();
            }
            return;
        }

        DialogueSystem.Instance.StartDialogue(dialogue, onEnd);
    }

    // 브로커한테 답한 날은 거기서 하루가 끝남
    // 우주선은 밤 상점에서 사는 거라, 답하고 나면 그날 밤에 더 할 게 없음
    void EndDayAfterBroker()
    {
        if (phase == GamePhase.Ending || phase == GamePhase.GameOver)
        {
            return; // 대사 도중에 끝났으면 하루를 또 넘기지 않음
        }

        SleepAndAdvance();
    }

    // 브로커 관련 상태가 바뀌었다고 알려줌 (선택창이 듣고 켜고 끔)
    void NotifyBrokerChanged()
    {
        if (OnBrokerStateChanged != null)
        {
            OnBrokerStateChanged();
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
                PlayEndingDialogue(content.databaseEndingDialogue, EndingType.DatabaseAccess);
            }
            else
            {
                PlayEndingDialogue(content.reportEndingDialogue, EndingType.ReportBroker);
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

    // 엔딩 대사부터 틀고 끝냄 (대사가 없으면 바로 엔딩)
    void PlayEndingDialogue(DialogueData dialogue, EndingType type)
    {
        _pendingEnding = type;

        if (dialogue == null || DialogueSystem.Instance == null)
        {
            Ending(type);
            return;
        }

        DialogueSystem.Instance.StartDialogue(dialogue, OnEndingDialogueDone);
    }

    // 엔딩 대사가 끝났을 때
    void OnEndingDialogueDone()
    {
        Ending(_pendingEnding);
    }

    // 게임 오버
    public void GameOver(GameOverReason reason)
    {
        lastGameOverReason = reason;
        Debug.Log("게임 오버! 이유: " + reason);
        save.Clear(); // 죽었으니 이어하기 못 하게 저장 지움
        ChangePhase(GamePhase.GameOver);

        if (achievements != null)
        {
            achievements.Check(); // 죽어야 주는 업적도 있음
        }

        // 들킨 것과 쫓겨난 건 그림이 달라서 이유별로 갈라줌
        Sprite[] slides = gameOverSlides;
        if (reason == GameOverReason.RentDefault && rentOverSlides != null && rentOverSlides.Length > 0)
        {
            slides = rentOverSlides;
        }

        PlayEndCutscene(slides);
    }

    // 엔딩
    public void Ending(EndingType type)
    {
        lastEnding = type;
        Debug.Log("엔딩! " + type);
        save.Clear(); // 엔딩 봤으니 이 회차는 끝 (새로 시작해야 함)
        ChangePhase(GamePhase.Ending);

        if (achievements != null)
        {
            achievements.Check(); // 엔딩 업적
        }

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

    // 타이틀(로비)로
    // 죽거나 엔딩 보면 저장은 이미 지워놨으니 로비에서 새로 시작하면 됨
    public void RestartToTitle()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
    }
}
