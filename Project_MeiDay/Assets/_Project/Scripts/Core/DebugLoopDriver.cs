using UnityEngine;

// 임시 디버그용 - 진짜 UI 만들기 전에 하루 루프 도는지 버튼으로 확인
// TODO: 진짜 UI 생기면 이거 지우기
public class DebugLoopDriver : MonoBehaviour
{
    int _selectedCard = 0;  // 지금 고른 카드 번호
    float _autoRate = 0.8f; // 자동 플레이 정답률
    int _autoDays = 0;      // 남은 자동 진행 일수

    int _fontSize = 18;             // 글자 크기 (화면에 따라 +/- 로 조절)
    Vector2 _scroll = Vector2.zero; // 내용이 길어지면 스크롤

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

        // 자동 진행 중이면 한 프레임에 하루씩 소화 (끝나거나 게임오버면 멈춤)
        if (gm.phase == GamePhase.Ending || gm.phase == GamePhase.GameOver)
        {
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
            int amount = gm.config.shopExpPerLevel * 2;
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
        GUILayout.BeginHorizontal();
        GUILayout.Label("정답률 " + Mathf.RoundToInt(_autoRate * 100) + "%", GUILayout.Width(140));
        _autoRate = GUILayout.HorizontalSlider(_autoRate, 0f, 1f, GUILayout.Height(24));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("1일"))
        {
            _autoDays = 1;
        }
        if (GUILayout.Button("7일"))
        {
            _autoDays = 7;
        }
        if (GUILayout.Button("30일"))
        {
            _autoDays = 30;
        }
        GUILayout.EndHorizontal();

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

        GUILayout.EndScrollView();
        GUILayout.EndArea();
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

        string bookLabel = "카트에서 나온 책: " + (game.currentGenre + 1) + "번 장르";
        if (game.currentGolden == true)
        {
            bookLabel += " (황금 책!)";
        }
        GUILayout.Label(bookLabel);
        GUILayout.BeginHorizontal();
        for (int i = 0; i < game.genreCount; i++)
        {
            if (GUILayout.Button((i + 1) + "번 칸"))
            {
                game.PlaceBook(i);
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
        GUILayout.Label("P = 손님, G = 황금 손님, = 구간일 때 던질 것");

        if (GUILayout.Button("홍보물 던지기!"))
        {
            game.Throw();
        }

        // OnGUI 는 한 프레임에 여러 번 불려서 그리기 단계에서만 진행시킴
        if (Event.current.type == EventType.Repaint)
        {
            game.Tick(Time.deltaTime);
        }
    }

    // 손님 위치를 글자 줄로 그려줌 (= 구간이 던질 타이밍)
    string StreetBar(PromoGame game)
    {
        char[] bar = new char[25];
        for (int i = 0; i < bar.Length; i++)
        {
            float t = (float)i / (bar.Length - 1);
            if (t >= 0.35f && t <= 0.65f)
            {
                bar[i] = '=';
            }
            else
            {
                bar[i] = '-';
            }
        }

        for (int i = 0; i < game.peoplePos.Count; i++)
        {
            int p = Mathf.RoundToInt(game.peoplePos[i] * (bar.Length - 1));
            if (game.peopleGolden[i] == true)
            {
                bar[p] = 'G';
            }
            else
            {
                bar[p] = 'P';
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

            if (gm.shop.CanBuy(item) == false)
            {
                GUILayout.Label(label + " (품절)");
                continue;
            }

            if (GUILayout.Button(label + " - " + item.price + "cr"))
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
        if (GUILayout.Button("확인 (해석 제출)"))
        {
            cm.ConfirmTranslation();
        }
    }

    // 처리 단계 디버그 UI
    void DrawResolve(ComplaintManager cm, ComplaintDefinition c)
    {
        GUILayout.Label("[처리 단계]");
        GUILayout.Label("틀린 횟수: " + cm.resolutionWrongCount + " / " + GameManager.instance.config.maxWrongTries);
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
                // 실수: 빈 답안 제출해서 해석 실수 하나 쌓음 (쌓이면 민원인이 알아서 나감)
                cm.ConfirmTranslation();
            }

            if (gm.phase == GamePhase.GameOver)
            {
                return;
            }
        }

        if (gm.phase == GamePhase.DayResult)
        {
            Debug.Log("[자동] " + gm.calendar.day + "일차: 크레딧 " + gm.economy.credits
                + ", 의심도 " + gm.suspicion.current
                + ", 성공 " + cm.successCount + " / " + cm.todayComplaints.Count);
            gm.ConfirmDayResult();
        }

        if (gm.phase == GamePhase.NightShop)
        {
            gm.SleepAndAdvance();
        }
    }
}
