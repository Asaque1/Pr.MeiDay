using UnityEngine;
using UnityEngine.UI;

// 게이지 막대 채우기
//
// Image 의 Filled 로 깎으면 스프라이트를 칸에 통째로 늘려 그린 뒤 잘라내서,
// 캡슐 모양 막대의 둥근 끝이 가로로 눌린 채로 나옴 (468x57 그림을 658x14 로 늘리면 5.7배)
// 대신 가로 폭을 줄이면 9슬라이스가 그대로 먹어서 끝 모양이 안 망가짐
//
// AngerGauge 가 이미 같은 이유로 이 방식을 쓰고 있음
public static class UiBar
{
    // ratio: 0 = 빈 칸, 1 = 꽉 참
    public static void Set(Image fill, float ratio)
    {
        if (fill == null)
        {
            return;
        }

        if (fill.type != Image.Type.Sliced)
        {
            fill.type = Image.Type.Sliced;
        }

        // 폭이 테두리(둥근 끝 두 개)보다 좁아지면 9슬라이스가 찌그러짐
        // 그 밑으로는 아예 숨기는 게 깔끔함
        bool show = ratio > 0.001f;
        if (fill.enabled != show)
        {
            fill.enabled = show;
        }
        if (show == false)
        {
            return;
        }

        RectTransform rt = fill.rectTransform;

        // 세로는 건드리지 않고 가로만 왼쪽에 붙여서 줄임
        rt.anchorMin = new Vector2(0f, rt.anchorMin.y);
        rt.anchorMax = new Vector2(Mathf.Clamp01(ratio), rt.anchorMax.y);
        rt.offsetMin = new Vector2(0f, rt.offsetMin.y);
        rt.offsetMax = new Vector2(0f, rt.offsetMax.y);
    }
}
