using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 낮 화면 - 외계어 해석(카드 맞추기)이랑 처리 고르기를 그려줌
public class TranslationView : MonoBehaviour
{
    [SerializeField] TMP_Text complaintLineText; // 민원인 대사
    [SerializeField] TMP_Text quotaText;         // 오늘 몇 번째 민원인지 (n/n)
    [SerializeField] TMP_Text wrongText;         // 틀린 횟수
    [SerializeField] Transform slotParent;       // 상자들 놓이는 자리
    [SerializeField] Transform cardParent;       // 카드들 놓이는 자리
    [SerializeField] WordSlotUI slotPrefab;      // 상자 프리팹
    [SerializeField] WordCardUI cardPrefab;      // 카드 프리팹
    [SerializeField] GameObject translatePart;   // 해석 단계에만 보이는 묶음 (확인 버튼 등)
    [SerializeField] GameObject resolvePart;     // 처리 단계에만 보이는 묶음 (처리 버튼 3개)

    // 왼쪽 창에 뜨는 민원인 그림
    // 네임드는 전용 그림이 있고, 없는 민원인은 파츠로 조합해서 만듦
    [SerializeField] UnityEngine.UI.Image complainantImage;
    [SerializeField] NpcPortrait complainantParts;

    [Header("처리 단계에만 열리는 것들")]
    // 카드를 다 꽂고 해석이 맞아야 여기 버튼들이 나옴
    // 잠글 때는 아예 숨김 - 눌리지도 않는데 보이면 고장난 걸로 오해함
    [SerializeField] GameObject pdaButton;     // PDA 여는 버튼
    [SerializeField] GameObject pdaPanel;      // PDA 창 (다음 민원으로 넘어가면 닫아줌)
    [SerializeField] GameObject declareButton; // 신고 여는 버튼

    [Header("서류 발급 화면 (PDA 안에서 열림)")]
    [SerializeField] GameObject printPart;   // 서류 발급 화면 묶음
    [SerializeField] GameObject pdaTextPart; // 민원 내용 스크롤 (발급 화면 열면 가림)
    [SerializeField] GameObject declarePanel; // 수배자 신고 창 (발급 화면의 신고 버튼이 엶)

    // PDA 를 열면 대사판이 가려져서, 무슨 민원이었는지 여기 다시 적어줌
    [SerializeField] TMP_Text pdaComplaintText;

    [Header("대사 나오는 속도")]
    [SerializeField] float charDelay = 0.045f; // 글자 하나 나오는 간격

    [Header("빈칸")]
    // 빈칸 하나가 몇 글자만큼 자리를 차지할지 (칸마다 똑같음)
    // 정답 길이에 맞추면 길이가 힌트가 되니까 일부러 고정
    [SerializeField] int blankSpaces = 8;

    // 위아래 줄 빈칸 사이에 남기는 틈
    // 줄 높이를 꽉 채우면 윗줄 빈칸 바닥이랑 아랫줄 빈칸 천장이 딱 맞닿아서 겹쳐 보임
    [SerializeField] float blankLineGap = 11f;

    // 빈칸 좌우에 남기는 틈 - 안 두면 빈칸 테두리가 바로 옆 글자를 물어버림
    [SerializeField] float blankSideGap = 5f;

    [Header("카드 배치 (손패처럼 겹쳐서)")]
    [SerializeField] float cardTilt = -6f;          // 카드 기울기 (도)
    [SerializeField] float cardStepMaxRate = 0.86f; // 카드 간격 최대 (카드 폭 대비, 1이면 안 겹침)
    [SerializeField] float cardStepMinRate = 0.62f; // 최소 간격 - 이보다 좁히면 글자가 가려짐
    [SerializeField] float cardDropY = -2f;         // 오른쪽으로 갈수록 조금씩 내려가는 정도

    // 빈칸 자리를 글 속에서 찾을 때 쓰는 표시
    // 화면엔 안 보임 (투명하게 넣어둠). 보이는 건 이 자리에 얹는 빈칸 오브젝트
    // 종류별 색은 빈칸 오브젝트(WordSlotUI)가 들고 있어서 여기선 색을 안 씀
    const string BlankOpen = "[";
    const string BlankClose = "]";

    // 줄바꿈이 안 되는 공백 (U+00A0)
    // 보통 공백을 쓰면 줄 끝에서 빈칸 한가운데가 잘려 두 줄에 걸쳐버림
    const char NoBreakSpace = '\u00A0';

    List<WordSlotUI> _slotUis = new List<WordSlotUI>();
    List<WordCardUI> _cardUis = new List<WordCardUI>();

    // 빈칸마다 대사에서 차지하는 글자 범위 (_slotFrom ~ _slotCloseAt)
    // 타이핑 도중에 글이 위아래로 움직여서 매번 다시 재야 함
    List<int> _slotFrom = new List<int>();
    List<int> _slotCloseAt = new List<int>(); // 타이핑이 여기를 지나면 그 빈칸이 나타남

    Coroutine _typeRoutine; // 대사 한 글자씩 찍는 연출
    int _typedFor = -1;     // 몇 번째 민원 대사를 이미 찍었는지
    bool _printOpen = false; // 서류 발급 화면이 열려있는지 (열려있으면 아래 버튼줄을 안 되돌림)
                            // 카드를 꽂을 때마다 화면을 다시 그리는데, 이게 없으면 그때마다 대사가 처음부터 다시 나옴

    void OnEnable()
    {
        if (GameManager.instance == null)
        {
            return; // 아직 게임 준비 전이면 패스 (다음에 켜질 때 됨)
        }

        GameManager.instance.complaint.OnComplaintUpdated += Rebuild;
        Rebuild();
    }

    void OnDisable()
    {
        // 꺼지면 코루틴이 유니티한테 강제로 멈춰지는데 손잡이는 남아있음
        // 그대로 두면 다시 켰을 때 "찍는 중"으로 오해해서 대사가 영영 안 끝남
        StopTyping();

        if (GameManager.instance != null)
        {
            GameManager.instance.complaint.OnComplaintUpdated -= Rebuild;
        }
    }

    // 지금 민원 상태대로 화면 다시 그리기
    void Rebuild()
    {
        ComplaintManager cm = GameManager.instance.complaint;

        // 지금 받는 민원이 없거나(다 끝났거나 아침 대사 중) 손님이 아직 안 왔으면 창구를 비워둠
        bool empty = (cm.currentIndex >= cm.todayComplaints.Count
            || cm.entry == ComplaintEntry.Waiting);

        if (empty == true)
        {
            StopTyping();
            _typedFor = -1;
            ShowWorkUi(false);
            ShowComplainantSeat(false);
            LockResolveUi();
            return;
        }

        ComplaintDefinition c = cm.todayComplaints[cm.currentIndex];

        // 손님은 앉았음 - 얼굴부터 보여줌
        // 자리를 먼저 켜야 함. ShowComplainant 가 전용 그림/파츠 중 안 쓰는 쪽을 다시 꺼줌
        ShowComplainantSeat(true);
        ShowComplainant(c);
        SetObject(quotaText, true);
        quotaText.text = (cm.currentIndex + 1) + " / " + cm.todayComplaints.Count;

        // 앉기만 하고 아직 말은 안 하는 동안 (대사창은 비워둠)
        if (cm.entry == ComplaintEntry.Arrived)
        {
            StopTyping();
            _typedFor = -1;
            SetObject(complaintLineText, true);
            complaintLineText.text = "";
            SetObject(wrongText, false);
            SetObject(slotParent, false);
            SetObject(cardParent, false);
            SetPart(translatePart, false);
            LockResolveUi();
            return;
        }

        // 여기부터는 말하는 중이거나 말이 끝난 상태
        SetObject(complaintLineText, true);
        complaintLineText.text = BuildComplaintLine(c, cm.translation);
        RefreshPdaText(c, cm.translation);

        if (cm.stage == ComplaintStage.Translate)
        {
            wrongText.text = "해석 실수: " + cm.translation.wrongCount + " / " + GameManager.instance.config.maxWrongTries;
        }
        else
        {
            wrongText.text = "처리 실수: " + cm.resolutionWrongCount + " / " + GameManager.instance.config.maxResolutionWrongTries;
        }

        // 말하는 중이면 한 글자씩 찍고, 그동안은 카드도 처리도 못 만짐
        if (cm.entry == ComplaintEntry.Speaking)
        {
            SetObject(wrongText, false);
            SetObject(slotParent, false);
            SetObject(cardParent, false);
            SetPart(translatePart, false);
            LockResolveUi();

            // 빈칸은 자리만 미리 잡아두고 전부 숨김 - 타이핑이 지나가면서 하나씩 나타남
            RebuildSlots(cm.translation);
            RefreshSlotVisibility(0);

            StartTyping(cm.currentIndex);
            return;
        }

        // 말 끝남 - 대사는 전부 보이게
        StopTyping();
        complaintLineText.maxVisibleCharacters = int.MaxValue;
        SetObject(wrongText, true);

        bool translating = (cm.stage == ComplaintStage.Translate);

        // 손패는 해석 단계에만
        // 빈칸은 처리 단계에도 남겨둠 - 무슨 민원이었는지 보면서 처리를 골라야 하니까
        SetObject(slotParent, false); // 빈칸이 대사 안으로 들어가서 이 자리는 이제 안 씀
        SetObject(cardParent, translating);

        // 확인 버튼은 빈칸을 다 채워야 나옴
        SetPart(translatePart, translating == true && cm.translation.IsAllFilled() == true);

        // 처리 단계가 되어야 PDA 와 신고가 열림
        bool resolving = (cm.stage == ComplaintStage.Resolve);
        SetPart(pdaButton, resolving);
        SetPart(declareButton, resolving);
        SetPart(resolvePart, resolving == true && _printOpen == false);
        if (resolving == false)
        {
            SetPart(pdaPanel, false); // 열어둔 채로 다음 민원으로 넘어가는 것 방지
            ClosePrintPart();
        }

        RebuildSlots(cm.translation);
        RefreshSlotVisibility(int.MaxValue); // 말 다 했으니 빈칸도 전부 보임
        RebuildCards(cm.translation);
    }

    // 처리 쪽(PDA / 신고 / 처리 버튼)을 전부 닫아둠
    void LockResolveUi()
    {
        ClosePrintPart();
        SetPart(resolvePart, false);
        SetPart(pdaButton, false);
        SetPart(pdaPanel, false);
        SetPart(declareButton, false);
    }

    // 대사 한 글자씩 찍기 시작 (이미 찍은 민원이면 아무것도 안 함)
    void StartTyping(int complaintIndex)
    {
        // 이미 찍는 중이면 그대로 둠 (카드 꽂을 때마다 처음부터 다시 나오면 안 됨)
        // 연출이 중간에 끊겼으면(_typeRoutine 이 비어있음) 다시 돌려줌 - 안 그러면 말이 안 끝나서 카드를 영영 못 꽂음
        if (_typedFor == complaintIndex && _typeRoutine != null)
        {
            return;
        }

        _typedFor = complaintIndex;

        if (_typeRoutine != null)
        {
            StopCoroutine(_typeRoutine);
        }
        _typeRoutine = StartCoroutine(TypeLine());
    }

    // 돌던 타이핑 연출 멈추기
    void StopTyping()
    {
        if (_typeRoutine != null)
        {
            StopCoroutine(_typeRoutine);
            _typeRoutine = null;
        }
    }

    // 대사가 한 글자씩 나오는 연출
    // 글자를 잘라 붙이면 <color> 같은 태그가 중간에 끊겨서 깨짐
    // 그래서 글은 통째로 넣어두고 몇 글자까지 보여줄지만 늘림
    IEnumerator TypeLine()
    {
        complaintLineText.maxVisibleCharacters = 0;
        complaintLineText.ForceMeshUpdate(); // 글자 수를 세려면 한 번 그려봐야 함

        int total = complaintLineText.textInfo.characterCount;

        for (int i = 1; i <= total; i++)
        {
            complaintLineText.maxVisibleCharacters = i;

            // 글자가 늘면 문단이 움직이니까 자리를 다시 재고
            complaintLineText.ForceMeshUpdate();
            RepositionSlots();

            // 이 자리를 지났으면 빈칸이 툭 나타남 (그리고 뒤 글자가 이어서 나옴)
            RefreshSlotVisibility(i);

            // 타이핑 소리 (시끄러우면 몇 글자마다 한 번으로 조절)
            if (SoundManager.instance != null)
            {
                SoundManager.instance.PlaySfx(SoundManager.instance.typeTick);
            }

            yield return new WaitForSeconds(charDelay);
        }

        complaintLineText.maxVisibleCharacters = int.MaxValue;
        complaintLineText.ForceMeshUpdate();
        RepositionSlots(); // 다 나온 자리로 마지막에 한 번 더 맞춰줌
        RefreshSlotVisibility(int.MaxValue);
        _typeRoutine = null;

        // 말 다 했다고 알려줌 - 여기서부터 카드를 꽂을 수 있음
        GameManager.instance.complaint.OnLineFinished();
    }

    // 왼쪽 창에 지금 민원인 보여주기
    // 전용 그림이 있으면 그걸 쓰고, 없으면 파츠로 조합해서 만듦
    // 민원인 자리 통째로 비우기 / 되살리기
    // 전용 그림이랑 파츠 조합 둘 중 뭘 쓰는지는 ShowComplainant 가 정하니까
    // 여기서는 자리만 켜고 끔 (안 그러면 손님이 오기 전인데 얼굴만 남아있음)
    void ShowComplainantSeat(bool on)
    {
        SetObject(complainantImage, on);
        SetObject(complainantParts, on);
    }

    void ShowComplainant(ComplaintDefinition c)
    {
        Sprite portrait = null;
        string seed = c.id;
        if (c.complainant != null)
        {
            portrait = c.complainant.portrait;
            seed = c.complainant.id;
        }

        // 전용 그림이 있는 경우
        if (portrait != null)
        {
            if (complainantParts != null)
            {
                complainantParts.gameObject.SetActive(false);
            }
            if (complainantImage != null)
            {
                complainantImage.enabled = true;
                complainantImage.sprite = portrait;
                complainantImage.color = Color.white; // 자리만 잡아둔 검은색이 남아있으면 그림이 안 보임
                complainantImage.preserveAspect = true;
            }
            return;
        }

        // 전용 그림이 없으면 파츠 조합 (같은 민원인이면 항상 같은 얼굴)
        if (complainantImage != null)
        {
            complainantImage.enabled = false;
        }
        if (complainantParts != null)
        {
            complainantParts.gameObject.SetActive(true);
            complainantParts.Build(seed);
        }
    }

    // 민원인이 하는 말 만들기
    // 데이터엔 "저, 그, [단어1], [단어2] 걸로 [단어3] 주세요." 처럼 자리만 비워놨음
    //
    // 빈칸은 글자가 아니라 오브젝트임 (카드를 거기다 대고 꽂아야 하니까)
    // 그래서 글에는 투명한 대괄호로 자리만 잡아두고, 그 위에 빈칸 오브젝트를 얹음
    // 자리를 글로 잡아두는 이유는 줄바꿈 때문 - 빈칸도 글자처럼 줄을 넘어가야 함
    string BuildComplaintLine(ComplaintDefinition c, TranslationController tc)
    {
        string line = c.complaintLine;

        for (int i = 0; i < tc.slots.Count; i++)
        {
            line = line.Replace("[단어" + (i + 1) + "]", HiddenGap(i));
        }

        return line;
    }

    // PDA 안에 적어줄 민원 요약
    // 처리 단계엔 PDA 가 대사판을 덮어버려서, 여기서 다시 못 읽으면 뭘 골라야 할지 알 수가 없음
    void RefreshPdaText(ComplaintDefinition c, TranslationController tc)
    {
        if (pdaComplaintText == null)
        {
            return;
        }

        string who = "민원인 미상";
        if (c.complainant != null && string.IsNullOrEmpty(c.complainant.displayName) == false)
        {
            who = c.complainant.displayName;
        }

        // 꽂아둔 단어를 문장에 그대로 끼워넣음 (아직 빈 칸은 밑줄로)
        string line = c.complaintLine;
        for (int i = 0; i < tc.slots.Count; i++)
        {
            string word = "____";
            if (tc.slots[i].placedWord != null)
            {
                word = tc.slots[i].placedWord.meaning;
            }
            line = line.Replace("[단어" + (i + 1) + "]", "<b><color=#F2941F>" + word + "</color></b>");
        }

        // PDA 화면이 진남색이라 글자는 밝은 쪽
        pdaComplaintText.text = "<size=90%><color=#7FA3C6>민원인</color></size>  <b>" + who + "</b>\n"
            + "<color=#31578C>────────────────────</color>\n"
            + line;
    }

    // 빈칸 하나가 차지할 자리 (안 보이는 글자)
    //
    // link 로 몇 번째 칸인지 박아둠 - 자리 순서로 세면 안 됨
    // 대사에 같은 자리표시가 두 번 나오면(예: [단어1] 이 두 군데) 순서랑 칸 번호가 어긋남
    //
    // 속을 일반 공백이 아니라 줄바꿈 안 되는 공백으로 채움
    // 일반 공백이면 줄 끝에서 빈칸 한가운데가 잘려서 두 줄에 걸쳐버림
    string HiddenGap(int slotIndex)
    {
        return "<link=" + slotIndex + "><color=#00000000>"
            + BlankOpen + new string(NoBreakSpace, blankSpaces) + BlankClose
            + "</color></link>";
    }

    // 민원 작업 UI 전체 켜고 끄기
    // WorkUI 를 직접 끄면 여기 붙은 이 스크립트도 같이 꺼져서 다시 켤 사람이 없어짐
    // 그래서 컨테이너는 두고 안에 있는 것들만 끔
    void ShowWorkUi(bool on)
    {
        SetObject(complaintLineText, on);
        SetObject(quotaText, on);
        SetObject(wrongText, on);
        SetObject(slotParent, on);
        SetObject(cardParent, on);

        // 꺼질 때는 단계 묶음도 같이 (켤 때는 지금 단계에 맞는 쪽만 아래에서 켜줌)
        if (on == false)
        {
            SetPart(translatePart, false);
            SetPart(resolvePart, false);
        }
    }

    // 오브젝트 켜고 끄기 (아직 안 꽂아뒀어도 안 터지게)
    void SetObject(Component target, bool on)
    {
        if (target == null)
        {
            return;
        }

        target.gameObject.SetActive(on);
    }

    // 묶음 켜고 끄기 (아직 안 꽂아뒀어도 안 터지게)
    void SetPart(GameObject part, bool on)
    {
        if (part == null)
        {
            return;
        }

        part.SetActive(on);
    }

    // 만들어둔 빈칸 치우기
    void ClearSlotUis()
    {
        for (int i = 0; i < _slotUis.Count; i++)
        {
            if (_slotUis[i] == null)
            {
                continue;
            }

            // Destroy 는 이 프레임 끝에야 실제로 지워짐
            // 같은 프레임에 다시 그리면 헌 빈칸이 새 빈칸이랑 겹쳐 보이고 드롭도 가로챔
            // 그래서 먼저 꺼두고 지움
            _slotUis[i].gameObject.SetActive(false);
            Destroy(_slotUis[i].gameObject);
        }
        _slotUis.Clear();
        _slotFrom.Clear();
        _slotCloseAt.Clear();
    }

    // 대사 안에 빈칸 오브젝트를 박아 넣기
    // 글에 잡아둔 투명한 대괄호 자리를 찾아서, 그 자리에 딱 맞게 빈칸을 얹음
    // 빈칸이 대사의 자식이라 대사가 움직이면 같이 따라감
    void RebuildSlots(TranslationController tc)
    {
        ClearSlotUis();

        if (slotPrefab == null || complaintLineText == null)
        {
            Debug.LogWarning("해석 화면에 빈칸 프리팹이나 대사가 연결 안 됨");
            return;
        }

        // 글자 자리를 읽으려면 한 번 그려봐야 함
        complaintLineText.ForceMeshUpdate();
        TMP_TextInfo info = complaintLineText.textInfo;

        // 자리마다 link 로 칸 번호를 박아뒀으니 그걸 그대로 읽음
        // 자리 순서로 세지 않는 이유: 대사에 같은 자리표시가 두 번 나오면 번호가 어긋남
        for (int i = 0; i < info.linkCount; i++)
        {
            TMP_LinkInfo link = info.linkInfo[i];

            int slotIndex = -1;
            if (int.TryParse(link.GetLinkID(), out slotIndex) == false)
            {
                continue; // 우리가 박은 표시가 아님
            }

            if (slotIndex < 0 || slotIndex >= tc.slots.Count)
            {
                continue; // 대사에 적힌 칸 번호가 데이터랑 안 맞음
            }

            int from = link.linkTextfirstCharacterIndex;
            int to = from + link.linkTextLength - 1;
            if (from < 0 || to < from || to >= info.characterCount)
            {
                continue;
            }

            // 먼저 목록에 넣어둬야 Set 에서 뭐가 잘못돼도 고아 오브젝트가 안 남음
            WordSlotUI slotUi = Instantiate(slotPrefab, complaintLineText.rectTransform);
            _slotUis.Add(slotUi);
            _slotFrom.Add(from);
            _slotCloseAt.Add(to); // 타이핑이 여기를 지나야 이 빈칸이 나타남

            PlaceOverArea(slotUi, AreaOf(info, from, to));

            // 칸 안 단어는 대사랑 같은 크기까지 커질 수 있게 (더 크면 어색함)
            slotUi.SetMaxFontSize(complaintLineText.fontSize);

            // 카드를 꽂으면 카드가 이 안으로 들어오는 게 아니라
            // 카드에 쓰인 글자만 이 칸에 옮겨 적힘 (Set 이 알아서 함)
            slotUi.Set(this, slotIndex, tc.slots[slotIndex]);
        }
    }

    // 글자 몇 개가 차지하는 네모 (대사 안에서의 자리)
    Rect AreaOf(TMP_TextInfo info, int from, int to)
    {
        float left = float.MaxValue;
        float right = float.MinValue;

        for (int i = from; i <= to; i++)
        {
            TMP_CharacterInfo ci = info.characterInfo[i];

            // 공백은 안 그려져도 자리 정보는 있음 (빈칸 속이 전부 공백이라 이게 필요함)
            if (ci.bottomLeft.x < left) left = ci.bottomLeft.x;
            if (ci.topRight.x > right) right = ci.topRight.x;
        }

        // 세로는 글자 상자가 아니라 그 줄 전체 높이를 씀
        // 대괄호 상자에 맞추면 칸이 납작해서, 안에 넣는 단어가 잘게 줄어들어 안 읽힘
        int lineNo = info.characterInfo[from].lineNumber;
        float top = info.lineInfo[lineNo].ascender;
        float bottom = info.lineInfo[lineNo].descender;

        // 줄 높이를 꽉 채우면 위아래 줄 빈칸이 딱 맞닿아서 겹쳐 보임 - 위아래를 조금씩 깎음
        float half = blankLineGap * 0.5f;
        if (top - bottom > blankLineGap + 4f)
        {
            top -= half;
            bottom += half;
        }

        // 좌우도 깎아둠 - 안 그러면 빈칸 테두리가 바로 옆 글자에 딱 붙어서 글자를 물어버림
        float side = blankSideGap;
        if (right - left > blankSideGap * 2f + 10f)
        {
            left += side;
            right -= side;
        }

        return Rect.MinMaxRect(left, bottom, right, top);
    }

    // 빈칸을 그 자리에 딱 맞게 올려놓기
    void PlaceOverArea(WordSlotUI slotUi, Rect area)
    {
        RectTransform rt = slotUi.GetComponent<RectTransform>();
        if (rt == null)
        {
            return;
        }

        // 대사 한가운데를 기준으로 잡아야 글자 좌표를 그대로 쓸 수 있음
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.localScale = Vector3.one;
        rt.sizeDelta = new Vector2(area.width, area.height);
        rt.anchoredPosition = new Vector2(area.center.x, area.center.y);
    }

    // 빈칸들을 지금 글자 자리에 다시 맞춰줌
    //
    // 타이핑 중에는 이걸 매 글자마다 불러야 함
    // 대사가 세로 가운데 정렬이라, 둘째 줄에 글자가 처음 찍히는 순간
    // 문단 전체가 반 줄만큼 위로 올라감 - 처음 잰 자리에 그냥 두면 글자랑 어긋나 보임
    void RepositionSlots()
    {
        if (complaintLineText == null)
        {
            return;
        }

        TMP_TextInfo info = complaintLineText.textInfo;
        if (info == null)
        {
            return;
        }

        for (int i = 0; i < _slotUis.Count; i++)
        {
            if (_slotUis[i] == null)
            {
                continue;
            }

            int from = _slotFrom[i];
            int to = _slotCloseAt[i];
            if (from < 0 || to < from || to >= info.characterCount)
            {
                continue; // 글이 바뀌어서 범위가 안 맞음
            }

            PlaceOverArea(_slotUis[i], AreaOf(info, from, to));
        }
    }

    // 타이핑이 지나간 빈칸만 보여주기
    // 대사가 흐르다가 빈칸 자리에서 칸이 툭 나타나고, 그다음 글자가 이어짐
    void RefreshSlotVisibility(int visibleChars)
    {
        for (int i = 0; i < _slotUis.Count; i++)
        {
            if (_slotUis[i] == null)
            {
                continue;
            }

            bool passed = (visibleChars > _slotCloseAt[i]);
            _slotUis[i].gameObject.SetActive(passed);
        }
    }

    // 카드들 지우고 다시 만들기
    void RebuildCards(TranslationController tc)
    {
        for (int i = 0; i < _cardUis.Count; i++)
        {
            if (_cardUis[i] == null)
            {
                continue;
            }

            // Destroy 는 이 프레임 끝에야 실제로 지워짐
            // 카드를 꽂은 그 프레임에는 헌 카드가 남아서 손패가 두 배로 보임
            _cardUis[i].gameObject.SetActive(false);
            Destroy(_cardUis[i].gameObject);
        }
        _cardUis.Clear();

        if (cardPrefab == null || cardParent == null)
        {
            Debug.LogWarning("해석 화면에 카드 프리팹이나 놓을 자리가 연결 안 됨");
            return;
        }

        for (int i = 0; i < tc.cards.Count; i++)
        {
            if (tc.cards[i].isRemoved == true)
            {
                continue; // 떨어져 사라진 카드는 안 보여줌
            }
            if (tc.cards[i].isUsed == true)
            {
                continue; // 빈칸에 꽂아둔 카드는 손패에서 빠짐 (그 칸 안에 그려짐)
            }

            WordCardUI cardUi = Instantiate(cardPrefab, cardParent);
            _cardUis.Add(cardUi);
            cardUi.Set(i, tc.cards[i]);
        }

        LayoutCards();
    }

    // 카드를 손패처럼 한 줄로 비스듬히 겹쳐 놓기
    // 격자로 반듯하게 깔면 서류 뭉치 느낌이 안 남
    void LayoutCards()
    {
        RectTransform parentRt = cardParent as RectTransform;
        if (parentRt == null || _cardUis.Count == 0)
        {
            return;
        }

        RectTransform firstRt = _cardUis[0].GetComponent<RectTransform>();
        if (firstRt == null)
        {
            return;
        }

        float cardWidth = firstRt.sizeDelta.x;
        int count = _cardUis.Count;

        float available = parentRt.rect.width;

        // 자리에 맞춰 간격을 좁히되, 너무 좁히면 글자가 가려져서 하한을 둠
        float step = cardWidth * cardStepMaxRate;
        if (count > 1)
        {
            float fit = (available - cardWidth) / (count - 1);
            if (fit < step)
            {
                step = fit;
            }
        }

        // 하한까지 좁혀도 안 들어가면 카드를 통째로 줄임 (화면 밖으로 나가는 것보다 나음)
        float minStep = cardWidth * cardStepMinRate;
        float scale = 1f;
        if (step < minStep)
        {
            step = minStep;
            float total = minStep * (count - 1) + cardWidth;
            if (total > available && total > 0f)
            {
                scale = available / total;
            }
        }

        float startX = -step * scale * (count - 1) * 0.5f;

        // 처짐도 가운데를 기준으로 나눔
        // 첫 장을 0 으로 두면 카드가 많을수록 오른쪽 끝이 자꾸 내려가서 화면 밖으로 나감
        float middle = (count - 1) * 0.5f;

        for (int i = 0; i < count; i++)
        {
            RectTransform rt = _cardUis[i].GetComponent<RectTransform>();
            if (rt == null)
            {
                continue;
            }

            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localScale = new Vector3(scale, scale, 1f);
            rt.anchoredPosition = new Vector2(startX + step * scale * i, cardDropY * scale * (i - middle));
            rt.localRotation = Quaternion.Euler(0f, 0f, cardTilt);

            // 뒤 카드가 이 카드의 오른쪽을 덮으니 글자는 안 덮이는 폭 안에만
            // 맨 끝 카드는 덮는 게 없어서 카드 폭을 다 씀
            float visible = step;
            if (i == count - 1)
            {
                visible = cardWidth;
            }
            _cardUis[i].FitTextWidth(visible);
        }
    }

    // 카드가 상자에 떨어졌을 때 (상자나 대사 빈칸이 불러줌)
    public void OnCardDropped(int cardIndex, int slotIndex)
    {
        // 손님이 아직 말하는 중이거나 처리 단계면 카드를 못 만짐
        if (GameManager.instance.complaint.CanUseCards() == false)
        {
            return;
        }

        GameManager.instance.complaint.translation.PlaceCard(cardIndex, slotIndex);
        Rebuild();
    }

    // 상자 눌러서 비우기 (상자가 불러줌)
    public void OnSlotClicked(int slotIndex)
    {
        if (GameManager.instance.complaint.CanUseCards() == false)
        {
            return;
        }

        GameManager.instance.complaint.translation.ClearSlot(slotIndex);
        Rebuild();
    }

    // ---- 아래는 버튼 OnClick 에 연결해서 씀 ----

    // 확인 (해석 제출)
    public void OnClickConfirm()
    {
        GameManager.instance.complaint.ConfirmTranslation();
    }

    // 처리: 관련 부서로 연결
    public void OnClickTransfer()
    {
        GameManager.instance.complaint.SelectResolution(ComplaintResolution.DepartmentTransfer);
    }

    // 처리: 경호원 부르기
    public void OnClickSecurity()
    {
        GameManager.instance.complaint.SelectResolution(ComplaintResolution.CallSecurity);
    }

    // 처리: 서류 발급 - 바로 끝내지 않고 PDA 안에 발급 화면을 엶
    public void OnClickPrint()
    {
        ShowPrintPart(true);
    }

    // 발급 화면의 '문서 접수' - 여기서 민원이 끝남
    public void OnClickPrintAccept()
    {
        ShowPrintPart(false);
        GameManager.instance.complaint.SelectResolution(ComplaintResolution.DocumentPrint);
    }

    // 발급 화면의 '신고' - 수배자 신고 창을 엶 (발급 화면은 뒤에 그대로 둠)
    public void OnClickPrintDeclare()
    {
        if (declarePanel != null)
        {
            declarePanel.SetActive(true);
        }
    }

    // 발급 화면 열고 닫기 (열면 민원 내용이랑 아래 버튼줄은 가림)
    void ShowPrintPart(bool show)
    {
        _printOpen = show;
        SetPart(printPart, show);
        SetPart(pdaTextPart, show == false);
        SetPart(resolvePart, show == false);
    }

    // 민원이 끝나거나 넘어갈 때 - 발급 화면만 닫고 나머지는 Refresh 가 알아서 함
    void ClosePrintPart()
    {
        _printOpen = false;
        SetPart(printPart, false);
        SetPart(pdaTextPart, true);
    }
}
