using UnityEngine;
using UnityEngine.EventSystems;

// 조명 좌/우 버튼 - 누르고 있는 동안 계속 움직여야 해서 따로 둠
// (일반 Button 의 OnClick 은 뗄 때 한 번만 와서 '누르고 있기' 를 못 만듦)
public class SpotlightHoldButton : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] SpotlightView view;
    [SerializeField] int direction = -1; // -1 왼쪽, 1 오른쪽

    public void OnPointerDown(PointerEventData eventData)
    {
        if (view == null)
        {
            return;
        }

        if (direction < 0)
        {
            view.OnPressLeft();
        }
        else
        {
            view.OnPressRight();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Stop();
    }

    // 누른 채로 버튼 밖으로 끌고 나가면 뗀 걸 못 받아서 여기서도 멈춰줌
    public void OnPointerExit(PointerEventData eventData)
    {
        Stop();
    }

    void Stop()
    {
        if (view != null)
        {
            view.OnRelease();
        }
    }
}
