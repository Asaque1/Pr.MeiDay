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

    // 칸 테두리랑 글자 사이 여백 (칸이 작아서 조금만 줘도 글자가 확 줄어듦)
    [SerializeField] float textPadding = 1f;
    // 이보다 작아지면 못 읽으니 여기서 멈춤 (그래도 안 들어가면 칸을 넓혀야 함)
    [SerializeField] float minFontSize = 6f;
    // 칸 안 글자가 대사보다 커지면 어색해서 대사 크기까지만 (0 이면 프리팹 값 그대로)
    [SerializeField] float maxFontSizeOverride = 0f;

    TranslationView _view;
    int _slotIndex;
    float _maxFontSize = 0f; // 프리팹에 적혀있던 원래 크기 (이보다 커지진 않음)

    void Awake()
    {
        if (wordText == null)
        {
            return;
        }

        // 자동 크기를 켜면 fontSize 가 계산값으로 바뀌어서, 켜기 전에 원래 값을 챙겨둬야 함
        if (wordText.enableAutoSizing == true)
        {
            _maxFontSize = wordText.fontSizeMax;
        }
        else
        {
            _maxFontSize = wordText.fontSize;
        }
    }

    // 만들어질 때 view 가 채워줌 (같은 칸이 대사에 두 번 나오면 이게 두 개 만들어짐)
    public void Set(TranslationView view, int slotIndex, WordSlot slot)
    {
        _view = view;
        _slotIndex = slotIndex;

        // 카드 자체가 아니라 카드에 쓰인 글자만 넘어옴
        // 아직 아무것도 안 꽂았으면 진짜로 비워둠 (미리 뭘 써두면 힌트가 됨)
        if (slot.placedWord != null)
        {
            wordText.text = slot.placedWord.meaning;
        }
        else
        {
            wordText.text = "";
        }

        background.color = ColorOf(slot.category);

        FitTextToBox();
    }

    // 글자가 칸 밖으로 안 나가게 맞춰줌
    //
    // 프리팹의 글자칸은 135x40 로 고정돼 있는데, 대사 안에 박히는 빈칸은 그보다 훨씬 작음
    // 그대로 두면 글자가 칸을 뚫고 나가거나 줄이 바뀌어버림
    void FitTextToBox()
    {
        if (wordText == null)
        {
            return;
        }

        // 글자칸을 빈칸에 꽉 맞춤 (빈칸 크기는 대사가 정하니까 거기 따라감)
        RectTransform rt = wordText.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(textPadding, textPadding);
        rt.offsetMax = new Vector2(-textPadding, -textPadding);

        // 한 줄로만 (칸 안에서 줄이 바뀌면 안 됨)
        wordText.textWrappingMode = TextWrappingModes.NoWrap;
        wordText.overflowMode = TextOverflowModes.Overflow;

        // 긴 단어는 칸에 맞게 줄어듦
        wordText.enableAutoSizing = true;
        wordText.fontSizeMin = minFontSize;

        float cap = _maxFontSize;
        if (maxFontSizeOverride > 0f)
        {
            cap = maxFontSizeOverride;
        }
        if (cap > 0f)
        {
            wordText.fontSizeMax = cap;
        }
    }

    // 대사 글자 크기를 알려줌 - 칸 안 단어가 대사보다 커지면 어색해서 여기까지만 키움
    // (Set 보다 먼저 불러야 함)
    public void SetMaxFontSize(float size)
    {
        maxFontSizeOverride = size;
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
