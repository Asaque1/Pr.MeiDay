using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 파티용품점 알바 화면 - 지나가는 행인이 구간에 들어왔을 때 홍보물을 던짐
// 행인 표시는 track 의 자식으로 만들어짐 (personPrefab 을 인스펙터에 넣을 것)
public class PromoView : MonoBehaviour
{
    [SerializeField] RectTransform track;        // 길 (이 폭 안에서 행인이 걸어감)
    [SerializeField] RectTransform personPrefab; // 행인 표시 하나
    [SerializeField] RectTransform zoneMark;     // 던지면 닿는 구간 표시 (없어도 됨)
    [SerializeField] TMP_Text timeText;          // 남은 시간
    [SerializeField] TMP_Text countText;         // 나눠준 수 / 황금 / 헛던짐

    [SerializeField] float goldenScale = 1.4f; // 황금 행인은 좀 크게 그림

    ArbeitView _owner;
    List<RectTransform> _marks = new List<RectTransform>(); // 지금 화면에 있는 행인 표시들

    // 알바 화면이 불러줌
    public void Play(ArbeitView owner)
    {
        _owner = owner;
        gameObject.SetActive(true);

        if (personPrefab == null || track == null)
        {
            Debug.LogWarning("판촉 화면에 행인 프리팹이나 길이 연결 안 됨 - 글자만 나옴");
        }

        ClearMarks();
        PlaceZone();
    }

    void OnDisable()
    {
        ClearMarks(); // 화면 닫힐 때 남은 표시 정리
    }

    void Update()
    {
        PromoGame game = GetGame();
        if (game == null)
        {
            return;
        }

        game.Tick(Time.deltaTime);
        Refresh(game);

        if (game.IsDone() == true)
        {
            float rate = game.GetRate();
            gameObject.SetActive(false);

            if (_owner != null)
            {
                _owner.OnGameFinished(rate);
            }
        }
    }

    // 행인 표시 수를 맞추고 자리 옮기기
    void Refresh(PromoGame game)
    {
        // 늘어난 만큼 새로 만들고 (행인 프리팹 안 꽂아뒀으면 그림만 없이 진행)
        while (personPrefab != null && track != null && _marks.Count < game.peoplePos.Count)
        {
            RectTransform mark = Instantiate(personPrefab, track);
            _marks.Add(mark);
        }

        // 줄어든 만큼 지움
        while (_marks.Count > game.peoplePos.Count)
        {
            int last = _marks.Count - 1;
            Destroy(_marks[last].gameObject);
            _marks.RemoveAt(last);
        }

        float width = 0f;
        if (track != null)
        {
            width = track.rect.width;
        }

        for (int i = 0; i < _marks.Count && i < game.peoplePos.Count; i++)
        {
            RectTransform mark = _marks[i];
            if (mark == null)
            {
                continue;
            }

            Vector2 p = mark.anchoredPosition;
            p.x = (game.peoplePos[i] - 0.5f) * width;
            mark.anchoredPosition = p;

            // 황금 행인은 크게 해서 눈에 띄게
            float scale = 1f;
            if (game.peopleGolden[i] == true)
            {
                scale = goldenScale;
            }
            mark.localScale = new Vector3(scale, scale, 1f);
        }

        if (timeText != null)
        {
            timeText.text = "남은 시간 " + Mathf.CeilToInt(game.timeLeft) + "초";
        }

        if (countText != null)
        {
            countText.text = "나눠줌 " + game.handedCount + " / " + game.totalPeople
                + "    황금 " + game.goldenCount
                + "    헛던짐 " + game.missCount;
        }
    }

    // 던지면 닿는 구간을 길 위에 표시해둠 (한 번만)
    void PlaceZone()
    {
        PromoGame game = GetGame();
        if (zoneMark == null || track == null || game == null)
        {
            return;
        }

        float width = track.rect.width;
        float center = (game.throwMin + game.throwMax) / 2f;

        Vector2 p = zoneMark.anchoredPosition;
        p.x = (center - 0.5f) * width;
        zoneMark.anchoredPosition = p;

        Vector2 size = zoneMark.sizeDelta;
        size.x = (game.throwMax - game.throwMin) * width;
        zoneMark.sizeDelta = size;
    }

    // 행인 표시 전부 치우기
    void ClearMarks()
    {
        for (int i = 0; i < _marks.Count; i++)
        {
            if (_marks[i] != null)
            {
                Destroy(_marks[i].gameObject);
            }
        }
        _marks.Clear();
    }

    // 지금 도는 판촉 규칙 (없으면 null)
    PromoGame GetGame()
    {
        if (GameManager.instance == null)
        {
            return null;
        }

        return GameManager.instance.arbeit.promo;
    }

    // 그만두기 버튼 OnClick 에 연결 (알바 횟수는 돌려받음)
    public void OnClickQuit()
    {
        gameObject.SetActive(false);

        if (_owner != null)
        {
            _owner.ShowList();
        }
    }

    // 던지기 버튼 OnClick 에 연결
    public void OnClickThrow()
    {
        PromoGame game = GetGame();
        if (game == null || game.IsDone() == true)
        {
            return;
        }

        game.Throw();
    }
}
