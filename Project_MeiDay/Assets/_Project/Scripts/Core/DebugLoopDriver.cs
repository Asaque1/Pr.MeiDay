using System.Collections.Generic;
using UnityEngine;

// 임시 디버그용 - 진짜 UI 만들기 전에 하루 루프 도는지 버튼으로 확인
// TODO: 진짜 UI 생기면 이거 지우기
public class DebugLoopDriver : MonoBehaviour
{
    int _selectedCard = 0;  // 지금 고른 카드 번호
    float _autoRate = 0.8f; // 자동 플레이 정답률
    int _autoDays = 0;      // 남은 자동 진행 일수

    float _autoArbeitRate = 0.8f;   // 자동 플레이에서 알바 수행률
    bool _autoReportBroker = false; // 자동 플레이에서 브로커를 신고할지 (끄면 DB 접근)

    // 자동 플레이 결과 요약용 (밸런스 회의에 쓸 숫자)
    bool _simRunning = false;  // 자동 플레이 중인지 (요약을 한 번만 찍으려고)
    int _simSpaceshipDay = 0;  // 우주선 산 날 (0이면 못 삼)
    int _simArbeitDone = 0;    // 알바 총 횟수
    int _simDays = 0;          // 몇 일 돌렸는지
    int _simComplaints = 0;    // 나온 민원 총 수
    int _simFail = 0;          // 놓친 민원 수
    bool _batchMode = false;   // 여러 판 돌리는 중 (하루하루 로그를 끄려고)

    int _fontSize = 18;             // 글자 크기 (화면에 따라 +/- 로 조절)
    Vector2 _scroll = Vector2.zero; // 내용이 길어지면 스크롤

    bool _showAchievements = false; // 업적 목록(수첩) 펼침 여부
    string _achToast = "";          // 방금 딴 업적 알림 문구
    float _achToastLeft = 0f;       // 알림 남은 시간

    void Start()
    {
        // 업적 따면 화면 위에 잠깐 띄워줌 (진짜 토스트 UI 생기기 전까지 임시)
        if (GameManager.instance != null && GameManager.instance.achievements != null)
        {
            GameManager.instance.achievements.OnUnlocked += ShowAchToast;
        }
    }

    void OnDestroy()
    {
        if (GameManager.instance != null && GameManager.instance.achievements != null)
        {
            GameManager.instance.achievements.OnUnlocked -= ShowAchToast;
        }
    }

    void Update()
    {
        if (_achToastLeft > 0f)
        {
            _achToastLeft -= Time.deltaTime;
        }
    }

    // 업적 알림 띄우기
    void ShowAchToast(AchievementDefinition ach)
    {
        _achToast = "업적 달성!  " + ach.title;
        _achToastLeft = 4f;
    }

    void OnGUI()
    {
        GameManager gm = GameManager.instance;
        if (gm == null)
        {
            return;
        }

        // 기본 글자가 너무 작아서 키워줌 (버튼도 손가락 크기로)
        GUI.skin.label.fontSize = _fontSize;
        GUI.skin.button.fontSize = _fontSize;
        GUI.skin.box.fontSize = _fontSize;
        GUI.skin.button.padding = new RectOffset(10, 10, 7, 7);
        GUI.skin.label.wordWrap = true;

        // 화면 높이에 맞춰서 창을 잡고, 넘치면 스크롤로 봄
        float boxWidth = 520;
        float boxHeight = Screen.height - 40;
        GUILayout.BeginArea(new Rect(20, 20, boxWidth, boxHeight), GUI.skin.box);
        _scroll = GUILayout.BeginScrollView(_scroll);

        // 자동 플레이 도는 동안엔 진짜 저장이랑 업적 기록을 막아둠
        // (안 그러면 30일 자동 한 번에 업적이 뚫리고 세이브도 시뮬 상태로 덮어써짐)
        bool simming = (_autoDays > 0 || _batchMode == true);
        gm.save.skipSave = simming;
        if (gm.achievements != null)
        {
            gm.achievements.skipRecord = simming;
        }

        // 자동 진행 중이면 한 프레임에 하루씩 소화 (끝나거나 게임오버면 멈춤)
        if (gm.phase == GamePhase.Ending || gm.phase == GamePhase.GameOver)
        {
            if (_simRunning == true)
            {
                LogSimSummary(gm); // 끝났으니 결과 정리해서 콘솔에 찍음
            }
            _autoDays = 0;
        }
        // OnGUI 는 한 프레임에 여러 번 불리니까 Layout 때만 하루를 소화함 (안 그러면 두 배로 빨리 감)
        if (Event.current.type == EventType.Layout
            && _autoDays > 0
            && (gm.phase == GamePhase.DayWork || gm.phase == GamePhase.DayResult))
        {
            _autoDays--;
            AutoPlayOneDay(gm);
        }

        // 정해둔 일수를 다 돌았는데 아직 안 끝났으면 왜 못 끝났는지 알려줌 (조용히 멈추면 안 됨)
        if (_simRunning == true && _autoDays == 0
            && gm.phase != GamePhase.Ending && gm.phase != GamePhase.GameOver)
        {
            LogSimStuck(gm);
        }

        // 지금 상태 (맨 위에 크게)
        GUILayout.Label("■ " + PhaseName(gm.phase) + "   /   " + gm.calendar.day + "일차");
        GUILayout.Label("크레딧 " + gm.economy.credits
            + "      의심도 " + Mathf.RoundToInt(gm.suspicion.current)
            + " / " + Mathf.RoundToInt(gm.config.maxSuspicion));
        if (gm.shopUnlocked == true)
        {
            GUILayout.Label("남은 알바 " + gm.arbeit.arbeitLeftToday + "회"
                + "      상점레벨 합 " + ShopLevelSum(gm));
        }
        else
        {
            GUILayout.Label("상점 잠김 (2일차 아침에 열림)");
        }
        DrawLine();

        // 테스트용 치트
        GUILayout.Label("- 치트 -");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("+500cr"))
        {
            gm.economy.AddCredits(500);
        }
        if (GUILayout.Button("의심도 -20"))
        {
            gm.suspicion.ReduceSuspicion(20);
        }
        if (gm.shopUnlocked == false && GUILayout.Button("상점 해금"))
        {
            gm.UnlockShop();
        }
        GUILayout.EndHorizontal();

        // 후반부(우주선 -> 브로커 -> 엔딩)를 알바 19번 안 하고 바로 보려고 넣은 치트
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("상점레벨 +2씩"))
        {
            // 0->1 이랑 1->2 에 드는 만큼 한 번에 넣어줌
            int amount = gm.config.shopLevelExpBase * 2 + gm.config.shopLevelExpStep;
            gm.shop.AddShopExp(ShopType.Bookstore, amount);
            gm.shop.AddShopExp(ShopType.Theater, amount);
            gm.shop.AddShopExp(ShopType.PartyShop, amount);
        }
        if (gm.hasSpaceship == false && GUILayout.Button("우주선 바로 얻기"))
        {
            gm.hasSpaceship = true;
        }
        GUILayout.EndHorizontal();

        DrawLine();

        // 자동 플레이 (밸런스 확인용 - 정답률 굴려가며 하루를 통째로 돌림)
        GUILayout.Label("- 자동 플레이 -");
        // 슬라이더 값은 5% 단위로 맞춤 (계단 경계에서 결과가 갈리는데 라벨로는 구분이 안 되니까)
        GUILayout.BeginHorizontal();
        GUILayout.Label("카드 정답률 " + Mathf.RoundToInt(_autoRate * 100) + "%", GUILayout.Width(200));
        _autoRate = SnapRate(GUILayout.HorizontalSlider(_autoRate, 0f, 1f, GUILayout.Height(24)));
        GUILayout.EndHorizontal();

        // 카드 제출 한 번당 확률이라, 민원 하나를 끝내 성공하는 확률은 이거보다 훨씬 높음
        GUILayout.Label("  -> 민원 성공률 " + Mathf.RoundToInt(EffectiveSuccess(gm) * 100)
            + "% (" + gm.config.maxWrongTries + "번까지 다시 하니까)");

        GUILayout.BeginHorizontal();
        GUILayout.Label("알바 수행률 " + Mathf.RoundToInt(_autoArbeitRate * 100) + "%", GUILayout.Width(200));
        _autoArbeitRate = SnapRate(GUILayout.HorizontalSlider(_autoArbeitRate, 0f, 1f, GUILayout.Height(24)));
        GUILayout.EndHorizontal();

        GUILayout.Label("  -> 보수·경험치 " + Mathf.RoundToInt(ArbeitTier(_autoArbeitRate) * 100) + "% 받음");

        string brokerLabel = "브로커: DB 접근 루트";
        if (_autoReportBroker == true)
        {
            brokerLabel = "브로커: 신고 루트";
        }
        if (GUILayout.Button(brokerLabel))
        {
            _autoReportBroker = !_autoReportBroker;
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("1일"))
        {
            StartSim(1, false);
        }
        if (GUILayout.Button("7일"))
        {
            StartSim(7, false);
        }
        if (GUILayout.Button("30일"))
        {
            StartSim(30, false);
        }
        GUILayout.EndHorizontal();

        // 엔딩(또는 게임오버) 날 때까지 돌리고 결과를 콘솔에 요약해줌
        if (GUILayout.Button("엔딩까지 돌리기 (결과 요약)"))
        {
            StartSim(300, true);
        }

        // 한 번만 돌리면 운에 따라 결과가 갈리니까 여러 판을 돌려서 가운데값을 봄
        if (GUILayout.Button("20판 돌려서 통계 내기"))
        {
            RunBatch(20);
        }

        // 글자가 작거나 크면 여기서 조절
        GUILayout.BeginHorizontal();
        GUILayout.Label("글자 크기 " + _fontSize, GUILayout.Width(140));
        if (GUILayout.Button("-", GUILayout.Width(50)) && _fontSize > 10)
        {
            _fontSize--;
        }
        if (GUILayout.Button("+", GUILayout.Width(50)) && _fontSize < 36)
        {
            _fontSize++;
        }
        GUILayout.EndHorizontal();

        DrawLine();

        // 하드모드면 진행 상황 + 아직 답 안 했으면 브로커 선택창 먼저
        if (gm.hardMode == true)
        {
            GUILayout.Label("[하드모드] " + gm.hardModeDay + " / " + gm.config.hardModeDays + "일");
            if (gm.brokerChoice == BrokerChoice.None && gm.events.currentEvent == null)
            {
                GUILayout.Label("브로커: 자, 이제 어떻게 할 거야?");
                if (GUILayout.Button("DB 접근시키기 (돈 뜯김)"))
                {
                    gm.ChooseBroker(BrokerChoice.DatabaseAccess);
                }
                if (GUILayout.Button("신고하기 (매일 의심도 증가)"))
                {
                    gm.ChooseBroker(BrokerChoice.ReportBroker);
                }
            }
            GUILayout.Space(10);
        }

        // 페이즈마다 버튼 다르게 보여줌
        if (gm.phase == GamePhase.Intro)
        {
            if (gm.save.HasSave() == true)
            {
                if (GUILayout.Button("이어하기"))
                {
                    gm.ContinueGame();
                }
            }

            if (GUILayout.Button("새 게임"))
            {
                gm.GameStart();
            }
        }
        else if (gm.phase == GamePhase.DayWork)
        {
            // 아침 이벤트 대사 나오는 중이면 그것부터
            if (gm.events.currentEvent != null)
            {
                DrawDialogue(gm.events);
            }
            else
            {
                ComplaintManager cm = gm.complaint;
                if (cm.currentIndex < cm.todayComplaints.Count)
                {
                    ComplaintDefinition c = cm.todayComplaints[cm.currentIndex];
                    GUILayout.Label("민원 " + (cm.currentIndex + 1) + " / " + cm.todayComplaints.Count);

                    // 해석 단계면 카드 맞추기, 처리 단계면 처리 고르기
                    if (cm.stage == ComplaintStage.Translate)
                    {
                        DrawTranslate(cm.translation, cm);
                    }
                    else
                    {
                        DrawResolve(cm, c);
                    }
                }
            }
        }
        else if (gm.phase == GamePhase.DayResult)
        {
            ComplaintManager cm = gm.complaint;
            GUILayout.Label("- 오늘 결산 -");
            GUILayout.Label("성공: " + cm.successCount + "  실패: " + cm.failCount);
            GUILayout.Label("오늘 수입: " + cm.earnedToday);
            if (cm.mistakeCount == 0 && cm.successCount > 0)
            {
                GUILayout.Label("무실수 보너스!");
            }

            GUILayout.Label("식비: -" + gm.economy.foodPaidToday);
            if (gm.economy.rentWasDueToday == true)
            {
                if (gm.economy.rentMissedToday == true)
                {
                    GUILayout.Label("월세 미납: " + gm.economy.rentMissCount + " / " + gm.config.rentMissLimit);
                }
                else
                {
                    GUILayout.Label("월세: -" + gm.economy.rentPaidToday);
                }
            }
            GUILayout.Label("남은 크레딧: " + gm.economy.credits);

            string confirmLabel = "결산 확인 -> 밤으로";
            if (gm.shopUnlocked == false)
            {
                confirmLabel = "결산 확인 -> 다음날 (상점 잠김)";
            }
            if (GUILayout.Button(confirmLabel))
            {
                gm.ConfirmDayResult();
            }
        }
        else if (gm.phase == GamePhase.NightShop)
        {
            DrawShop(gm);
            GUILayout.Space(6);
            DrawArbeit(gm);

            GUILayout.Space(6);
            ItemDefinition ticket = gm.FindSpaceshipItem();
            if (gm.hasSpaceship == true)
            {
                GUILayout.Label("우주선 보유중 (자면 하드모드 시작)");
            }
            else if (ticket == null)
            {
                GUILayout.Label("우주선표 아이템이 데이터에 없음");
            }
            else
            {
                GUILayout.Label("우주선 가격: " + ticket.price);
                if (gm.shop.CanBuy(ticket) == false)
                {
                    GUILayout.Label("아직 못 삼 (상점 레벨 합 " + gm.config.spaceshipShopLevel + " 이상 필요)");
                }
                else if (GUILayout.Button("우주선 구매"))
                {
                    gm.TryBuySpaceship();
                }
            }

            if (GUILayout.Button("자기 -> 다음날"))
            {
                gm.SleepAndAdvance();
            }
        }
        else if (gm.phase == GamePhase.Ending || gm.phase == GamePhase.GameOver)
        {
            GUILayout.Label(">>> " + gm.phase + " <<<");
            if (gm.phase == GamePhase.Ending)
            {
                GUILayout.Label("엔딩: " + gm.lastEnding);
            }
            else
            {
                GUILayout.Label("이유: " + gm.lastGameOverReason);
            }
            if (GUILayout.Button("타이틀로"))
            {
                gm.RestartToTitle();
            }
        }

        DrawLine();
        DrawAchievements(gm);

        GUILayout.EndScrollView();
        GUILayout.EndArea();

        // 업적 알림은 창 밖 위쪽에 따로 띄움
        if (_achToastLeft > 0f)
        {
            GUI.Box(new Rect(Screen.width / 2 - 200, 20, 400, 44), _achToast);
        }
    }

    // 수첩 - 업적 목록
    void DrawAchievements(GameManager gm)
    {
        if (gm.achievements == null)
        {
            GUILayout.Label("- 업적 - (매니저 연결 안 됨)");
            return;
        }

        List<AchievementDefinition> all = gm.content.achievements;

        // 지금 데이터에 있는 업적만 셈 (id 를 바꾸거나 지우면 옛 기록이 남아서 11/10 같은 게 나옴)
        int got = 0;
        for (int i = 0; i < all.Count; i++)
        {
            if (all[i] != null && gm.achievements.HasUnlocked(all[i].id) == true)
            {
                got++;
            }
        }

        if (GUILayout.Button("- 수첩 (업적 " + got + " / " + all.Count + ") -"))
        {
            _showAchievements = !_showAchievements;
        }

        if (_showAchievements == false)
        {
            return;
        }

        GUILayout.Label("처리한 민원 " + gm.achievements.totalComplaints
            + "   알바 " + gm.achievements.totalArbeit
            + "   무실수 " + gm.achievements.totalPerfectDays + "일");

        // 시뮬 돌려서 기록이 더럽혀졌을 때 되돌리는 용도
        if (GUILayout.Button("업적 기록 초기화"))
        {
            gm.achievements.ResetAll();
        }

        if (all.Count == 0)
        {
            GUILayout.Label("  업적 데이터가 아직 없음");
            return;
        }

        for (int i = 0; i < all.Count; i++)
        {
            AchievementDefinition ach = all[i];
            if (ach == null)
            {
                continue;
            }

            bool has = gm.achievements.HasUnlocked(ach.id);

            // 숨김 업적은 따기 전엔 물음표로만
            if (has == false && ach.hidden == true)
            {
                GUILayout.Label("  [ ? ]  ???");
                continue;
            }

            string mark = "[ ]";
            if (has == true)
            {
                mark = "[O]";
            }
            GUILayout.Label("  " + mark + "  " + ach.title + " - " + ach.description);
        }
    }

    // 페이즈를 한국말로 바꿔줌 (팀원들이 보기 편하게)
    string PhaseName(GamePhase phase)
    {
        if (phase == GamePhase.Intro)
        {
            return "타이틀";
        }
        if (phase == GamePhase.DayWork)
        {
            return "낮 - 민원 처리";
        }
        if (phase == GamePhase.DayResult)
        {
            return "결산";
        }
        if (phase == GamePhase.NightShop)
        {
            return "밤 - 상점 / 알바";
        }
        if (phase == GamePhase.Ending)
        {
            return "엔딩";
        }

        return "게임 오버";
    }

    // 우주선 조건에 쓰는 상점 레벨 합 (브로커 빼고 셋)
    int ShopLevelSum(GameManager gm)
    {
        return gm.shop.GetShopLevel(ShopType.Bookstore)
            + gm.shop.GetShopLevel(ShopType.Theater)
            + gm.shop.GetShopLevel(ShopType.PartyShop);
    }

    // 구분선 하나 긋기
    void DrawLine()
    {
        GUILayout.Space(6);
        GUILayout.Box("", GUILayout.Height(2), GUILayout.ExpandWidth(true));
        GUILayout.Space(6);
    }

    void DrawArbeit(GameManager gm)
    {
        GUILayout.Label("남은 알바 횟수: " + gm.arbeit.arbeitLeftToday);
        if (gm.arbeit.currentArbeit != null)
        {
            GUILayout.Label("알바 중: " + gm.arbeit.currentArbeit.minigameName);

            // 책 정리 알바면 진짜로 플레이
            if (gm.arbeit.bookSort != null)
            {
                DrawBookSort(gm);
                return;
            }

            // 조명 연출 알바면 진짜로 플레이
            if (gm.arbeit.spotlight != null)
            {
                DrawSpotlight(gm);
                return;
            }

            // 판촉 알바면 진짜로 플레이
            if (gm.arbeit.promo != null)
            {
                DrawPromo(gm);
                return;
            }
            if (GUILayout.Button("잘 끝냄(수행률 100%"))
            {
                gm.arbeit.FinishArbeit(1f);
            }
            if(GUILayout.Button("대충 끝냄 수행률 50%"))
            {
                gm.arbeit.FinishArbeit(0.5f);
            }
            return;
        }
        if (gm.arbeit.arbeitLeftToday <= 0)
        {
            GUILayout.Label("오늘 알바 끝");
            return;
        }
        if (gm.content.minigames.Count == 0)
        {
            GUILayout.Label("(알바 없음 - 미니게임 데이터 비었음)");
            return;
        }
        for(int i = 0; i < gm.content.minigames.Count; i++)
        {
            MinigameDefinition arbeit = gm.content.minigames[i];
            if(GUILayout.Button(ShopTag(arbeit.shopType) + " " + arbeit.minigameName + " ( " + arbeit.basePay + "cr)"))
            {
                gm.arbeit.StartArbeit(arbeit);
            }
        }
    }
    // 책 정리 미니게임 디버그 UI
    void DrawBookSort(GameManager gm)
    {
        BookSortGame game = gm.arbeit.bookSort;

        GUILayout.Label("정리한 책: " + game.sortedCount + " / " + game.totalBooks
            + "  (황금 " + game.goldenCount + " / 실수 " + game.missCount + ")");

        // 책 다 나왔으면 정산
        if (game.IsDone() == true)
        {
            GUILayout.Label("정리 끝! 수행률 " + Mathf.RoundToInt(game.GetRate() * 100) + "%");
            if (GUILayout.Button("보수 받기"))
            {
                gm.arbeit.FinishArbeit(game.GetRate());
            }
            return;
        }

        // 카트에 실린 책들을 줄줄이 보여주고, 맨 앞 책을 골라 꽂아봄
        string cart = "카트: ";
        for (int i = 0; i < game.cartGenre.Count; i++)
        {
            cart += (game.cartGenre[i] + 1) + "번" + (game.cartGolden[i] ? "(황금) " : " ");
        }
        GUILayout.Label(cart);
        GUILayout.Label("맨 앞 책을 어느 칸에 꽂을지 고르기 (진짜 판은 끌어다 놓음)");

        GUILayout.BeginHorizontal();
        for (int i = 0; i < game.genreCount; i++)
        {
            if (GUILayout.Button((i + 1) + "번 칸"))
            {
                game.PlaceBook(0, i);
            }
        }
        GUILayout.EndHorizontal();
    }

    // 조명 연출 미니게임 디버그 UI (버튼 꾹 눌러서 조명 움직이기)
    void DrawSpotlight(GameManager gm)
    {
        SpotlightGame game = gm.arbeit.spotlight;

        // 시간 다 됐으면 정산
        if (game.IsDone() == true)
        {
            GUILayout.Label("공연 끝! 수행률 " + Mathf.RoundToInt(game.GetRate() * 100) + "%");
            if (GUILayout.Button("보수 받기"))
            {
                gm.arbeit.FinishArbeit(game.GetRate());
            }
            return;
        }

        GUILayout.Label("남은 시간: " + Mathf.CeilToInt(game.timeLeft) + "초  (비춘 시간 " + game.onTargetTime.ToString("F1") + "초)");
        GUILayout.Label("무대: " + StageBar(game.actorPos, game.lightPos));
        GUILayout.Label("A = 배우, V = 조명, O = 비추는 중");
        if (game.actorGolden == true)
        {
            GUILayout.Label("황금 배우 등장! (비추면 두 배)");
        }

        int dir = 0;
        GUILayout.BeginHorizontal();
        if (GUILayout.RepeatButton("< 조명 왼쪽"))
        {
            dir = -1;
        }
        if (GUILayout.RepeatButton("조명 오른쪽 >"))
        {
            dir = 1;
        }
        GUILayout.EndHorizontal();

        // OnGUI 는 한 프레임에 여러 번 불려서 그리기 단계에서만 진행시킴
        if (Event.current.type == EventType.Repaint)
        {
            game.Tick(Time.deltaTime, dir);
        }
    }

    // 배우랑 조명 위치를 글자 줄로 그려줌
    string StageBar(float actor, float light)
    {
        char[] bar = new char[25];
        for (int i = 0; i < bar.Length; i++)
        {
            bar[i] = '-';
        }

        int a = Mathf.RoundToInt(actor * (bar.Length - 1));
        int l = Mathf.RoundToInt(light * (bar.Length - 1));
        bar[l] = 'V';
        if (a == l)
        {
            bar[a] = 'O';
        }
        else
        {
            bar[a] = 'A';
        }

        return new string(bar);
    }

    // 판촉 미니게임 디버그 UI (타이밍 맞춰 던지기)
    void DrawPromo(GameManager gm)
    {
        PromoGame game = gm.arbeit.promo;

        // 시간 다 됐으면 정산
        if (game.IsDone() == true)
        {
            GUILayout.Label("판촉 끝! 수행률 " + Mathf.RoundToInt(game.GetRate() * 100) + "%"
                + " (줌 " + game.handedCount + " / 황금 " + game.goldenCount + " / 헛손질 " + game.missCount + ")");
            if (GUILayout.Button("보수 받기"))
            {
                gm.arbeit.FinishArbeit(game.GetRate());
            }
            return;
        }

        GUILayout.Label("남은 시간: " + Mathf.CeilToInt(game.timeLeft) + "초  줌 " + game.handedCount + " / 헛손질 " + game.missCount);
        GUILayout.Label("길거리: " + StreetBar(game));
        GUILayout.Label("P = 손님, G = 황금 손님 (진짜 판은 눌러서 겨눔)");

        // 여기선 겨눌 수가 없으니 맨 앞 손님이 갈 자리로 대신 던져줌
        if (GUILayout.Button("맨 앞 손님한테 던지기!"))
        {
            if (game.people.Count > 0)
            {
                PromoPerson target = game.people[0];
                game.Throw(target.pos + target.dir * target.speed * game.flyTime);
            }
        }

        // OnGUI 는 한 프레임에 여러 번 불려서 그리기 단계에서만 진행시킴
        if (Event.current.type == EventType.Repaint)
        {
            game.Tick(Time.deltaTime);
        }
    }

    // 손님 위치를 글자 줄로 그려줌 (가로 자리만. 세로는 글자로 못 그리니 생략)
    string StreetBar(PromoGame game)
    {
        char[] bar = new char[25];
        for (int i = 0; i < bar.Length; i++)
        {
            bar[i] = '-';
        }

        for (int i = 0; i < game.people.Count; i++)
        {
            PromoPerson p = game.people[i];
            int at = Mathf.RoundToInt(Mathf.Clamp01(p.pos.x) * (bar.Length - 1));
            if (p.golden == true)
            {
                bar[at] = 'G';
            }
            else
            {
                bar[at] = 'P';
            }
        }

        return new string(bar);
    }

    // 밤 상점 디버그 UI (아이템 사기)
    void DrawShop(GameManager gm)
    {
        GUILayout.Label("상점 레벨 - 서점 " + gm.shop.GetShopLevel(ShopType.Bookstore)
            + " / 연극단 " + gm.shop.GetShopLevel(ShopType.Theater)
            + " / 파티 " + gm.shop.GetShopLevel(ShopType.PartyShop));
        if (gm.content.items.Count == 0)
        {
            GUILayout.Label("(파는 물건 없음 - 아이템 데이터 비었음)");
            return;
        }

        for (int i = 0; i < gm.content.items.Count; i++)
        {
            ItemDefinition item = gm.content.items[i];

            // 해금 안 된 물건은 뭔지 안 보여줌
            if (gm.shop.IsUnlocked(item) == false)
            {
                GUILayout.Label(ShopTag(item.shopType) + " ??? (잠김)");
                continue;
            }

            string label = ShopTag(item.shopType) + " " + item.itemName;
            if (item.kind == ItemKind.Permanent)
            {
                label += " lv" + gm.inventory.GetItemLevel(item.id) + "/" + item.maxLevel;
            }

            // 품절인지 아직 조건이 안 된 건지 구별해서 보여줌
            string reason = gm.shop.BuyBlockReason(item);
            if (reason.Length > 0)
            {
                GUILayout.Label(label + "  (" + reason + ")");
                continue;
            }

            if (GUILayout.Button(label + " - " + gm.shop.PriceFor(item) + "cr"))
            {
                gm.shop.Buy(item);
            }
        }
    }

    // 상점 이름 짧은 태그
    string ShopTag(ShopType type)
    {
        if (type == ShopType.Bookstore)
        {
            return "[서점]";
        }
        if (type == ShopType.Theater)
        {
            return "[연극단]";
        }
        if (type == ShopType.PartyShop)
        {
            return "[파티]";
        }
        return "[브로커]";
    }

    // 아침 이벤트 대사 디버그 UI
    void DrawDialogue(EventManager em)
    {
        EventLine line = em.CurrentLine();
        if (line == null)
        {
            return;
        }

        string who = "???";
        if (line.speaker != null)
        {
            who = line.speaker.displayName;
        }

        GUILayout.Label("[이벤트] " + em.currentEvent.id + " (" + (em.lineIndex + 1) + " / " + em.currentEvent.lines.Count + ")");
        GUILayout.Label(who + ": " + line.text);

        if (GUILayout.Button("다음 대사"))
        {
            em.AdvanceLine();
        }
    }

    // 해석 단계 디버그 UI (상자 + 카드)
    void DrawTranslate(TranslationController tc, ComplaintManager cm)
    {
        GUILayout.Label("[해석 단계]  틀린 횟수: " + tc.wrongCount);

        // 상자들
        for (int i = 0; i < tc.slots.Count; i++)
        {
            WordSlot s = tc.slots[i];
            string placed = "( 빈칸 )";
            if (s.placedWord != null)
            {
                placed = s.placedWord.meaning;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label(i + "번 [" + s.category + "] : " + placed);
            if (GUILayout.Button("비우기", GUILayout.Width(55)))
            {
                tc.ClearSlot(i);
            }
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(4);
        GUILayout.Label("고른 카드: " + _selectedCard);

        // 카드들 (누르면 고름)
        for (int i = 0; i < tc.cards.Count; i++)
        {
            WordCard card = tc.cards[i];
            if (card.isRemoved == true)
            {
                continue; // 떨어져 사라진 카드
            }

            string mark = "";
            if (card.isUsed == true)
            {
                mark = " (놓음)";
            }
            if (GUILayout.Button(i + " : " + card.word.meaning + mark))
            {
                _selectedCard = i;
            }
        }

        // 고른 카드를 몇 번 상자에 넣을지
        GUILayout.Label("아래 눌러서 상자에 넣기:");
        GUILayout.BeginHorizontal();
        for (int i = 0; i < tc.slots.Count; i++)
        {
            if (GUILayout.Button("-> " + i, GUILayout.Width(45)))
            {
                tc.PlaceCard(_selectedCard, i);
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(4);

        // 빈칸을 다 채워야 제출이 됨 (실제 화면의 확인 버튼도 그때 나옴)
        if (tc.IsAllFilled() == false)
        {
            GUILayout.Label("빈칸을 다 채워야 제출 가능");
        }

        if (GUILayout.Button("확인 (해석 제출)"))
        {
            // 디버그 화면은 등장/대사 연출을 안 거치니까 말 끝난 걸로 쳐줌
            cm.entry = ComplaintEntry.Ready;
            cm.ConfirmTranslation();
        }
    }

    // 처리 단계 디버그 UI
    void DrawResolve(ComplaintManager cm, ComplaintDefinition c)
    {
        GUILayout.Label("[처리 단계]");
        GUILayout.Label("틀린 횟수: " + cm.resolutionWrongCount + " / " + GameManager.instance.config.maxResolutionWrongTries);
        if (GUILayout.Button("정답 처리"))
        {
            cm.SelectResolution(c.correctResolution);
        }
        if (GUILayout.Button("오답 처리"))
        {
            cm.SelectResolution(WrongOf(c.correctResolution));
        }
    }

    // 정답 아닌 처리 하나 골라줌 (오답 흉내내기)
    ComplaintResolution WrongOf(ComplaintResolution correct)
    {
        if (correct == ComplaintResolution.CallSecurity)
        {
            return ComplaintResolution.DocumentPrint;
        }

        return ComplaintResolution.CallSecurity;
    }

    // 하루를 자동으로 돌림 (밸런스 보는 용도. 정답률만큼 성공하고 나머진 실수 냄)
    void AutoPlayOneDay(GameManager gm)
    {
        // 아침 이벤트 대사는 그냥 넘김
        int guard = 0;
        while (gm.events.currentEvent != null && guard < 200)
        {
            guard++;
            gm.events.AdvanceLine();
        }

        // 하드모드 첫 아침이면 브로커한테 답부터 함 (안 하면 7일 시계가 안 돌아서 엔딩이 안 옴)
        if (gm.hardMode == true && gm.brokerChoice == BrokerChoice.None)
        {
            if (_autoReportBroker == true)
            {
                gm.ChooseBroker(BrokerChoice.ReportBroker);
            }
            else
            {
                gm.ChooseBroker(BrokerChoice.DatabaseAccess);
            }
        }

        ComplaintManager cm = gm.complaint;
        guard = 0;
        while (gm.phase == GamePhase.DayWork && cm.currentIndex < cm.todayComplaints.Count && guard < 200)
        {
            guard++;
            ComplaintDefinition c = cm.todayComplaints[cm.currentIndex];

            if (Random.value < _autoRate)
            {
                // 성공: 해석 통과한 셈 치고 바로 맞게 처리
                cm.stage = ComplaintStage.Resolve;
                cm.SelectResolution(c.correctResolution);
            }
            else
            {
                // 실수: 해석 실수 하나 쌓음 (쌓이면 민원인이 알아서 나감)
                // 예전엔 빈 답안을 제출했는데, 이제 빈칸을 다 채워야 확인 버튼이 나와서 그 길이 막힘
                cm.AddTranslationMistake();
            }

            if (gm.phase == GamePhase.GameOver)
            {
                return;
            }
        }

        if (gm.phase == GamePhase.DayResult)
        {
            _simDays++;
            _simComplaints += cm.todayComplaints.Count;
            _simFail += cm.failCount;

            // 여러 판 돌릴 땐 하루하루 찍으면 콘솔이 수백 줄이라 통계만 남김
            if (_batchMode == false)
            {
                Debug.Log("[자동] " + gm.calendar.day + "일차"
                    + " | 크레딧 " + gm.economy.credits
                    + " | 의심도 " + Mathf.RoundToInt(gm.suspicion.current)
                    + " | 상점레벨합 " + ShopLevelSum(gm)
                    + " | 성공 " + cm.successCount + "/" + cm.todayComplaints.Count);
            }
            gm.ConfirmDayResult();
        }

        // 상점이 열려 있으면 밤도 자동으로 (알바 + 우주선 구매)
        if (gm.phase == GamePhase.NightShop)
        {
            AutoPlayNight(gm);
            gm.SleepAndAdvance();
        }
    }

    // 자동 플레이 시작 (횟수 정하고 결과 카운터 초기화)
    // summary 는 끝까지 돌려서 결과 요약을 찍을 실행인지
    void StartSim(int days, bool summary)
    {
        GameManager gm = GameManager.instance;

        // 이미 끝난 화면에서 누르면 지난 판 숫자로 엉뚱한 요약이 찍혀서 막음
        if (gm.phase != GamePhase.DayWork && gm.phase != GamePhase.DayResult
            && gm.phase != GamePhase.NightShop)
        {
            Debug.LogWarning("[자동] 게임이 도는 중에만 돌릴 수 있음 (지금: " + PhaseName(gm.phase) + ")");
            return;
        }

        _autoDays = days;
        _simRunning = summary;
        _simSpaceshipDay = 0;
        _simArbeitDone = 0;
        _simDays = 0;
        _simComplaints = 0;
        _simFail = 0;
    }

    // 여러 판을 연달아 돌려서 분포를 봄 (한 판만 보면 운 좋은/나쁜 한 점을 결론으로 읽게 됨)
    void RunBatch(int runs)
    {
        GameManager gm = GameManager.instance;
        int dayCap = 200; // 한 판이 이 날짜를 넘으면 못 끝낸 걸로 침

        List<int> endDays = new List<int>();   // 끝난 날들
        List<int> shipDays = new List<int>();  // 우주선 산 날들
        int endingCount = 0;                   // 엔딩까지 간 판
        int deadCount = 0;                     // 게임오버 난 판
        int stuckCount = 0;                    // 날짜 안에 못 끝낸 판

        // 시뮬 도는 동안엔 진짜 저장이랑 업적 기록을 건드리면 안 됨
        gm.save.skipSave = true;
        if (gm.achievements != null)
        {
            gm.achievements.skipRecord = true;
        }
        _batchMode = true;
        _autoDays = 0;
        _simRunning = false;

        // 도중에 뭐가 터져도 저장이 꺼진 채로 남으면 안 되니까 finally 로 되돌림
        try
        {
            for (int r = 0; r < runs; r++)
            {
                gm.RestartForSim();
                _simSpaceshipDay = 0;

                int guard = 0;
                while (guard < dayCap
                    && gm.phase != GamePhase.Ending
                    && gm.phase != GamePhase.GameOver)
                {
                    guard++;
                    AutoPlayOneDay(gm);
                }

                if (gm.phase == GamePhase.Ending)
                {
                    endingCount++;
                    endDays.Add(gm.calendar.day);
                }
                else if (gm.phase == GamePhase.GameOver)
                {
                    deadCount++;
                    endDays.Add(gm.calendar.day);
                }
                else
                {
                    stuckCount++;
                }

                if (_simSpaceshipDay > 0)
                {
                    shipDays.Add(_simSpaceshipDay);
                }
            }
        }
        finally
        {
            gm.save.skipSave = false;
            if (gm.achievements != null)
            {
                gm.achievements.skipRecord = false;
            }
            _batchMode = false;

            // 마지막 판의 끝 상태가 그대로 남으면 그 뒤에 한 번만 자도 진짜 세이브에 덮어써짐
            // 타이틀로 돌려놔서 새 게임으로만 이어가게 함
            gm.ChangePhase(GamePhase.Intro);
        }

        Debug.Log("===== " + runs + "판 통계 =====");
        Debug.Log("  엔딩 " + endingCount + "판 / 게임오버 " + deadCount + "판 / "
            + dayCap + "일 안에 못 끝냄 " + stuckCount + "판");
        Debug.Log("  끝난 날 가운데값 " + Median(endDays) + "일차"
            + " (제일 빠른 " + Min(endDays) + " ~ 제일 늦은 " + Max(endDays) + ")");
        Debug.Log("  우주선 산 날 가운데값 " + Median(shipDays) + "일차"
            + " (" + shipDays.Count + "판에서 구매)");
        Debug.Log("  설정: 카드 정답률 " + Mathf.RoundToInt(_autoRate * 100)
            + "% -> 민원 성공률 " + Mathf.RoundToInt(EffectiveSuccess(gm) * 100) + "%"
            + " / 알바 수행률 " + Mathf.RoundToInt(_autoArbeitRate * 100)
            + "% -> 보수 " + Mathf.RoundToInt(ArbeitTier(_autoArbeitRate) * 100) + "%");
        Debug.Log("  * 통계 돌리는 동안 진짜 저장은 안 건드렸음. 끝나고 타이틀로 돌려놨으니 새 게임으로 시작할 것");
    }

    // 가운데값 (없으면 0)
    int Median(List<int> values)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        List<int> sorted = new List<int>(values);
        sorted.Sort();
        return sorted[sorted.Count / 2];
    }

    // 제일 작은 값 (없으면 0)
    int Min(List<int> values)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        int best = values[0];
        for (int i = 1; i < values.Count; i++)
        {
            if (values[i] < best)
            {
                best = values[i];
            }
        }
        return best;
    }

    // 제일 큰 값 (없으면 0)
    int Max(List<int> values)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        int best = values[0];
        for (int i = 1; i < values.Count; i++)
        {
            if (values[i] > best)
            {
                best = values[i];
            }
        }
        return best;
    }

    // 슬라이더를 5% 단위로 맞춤 (0.799 랑 0.800 이 결과를 3일씩 바꿔서 재현이 안 됨)
    float SnapRate(float value)
    {
        return Mathf.Round(value * 20f) / 20f;
    }

    // 알바 수행률이 실제로 받는 비율 (ArbeitManager 의 계단이랑 같아야 함)
    float ArbeitTier(float rate)
    {
        if (rate >= 0.8f)
        {
            return 1f;
        }
        if (rate >= 0.6f)
        {
            return 0.75f;
        }
        if (rate >= 0.4f)
        {
            return 0.5f;
        }

        return 0f;
    }

    // 민원 하나를 끝내 성공할 확률 (제출을 여러 번 할 수 있으니 카드 정답률보다 높음)
    float EffectiveSuccess(GameManager gm)
    {
        int tries = gm.config.maxWrongTries;
        if (tries < 1)
        {
            tries = 1;
        }

        return 1f - Mathf.Pow(1f - _autoRate, tries);
    }

    // 자동 플레이의 밤 - 알바 남은 만큼 하고, 살 수 있으면 우주선 산다
    void AutoPlayNight(GameManager gm)
    {
        int guard = 0;
        while (gm.arbeit.CanWork() == true && guard < 20)
        {
            guard++;
            MinigameDefinition pick = PickArbeitForSim(gm);
            if (pick == null)
            {
                break; // 알바 데이터가 아직 없음
            }

            gm.arbeit.StartArbeit(pick);
            gm.arbeit.FinishArbeit(_autoArbeitRate);
            _simArbeitDone++;
        }

        ItemDefinition ticket = gm.FindSpaceshipItem();
        if (ticket != null && gm.hasSpaceship == false && gm.shop.CanBuy(ticket) == true)
        {
            if (gm.TryBuySpaceship() == true)
            {
                _simSpaceshipDay = gm.calendar.day;
                Debug.Log("[자동] " + gm.calendar.day + "일차에 우주선 구매!");
            }
        }
    }

    // 시뮬레이션에서 할 알바 고르기 (경험치 제일 낮은 상점부터 - 레벨을 골고루 올려야 우주선이 풀림)
    MinigameDefinition PickArbeitForSim(GameManager gm)
    {
        List<MinigameDefinition> all = gm.content.minigames;
        MinigameDefinition best = null;
        int bestExp = 0;

        for (int i = 0; i < all.Count; i++)
        {
            MinigameDefinition m = all[i];
            if (m == null)
            {
                continue;
            }

            int exp = gm.shop.shopExp[(int)m.shopType];
            if (best == null || exp < bestExp)
            {
                best = m;
                bestExp = exp;
            }
        }

        return best;
    }

    // 다 돌렸는데 엔딩에 못 닿았을 때 - 어디서 막혔는지 짚어줌 (조용히 끝나면 숫자를 오해함)
    void LogSimStuck(GameManager gm)
    {
        Debug.LogWarning("===== 엔딩에 못 닿고 멈춤 (" + gm.calendar.day + "일차까지 돌림) =====");

        if (gm.content.minigames.Count == 0)
        {
            Debug.LogWarning("  알바 데이터가 없어서 상점 경험치를 못 올림");
        }
        if (gm.FindSpaceshipItem() == null)
        {
            Debug.LogWarning("  우주선표 아이템이 데이터에 없음");
        }
        if (gm.hasSpaceship == false && ShopLevelSum(gm) < gm.config.spaceshipShopLevel)
        {
            Debug.LogWarning("  상점레벨 합 " + ShopLevelSum(gm) + " < 조건 " + gm.config.spaceshipShopLevel
                + "  (알바 수행률이 40% 밑이면 경험치가 0이라 영원히 안 오름)");
        }
        if (gm.hasSpaceship == false)
        {
            Debug.LogWarning("  아직 우주선을 못 샀음 (크레딧 " + gm.economy.credits + ")");
        }

        LogSimSummary(gm); // 거기까지의 숫자도 같이 남김
    }

    // 자동 플레이가 끝났을 때 결과 요약 (밸런스 회의에 쓸 숫자)
    void LogSimSummary(GameManager gm)
    {
        _simRunning = false;

        string result = "아직 진행 중 (엔딩 못 봄)";
        if (gm.phase == GamePhase.Ending)
        {
            result = "엔딩 (" + gm.lastEnding + ")";
        }
        else if (gm.phase == GamePhase.GameOver)
        {
            result = "게임 오버 (" + gm.lastGameOverReason + ")";
        }

        string ship = "끝까지 못 삼";
        if (_simSpaceshipDay > 0)
        {
            ship = _simSpaceshipDay + "일차";
        }

        // 하루에 몇 건 나왔는지 (민원 데이터가 적으면 할당량보다 적게 나와서 숫자가 확 달라짐)
        string perDay = "0";
        if (_simDays > 0)
        {
            perDay = ((float)_simComplaints / _simDays).ToString("0.0");
        }

        Debug.Log("===== 자동 플레이 결과 =====");
        Debug.Log("  " + gm.calendar.day + "일차에 끝남 -> " + result);
        Debug.Log("  우주선 구매: " + ship);
        Debug.Log("  알바 " + _simArbeitDone + "회, 상점레벨 합 " + ShopLevelSum(gm)
            + " (우주선 조건 " + gm.config.spaceshipShopLevel + ")");
        Debug.Log("  남은 크레딧 " + gm.economy.credits
            + ", 의심도 " + Mathf.RoundToInt(gm.suspicion.current));
        Debug.Log("  민원 하루 평균 " + perDay + "건 (할당량 설정은 " + gm.config.complaintsPerDay
            + "건), 놓친 민원 " + _simFail + "건");
        Debug.Log("  설정: 카드 정답률 " + Mathf.RoundToInt(_autoRate * 100)
            + "% -> 민원 성공률 " + Mathf.RoundToInt(EffectiveSuccess(gm) * 100) + "%"
            + " / 알바 수행률 " + Mathf.RoundToInt(_autoArbeitRate * 100)
            + "% -> 보수 " + Mathf.RoundToInt(ArbeitTier(_autoArbeitRate) * 100) + "%");
        Debug.Log("  * 이 시뮬레이션이 안 하는 것: 처리(3택) 오답, 아이템 구매, 버프·밀수템."
            + " 실제 플레이보다 의심도가 낮고 수입이 후하게 나옴");
    }
}
