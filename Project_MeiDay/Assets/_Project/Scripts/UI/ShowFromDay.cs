using UnityEngine;

// 정해둔 일차가 되기 전에는 버튼(이나 아무 오브젝트)을 숨겨두는 부품
// 첫날에 볼 것도 없는 창을 열어보게 하면 어수선해서, 며칠째부터 보여줄지 정할 때 씀
//
// 주의: 숨길 대상이 아니라 그 부모에 붙일 것
// 자기 자신을 끄면 Update 가 멈춰서 다시 켜줄 수가 없음
public class ShowFromDay : MonoBehaviour
{
    [SerializeField] GameObject target; // 숨겼다 보여줄 것
    [SerializeField] int fromDay = 2;   // 며칠째부터 보여줄지

    void Update()
    {
        if (target == null || GameManager.instance == null)
        {
            return;
        }

        // 타이틀에서는 어차피 화면째 안 보이니 일차만 봄
        bool show = (GameManager.instance.calendar.day >= fromDay);

        if (target.activeSelf != show)
        {
            target.SetActive(show);
        }
    }
}
