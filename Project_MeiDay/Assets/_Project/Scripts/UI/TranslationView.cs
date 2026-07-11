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

        // 오늘 민원 다 끝났으면 그릴 게 없음
        if (cm.currentIndex >= cm.todayComplaints.Count)
        {
            return;
        }

        ComplaintDefinition c = cm.todayComplaints[cm.currentIndex];
        complaintLineText.text = c.complaintLine;
        quotaText.text = (cm.currentIndex + 1) + " / " + cm.todayComplaints.Count;
        wrongText.text = "틀린 횟수: " + cm.translation.wrongCount + " / " + GameManager.instance.config.maxWrongTries;

        // 단계에 맞는 쪽만 보여줌
        translatePart.SetActive(cm.stage == ComplaintStage.Translate);
        resolvePart.SetActive(cm.stage == ComplaintStage.Resolve);

        RebuildSlots(cm.translation);
        RebuildCards(cm.translation);
    }

    // 상자들 지우고 다시 만들기
    void RebuildSlots(TranslationController tc)
    {
        for (int i = 0; i < _slotUis.Count; i++)
        {
            Destroy(_slotUis[i].gameObject);
        }
        _slotUis.Clear();

        for (int i = 0; i < tc.slots.Count; i++)
        {
            WordSlotUI slotUi = Instantiate(slotPrefab, slotParent);
            slotUi.Set(this, i, tc.slots[i]);
            _slotUis.Add(slotUi);
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

        for (int i = 0; i < tc.cards.Count; i++)
        {
            WordCardUI cardUi = Instantiate(cardPrefab, cardParent);
            cardUi.Set(i, tc.cards[i]);
            _cardUis.Add(cardUi);
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

    // 처리: 직접 처리
    public void OnClickDirect()
    {
        GameManager.instance.complaint.SelectResolution(ComplaintResolution.DirectHandle);
    }
}
