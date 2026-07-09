using UnityEngine;

// 게임 숫자 설정 모음 (밸런스 맞출 때 여기서 바꾸면 됨)
[CreateAssetMenu(fileName = "GameConfig", menuName = "MeiDay/Game Config")]
public class GameConfig : ScriptableObject
{
    [Header("기간 / 돈")]
    public int totalDays = 31;        // 총 며칠 동안 하는지
    public int foodCost = 10;         // 매일 나가는 밥값
    public int rentCost = 120;        // 월세
    public int rentEveryDays = 6;     // 며칠마다 월세 내는지
    public int rentMissLimit = 3;     // 월세 몇 번 밀리면 게임오버
    public int spaceshipPrice = 3000; // 우주선 값 (탈출 목표 금액)
    public int rentDunEveryDays = 3;  // 월세 밀렸을 때 며칠 간격으로 독촉하는지

    [Header("하드모드")]
    public int hardModeDays = 7;                // 우주선 산 뒤 며칠 버티면 탈출 엔딩
    public int hardModeSuspicionPerDay = 8;     // 의심도 택했을 때 매일 오르는 양
    public int hardModeCreditDrainPerDay = 200; // 크레딧 택했을 때 매일 나가는 돈

    [Header("의심도")]
    public float maxSuspicion = 100;            // 의심도 최대치 (넘으면 게임오버)
    public float suspicionWrongTranslation = 5; // 해석 틀렸을 때 오르는 양
    public float suspicionWrongResolution = 12; // 처리 틀렸을 때 오르는 양

    [Header("민원")]
    public int complaintsPerDay = 3;  // 하루에 처리하는 민원 수
    public int maxWrongTries = 3;     // 해석 몇 번 틀리면 민원인이 떠나는지
    public int wrongAnswerFine = 20;  // 민원인 떠날 때 내는 벌금
    public int perfectDayBonus = 30;  // 하루 다 맞췄을 때 주는 보너스
}
