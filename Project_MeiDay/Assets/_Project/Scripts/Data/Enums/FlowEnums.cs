// 게임이 지금 어느 단계인지
public enum GamePhase
{
    Intro,     // 도입부
    DayWork,   // 낮 - 민원 처리
    DayResult, // 낮 끝 - 결과 화면
    NightShop, // 밤 - 상점
    Ending,    // 엔딩
    GameOver,  // 게임 오버
}

// 엔딩 종류 (23일에 갈림)
public enum EndingType
{
    None,
    DatabaseAccess, // 브로커를 DB에 접근시킴
    ReportBroker,   // 브로커를 신고함
    NoShip,         // 우주선 안 삼 (무한 플레이)
}

// 게임 오버 이유
public enum GameOverReason
{
    SuspicionExceeded, // 의심도가 꽉 참 (들킴)
    RentDefault,       // 월세 3번 밀림 (쫓겨남)
}
