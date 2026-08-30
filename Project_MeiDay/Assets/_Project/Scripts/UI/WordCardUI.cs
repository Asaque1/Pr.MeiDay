using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 단어 카드 한 장 UI (드래그해서 상자에 넣는 거)
public class WordCardUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
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

    [Header("손 올렸을 때")]
    // "이거 집어서 끌 수 있겠는데" 싶게, 카드를 집으려고 살짝 뽑아든 모양으로 만듦
    [SerializeField] float hoverRise = 14f;   // 위로 얼마나 뜨는지
    [SerializeField] float hoverGrow = 0.07f; // 얼마나 커지는지 (1 = 두 배)
    [SerializeField] float dragShrink = 0.6f; // 드는 동안 줄일 비율 (커서 밑이 가려지지 않게)
    [SerializeField] float hoverSpeed = 12f;  // 얼마나 빨리 뜨고 가라앉는지

    bool _canDrag = true;    // 이미 상자에 놓은 카드는 못 끎
    Vector3 _homePos;        // 드래그 시작 전 원래 자리
    float _maxFontSize = 0f; // 프리팹에 적어둔 글자 크기 상한 (이보다 커지진 않음)

    // 프리팩에 잡아둔 글자 칸 (카드 가운데 꽉 차는 자리)
    bool _textRestSaved = false;
    Vector2 _textAnchorMin, _textAnchorMax, _textPivot, _textSize, _textPos;

    // 손 올렸을 때 되돌릴 원래 모습 (LayoutCards 가 잡아준 자리)
    bool _hover = false;
    bool _poseSaved = false;
    float _amount = 0f;      // 0 = 제자리, 1 = 다 뜬 상태
    Vector2 _restPos;
    Quaternion _restRot;
    Vector3 _restScale;

    void Awake()
    {
        if (wordText != null)
        {
            _maxFontSize = wordText.fontSizeMax;
        }

        SaveTextRest();
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

    // 프리팩에 잡혀 있는 글자 칸을 한 번 기억해둔다
    void SaveTextRest()
    {
        if (_textRestSaved == true || wordText == null)
        {
            return;
        }

        RectTransform rt = wordText.rectTransform;
        _textAnchorMin = rt.anchorMin;
        _textAnchorMax = rt.anchorMax;
        _textPivot = rt.pivot;
        _textSize = rt.sizeDelta;
        _textPos = rt.anchoredPosition;
        _textRestSaved = true;
    }

    // 카드 글자는 언제나 카드 한가운데
    //
    // 손패는 뒤 카드가 앞 카드의 오른쪽을 덮어서 글자가 좀 가려짐
    // 예전엔 그걸 피하려고 글자를 왼쪽으로 몰아됐는데,
    // 그러면 카드마다 글자 자리가 달라져서 줄이 안 맞음
    // 가려서 좀 잘려도 가운데가 보기 낫기로 함
    //
    // visibleWidth: 안 가려지는 폭. 이젠 자리를 정하는 데엔 안 씁
    public void FitTextWidth(float visibleWidth)
    {
        if (wordText == null)
        {
            return;
        }

        SaveTextRest();

        RectTransform rt = wordText.rectTransform;
        rt.anchorMin = _textAnchorMin;
        rt.anchorMax = _textAnchorMax;
        rt.pivot = _textPivot;
        rt.sizeDelta = _textSize;
        rt.anchoredPosition = _textPos;

        // 방금 넣은 값이라 rect 가 아직 안 바뀜을 수 있어서 카드 폭으로 직접 계산함
        RectTransform self = GetComponent<RectTransform>();
        float w = self.rect.width * (_textAnchorMax.x - _textAnchorMin.x) + _textSize.x;
        if (w < 10f)
        {
            w = 10f;
        }

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

    // ---- 손 올렸을 때 ----

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (CanPickUp() == false)
        {
            return; // 못 끄는 카드가 반응하면 거짓말이 됨
        }

        SavePose();
        _hover = true;

        // 오른쪽 카드가 이 카드를 덮고 있어서, 안 올리면 뜬 게 반쯤 가려짐
        // 자리는 LayoutCards 가 좌표로 직접 잡아주고 CardParent 의 격자는 꺼져 있어서
        // 순서를 바꿔도 카드가 밀리지 않음 (뗄 때 RestoreOrder 로 다시 세움)
        transform.SetAsLastSibling();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _hover = false;
    }

    // 지금 손이 올라가 있는 카드인지 (순서 다시 세울 때 씀)
    public bool IsLifted()
    {
        return _hover;
    }

    // 손패 순서를 카드 번호대로 다시 세움
    // 뜰 때 저장해둔 형제 번호로 되돌리면, 그 사이에 옆 카드가 움직였을 때 순서가 엉킴
    // (예전에 이 순서를 건드렸다가 카드가 겹쳐 보이는 문제가 있었음)
    void RestoreOrder()
    {
        Transform parent = transform.parent;
        if (parent == null)
        {
            return;
        }

        List<WordCardUI> cards = new List<WordCardUI>();
        for (int i = 0; i < parent.childCount; i++)
        {
            WordCardUI card = parent.GetChild(i).GetComponent<WordCardUI>();
            if (card != null)
            {
                cards.Add(card);
            }
        }

        cards.Sort(delegate (WordCardUI a, WordCardUI b)
        {
            // 아직 떠 있는 카드는 맨 위에 둠 (한 장에서 옆 장으로 손이 넘어가는 중)
            if (a.IsLifted() != b.IsLifted())
            {
                return a.IsLifted() == true ? 1 : -1;
            }
            return a.cardIndex.CompareTo(b.cardIndex);
        });

        for (int i = 0; i < cards.Count; i++)
        {
            cards[i].transform.SetSiblingIndex(i);
        }
    }

    // 지금 이 카드를 집을 수 있는 상태인지
    bool CanPickUp()
    {
        if (_canDrag == false)
        {
            return false;
        }
        if (GameManager.instance == null)
        {
            return false;
        }

        // 손님이 아직 말하는 중이거나 처리 단계면 못 만짐
        return GameManager.instance.complaint.CanUseCards();
    }

    void SavePose()
    {
        if (_poseSaved == true)
        {
            return;
        }

        RectTransform rt = (RectTransform)transform;
        _restPos = rt.anchoredPosition;
        _restRot = rt.localRotation;
        _restScale = rt.localScale;
        _poseSaved = true;
    }

    // 뜬 걸 그 자리에서 바로 내려놓기 (드래그 시작할 때)
    void PutBack()
    {
        if (_poseSaved == false)
        {
            return;
        }

        RectTransform rt = (RectTransform)transform;
        rt.anchoredPosition = _restPos;
        rt.localRotation = _restRot;
        rt.localScale = _restScale;

        _hover = false;
        _amount = 0f;
        _poseSaved = false;

        RestoreOrder();
    }

    void Update()
    {
        if (_poseSaved == false)
        {
            return; // 아직 손이 안 닿은 카드
        }

        // 손을 올린 뒤에 말이 끝나거나 단계가 바뀌면 도로 내려놓음
        float want = 0f;
        if (_hover == true && CanPickUp() == true)
        {
            want = 1f;
        }

        if (Mathf.Approximately(_amount, want) == true)
        {
            if (want == 0f)
            {
                PutBack(); // 다 내려왔으면 순서까지 되돌리고 손 뗌
            }
            return;
        }

        _amount = Mathf.MoveTowards(_amount, want, Time.deltaTime * hoverSpeed);

        RectTransform rt = (RectTransform)transform;
        rt.anchoredPosition = _restPos + new Vector2(0f, hoverRise * _amount);
        rt.localRotation = Quaternion.Slerp(_restRot, Quaternion.identity, _amount); // 비뚤어진 걸 펴줌
        rt.localScale = _restScale * (1f + hoverGrow * _amount);
    }

    // ---- 드래그 ----

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_canDrag == false)
        {
            return;
        }

        PutBack(); // 뜬 자리로 돌아가면 안 되니까 먼저 내려놓고 시작
        _homePos = transform.position;

        if (SoundManager.instance != null)
        {
            SoundManager.instance.PlaySfx(SoundManager.instance.cardPick);
        }

        // 형제 순서는 절대 건드리지 않음
        // 예전엔 SetAsLastSibling 으로 맨 위에 올렸는데, 카드들이 격자(Grid Layout) 안에 있어서
        // 순서가 바뀌면 오른쪽 카드들이 왼쪽으로 당겨지고, 떼면서 옛 자리로 되돌리니 그 자리에 겹쳤음
        // 카드 묶음이 상자 묶음보다 아래(나중)에 있어서 순서를 안 바꿔도 상자 위에 그려짐
        canvasGroup.blocksRaycasts = false; // 내가 막고 있으면 상자가 드롭을 못 받음

        // 드는 동안엔 카드를 줄여둠
        // 원래 크기로 들면 커서 밑에 놓을 자리가 가려져서 어디에 넣는지 안 보였음
        ((RectTransform)transform).localScale = _restScale * dragShrink;
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
        ((RectTransform)transform).localScale = _restScale; // 줄여뒀던 크기 되돌리기
        transform.position = _homePos; // 일단 제자리로 (넣기 성공했으면 view 가 새로 그려줌)
    }
}
