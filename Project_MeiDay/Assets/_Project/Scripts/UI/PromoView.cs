using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 파티용품점 알바 화면 - 지나가는 행인이 구간에 들어왔을 때 전단지를 던짐
// 행인 표시는 track 의 자식으로 만들어짐 (personPrefab 을 인스펙터에 넣을 것)
public class PromoView : MonoBehaviour
{
    [Header("길")]
    [SerializeField] RectTransform track;        // 길 (이 폭 안에서 행인이 걸어감)
    [SerializeField] RectTransform personPrefab; // 행인 표시 하나
    [SerializeField] Image zoneMark;             // 던지면 닿는 구간 표시 (없어도 됨)

    [Header("위/아래 줄")]
    [SerializeField] Image timerFill;       // 남은 시간 막대 (Filled 이미지)
    [SerializeField] TMP_Text limitText;    // "제한 시간: N초"
    [SerializeField] TMP_Text progressText; // "0/6"
    [SerializeField] TMP_Text countText;    // 황금/헛던짐 (안 꽂아둬도 됨)

    [Header("모양")]
    [SerializeField] float goldenScale = 1.35f; // 황금 행인은 좀 크게 그림
    [SerializeField] Color personColor = new Color(0.78f, 0.82f, 0.92f);
    [SerializeField] Color goldenColor = new Color(0.98f, 0.80f, 0.28f);

    // 구간 표시는 처음 몇 초만 보여주고 지움 (계속 켜두면 눈으로만 찍게 됨)
    [SerializeField] float zoneShowTime = 3f;
    [SerializeField] float zoneFadeTime = 1f;

    ArbeitView _owner;
    List<RectTransform> _marks = new List<RectTransform>(); // 지금 화면에 있는 행인 표시들
    float _elapsed = 0f;
    Color _zoneColor;
    bool _zoneColorRead = false;

    // 구간 색은 딱 한 번만 기억해둠
    // 판이 끝날 때 투명해지니까, 시작할 때 읽으면 두 번째 판부터 안 보임
    void Awake()
    {
        ReadZoneColor();
    }

    void ReadZoneColor()
    {
        if (_zoneColorRead == true || zoneMark == null)
        {
            return;
        }

        _zoneColor = zoneMark.color;
        _zoneColorRead = true;
    }

    // 알바 화면이 불러줌
    public void Play(ArbeitView owner)
    {
        _owner = owner;
        _elapsed = 0f;
        gameObject.SetActive(true);

        if (personPrefab == null || track == null)
        {
            Debug.LogWarning("판촉 화면에 행인 프리팹이나 길이 연결 안 됨 - 글자만 나옴");
        }

        ReadZoneColor();
        ClearMarks();
        PlaceZone();

        PromoGame game = GetGame();
        if (game != null && limitText != null)
        {
            limitText.text = "제한 시간: " + Mathf.RoundToInt(game.duration) + "초";
        }
    }

    void OnDisable()
    {
        ClearMarks(); // 화면 닫힐 때 남은 표시 정리
    }

    void Update()
    {
        PromoGame game = GetGame();
        if (game == null || _owner == null)
        {
            return;
        }

        _elapsed += Time.deltaTime;
        game.Tick(Time.deltaTime);
        Refresh(game);
        FadeZone();

        if (game.IsDone() == true)
        {
            Finish(game);
        }
    }

    // 행인 표시 수를 맞추고 자리 옮기기
    void Refresh(PromoGame game)
    {
        // 늘어난 만큼 새로 만들고 (행인 프리팹 안 꽂아뒀으면 그림만 없이 진행)
        while (personPrefab != null && track != null && _marks.Count < game.peoplePos.Count)
        {
            RectTransform mark = Instantiate(personPrefab, track);
            mark.gameObject.SetActive(true);
            _marks.Add(mark);
        }

        // 줄어든 만큼 지움
        while (_marks.Count > game.peoplePos.Count)
        {
            int last = _marks.Count - 1;
            RemoveMark(last);
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

            // 황금 행인은 크고 노랗게 해서 눈에 띄게
            bool golden = game.peopleGolden[i];
            float scale = golden ? goldenScale : 1f;
            mark.localScale = new Vector3(scale, scale, 1f);

            Image[] parts = mark.GetComponentsInChildren<Image>(true);
            for (int j = 0; j < parts.Length; j++)
            {
                parts[j].color = golden ? goldenColor : personColor;
            }
        }

        if (timerFill != null && game.duration > 0f)
        {
            timerFill.fillAmount = Mathf.Clamp01(game.timeLeft / game.duration);
        }
        if (progressText != null)
        {
            progressText.text = game.handedCount + "/" + game.totalPeople;
        }
        if (countText != null)
        {
            countText.text = "황금 " + game.goldenCount + "    헛던짐 " + game.missCount;
        }
    }

    // 구간 표시를 서서히 지움
    void FadeZone()
    {
        if (zoneMark == null || zoneFadeTime <= 0f)
        {
            return;
        }

        float over = _elapsed - zoneShowTime;
        if (over <= 0f)
        {
            return;
        }

        float left = 1f - over / zoneFadeTime;
        if (left < 0f)
        {
            left = 0f;
        }

        Color c = _zoneColor;
        c.a = _zoneColor.a * left;
        zoneMark.color = c;
    }

    // 던지면 닿는 구간을 길 위에 표시해둠 (한 번만)
    void PlaceZone()
    {
        PromoGame game = GetGame();
        if (zoneMark == null || track == null || game == null)
        {
            return;
        }

        RectTransform zone = zoneMark.rectTransform;
        float width = track.rect.width;
        float center = (game.throwMin + game.throwMax) / 2f;

        Vector2 p = zone.anchoredPosition;
        p.x = (center - 0.5f) * width;
        zone.anchoredPosition = p;

        Vector2 size = zone.sizeDelta;
        size.x = (game.throwMax - game.throwMin) * width;
        zone.sizeDelta = size;

        zoneMark.color = _zoneColor;
    }

    // 행인 표시 하나 치우기 (같은 프레임에 또 세지 않게 먼저 끔)
    void RemoveMark(int index)
    {
        RectTransform mark = _marks[index];
        _marks.RemoveAt(index);

        if (mark != null)
        {
            mark.gameObject.SetActive(false);
            Destroy(mark.gameObject);
        }
    }

    // 행인 표시 전부 치우기
    void ClearMarks()
    {
        for (int i = _marks.Count - 1; i >= 0; i--)
        {
            RemoveMark(i);
        }
    }

    // 시간 다 됐으면 수행률 넘기고 화면 닫기
    void Finish(PromoGame game)
    {
        float rate = 0f;
        if (game != null)
        {
            rate = game.GetRate();
        }

        ArbeitView owner = _owner;
        _owner = null; // Update 가 한 번 더 부르지 않게 먼저 끊음

        gameObject.SetActive(false);

        if (owner != null)
        {
            owner.OnGameFinished(rate);
        }
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
        ArbeitView owner = _owner;
        _owner = null;

        gameObject.SetActive(false);

        if (owner != null)
        {
            owner.ShowList();
        }
    }

    // 전단지 버튼 OnClick 에 연결
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
