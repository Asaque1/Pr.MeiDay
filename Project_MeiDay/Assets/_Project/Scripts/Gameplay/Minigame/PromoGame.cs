using System.Collections.Generic;
using UnityEngine;

// 길을 돌아다니는 행인 하나
public class PromoPerson
{
    public Vector2 pos;      // 길 안에서의 자리 (가로세로 0~1)
    public Vector2 dir;      // 걸어가는 방향
    public float speed;      // 걷는 빠르기
    public bool golden;      // 황금 행인인지 (보너스)
}

// 던져놓고 아직 날아가는 중인 전단지 하나
public class PromoFlyer
{
    public Vector2 from;   // 던진 자리
    public Vector2 to;     // 노린 자리
    public float flown;    // 날아간 시간
    public float flyTime;  // 닿는 데 걸리는 시간
    public bool hit;       // 맞았는지 (닿은 뒤에 정해짐)
    public bool done;      // 닿았는지
}

// 파티용품점 알바 - 판촉 미니게임 규칙 (화면은 따로, 여기는 규칙만)
// 길을 돌아다니는 행인을 겨눠서 전단지를 던짐. 전단지가 날아가는 동안 행인도 걸으니까
// 지금 있는 자리가 아니라 갈 자리를 노려야 맞음. 황금 행인은 보너스
public class PromoGame
{
    public float timeLeft = 0f;  // 남은 시간
    public float duration = 20f; // 전체 시간

    public int handedCount = 0; // 잘 쥐여준 수
    public int goldenCount = 0; // 잘 쥐여준 황금 행인 수 (보너스, 최대치엔 안 셈)
    public int missCount = 0;   // 헛던진 수
    public int totalPeople = 0; // 나온 일반 행인 수 (수행률의 최대치)

    public List<PromoPerson> people = new List<PromoPerson>();
    public List<PromoFlyer> flyers = new List<PromoFlyer>();

    // 아래 수치는 돌려보면서 맞춘 것
    // 잘하면 98%, 보통 83%, 처음이면 58% 쯤 나옴 (기획서 80/60/40 계단에 셋 다 걸리게)
    // 100% 는 되긴 하는데 세 판에 한 번쯤임 - 끝물에 나온 행인은 손쓸 새가 없어서
    // 손이 빠른 것만으로는 안 되고 겨눠야 하니까, 사람 나오는 간격이 난이도의 핵심임
    public float speedMin = 0.16f;     // 행인이 느릴 때
    public float speedMax = 0.30f;     // 행인이 빠를 때
    public float spawnGapMin = 0.46f;  // 다음 행인이 나오는 간격
    public float spawnGapMax = 0.74f;
    public float goldenPerGame = 1f;   // 한 판에 나올 황금 행인 기대 수

    // 행인 그림 크기(세로 74)에 맞춘 값. 좁게 잡으면 머리를 눌러도 빗나가서 억울함
    // 난이도는 판정폭이 아니라 사람 나오는 속도로 잡는 게 맞음 (넓혀도 결과가 거의 같았음)
    public float hitRadius = 0.2f;      // 이 안에 떨어지면 맞은 걸로 침
    public float flyTime = 0.28f;       // 전단지가 날아가는 시간
    public float throwCooldown = 0.28f; // 다음 던지기까지 쉬는 시간 (연타 막기)

    // 길이 가로로 긴 판이라, 그냥 재면 가로로 납작한 타원으로 맞음
    // 가로를 이 배수만큼 눌러서 화면에서 동그랗게 보이도록 맞춤
    public float aspect = 2.4f;

    // 전단지 상자 자리 (여기서 날아감)
    public Vector2 throwFrom = new Vector2(0.5f, -0.15f);

    float _nextSpawnIn = 0f;
    float _coolLeft = 0f;

    // 게임 준비
    public void StartGame(float seconds)
    {
        duration = seconds;
        timeLeft = seconds;
        handedCount = 0;
        goldenCount = 0;
        missCount = 0;
        totalPeople = 0;
        people.Clear();
        flyers.Clear();
        _nextSpawnIn = 0.3f;
        _coolLeft = 0f;

        // 처음부터 몇 명 걸어다니고 있어야 시작하자마자 던질 게 있음
        for (int i = 0; i < 3; i++)
        {
            Spawn();
            people[people.Count - 1].pos.x = Random.Range(0.2f, 0.8f);
        }
    }

    // 한 프레임 진행
    public void Tick(float dt)
    {
        if (IsDone() == true)
        {
            return;
        }

        timeLeft -= dt;

        if (_coolLeft > 0f)
        {
            _coolLeft -= dt;
        }

        // 행인이 계속 나옴
        _nextSpawnIn -= dt;
        if (_nextSpawnIn <= 0f)
        {
            Spawn();
            _nextSpawnIn = Random.Range(spawnGapMin, spawnGapMax);
        }

        MovePeople(dt);
        MoveFlyers(dt);
    }

    // 행인들 걷게 하고, 길 밖으로 나간 사람은 지움
    void MovePeople(float dt)
    {
        for (int i = people.Count - 1; i >= 0; i--)
        {
            PromoPerson p = people[i];
            p.pos += p.dir * p.speed * dt;

            // 위아래로는 길 안에서만 (가장자리에 닿으면 반대로 꺾음)
            if (p.pos.y < 0.1f || p.pos.y > 0.9f)
            {
                p.dir.y = -p.dir.y;
                p.pos.y = Mathf.Clamp(p.pos.y, 0.1f, 0.9f);
            }

            // 좌우로 벗어나면 그냥 지나간 것
            if (p.pos.x < -0.1f || p.pos.x > 1.1f)
            {
                people.RemoveAt(i);
            }
        }
    }

    // 던진 전단지를 날리고, 닿으면 맞았는지 따짐
    void MoveFlyers(float dt)
    {
        for (int i = flyers.Count - 1; i >= 0; i--)
        {
            PromoFlyer f = flyers[i];
            if (f.done == true)
            {
                continue; // 화면이 한 프레임 보여주고 지워감
            }

            f.flown += dt;
            if (f.flown < f.flyTime)
            {
                continue;
            }

            f.done = true;
            f.hit = Land(f.to);
        }
    }

    // 전단지가 떨어진 자리에 사람이 있었는지
    bool Land(Vector2 spot)
    {
        int best = -1;
        float bestDist = 0f;

        for (int i = 0; i < people.Count; i++)
        {
            float dist = Dist(people[i].pos, spot);
            if (dist > hitRadius)
            {
                continue;
            }

            // 겹쳐 있으면 제일 가까운 사람이 받음
            if (best < 0 || dist < bestDist)
            {
                best = i;
                bestDist = dist;
            }
        }

        if (best < 0)
        {
            missCount++;
            return false;
        }

        if (people[best].golden == true)
        {
            goldenCount++;
        }
        else
        {
            handedCount++;
        }

        people.RemoveAt(best);
        return true;
    }

    // 화면에서 보이는 대로의 거리 (가로가 긴 판이라 가로를 눌러서 잼)
    float Dist(Vector2 a, Vector2 b)
    {
        float dx = (a.x - b.x) * aspect;
        float dy = a.y - b.y;
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    // 행인 한 명 등장 (왼쪽이나 오른쪽 끝에서 걸어 들어옴)
    void Spawn()
    {
        PromoPerson p = new PromoPerson();
        bool fromLeft = Random.value < 0.5f;

        p.pos = new Vector2(fromLeft ? -0.05f : 1.05f, Random.Range(0.15f, 0.85f));

        // 가로로 걷되 위아래로 조금 흔들리게 (한 줄로 지나가면 심심함)
        float side = fromLeft ? 1f : -1f;
        p.dir = new Vector2(side, Random.Range(-0.35f, 0.35f)).normalized;
        p.speed = Random.Range(speedMin, speedMax);
        p.golden = Random.value < GoldenChance();

        if (p.golden == false)
        {
            totalPeople++; // 황금 행인은 최대치에 안 셈
        }

        people.Add(p);
    }

    // 지금 던질 수 있는지 (쉬는 중이면 못 던짐)
    public bool CanThrow()
    {
        return _coolLeft <= 0f && IsDone() == false;
    }

    // 이 자리를 겨눠서 전단지 던지기 (화면이 클릭한 자리를 넘겨줌)
    public void Throw(Vector2 aim)
    {
        if (CanThrow() == false)
        {
            return;
        }

        _coolLeft = throwCooldown;

        PromoFlyer f = new PromoFlyer();
        f.from = throwFrom;
        f.to = aim;
        f.flown = 0f;
        f.flyTime = flyTime;
        flyers.Add(f);
    }

    // 다 닿은 전단지는 화면이 치우고 나서 여기로 알려줌
    public void ClearLandedFlyers()
    {
        for (int i = flyers.Count - 1; i >= 0; i--)
        {
            if (flyers[i].done == true)
            {
                flyers.RemoveAt(i);
            }
        }
    }

    // 행인이 나올 때마다 굴리니까, 한 판에 몇 명 나오는지로 나눠야 한 명쯤 나옴
    float GoldenChance()
    {
        float avgGap = (spawnGapMin + spawnGapMax) / 2f;
        if (duration <= 0f || avgGap <= 0f)
        {
            return 0f;
        }

        float personCount = duration / avgGap;
        if (personCount < 1f)
        {
            personCount = 1f;
        }

        return goldenPerGame / personCount;
    }

    // 시간 다 됐는지
    public bool IsDone()
    {
        return timeLeft <= 0f;
    }

    // 수행률 (일반 행인 대비 쥐여준 비율. 황금은 순수 보너스라 넘칠 수 있음)
    public float GetRate()
    {
        if (totalPeople == 0)
        {
            return 0f;
        }
        return Mathf.Clamp01((handedCount + goldenCount) / (float)totalPeople);
    }
}
