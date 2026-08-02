// 업적이 언제 달성되는지 (조건 종류)
// 새로 추가할 땐 맨 아래에 붙일 것 (중간에 끼우면 이미 만든 에셋 값이 밀림)
public enum AchievementCondition
{
    DayReached,       // 며칠차까지 버팀
    CreditsReached,   // 크레딧을 얼마까지 모아봄
    ComplaintsDone,   // 민원을 몇 건 처리함 (누적)
    ArbeitDone,       // 알바를 몇 번 함 (누적)
    ShopLevelSum,     // 서점+연극단+파티 상점 레벨 합
    PerfectDays,      // 하루를 무실수로 끝낸 날 수 (누적)
    BoughtSpaceship,  // 우주선을 삼
    EndingDatabase,   // DB 접근 엔딩을 봄
    EndingReport,     // 신고 엔딩을 봄
    GameOverAny,      // 한 번 죽어봄
    GameOverSuspicion, // 의심도가 꽉 차서 죽음
    GameOverRent,      // 월세 밀려서 쫓겨남
    ItemOwned,         // 특정 아이템을 삼 (requiredItem 에 넣어둘 것)
}
