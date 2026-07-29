using UnityEngine;

// 게임 숫자 설정 모음 (밸런스 맞출 때 여기서 바꾸면 됨)
[CreateAssetMenu(fileName = "GameConfig", menuName = "MeiDay/Game Config")]
public class GameConfig : ScriptableObject
{
    [Header("기간 / 돈")]
    public int startingCredits = 100; // 새 게임 시작 크레딧
    public int foodCost = 10;         // 매일 나가는 밥값
    public int rentCost = 120;        // 월세
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
    public int perfectDayBonus = 30;    // 하루 다 맞췄을 때 주는 보너스
    public int wrongCardsPerSlot = 2;   // 상자 하나당 깔리는 오답 카드 수 (정답 1 + 오답 2 = 3장)

    [Header("아르바이트")]
    public int arbeitStart = 0;     // 상점 해금 전 알바 횟수 (해금되면 여기에 +1)
    public int arbeitMax = 3;       // 기본 횟수 상한 (위조 신분증 보너스랑 이월분은 이 위로 더 붙음)
    public int arbeitCarryMax = 1;  // 안 쓴 알바 횟수를 다음 날로 넘길 수 있는 최대치

    [Header("상점")]
    public int shopExpPerBuy = 10;   // 아이템 하나 살 때 오르는 상점 경험치
    public int shopExpPerLevel = 30; // 상점 레벨 하나에 필요한 경험치 (레벨 최대 5)

    [Header("이벤트")]
    public bool eventsInOrder = false; // 켜면 랜덤 이벤트가 확률 없이 목록 순서대로 나옴 (일정 밀릴 때 스위치)


}
