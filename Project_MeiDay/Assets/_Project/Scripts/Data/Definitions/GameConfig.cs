using UnityEngine;

// 게임 숫자 설정 모음 (밸런스 맞출 때 여기서 바꾸면 됨)
[CreateAssetMenu(fileName = "GameConfig", menuName = "MeiDay/Game Config")]
public class GameConfig : ScriptableObject
{
    [Header("기간 / 돈")]
    public int startingCredits = 350; // 새 게임 시작 크레딧
    public int foodCost = 20;         // 매일 나가는 밥값
    public int rentTutorialCost = 100; // 첫 월세 (튜토라 싸게 받음)
    public int rentBaseCost = 125;     // 두 번째부터의 기본 월세
    public int rentGrowPerStep = 30;   // 월세가 한 단계 오를 때 붙는 금액
    public int rentGrowEveryDays = 5;  // 며칠마다 월세가 한 단계씩 오르는지
    public int firstRentDay = 3;      // 첫 월세 내는 날
    public int rentEveryDays = 6;     // 며칠마다 월세 내는지
    public int rentMissLimit = 3;     // 월세 몇 번 밀리면 게임오버
    public int spaceshipShopLevel = 5; // 우주선 해금에 필요한 상점 레벨 합 (값은 브로커 아이템에 적혀있음)
    public int rentDunEveryDays = 3;  // 월세 밀렸을 때 며칠 간격으로 독촉하는지

    [Header("하드모드")]
    public int hardModeDays = 7;             // 브로커 선택 뒤 며칠째 아침에 엔딩인지
    public int hardModeExtraCost = 30;       // 하드모드 동안 매일 나가는 추가 지출
    public float brokerTakeRate = 0.3f;      // DB 접근 골랐을 때 브로커가 떼가는 돈 비율
    public float reportSuspicionPerDay = 8;  // 신고 골랐을 때 소문 때문에 매일 오르는 의심도

    [Header("의심도")]
    public float maxSuspicion = 100;            // 의심도 최대치 (넘으면 게임오버)
    public float suspicionWrongTranslation = 5; // 해석 틀렸을 때 오르는 양
    public float suspicionWrongResolution = 12; // 처리 틀렸을 때 오르는 양
    public float suspicionPerfectClear = 3;     // 한 번도 안 틀리고 민원 처리하면 내려가는 양

    [Header("민원")]
    public int complaintsPerDay = 3;    // 1일차 할당량
    public int complaintsGrowEvery = 5; // 며칠마다 할당량이 1씩 늘어나는지
    public int complaintsMax = 6;       // 할당량 상한
    public int maxWrongTries = 3;       // 해석 몇 번 틀리면 민원인이 떠나는지
    // 처리는 3지선다라 찍어도 2/3 확률로 틀리고 의심도도 훨씬 많이 오름
    // 해석이랑 같은 제한을 쓰면 처리 쪽이 사실상 즉사라서 따로 뺌
    public int maxResolutionWrongTries = 2;
    public int perfectDayBonus = 30;    // 하루 다 맞췄을 때 주는 보너스
    // 상자 하나당 깔리는 오답 카드 수 (정답 1 + 오답 2 = 3장)
    // 1일차엔 wrongCardsPerSlot, wrongCardsFullDay 일차가 되면 wrongCardsPerSlotMax 까지 늘어남
    public int wrongCardsPerSlot = 2;
    public int wrongCardsPerSlotMax = 4;
    public int wrongCardsFullDay = 40;

    [Header("민원인 인내심")]
    // 말 끝난 뒤 이 시간 안에 처리 못하면 민원인이 화냄 (실수 한 번으로 침)
    // 1일차엔 angerTimeFirstDay 초, angerTimeFullDay 일차엔 angerTimeLastDay 초까지 줄어듦
    // 0 이하로 두면 시간 제한 없음
    public float angerTimeFirstDay = 40f;
    public float angerTimeLastDay = 15f;
    public int angerTimeFullDay = 40;

    // 이 일차부터 인내심을 잼 - 그 전에는 게이지가 꽉 찬 채로 안 줄어듦
    // 처음 며칠은 조작을 익히는 기간이라 시간에 쫓기면 배우기 전에 실수부터 쌓임
    public int angerStartDay = 3;

    [Header("아르바이트")]
    public int arbeitStart = 0;     // 상점 해금 전 알바 횟수 (해금되면 여기에 +1)
    public int arbeitMax = 3;       // 기본 횟수 상한 (위조 신분증 보너스랑 이월분은 이 위로 더 붙음)
    public int arbeitCarryMax = 1;  // 안 쓴 알바 횟수를 다음 날로 넘길 수 있는 최대치

    [Header("상점")]
    // 상점 레벨 하나 올리는 데 필요한 경험치 (0레벨 시작, 최대 5)
    // 기본 + 단계*지금레벨  ->  100, 150, 200, 250, 300 (누적 1000)
    public int shopLevelExpBase = 100;
    public int shopLevelExpStep = 50;

    // 영구템 몇 레벨짜리를 샀을 때 주는 상점 경험치
    // 1레벨이 기본값이고 레벨마다 비율+고정으로 뜀  ->  30, 40, 52, 67, 86
    // 1회성/랜덤박스는 레벨이 없어서 1레벨 값을 줌
    public int shopExpFirstLevel = 30;
    public float shopExpGrowRate = 1.25f;
    public float shopExpGrowAdd = 2.5f;

    // 영구템 레벨을 올릴 때 값이 얼마나 뛰는지
    // 1레벨은 아이템에 적힌 값 그대로, 2레벨부터: 기본 + 변동치 + (레벨당*레벨) + 전 단계 값*(레벨/나눔값)
    // 기본값이면 200 -> 275 -> 360 -> 481 -> 665 (만렙까지 총 1981)
    public int itemPriceBase = 200;
    public int itemPriceStep = 20;
    public int itemPricePerLevel = 15;
    public int itemPriceCarryDiv = 5;

    // 1회성 / 랜덤박스 값
    // 아이템에 적힌 값(기본값) + 변동치 에, 그동안 산 횟수만큼 % 가 붙고 일차만큼 더 붙음
    // 산 횟수는 매일 조금씩 깎여서, 몰아서 사든 나눠 사든 부담이 비슷하게 감
    public int consumablePriceStep = 0;      // 변동치
    public int consumablePricePerBuy = 3;    // 하나 살 때마다 붙는 %
    public int consumablePricePerDay = 10;   // 일차마다 붙는 값
    public int consumableBuyDecayPerDay = 5; // 하루 지날 때 깎이는 구매 횟수

    // 랜덤박스 보상 = 0 ~ (낸 값 * 이 배율) + 아이템에 적힌 추가값
    // 돈 나오는 랜덤박스는 아이템의 sidePower 만큼 폭이 더 벌어져서 도박성이 커짐
    public float randomBoxMaxRate = 1.8f;

    [Header("이벤트")]
    public bool eventsInOrder = false; // 켜면 랜덤 이벤트가 확률 없이 목록 순서대로 나옴 (일정 밀릴 때 스위치)



    // 일차에 맞는 오답 카드 수 (1일차 -> 마지막날까지 고르게 늘어남)
    public int WrongCardsForDay(int day)
    {
        return Mathf.RoundToInt(ByDay(wrongCardsPerSlot, wrongCardsPerSlotMax, wrongCardsFullDay, day));
    }

    // 일차에 맞는 민원인 인내심 (1일차 -> 마지막날까지 고르게 줄어듦)
    public float AngerTimeForDay(int day)
    {
        return ByDay(angerTimeFirstDay, angerTimeLastDay, angerTimeFullDay, day);
    }

    // 1일차 값에서 시작해 fullDay 일차에 마지막 값이 되게 섞어줌
    float ByDay(float first, float last, int fullDay, int day)
    {
        if (fullDay <= 1)
        {
            return last;
        }

        float t = (float)(day - 1) / (fullDay - 1);
        if (t < 0f)
        {
            t = 0f;
        }
        if (t > 1f)
        {
            t = 1f; // 마지막 일차를 넘겨도 더는 안 변함
        }

        return Mathf.Lerp(first, last, t);
    }
}
