using UnityEngine;
using UnityEngine.UI;

// 민원인이 참아주는 시간을 타이머 막대로 보여줌
// 가득 찬 채로 시작해서 시간이 갈수록 줄어들고, 다 닳으면 민원인이 화냄
// (분노를 올리는 건 ComplaintManager 몫. 여기는 보여주기만 함)
//
// 오브젝트를 껐다 켜지 않고 그림만 껐다 켬
// 자기가 붙어있는 오브젝트를 꺼버리면 Update 가 안 돌아서 다시 못 켬
public class AngerGauge : MonoBehaviour
{
    [SerializeField] Image back;  // 게이지 바탕 (안 보일 땐 같이 꺼둠)
    [SerializeField] Image fill;  // 줄어드는 막대

    // 막대를 fillAmount 로 깎지 않고 가로 폭을 직접 줄임
    // 스프라이트를 안 써서 그림이 눌리거나 모서리가 뭉개질 일이 없음
    RectTransform _fillRect;

    // 얼마 안 남았을 때 색이 진해지면 눈치채기 쉬움
    [SerializeField] Color calmColor = new Color(0.85f, 0.35f, 0.3f);
    [SerializeField] Color angryColor = new Color(0.95f, 0.12f, 0.12f);

    void Update()
    {
        float ratio = 0f;
        bool show = ShouldShow(out ratio);

        if (back != null && back.enabled != show)
        {
            back.enabled = show;
        }
        if (fill != null && fill.enabled != show)
        {
            fill.enabled = show;
        }

        if (show == false || fill == null)
        {
            return;
        }

        if (_fillRect == null)
        {
            _fillRect = fill.rectTransform;
        }

        // 왼쪽에 붙여두고 오른쪽 끝만 당김
        _fillRect.anchorMin = new Vector2(0f, 0f);
        _fillRect.anchorMax = new Vector2(ratio, 1f);
        _fillRect.offsetMin = new Vector2(1f, 1f);
        _fillRect.offsetMax = new Vector2(-1f, -1f);

        fill.color = Color.Lerp(angryColor, calmColor, ratio); // 남은 게 적을수록 빨개짐
    }

    // 지금 게이지를 보여줄 상황인지, 얼마나 남았는지 (1 = 가득, 0 = 다 닳음)
    bool ShouldShow(out float ratio)
    {
        ratio = 0f;

        GameManager gm = GameManager.instance;
        if (gm == null || gm.phase != GamePhase.DayWork || gm.complaint == null)
        {
            return false; // 낮 업무 중이 아니면 볼 일 없음
        }

        // 정해둔 일차 전에는 게이지 자체가 없음
        // 처음 며칠은 조작을 익히는 기간이라 시간에 쫓기게 하지 않음 (시계도 같이 멈춰있음)
        if (gm.calendar.day < gm.config.angerStartDay)
        {
            return false;
        }

        ComplaintManager cm = gm.complaint;

        // 민원 내용이 다 뜨기 전엔 가득 찬 채로 멈춰 있음
        // 읽는 동안 줄어들면 글 읽느라 시간을 까먹는 셈이라 억울함
        if (cm.entry == ComplaintEntry.Speaking)
        {
            ratio = 1f;
            return true;
        }

        if (cm.entry != ComplaintEntry.Ready || cm.angerTimeMax <= 0f)
        {
            return false; // 손님이 없거나 시간 제한을 꺼둔 설정
        }

        // 남은 시간만큼 (가득 -> 빈 칸)
        ratio = cm.angerTimeLeft / cm.angerTimeMax;
        if (ratio < 0f)
        {
            ratio = 0f;
        }
        if (ratio > 1f)
        {
            ratio = 1f;
        }

        return true;
    }
}
