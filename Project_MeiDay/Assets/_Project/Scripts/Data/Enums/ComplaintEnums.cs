// 민원 처리 방법 (이 중에 맞는 걸 골라야 함)
public enum ComplaintResolution
{
    DepartmentTransfer, // 관련 부서로 연결
    CallSecurity,       // 경호원 부르기
    DirectHandle,       // 직접 처리
}

// 직접 처리일 때 뭘로 처리하는지
public enum DirectHandleType
{
    None,
    DocumentPrint, // 문서 출력
    Minigame,      // 미니게임
}
