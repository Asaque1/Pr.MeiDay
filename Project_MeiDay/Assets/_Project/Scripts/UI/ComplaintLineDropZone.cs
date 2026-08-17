using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class ComplaintLineDropZone : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    [SerializeField] TranslationView view;    // 해석 화면
    [SerializeField] TMP_Text lineText;       // 민원 대사 글자
    [SerializeField] float extraTouch = 6f;   // 빈칸 판정을 살짝 넉넉하게 (px)

    void Awake()
    {
        // 같은 오브젝트에 글자가 있으면 알아서 찾아 씀
        if (lineText == null)
        {
            lineText = GetComponent<TMP_Text>();
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null || view == null)
        {
            return;
        }

        WordCardUI card = eventData.pointerDrag.GetComponent<WordCardUI>();
        if (card == null)
        {
            return; // 카드 아닌 게 떨어짐
        }

        int slotIndex = SlotIndexAt(eventData);
        if (slotIndex < 0)
        {
            return; // 빈칸이 아닌 곳에 놓음 - 아무 일도 안 함
        }

        view.OnCardDropped(card.cardIndex, slotIndex);
    }

    // 채워둔 빈칸을 누르면 다시 비움
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || view == null)
        {
            return;
        }

        int slotIndex = SlotIndexAt(eventData);
        if (slotIndex < 0)
        {
            return;
        }

        view.OnSlotClicked(slotIndex);
    }

    // 누른 지점이 몇 번째 빈칸 위인지 (빈칸이 아니면 -1)
    int SlotIndexAt(PointerEventData eventData)
    {
        if (lineText == null)
        {
            return -1;
        }

        TMP_TextInfo info = lineText.textInfo;
        if (info == null)
        {
            return -1;
        }

        int pair = 0;
        int openAt = -1;

        for (int i = 0; i < info.characterCount; i++)
        {
            char ch = info.characterInfo[i].character;

            if (ch == '[')
            {
                openAt = i;
                continue;
            }

            if (ch != ']' || openAt < 0)
            {
                continue;
            }

            // 대괄호 한 짝을 찾음 - 이 구간이 pair 번째 빈칸
            if (IsInside(info, openAt, i, eventData) == true)
            {
                return pair;
            }

            pair++;
            openAt = -1;
        }

        return -1;
    }

    // 대괄호 구간(from~to)이 차지하는 화면 영역 안에 눌린 지점이 들어오는지
    bool IsInside(TMP_TextInfo info, int from, int to, PointerEventData eventData)
    {
        float left = float.MaxValue;
        float right = float.MinValue;
        float bottom = float.MaxValue;
        float top = float.MinValue;

        for (int i = from; i <= to; i++)
        {
            TMP_CharacterInfo ci = info.characterInfo[i];

            // 공백은 그려지지 않아도 자리 정보는 있음 (빈칸 안쪽이 전부 공백이라 이게 필요함)
            if (ci.bottomLeft.x < left) left = ci.bottomLeft.x;
            if (ci.topRight.x > right) right = ci.topRight.x;
            if (ci.bottomLeft.y < bottom) bottom = ci.bottomLeft.y;
            if (ci.topRight.y > top) top = ci.topRight.y;
        }

        if (left > right || bottom > top)
        {
            return false; // 자리 정보가 이상함
        }

        // 글자 좌표를 화면 좌표로 옮김
        Transform t = lineText.transform;
        Vector3 bl = t.TransformPoint(new Vector3(left, bottom, 0f));
        Vector3 tr = t.TransformPoint(new Vector3(right, top, 0f));

        Camera cam = eventData.pressEventCamera;
        Vector2 blScreen = RectTransformUtility.WorldToScreenPoint(cam, bl);
        Vector2 trScreen = RectTransformUtility.WorldToScreenPoint(cam, tr);

        Rect area = new Rect(
            Mathf.Min(blScreen.x, trScreen.x) - extraTouch,
            Mathf.Min(blScreen.y, trScreen.y) - extraTouch,
            Mathf.Abs(trScreen.x - blScreen.x) + extraTouch * 2f,
            Mathf.Abs(trScreen.y - blScreen.y) + extraTouch * 2f);

        return area.Contains(eventData.position);
    }
}
