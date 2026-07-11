using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 단어 상자 한 칸 UI (카드를 여기에 드래그해서 넣음)
public class WordSlotUI : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    [SerializeField] TMP_Text wordText; // 놓인 단어 (비어있으면 사투리 보여줌)
    [SerializeField] Image background;  // 종류별 색 넣을 배경

    // 종류별 상자 색 (인스펙터에서 바꿀 수 있음)
    [SerializeField] Color documentColor = new Color(1f, 0.9f, 0.4f);   // 문서 (노랑)
    [SerializeField] Color targetColor = new Color(0.5f, 0.9f, 0.5f);   // 대상 (초록)
    [SerializeField] Color quantityColor = new Color(0.5f, 0.7f, 1f);   // 수량 (파랑)
    [SerializeField] Color departmentColor = new Color(1f, 0.5f, 0.5f); // 부서 (빨강)

    TranslationView _view;
    int _slotIndex;

    // 만들어질 때 view 가 채워줌
    public void Set(TranslationView view, int slotIndex, WordSlot slot)
    {
        _view = view;
        _slotIndex = slotIndex;

        if (slot.placedWord != null)
        {
            wordText.text = slot.placedWord.meaning;
        }
        else
        {
            // 빈 상자엔 외계인이 말한 사투리가 보임
            wordText.text = "[ " + slot.correctWord.dialectText + " ]";
        }

        background.color = ColorOf(slot.category);
    }

    // 카드가 위에 떨어졌을 때
    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
        {
            return;
        }

        WordCardUI card = eventData.pointerDrag.GetComponent<WordCardUI>();
        if (card == null)
        {
            return; // 카드 아닌 게 떨어짐
        }

        _view.OnCardDropped(card.cardIndex, _slotIndex);
    }

    // 상자 클릭하면 놓았던 카드 다시 빼기
    public void OnPointerClick(PointerEventData eventData)
    {
        // 왼쪽 클릭만 받음
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        _view.OnSlotClicked(_slotIndex);
    }

    // 종류별 상자 색 (기획서: 문서 노랑 / 대상 초록 / 수량 파랑 / 부서 빨강)
    Color ColorOf(WordCategory category)
    {
        if (category == WordCategory.Document)
        {
            return documentColor;
        }
        if (category == WordCategory.Target)
        {
            return targetColor;
        }
        if (category == WordCategory.Quantity)
        {
            return quantityColor;
        }

        return departmentColor;
    }
}
