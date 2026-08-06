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

    // 민원 대사에 끼워넣는 외계어 색 (16진수 고정)
    // 상자(WordSlotUI)랑 완전히 같은 색을 씀 - 같은 색끼리 짝지어 보면 됨
    // 문서 노랑 / 대상 초록 / 수량 파랑 / 부서 빨강
    // 인스펙터에 안 빼둔 이유: 씬에 저장된 옛 값이 코드 기본값을 덮어써서 안 바뀜
    const string DocumentHex = "FFE666";
    const string TargetHex = "80E680";
    const string QuantityHex = "80B3FF";
    const string DepartmentHex = "FF8080";

    List<WordSlotUI> _slotUis = new List<WordSlotUI>();
    List<WordCardUI> _cardUis = new List<WordCardUI>();

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
        if (GameManager.instance != null)
        {
            GameManager.instance.complaint.OnComplaintUpdated -= Rebuild;
        }
    }

    // 지금 민원 상태대로 화면 다시 그리기
    void Rebuild()
    {
        ComplaintManager cm = GameManager.instance.complaint;

        // 지금 받는 민원이 없으면 (다 끝났거나 아직 아침 대사 중) 해석/처리 묶음을 둘 다 숨김
        // 이걸 안 하면 어제 켜둔 처리 버튼이 다음 날 아침 대사창 위에 그대로 남아있음
        //
        // 작업 UI(민원 대사, 진행도, 상자, 카드)까지 같이 숨기는 것도 해봤는데,
        // 아침 대사를 못 넘기는 상황이 겹치면 화면이 통째로 비어버려서 일단 보류함
        // 대사창을 확실히 넘길 수 있게 되면 ShowWorkUi(false) 로 바꾸면 됨
        if (cm.currentIndex >= cm.todayComplaints.Count)
        {
            SetPart(translatePart, false);
            SetPart(resolvePart, false);
            return;
        }

        ShowWorkUi(true); // 손님이 왔으니 작업 UI 켬 (숨겨진 상태였을 수도 있어서)

        ComplaintDefinition c = cm.todayComplaints[cm.currentIndex];
        complaintLineText.text = BuildComplaintLine(c, cm.translation);
        ShowComplainant(c);
        quotaText.text = (cm.currentIndex + 1) + " / " + cm.todayComplaints.Count;
        if (cm.stage == ComplaintStage.Translate)
        {
            wrongText.text = "해석 실수: " + cm.translation.wrongCount + " / " + GameManager.instance.config.maxWrongTries;
        }
        else
        {
            wrongText.text = "처리 실수: " + cm.resolutionWrongCount + " / " + GameManager.instance.config.maxResolutionWrongTries;
        }

        // 단계에 맞는 쪽만 보여줌
        SetPart(translatePart, cm.stage == ComplaintStage.Translate);
        SetPart(resolvePart, cm.stage == ComplaintStage.Resolve);

        RebuildSlots(cm.translation);
        RebuildCards(cm.translation);
    }

    // 왼쪽 창에 지금 민원인 보여주기
    // 전용 그림이 있으면 그걸 쓰고, 없으면 파츠로 조합해서 만듦
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
    // 데이터엔 "저, 그, [단어1], [단어2] 걸로 [단어3] 주세요." 처럼 자리만 비워놨고
    // 그 자리에 해당 상자의 정답 단어가 쓰는 외계어를 색까지 입혀서 끼워넣음
    // (이걸 안 하면 화면에 [단어1] 이 글자 그대로 나옴)
    // tc 에서 읽는 이유: 후보가 여럿이면 이번 판 정답은 민원 시작할 때 뽑히므로
    // 데이터에 적힌 고정 정답을 쓰면 대사랑 상자가 서로 다른 단어를 가리킴
    string BuildComplaintLine(ComplaintDefinition c, TranslationController tc)
    {
        string line = c.complaintLine;

        for (int i = 0; i < tc.slots.Count; i++)
        {
            WordDefinition word = tc.slots[i].correctWord;
            if (word == null)
            {
                continue; // 데이터에 빈 칸
            }

            // 종류색 + 굵게 (외계어라는 게 한눈에 보이게)
            string colored = "<b><color=#" + HexOf(tc.slots[i].category) + ">"
                + word.dialectText
                + "</color></b>";

            line = line.Replace("[단어" + (i + 1) + "]", colored);
        }

        return line;
    }

    // 종류별 색 (상자랑 같은 규칙: 문서 노랑 / 대상 초록 / 수량 파랑 / 부서 빨강)
    string HexOf(WordCategory category)
    {
        if (category == WordCategory.Document)
        {
            return DocumentHex;
        }
        if (category == WordCategory.Target)
        {
            return TargetHex;
        }
        if (category == WordCategory.Quantity)
        {
            return QuantityHex;
        }

        return DepartmentHex;
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

    // 상자들 지우고 다시 만들기
    void RebuildSlots(TranslationController tc)
    {
        for (int i = 0; i < _slotUis.Count; i++)
        {
            Destroy(_slotUis[i].gameObject);
        }
        _slotUis.Clear();

        // 프리팹이나 놓을 자리를 안 꽂아뒀으면 상자를 못 만듦
        if (slotPrefab == null || slotParent == null)
        {
            Debug.LogWarning("해석 화면에 상자 프리팹이나 놓을 자리가 연결 안 됨");
            return;
        }

        for (int i = 0; i < tc.slots.Count; i++)
        {
            // 먼저 목록에 넣어둬야 Set 에서 뭐가 잘못돼도 고아 오브젝트가 안 남음
            WordSlotUI slotUi = Instantiate(slotPrefab, slotParent);
            _slotUis.Add(slotUi);
            slotUi.Set(this, i, tc.slots[i]);
        }
    }

    // 카드들 지우고 다시 만들기
    void RebuildCards(TranslationController tc)
    {
        for (int i = 0; i < _cardUis.Count; i++)
        {
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

            WordCardUI cardUi = Instantiate(cardPrefab, cardParent);
            _cardUis.Add(cardUi);
            cardUi.Set(i, tc.cards[i]);
        }
    }

    // 카드가 상자에 떨어졌을 때 (상자가 불러줌)
    public void OnCardDropped(int cardIndex, int slotIndex)
    {
        GameManager.instance.complaint.translation.PlaceCard(cardIndex, slotIndex);
        Rebuild();
    }

    // 상자 눌러서 비우기 (상자가 불러줌)
    public void OnSlotClicked(int slotIndex)
    {
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

    // 처리: 문서 출력
    public void OnClickPrint()
    {
        GameManager.instance.complaint.SelectResolution(ComplaintResolution.DocumentPrint);
    }
}
