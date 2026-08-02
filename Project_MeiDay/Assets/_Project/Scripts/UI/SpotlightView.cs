using TMPro;
using UnityEngine;

// 극단 알바 화면 - 조명을 좌우로 움직여서 배우를 계속 비춤
// 좌/우 버튼은 EventTrigger 로 연결할 것
//   PointerDown -> OnPressLeft / OnPressRight,  PointerUp -> OnRelease
//   (이 프로젝트는 새 Input System 이라 키보드 Input.GetKey 는 못 씀)
// 배우/조명 표시는 track 의 자식으로 넣고 가운데 기준으로 두면 됨
public class SpotlightView : MonoBehaviour
{
    [SerializeField] RectTransform track;     // 무대 가로줄 (이 폭 안에서 움직임)
    [SerializeField] RectTransform actorMark; // 배우 표시
    [SerializeField] RectTransform lightMark; // 조명 표시
    [SerializeField] TMP_Text timeText;       // 남은 시간
    [SerializeField] TMP_Text stateText;      // 비추는 중인지 / 황금인지

    ArbeitView _owner;
    int _holdDir = 0; // -1 왼쪽, 0 가만히, 1 오른쪽

    // 알바 화면이 불러줌
    public void Play(ArbeitView owner)
    {
        _owner = owner;
        _holdDir = 0;
        gameObject.SetActive(true);
    }

    void Update()
    {
        SpotlightGame game = GetGame();
        if (game == null)
        {
            return;
        }

        game.Tick(Time.deltaTime, _holdDir);
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

    // 배우랑 조명 자리 옮기고 글자 갱신
    void Refresh(SpotlightGame game)
    {
        float width = 0f;
        if (track != null)
        {
            width = track.rect.width;
        }

        PlaceOnTrack(actorMark, game.actorPos, width);
        PlaceOnTrack(lightMark, game.lightPos, width);

        if (timeText != null)
        {
            timeText.text = "남은 시간 " + Mathf.CeilToInt(game.timeLeft) + "초";
        }

        if (stateText != null)
        {
            bool onTarget = Mathf.Abs(game.actorPos - game.lightPos) <= game.hitRange;
            string state = "놓치는 중";
            if (onTarget == true)
            {
                state = "비추는 중";
            }
            if (game.actorGolden == true)
            {
                state += "   [황금 배우]";
            }
            stateText.text = state;
        }
    }

    // 0~1 위치를 무대 가로줄 위 좌표로 바꿔서 놓기
    void PlaceOnTrack(RectTransform mark, float pos, float width)
    {
        if (mark == null)
        {
            return;
        }

        Vector2 p = mark.anchoredPosition;
        p.x = (pos - 0.5f) * width;
        mark.anchoredPosition = p;
    }

    // 지금 도는 조명 규칙 (없으면 null)
    SpotlightGame GetGame()
    {
        if (GameManager.instance == null)
        {
            return null;
        }

        return GameManager.instance.arbeit.spotlight;
    }

    // ---- 아래는 좌우 버튼 EventTrigger 에 연결해서 씀 ----

    // 그만두기 버튼 OnClick 에 연결 (알바 횟수는 돌려받음)
    public void OnClickQuit()
    {
        gameObject.SetActive(false);

        if (_owner != null)
        {
            _owner.ShowList();
        }
    }

    // 왼쪽 버튼 누르고 있는 동안
    public void OnPressLeft()
    {
        _holdDir = -1;
    }

    // 오른쪽 버튼 누르고 있는 동안
    public void OnPressRight()
    {
        _holdDir = 1;
    }

    // 버튼에서 손 뗐을 때 (좌우 둘 다 여기로)
    public void OnRelease()
    {
        _holdDir = 0;
    }
}
