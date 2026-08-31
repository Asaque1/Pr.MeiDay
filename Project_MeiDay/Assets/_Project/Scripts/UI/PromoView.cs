using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 파티용품점 알바 화면 - 길을 돌아다니는 행인을 겨눠서 전단지를 던짐
// 길 아무 데나 누르면 그 자리로 전단지가 날아감. 날아가는 동안 행인도 걸으니까 갈 자리를 노려야 함
public class PromoView : MonoBehaviour, IPointerDownHandler
{
    [Header("길")]
    [SerializeField] RectTransform field;         // 행인이 돌아다니는 판 (여기 안을 눌러야 던져짐)
    [SerializeField] RectTransform personPrefab;  // 행인 표시 하나 (꺼둔 채로 두면 됨)
    [SerializeField] RectTransform flyerPrefab;   // 날아가는 전단지 표시 (꺼둔 채로 두면 됨)
    [SerializeField] RectTransform flyerBox;      // 전단지 상자 (여기서 날아감. 없어도 됨)

    [Header("위/아래 줄")]
    [SerializeField] Image timerFill;       // 남은 시간 막대 (Filled 이미지)
    [SerializeField] TMP_Text limitText;    // "제한 시간: N초"
    [SerializeField] TMP_Text progressText; // "0/6"
    [SerializeField] TMP_Text countText;    // 황금/헛던짐 (안 꽂아둬도 됨)
    [SerializeField] TMP_Text guideText;    // 처음에만 나오는 안내 (안 꽂아둬도 됨)

    [Header("모양")]
    [SerializeField] float goldenScale = 1.3f;
    [SerializeField] Color personColor = new Color(0.78f, 0.82f, 0.92f);
    [SerializeField] Color goldenColor = new Color(0.98f, 0.80f, 0.28f);
    [SerializeField] float guideShowTime = 2.5f; // 안내를 보여주는 시간

    ArbeitView _owner;
    float _elapsed = 0f;

    // 행인/전단지마다 표시를 따로 물려둠
    // 예전엔 번호로 짝지어서, 가운데 행인이 사라지면 뒤 행인이 순간이동했음
    Dictionary<PromoPerson, RectTransform> _personMarks = new Dictionary<PromoPerson, RectTransform>();
    Dictionary<PromoFlyer, RectTransform> _flyerMarks = new Dictionary<PromoFlyer, RectTransform>();
    List<PromoPerson> _gonePeople = new List<PromoPerson>();
    List<PromoFlyer> _goneFlyers = new List<PromoFlyer>();

    // 알바 화면이 불러줌
    public void Play(ArbeitView owner)
    {
        _owner = owner;
        _elapsed = 0f;
        gameObject.SetActive(true);

        if (field == null || personPrefab == null)
        {
            Debug.LogWarning("판촉 화면에 길이나 행인 표시가 연결 안 됨 - 글자만 나옴");
        }

        ClearMarks();

        PromoGame game = GetGame();
        if (game != null)
        {
            // 판이 가로로 길어서, 맞았는지 잴 때 화면 비율을 알려줘야 동그랗게 맞음
            if (field != null && field.rect.height > 0f)
            {
                game.aspect = field.rect.width / field.rect.height;
            }

            // 전단지가 날아오는 자리를 상자 위치에 맞춰줌
            game.throwFrom = BoxSpot();

            if (limitText != null)
            {
                limitText.text = "남은 시간: " + Mathf.RoundToInt(game.duration) + "초";
            }
        }

        if (guideText != null)
        {
            guideText.text = "행인을 눌러 전단지를 던지세요";
            guideText.gameObject.SetActive(true);
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

        RefreshPeople(game);
        RefreshFlyers(game);
        RefreshTexts(game);
        FadeGuide();

        game.ClearLandedFlyers(); // 화면에 한 번 보여줬으니 이제 치워도 됨

        if (game.IsDone() == true)
        {
            Finish(game);
        }
    }

    // 길 안 눌렀을 때 그 자리로 던지기
    void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
    {
        PromoGame game = GetGame();
        if (game == null || _owner == null || field == null)
        {
            return;
        }

        if (game.CanThrow() == false)
        {
            return; // 아직 쉬는 중
        }

        Vector2 local;
        bool inside = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            field, eventData.position, eventData.pressEventCamera, out local);
        if (inside == false)
        {
            return;
        }

        // 길 밖을 눌렀으면 던지지 않음
        Rect r = field.rect;
        if (r.Contains(local) == false)
        {
            return;
        }

        Vector2 aim = new Vector2(
            (local.x - r.xMin) / r.width,
            (local.y - r.yMin) / r.height);

        game.Throw(aim);

        if (SoundManager.instance != null)
        {
            SoundManager.instance.PlaySfx(SoundManager.instance.cardPick);
        }
    }

    // 행인 표시 만들고 옮기기
    void RefreshPeople(PromoGame game)
    {
        if (field == null || personPrefab == null)
        {
            return;
        }

        // 새로 나온 행인은 표시를 만들어줌
        for (int i = 0; i < game.people.Count; i++)
        {
            PromoPerson p = game.people[i];
            if (_personMarks.ContainsKey(p) == true)
            {
                continue;
            }

            RectTransform mark = Instantiate(personPrefab, field);
            mark.gameObject.SetActive(true);
            _personMarks.Add(p, mark);

            // 황금 행인은 크고 노랗게
            float scale = p.golden ? goldenScale : 1f;
            mark.localScale = new Vector3(scale, scale, 1f);
            Image[] parts = mark.GetComponentsInChildren<Image>(true);
            for (int j = 0; j < parts.Length; j++)
            {
                parts[j].color = p.golden ? goldenColor : personColor;
            }
        }

        // 지나갔거나 전단지를 받은 행인은 표시를 치움
        _gonePeople.Clear();
        foreach (KeyValuePair<PromoPerson, RectTransform> pair in _personMarks)
        {
            if (game.people.Contains(pair.Key) == false)
            {
                _gonePeople.Add(pair.Key);
            }
        }
        for (int i = 0; i < _gonePeople.Count; i++)
        {
            RectTransform mark = _personMarks[_gonePeople[i]];
            _personMarks.Remove(_gonePeople[i]);
            if (mark != null)
            {
                Destroy(mark.gameObject);
            }
        }

        // 남은 행인은 제자리로
        foreach (KeyValuePair<PromoPerson, RectTransform> pair in _personMarks)
        {
            if (pair.Value != null)
            {
                pair.Value.anchoredPosition = FieldSpot(pair.Key.pos);
            }
        }
    }

    // 날아가는 전단지 표시
    void RefreshFlyers(PromoGame game)
    {
        if (field == null || flyerPrefab == null)
        {
            return;
        }

        for (int i = 0; i < game.flyers.Count; i++)
        {
            PromoFlyer f = game.flyers[i];
            if (_flyerMarks.ContainsKey(f) == true)
            {
                continue;
            }

            RectTransform mark = Instantiate(flyerPrefab, field);
            mark.gameObject.SetActive(true);
            _flyerMarks.Add(f, mark);
        }

        _goneFlyers.Clear();
        foreach (KeyValuePair<PromoFlyer, RectTransform> pair in _flyerMarks)
        {
            PromoFlyer f = pair.Key;
            RectTransform mark = pair.Value;

            if (game.flyers.Contains(f) == false)
            {
                _goneFlyers.Add(f);
                continue;
            }

            if (mark == null)
            {
                continue;
            }

            // 던진 자리에서 노린 자리로 곧게 날아감
            float t = 1f;
            if (f.flyTime > 0f)
            {
                t = Mathf.Clamp01(f.flown / f.flyTime);
            }
            Vector2 at = Vector2.Lerp(f.from, f.to, t);
            mark.anchoredPosition = FieldSpot(at);

            // 닿았으면 맞았는지에 따라 잠깐 표시가 달라짐
            if (f.done == true)
            {
                mark.localScale = new Vector3(1.4f, 1.4f, 1f);
                Image[] parts = mark.GetComponentsInChildren<Image>(true);
                for (int j = 0; j < parts.Length; j++)
                {
                    parts[j].color = f.hit ? goldenColor : new Color(1f, 1f, 1f, 0.35f);
                }

                if (f.hit == true && SoundManager.instance != null)
                {
                    SoundManager.instance.PlaySfx(SoundManager.instance.docAccept);
                }
            }
        }

        for (int i = 0; i < _goneFlyers.Count; i++)
        {
            RectTransform mark = _flyerMarks[_goneFlyers[i]];
            _flyerMarks.Remove(_goneFlyers[i]);
            if (mark != null)
            {
                Destroy(mark.gameObject);
            }
        }
    }

    void RefreshTexts(PromoGame game)
    {
        if (timerFill != null && game.duration > 0f)
        {
            UiBar.Set(timerFill, game.timeLeft / game.duration); // 폭을 줄임 - Filled 로 깎으면 둥근 끝이 눌림
        }

        // 숫자도 게이지랑 같이 깎임 (올림이라 시작하자마자 제한 시간 그대로 보이고 0 에서 끝남)
        if (limitText != null)
        {
            int left = Mathf.CeilToInt(game.timeLeft);
            if (left < 0)
            {
                left = 0;
            }
            limitText.text = "남은 시간: " + left + "초";
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

    // 안내 글은 잠깐만 보여줌
    void FadeGuide()
    {
        if (guideText == null)
        {
            return;
        }

        if (_elapsed >= guideShowTime && guideText.gameObject.activeSelf == true)
        {
            guideText.gameObject.SetActive(false);
        }
    }

    // 0~1 자리를 판 위 좌표로
    Vector2 FieldSpot(Vector2 normal)
    {
        if (field == null)
        {
            return Vector2.zero;
        }

        Rect r = field.rect;
        return new Vector2(r.xMin + normal.x * r.width, r.yMin + normal.y * r.height);
    }

    // 전단지 상자가 판 기준으로 어디쯤인지 (0~1)
    Vector2 BoxSpot()
    {
        if (field == null || flyerBox == null)
        {
            return new Vector2(0.5f, -0.15f);
        }

        Vector3 world = flyerBox.TransformPoint(Vector3.zero);
        Vector3 local = field.InverseTransformPoint(world);
        Rect r = field.rect;
        if (r.width <= 0f || r.height <= 0f)
        {
            return new Vector2(0.5f, -0.15f);
        }

        return new Vector2((local.x - r.xMin) / r.width, (local.y - r.yMin) / r.height);
    }

    // 표시 전부 치우기
    void ClearMarks()
    {
        foreach (KeyValuePair<PromoPerson, RectTransform> pair in _personMarks)
        {
            if (pair.Value != null)
            {
                Destroy(pair.Value.gameObject);
            }
        }
        _personMarks.Clear();

        foreach (KeyValuePair<PromoFlyer, RectTransform> pair in _flyerMarks)
        {
            if (pair.Value != null)
            {
                Destroy(pair.Value.gameObject);
            }
        }
        _flyerMarks.Clear();
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

    // 그만두기 버튼 OnClick 에 연결
    // 그냥 나가지 않고 한 만큼으로 정산함 (아직 못 한 건 전부 틀린 걸로 침)
    public void OnClickQuit()
    {
        Finish(GetGame());
    }
}
