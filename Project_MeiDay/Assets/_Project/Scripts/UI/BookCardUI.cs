using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 위에 나온 책 한 권 - 집어서 아래 카트 칸에 끌어다 놓는 카드
// 어디에 놓였는지는 BookSortView 가 판단함 (여기는 집고 따라다니고 놓는 것만)
public class BookCardUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] Image body;      // 책 색 (장르 색으로 칠함)
    [SerializeField] TMP_Text label;  // 장르 번호 글씨

    public int cartIndex = -1; // 카트에서 몇 번째 책인지

    BookSortView _owner;
    RectTransform _rect;
    Transform _homeParent;
    Vector2 _homePos;
    CanvasGroup _group;

    void Awake()
    {
        _rect = transform as RectTransform;
        _group = GetComponent<CanvasGroup>();
        if (_group == null)
        {
            _group = gameObject.AddComponent<CanvasGroup>();
        }
    }

    // 화면이 책 정보를 넣어줌
    public void Set(BookSortView owner, int index, string text, Color color, bool golden)
    {
        _owner = owner;
        cartIndex = index;

        if (body != null)
        {
            body.color = color;
        }
        if (label != null)
        {
            label.text = text;
        }

        // 황금 책은 좀 크게 해서 눈에 띄게
        transform.localScale = golden ? new Vector3(1.12f, 1.12f, 1f) : Vector3.one;
    }

    void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
    {
        if (_owner == null)
        {
            return;
        }

        _homeParent = transform.parent;
        _homePos = _rect.anchoredPosition;

        // 끄는 동안엔 카트 칸이 마우스를 받아야 하니까 자기는 클릭을 안 막음
        _group.blocksRaycasts = false;

        // 카트 칸 위로 올라오게 맨 앞으로 빼둠
        transform.SetParent(_owner.DragLayer(), true);
        transform.SetAsLastSibling();
    }

    void IDragHandler.OnDrag(PointerEventData eventData)
    {
        if (_owner == null)
        {
            return;
        }

        Vector3 world;
        bool ok = RectTransformUtility.ScreenPointToWorldPointInRectangle(
            _owner.DragLayer() as RectTransform, eventData.position, eventData.pressEventCamera, out world);
        if (ok == true)
        {
            transform.position = world;
        }
    }

    void IEndDragHandler.OnEndDrag(PointerEventData eventData)
    {
        _group.blocksRaycasts = true;

        if (_owner == null)
        {
            return;
        }

        // 칸에 놓였는지는 화면이 판단함. 아니면 제자리로 돌아옴
        bool placed = _owner.OnDropBook(this, eventData);
        if (placed == false)
        {
            GoHome();
        }
    }

    // 원래 자리로 되돌리기
    public void GoHome()
    {
        if (_homeParent != null)
        {
            transform.SetParent(_homeParent, false);
            _rect.anchoredPosition = _homePos;
        }
    }
}
