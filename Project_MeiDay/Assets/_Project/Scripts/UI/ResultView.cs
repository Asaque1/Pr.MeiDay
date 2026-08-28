using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 하루 결산 화면 (성공/실패/수입 보여줌)
public class ResultView : MonoBehaviour
{
    [SerializeField] TMP_Text summaryText; // 통째로 보여줄 때 (아래 줄 프리팹을 안 쓰면 이거)

    // 줄 프리팹으로 한 줄씩 찍어서 보여줄 때 (TextPref 같은 거)
    [SerializeField] Transform lineParent;
    [SerializeField] ResultLineUI linePrefab;

    List<ResultLineUI> _lines = new List<ResultLineUI>();

    // 패널 켜질 때 오늘 결과 채워줌
    void OnEnable()
    {
        if (GameManager.instance == null)
        {
            return;
        }

        List<string> lines = new List<string>();
        List<bool> goods = new List<bool>();
        BuildLines(lines, goods);

        // 줄 프리팹을 꽂아뒀으면 한 줄씩, 아니면 예전처럼 한 덩어리로
        if (linePrefab != null && lineParent != null)
        {
            ShowAsLines(lines, goods);
            return;
        }

        ShowAsOneText(lines);
    }

    // 오늘 결산 내용을 줄 단위로 만들어줌 (good 은 좋은 소식인지)
    void BuildLines(List<string> lines, List<bool> goods)
    {
        ComplaintManager cm = GameManager.instance.complaint;
        EconomyManager economy = GameManager.instance.economy;
        GameConfig config = GameManager.instance.config;

        lines.Add("성공: " + cm.successCount + "   실패: " + cm.failCount);
        goods.Add(cm.failCount == 0);

        lines.Add("오늘 수입: +" + cm.earnedToday + " cr");
        goods.Add(cm.earnedToday > 0);

        if (cm.mistakeCount == 0 && cm.successCount > 0)
        {
            lines.Add("무실수 보너스!");
            goods.Add(true);
        }
        else if (cm.mistakeCount > 0)
        {
            // 왜 무실수 보너스를 못 받았는지 보이게
            lines.Add("실수: " + cm.mistakeCount + "회");
            goods.Add(false);
        }

        lines.Add("식비: -" + economy.foodPaidToday + " cr");
        goods.Add(false);

        if (economy.rentWasDueToday == true)
        {
            if (economy.rentMissedToday == true)
            {
                lines.Add("월세: 미납 (" + economy.rentMissCount + " / " + config.rentMissLimit + ")");
                goods.Add(false);
            }
            else
            {
                lines.Add("월세: -" + economy.rentPaidToday + " cr");
                goods.Add(true);
            }
        }

        lines.Add("남은 크레딧: " + economy.credits + " cr");
        goods.Add(economy.credits > 0);
    }

    // 줄 프리팹을 하나씩 찍어서 보여주기
    void ShowAsLines(List<string> lines, List<bool> goods)
    {
        // 어제 찍어둔 줄 치우기
        for (int i = 0; i < _lines.Count; i++)
        {
            if (_lines[i] != null)
            {
                Destroy(_lines[i].gameObject);
            }
        }
        _lines.Clear();

        for (int i = 0; i < lines.Count; i++)
        {
            // 먼저 목록에 넣어둬야 Set 에서 뭐가 잘못돼도 고아 오브젝트가 안 남음
            ResultLineUI line = Instantiate(linePrefab, lineParent);
            _lines.Add(line);
            line.Set(lines[i], goods[i]);
        }

        // 줄로 보여주는 중이면 통짜 글자는 꺼둠
        // 글자만 비우면 같은 자리에 빈 줄로 남아서 아예 끔
        if (summaryText != null)
        {
            summaryText.gameObject.SetActive(false);
        }
    }

    // 예전처럼 한 덩어리로 보여주기
    void ShowAsOneText(List<string> lines)
    {
        if (summaryText == null)
        {
            return;
        }

        summaryText.gameObject.SetActive(true); // 줄 프리팹 쓰다가 꺼졌을 수도 있어서
        string text = "";
        for (int i = 0; i < lines.Count; i++)
        {
            if (i > 0)
            {
                text += "\n";
            }
            text += lines[i];
        }

        summaryText.text = text;
    }
}
