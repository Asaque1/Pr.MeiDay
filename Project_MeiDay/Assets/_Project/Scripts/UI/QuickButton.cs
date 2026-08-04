using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using CustomInspector;
using UnityEngine.UI;

public class QuickButton : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler, IPointerDownHandler, ISubmitHandler, ISelectHandler, IDeselectHandler
{
    //public enum buttonType {
    //    Close, //객체 활성화 끄기
    //    Open, //객체 활성화 켜기
    //    MoveScene, //씬 이동
    //    Events, //이벤트 트리거
    //}   
    
    [Header("ButtonControl")] //타입 설정과 대상들
    [FixedValues("Open","Close","MoveScene","Events")]
    [SerializeField] string bType; //버튼 속성; 필수 값
    [SerializeField] bool doBlock;

    [ShowIf(nameof(isTargetObject))]
    [SerializeField] GameObject targetObject; //대상 게임오브젝트, Close, Open에 사용.

    [ShowIf(nameof(isTargetScene))]
    [SerializeField] string targetSceneName; //씬 이동 대상 씬의 이름. MoveScene에 사용.

    [ShowIf(nameof(isTargetEvents))]
    [Header("Event")] //이벤트.
    [SerializeField] UnityEvent onClick; //클릭 시 활성화되는 이벤트

    [Header("Color setting")] // 호버, 클릭 등의 상황에서 색 변환. 기존 버튼의 모든 설정 가능
    [SerializeField] bool useColorChange; //색 변경을 활성화하겠다.
    //이후 조건에 맞춰서 색 변경.

    [ShowIf(nameof(useColorChange))]
    [SerializeField] Color32 normalColor;

    [ShowIf(nameof(useColorChange))]
    [SerializeField] Color32 highlightedColor;

    [ShowIf(nameof(useColorChange))]
    [SerializeField] Color32 pressedColor;

    [ShowIf(nameof(useColorChange))]
    [SerializeField] Color32 selectedColor;

    [ShowIf(nameof(useColorChange))]
    [SerializeField] Color32 disabledColor;

    [ShowIf(nameof(useColorChange))]
    [SerializeField] float fadeDuration;

    Graphic targetGraphic;
        

    private bool isTargetObject(){
        return bType == "Open" || bType == "Close";
    }

    private bool isTargetScene(){
        return bType == "MoveScene";
    }

    private bool isTargetEvents(){
        return bType == "Events";
    }

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData) // 클릭 시
    {
        DoClick();
    }

    public void DoClick()
    {
        switch (bType)
        { //버튼 타입에 따라서 변경되는 방식.
            case "Close":
                targetObject.SetActive(false); break;
            case "Open":
                targetObject.SetActive(true); break;
            case "MoveScene":
                SceneManager.LoadScene(targetSceneName); break;
            case "Events":
                onClick?.Invoke(); break;
        }
    }

    private void ChangeColor(Color32 color)
    {
        if (!useColorChange)
            return;

        if (targetGraphic == null)
            return;

        targetGraphic.CrossFadeColor(
            color,
            fadeDuration,
            true,
            true
        );
    }
    public void Start()
    {
        targetGraphic = GetComponent<Graphic>();

        if (doBlock)
        {
            ChangeColor(disabledColor);
        }
    }

    public void BlockSwitching()
    {
        doBlock = !doBlock;

        if (doBlock)
        {
            ChangeColor(disabledColor);
        }
        else
        {
            ChangeColor(normalColor);
        }

    }

    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
    {
        ChangeColor(highlightedColor);        
    }
    void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
    {
        ChangeColor(normalColor);
    }
    void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
    {
        ChangeColor(highlightedColor);
    }
    void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
    {
        ChangeColor(pressedColor);
    }
    void ISelectHandler.OnSelect(BaseEventData eventData)
    {
        ChangeColor(selectedColor);
    }
    void IDeselectHandler.OnDeselect(BaseEventData eventData)
    {
        ChangeColor(normalColor);
    }
    void ISubmitHandler.OnSubmit(BaseEventData eventData)
    {
        ChangeColor(pressedColor);
        DoClick();
    }

}
