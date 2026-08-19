// 민원 처리 방법 (이 중에 맞는 걸 골라야 함)
public enum ComplaintResolution
{
    DepartmentTransfer, // 관련 부서로 연결
    CallSecurity,       // 경호원 부르기
    DocumentPrint,      // 문서 출력
}

// 민원 한 건이 지금 어느 단계인지
public enum ComplaintStage
{
    Translate, // 외계어 해석 중
    Resolve,   // 처리 방법 고르는 중
}

// 민원인이 지금 어디까지 왔는지 (연출용)
// 창구는 비어있다가 -> 손님이 앉고 -> 말을 걸어옴
public enum ComplaintEntry
{
    Waiting,  // 아직 안 옴 (창구 비어있음)
    Arrived,  // 앉았는데 아직 말은 안 함
    Speaking, // 말하는 중 (대사가 한 글자씩 나오는 동안)
    Ready,    // 말 다 함 - 이제 카드를 꽂을 수 있음
}
