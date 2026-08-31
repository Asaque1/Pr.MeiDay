// 이벤트가 언제 나오는지
public enum EventTiming
{
    FixedDay, // 정해진 일차에 무조건 나옴
    Random,   // 확률로 나옴
    Trigger,  // 조건이 맞는 날 나옴
}

// 하루 중 어느 자리에 끼어드는지
// 값 순서를 바꾸면 이미 만든 이벤트가 다른 자리로 옮겨감 - 새 자리는 맨 뒤에 붙일 것
public enum EventSlot
{
    DayStart,       // 하루 시작, 첫 민원 오기 전
    AfterComplaint, // 민원 하나 처리하고 다음 민원 오기 전
    DayEnd,         // 민원 다 처리하고 결산 넘어가기 전
}

// 이벤트가 끝난 뒤 그 상점의 구매 탭을 어떻게 할지
// 상점 화면 자체를 여는 건 EventDefinition.unlocksShop 이 따로 함
// 이건 상점 안에서 물건을 살 수 있냐 없냐만 건드림 (알바는 그대로 됨)
public enum ShopBuyAction
{
    None,   // 안 건드림
    Unlock, // 이 상점에서 물건을 살 수 있게
    Lock,   // 이 상점에서 물건을 못 사게
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
