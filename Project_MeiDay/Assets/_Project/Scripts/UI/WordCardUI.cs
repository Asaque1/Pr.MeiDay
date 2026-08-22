using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 단어 카드 한 장 UI (드래그해서 상자에 넣는 거)
public class WordCardUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public int cardIndex; // 몇 번째 카드인지 (상자가 드롭 판정할 때 씀)

    [SerializeField] TMP_Text wordText;       // 카드에 쓰인 단어 (진짜 뜻)
    [SerializeField] CanvasGroup canvasGroup; // 드래그 중 레이캐스트 끄기 + 흐리게 하기용
    [SerializeField] Image background;        // 종류별 색 넣을 배경

    // 종류별 카드 그림 (Art/WordCards 의 word_card_*)
    // 빈칸(WordSlotUI)이랑 같은 규칙이라야 짝이 보임 - 문서만 노랑, 나머지는 초록
    [SerializeField] Sprite documentSprite;
    [SerializeField] Sprite otherSprite;

    // 그림을 안 꽂아뒀을 때 쓰는 색 (예전 방식)
    [SerializeField] Color documentColor = new Color(1f, 0.9f, 0.4f); // 문서 (노랑)
    [SerializeField] Color otherColor = new Color(0.5f, 0.9f, 0.5f);  // 나머지 (초록)

    [SerializeField] float textPadding = 5f; // 카드 테두리랑 글자 사이 여백

    bool _canDrag = true;    // 이미 상자에 놓은 카드는 못 끎
    Vector3 _homePos;        // 드래그 시작 전 원래 자리
    float _maxFontSize = 0f; // 프리팹에 적어둔 글자 크기 상한 (이보다 커지진 않음)

    void Awake()
    {
        if (wordText != null)
        {
            _maxFontSize = wordText.fontSizeMax;
        }
    }

    // 만들어질 때 view 가 채워줌
    public void Set(int index, WordCard card)
    {
        cardIndex = index;
        wordText.text = WithBreakHint(card.word.meaning);
        _canDrag = (card.isUsed == false);

        ApplyLook(card.word.category);

        // 이미 상자에 놓은 카드는 흐리게
        if (card.isUsed == true)
        {
            canvasGroup.alpha = 0.3f;
        }
        else
        {
            canvasGroup.alpha = 1f;
        }
    }

    // 세로 카드는 줄이 바뀌는데, 그냥 두면 "혼인증명 / 서" 처럼 한 글자만 넘어감
    // 서류 종류 앞에서 줄을 바꿔서 "혼인 / 증명서" 처럼 말이 되게 끊음
    //
    // TMP 는 눈에 안 보이는 띄개(U+200B)를 끊는 자리로 안 쳐줘서 진짜 줄바꿈을 넣음
    // 다르게 끊고 싶은 단어는 단어 데이터의 뜻에 공백을 넣으면 그 자리에서 끊김
    static readonly string[] BreakBefore = new string[] {
        "등록증명서", "이전신청서", "관계증명서", "증명서", "신청서", "신분증",
    };

    string WithBreakHint(string word)
    {
        if (string.IsNullOrEmpty(word))
        {
            return word;
        }

        // 이미 띄어쓰기로 끊을 자리를 정해뒀으면 건드리지 않음
        if (word.Contains(" ") == true)
        {
            return word;
        }

        for (int i = 0; i < BreakBefore.Length; i++)
        {
            string tail = BreakBefore[i];

            // 뒤가 그 종류로 끝나고, 앞에 남는 글자가 있어야 끊을 값어치가 있음
            if (word.Length <= tail.Length)
            {
                continue;
            }
            if (word.EndsWith(tail) == false)
            {
                continue;
            }

            int cut = word.Length - tail.Length;
            return word.Substring(0, cut) + "\n" + word.Substring(cut);
        }

        return word;
    }

    // 카드끼리 겹칠 때 안 가려지는 폭 안에만 글자를 두기
    // 손패는 뒤 카드가 앞 카드의 오른쪽을 덮으니, 글자가 그 밑으로 들어가면 잘려 보임
    public void FitTextWidth(float visibleWidth)
    {
        if (wordText == null)
        {
            return;
        }

        RectTransform self = GetComponent<RectTransform>();
        RectTransform rt = wordText.rectTransform;

        float w = visibleWidth - textPadding * 2f;
        if (w < 10f)
        {
            w = 10f; // 너무 좁으면 글자가 아예 안 보임
        }

        // 세로 카드라 높이는 넉넉함 - 폭만 안 가려지는 만큼으로 잡고 여러 줄로 쌓게 둠
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.sizeDelta = new Vector2(w, self.rect.height - textPadding * 3f);
        rt.anchoredPosition = new Vector2(textPadding, 0f);

        FitFontToWidest(w);
    }

    // 나눠놓은 토막 중 가장 긴 게 한 줄에 들어가도록 글자 크기 상한을 낮춤
    // 이걸 안 하면 "우주선 / 등록증명 / 서" 처럼 토막이 또 갈림
    // (자동 크기는 세로만 보고 줄이는데, 세로 카드는 높이가 넉넉해서 안 줄어듦)
    void FitFontToWidest(float width)
    {
        if (wordText == null || _maxFontSize <= 0f)
        {
            return;
        }

        string[] parts = wordText.text.Split('\n');
        int longest = 0;
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length > longest)
            {
                longest = parts[i].Length;
            }
        }

        if (longest <= 0)
        {
            return;
        }

        // 한글은 글자 하나가 대략 글자 크기만큼 넓음 (살짝 여유를 둠)
        float fit = width / (longest * 1.04f);
        if (fit > _maxFontSize)
        {
            fit = _maxFontSize;
        }
        if (fit < wordText.fontSizeMin)
        {
            fit = wordText.fontSizeMin;
        }

        wordText.fontSizeMax = fit;
    }

    // 종류에 맞는 그림/색 입히기 (문서만 노랑, 나머지는 전부 초록)
    void ApplyLook(WordCategory category)
    {
        if (background == null)
        {
            return;
        }

        bool isDoc = (category == WordCategory.Document);
        Sprite sp = isDoc ? documentSprite : otherSprite;

        if (sp != null)
        {
            // 테두리 색이 그림에 들어있으니 색은 흰색으로 두고 그림만 바꿈
            background.sprite = sp;
            background.color = Color.white;
            return;
        }

        background.color = isDoc ? documentColor : otherColor;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_canDrag == false)
        {
            return;
        }

        _homePos = transform.position;

        // 형제 순서는 절대 건드리지 않음
        // 예전엔 SetAsLastSibling 으로 맨 위에 올렸는데, 카드들이 격자(Grid Layout) 안에 있어서
        // 순서가 바뀌면 오른쪽 카드들이 왼쪽으로 당겨지고, 떼면서 옛 자리로 되돌리니 그 자리에 겹쳤음
        // 카드 묶음이 상자 묶음보다 아래(나중)에 있어서 순서를 안 바꿔도 상자 위에 그려짐
        canvasGroup.blocksRaycasts = false; // 내가 막고 있으면 상자가 드롭을 못 받음
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_canDrag == false)
        {
            return;
        }

        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_canDrag == false)
        {
            return;
        }

        canvasGroup.blocksRaycasts = true;
        transform.position = _homePos; // 일단 제자리로 (넣기 성공했으면 view 가 새로 그려줌)
    }
}
