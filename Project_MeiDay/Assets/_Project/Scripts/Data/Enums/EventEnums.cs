// 이벤트가 언제 나오는지
public enum EventTiming
{
    FixedDay, // 정해진 일차 아침에 무조건 나옴
    Random,   // 아침마다 확률로 나옴
    Trigger,  // 조건이 맞는 날 아침에 나옴
}

// Trigger 이벤트가 언제 튀어나오는지 정하는 조건
public enum EventCondition
{
    None,             // 조건 없음 (바로 나옴)
    DayAtLeast,       // 며칠차 이상
    CreditsAtLeast,   // 크레딧 얼마 이상
    SuspicionAtLeast, // 의심도 얼마 이상
    ShopLevelSum,     // 서점+연극단+파티 상점 레벨 합이 얼마 이상
    HasSpaceship,     // 우주선 샀으면
}
