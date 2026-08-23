using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 극단 알바 화면 - 조명을 좌우로 움직여서 무대 위 배우를 계속 비춤
// 좌/우 버튼에는 SpotlightHoldButton 을 붙여둠 (누르고 있는 동안 계속 움직이게)
// 이 프로젝트는 새 Input System 이라 키보드 Input.GetKey 는 못 씀
public class SpotlightView : MonoBehaviour
{
    [Header("무대")]
    [SerializeField] RectTransform track;     // 배우가 돌아다니는 가로 범위
    [SerializeField] RectTransform actorMark; // 배우
    [SerializeField] RectTransform lightMark; // 조명 빛줄기
    [SerializeField] Image actorBody;         // 배우 몸통 (황금 배우면 색이 바뀜)
    [SerializeField] Image lightBeam;         // 빛줄기 그림 (맞추면 밝아짐)

    [Header("위/아래 줄")]
    [SerializeField] Image timerFill;      // 남은 시간 막대 (Filled 이미지)
    [SerializeField] TMP_Text limitText;   // "제한 시간: N초"
    [SerializeField] TMP_Text stateText;   // "비추는 중" / "놓치는 중"

    [Header("색")]
    [SerializeField] Color actorColor = new Color(0.78f, 0.82f, 0.92f);
    [SerializeField] Color goldenColor = new Color(0.98f, 0.80f, 0.28f);
    [SerializeField] Color beamOn = new Color(1f, 0.88f, 0.48f, 0.42f);
    [SerializeField] Color beamOff = new Color(1f, 0.96f, 0.86f, 0.14f);

    ArbeitView _owner;
    int _holdDir = 0; // -1 왼쪽, 0 가만히, 1 오른쪽

    // 알바 화면이 불러줌
    public void Play(ArbeitView owner)
    {
        _owner = owner;
        _holdDir = 0;
        gameObject.SetActive(true);

        SpotlightGame game = GetGame();
        if (game == null)
        {
            return;
        }

        // 빛줄기 폭을 맞는 걸로 쳐주는 범위랑 같게 해둠 (눈으로 보고 맞출 수 있어야 함)
        if (lightMark != null && track != null)
        {
            Vector2 size = lightMark.sizeDelta;
            size.x = game.hitRange * 2f * track.rect.width;
            lightMark.sizeDelta = size;
        }

        if (limitText != null)
        {
            limitText.text = "제한 시간: " + Mathf.RoundToInt(game.duration) + "초";
        }

        Refresh(game);
    }

    void Update()
    {
        SpotlightGame game = GetGame();
        if (game == null || _owner == null)
        {
            return;
        }

        game.Tick(Time.deltaTime, _holdDir);
        Refresh(game);

        if (game.IsDone() == true)
        {
            Finish(game);
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

        bool onTarget = Mathf.Abs(game.actorPos - game.lightPos) <= game.hitRange;

        if (actorBody != null)
        {
            actorBody.color = (game.actorGolden == true) ? goldenColor : actorColor;
        }
        if (lightBeam != null)
        {
            lightBeam.color = onTarget ? beamOn : beamOff;
        }

        if (timerFill != null && game.duration > 0f)
        {
            timerFill.fillAmount = Mathf.Clamp01(game.timeLeft / game.duration);
        }

        if (stateText != null)
        {
            string state = onTarget ? "비추는 중……" : "놓치는 중……";
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

    // 시간 다 됐으면 수행률 넘기고 화면 닫기
    void Finish(SpotlightGame game)
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
        ArbeitView owner = _owner;
        _owner = null;

        gameObject.SetActive(false);

        if (owner != null)
        {
            owner.ShowList();
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
