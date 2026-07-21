// 어느 상점에서 파는지
public enum ShopType
{
    Bookstore, // 서점 (해석 관련 영구템)
    Theater,   // 연극단 (의심도 관련 영구템)
    PartyShop, // 파티용품점 (내일 하루 버프)
    Broker,    // 브로커 (1회성, 랜덤박스, 우주선표)
}

// 아이템 성격
public enum ItemKind
{
    Permanent,  // 영구템 (살 때마다 레벨이 오름)
    Consumable, // 1회성 (사면 바로 효과)
    RandomBox,  // 랜덤박스 (사면 범위 내 랜덤 크레딧)
}

// 아이템이 뭘 해주는지
public enum ItemEffect
{
    None,
    FewerWrongCards, // 상자당 오답 카드 수를 줄여줌 (서점 영구템)
    SuspicionResist, // 의심도 오르는 양을 줄여줌 (연극단 영구템)
    PayoutBuff,      // 내일 하루 성과금 증가 (파티용품점)
    ExtraComplaints, // 내일 민원인 증가 (파티용품점)
    ReduceSuspicion, // 의심도 바로 감소 (브로커 1회성)
    ArbeitCountUp,     // 알바 가능 횟수 늘려줌, 대신 낮 보수 깎임 (위조 신분증)
    DailyMoneyPercent, // 매일 돈이 % 로 붇지만 매일 의심도도 오름 (복제된 법인카드)
    DailyRandomBox,    // 매일 아침 랜덤박스가 굴러감 (밀수 루트 지도)
    FreeAnswerWord,    // 상자 하나가 미리 풀려있음, 대신 보수 깎임 (번역서)
}
