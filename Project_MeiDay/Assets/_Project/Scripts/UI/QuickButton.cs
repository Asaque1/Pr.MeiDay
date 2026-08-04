using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class QuickButton : MonoBehaviour, IPointerClickHandler
{
    public enum buttonType {
        Close, //객체 활성화 끄기
        Open, //객체 활성화 켜기
        MoveScene, //씬 이동
        Events, //이벤트 트리거
    }

    [Header("ButtonControl")] //타입 설정과 대상들
    [SerializeField] buttonType bType; //버튼 속성; 필수 값
    [SerializeField] GameObject targetObject; //대상 게임오브젝트, Close, Open에 사용.
    [SerializeField] string targetSceneName; //씬 이동 대상 씬의 이름. MoveScene에 사용.

    [Header("Event")] //이벤트.
    [SerializeField] UnityEvent onClick; //클릭 시 활성화되는 이벤트

    [Header("Color setting")] // 호버, 클릭 등의 상황에서 색 변환. 기존 버튼의 모든 설정 가능
    [SerializeField] bool useColorChange; //색 변경을 활성화하겠다.
    //이후 조건에 맞춰서 색 변경.


    void IPointerClickHandler.OnPointerClick(PointerEventData eventData) // 클릭 시
    {
        switch (bType) { //버튼 타입에 따라서 변경되는 방식.
            case buttonType.Close:
                targetObject.SetActive(false); break;
            case buttonType.Open:
                targetObject.SetActive(true); break;
            case buttonType.MoveScene:
                SceneManager.LoadScene(targetSceneName); break;
            case buttonType.Events:
                onClick.Invoke(); break;
        }
    }







}
