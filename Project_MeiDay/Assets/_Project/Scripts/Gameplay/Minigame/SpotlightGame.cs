using UnityEngine;

// 극단 알바 - 조명 연출 미니게임 규칙 (화면은 따로, 여기는 규칙만)
// 무대 위를 돌아다니는 배우를 조명으로 계속 비추고 있으면 점수가 참
public class SpotlightGame
{
    public float actorPos = 0.5f;   // 배우 위치 (0~1)
    public float lightPos = 0.5f;   // 조명 위치 (0~1)
    public float timeLeft = 0f;     // 남은 시간
    public float duration = 20f;    // 전체 시간
    public float onTargetTime = 0f; // 배우를 비추고 있던 시간

    public float lightSpeed = 0.6f;  // 조명이 초당 움직이는 거리
    public float actorSpeed = 0.25f; // 배우가 걷는 속도
    public float hitRange = 0.12f;   // 이 거리 안이면 비추는 걸로 침

    public bool actorGolden = false;   // 지금 배우가 황금 배우인지 (비추면 두 배)
    public float goldenChance = 0.15f; // 황금 배우가 나올 확률

    float _actorTarget = 0.5f; // 배우가 걸어가는 목표 지점
    float _nextMoveIn = 0f;    // 배우가 다음 목표를 정할 때까지 남은 시간

    // 게임 준비
    public void StartGame(float seconds)
    {
        duration = seconds;
        timeLeft = seconds;
        onTargetTime = 0f;
        actorPos = 0.5f;
        lightPos = 0.5f;
        _actorTarget = 0.5f;
        _nextMoveIn = 0f;
    }

    // 한 프레임 진행. holdDir: -1 왼쪽으로, 0 가만히, 1 오른쪽으로
    public void Tick(float dt, int holdDir)
    {
        if (IsDone() == true)
        {
            return;
        }

        timeLeft -= dt;

        // 배우는 목표 지점으로 걷고, 가끔 목표를 확 바꿈 (다음 배우로 교체되는 셈)
        _nextMoveIn -= dt;
        if (_nextMoveIn <= 0f)
        {
            _actorTarget = Random.value;
            _nextMoveIn = Random.Range(2f, 4f);
            actorGolden = Random.value < goldenChance; // 가끔 황금 배우 등장
        }
        actorPos = Mathf.MoveTowards(actorPos, _actorTarget, actorSpeed * dt);

        // 조명은 잡고 있는 방향으로 움직임
        lightPos = Mathf.Clamp01(lightPos + holdDir * lightSpeed * dt);

        // 비추고 있으면 점수가 참 (황금 배우는 두 배)
        if (Mathf.Abs(actorPos - lightPos) <= hitRange)
        {
            if (actorGolden == true)
            {
                onTargetTime += dt * 2f;
            }
            else
            {
                onTargetTime += dt;
            }
        }
    }

    // 시간 다 됐는지
    public bool IsDone()
    {
        return timeLeft <= 0f;
    }

    // 수행률 (전체의 8할만 비춰도 만점으로 쳐줌)
    public float GetRate()
    {
        if (duration <= 0f)
        {
            return 0f;
        }
        return Mathf.Clamp01(onTargetTime / (duration * 0.8f));
    }
}
