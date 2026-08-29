using UnityEngine;

// 극단 알바 - 조명 연출 미니게임 규칙 (화면은 따로, 여기는 규칙만)
// 무대 위 배우를 조명으로 계속 비추고 있으면 점수가 참
// 배우는 랜덤한 속도로 걸어다니다가, 가끔 아예 다음 배우로 바뀜 (무대 반대편에서 등장)
public class SpotlightGame
{
    public float actorPos = 0.5f;   // 배우 위치 (0~1)
    public float lightPos = 0.5f;   // 조명 위치 (0~1)
    public float timeLeft = 0f;     // 남은 시간
    public float duration = 20f;    // 전체 시간
    public float onTargetTime = 0f; // 배우를 비추고 있던 시간

    // 아래 수치는 돌려보면서 맞춘 것
    // 조명이 배우보다 무작정 빠르면 그냥 따라다니면 되는 게임이 돼서, 차이를 좁혀둠
    // 돌려보니 잘하면 95%, 보통 71%, 처음이면 61% 쯤 나옴 (기획서 80/60/40 계단에 셋 다 걸림)
    public float lightSpeed = 0.48f;    // 조명이 초당 움직이는 거리
    public float actorSpeedMin = 0.19f; // 배우가 느리게 걸을 때
    public float actorSpeedMax = 0.45f; // 배우가 빠르게 걸을 때
    public float hitRange = 0.076f;     // 이 거리 안이면 비추는 걸로 침

    public bool actorGolden = false; // 지금 배우가 황금 배우인지 (비추면 두 배)
    public float goldenPerGame = 1f; // 한 판에 나올 황금 배우 기대 횟수

    public float swapMin = 1.2f;   // 배우가 다음 행동을 정하는 간격
    public float swapMax = 2.6f;
    public float jumpChance = 0.4f; // 이 확률로 다음 배우로 교체 (아니면 그냥 걸어감)

    // 전체의 이만큼 비추면 만점 (놓치는 순간이 아예 없을 수는 없어서 조금 여유를 둠)
    // 쫓아다니기는 쉽게 풀어주되 만점 기준은 올려둠 - 100% 는 되긴 하되 어렵게
    public float fullMarkAt = 0.96f;

    public bool justSwapped = false; // 방금 배우가 바뀌었는지 (화면이 연출에 씀)

    float _actorTarget = 0.5f; // 배우가 걸어가는 목표 지점
    float _actorSpeed = 0.25f; // 이번에 걷는 빠르기
    float _nextMoveIn = 0f;    // 배우가 다음 행동을 정할 때까지 남은 시간

    // 게임 준비
    public void StartGame(float seconds)
    {
        duration = seconds;
        timeLeft = seconds;
        onTargetTime = 0f;
        actorPos = 0.5f;
        lightPos = 0.5f;
        _actorTarget = 0.5f;
        _actorSpeed = actorSpeedMin;
        _nextMoveIn = 0.8f;
        actorGolden = false;
        justSwapped = false;
    }

    // 한 프레임 진행. holdDir: -1 왼쪽으로, 0 가만히, 1 오른쪽으로
    public void Tick(float dt, int holdDir)
    {
        justSwapped = false;

        if (IsDone() == true)
        {
            return;
        }

        timeLeft -= dt;

        // 배우가 다음 행동을 정할 때
        _nextMoveIn -= dt;
        if (_nextMoveIn <= 0f)
        {
            NextActorMove();
        }

        actorPos = Mathf.MoveTowards(actorPos, _actorTarget, _actorSpeed * dt);

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

    // 배우가 다음에 뭘 할지 정하기 (걸어가거나, 다음 배우로 바뀌거나)
    void NextActorMove()
    {
        _nextMoveIn = Random.Range(swapMin, swapMax);
        _actorSpeed = Random.Range(actorSpeedMin, actorSpeedMax);
        actorGolden = Random.value < GoldenChance();

        if (Random.value < jumpChance)
        {
            // 다음 배우로 교체 - 지금 자리에서 먼 데서 새로 나옴 (조명을 크게 옮겨야 함)
            float away = (actorPos < 0.5f) ? Random.Range(0.6f, 0.95f) : Random.Range(0.05f, 0.4f);
            actorPos = away;
            _actorTarget = Random.value;
            justSwapped = true;
            return;
        }

        // 그냥 걸어감
        _actorTarget = Random.value;
    }

    // 배우가 바뀔 때마다 굴리니까, 한 판에 몇 번 바뀌는지로 나눠야 한 번쯤 나옴
    float GoldenChance()
    {
        float avgSwap = (swapMin + swapMax) / 2f;
        if (duration <= 0f || avgSwap <= 0f)
        {
            return 0f;
        }

        float swapCount = duration / avgSwap;
        if (swapCount < 1f)
        {
            swapCount = 1f;
        }

        return goldenPerGame / swapCount;
    }

    // 지금 배우를 비추고 있는지 (화면이 표시에 씀)
    public bool OnTarget()
    {
        return Mathf.Abs(actorPos - lightPos) <= hitRange;
    }

    // 시간 다 됐는지
    public bool IsDone()
    {
        return timeLeft <= 0f;
    }

    // 수행률
    public float GetRate()
    {
        if (duration <= 0f || fullMarkAt <= 0f)
        {
            return 0f;
        }
        return Mathf.Clamp01(onTargetTime / (duration * fullMarkAt));
    }
}
