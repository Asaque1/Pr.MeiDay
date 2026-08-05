using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// 단어 카드 한 장 UI (드래그해서 상자에 넣는 거)
public class WordCardUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public int cardIndex; // 몇 번째 카드인지 (상자가 드롭 판정할 때 씀)

    [SerializeField] TMP_Text wordText;       // 카드에 쓰인 단어 (진짜 뜻)
    [SerializeField] CanvasGroup canvasGroup; // 드래그 중 레이캐스트 끄기 + 흐리게 하기용

    bool _canDrag = true; // 이미 상자에 놓은 카드는 못 끎
    Vector3 _homePos;     // 드래그 시작 전 원래 자리

    // 만들어질 때 view 가 채워줌
    public void Set(int index, WordCard card)
    {
        cardIndex = index;
        wordText.text = card.word.meaning;
        _canDrag = (card.isUsed == false);

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
