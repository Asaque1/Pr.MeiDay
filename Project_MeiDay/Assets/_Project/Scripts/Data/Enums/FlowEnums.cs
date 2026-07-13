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

// 엔딩 종류 (우주선 사고 브로커 선택에 따라 갈림)
public enum EndingType
{
    None,
    DatabaseAccess, // 브로커를 DB에 접근시키고 탈출
    ReportBroker,   // 브로커를 신고하고 버텨서 탈출
}

// 게임 오버 이유
public enum GameOverReason
{
    SuspicionExceeded, // 의심도가 꽉 참 (들킴)
    RentDefault,       // 월세 3번 밀림 (쫓겨남)
}

// 우주선 산 다음 날 브로커가 주는 선택 (이게 엔딩 분기)
public enum BrokerChoice
{
    None,
    DatabaseAccess, // 브로커를 국가 DB에 접근시킴 (돈 일부 뜯김)
    ReportBroker,   // 브로커를 신고함 (소문 때문에 매일 의심도 증가)
}
