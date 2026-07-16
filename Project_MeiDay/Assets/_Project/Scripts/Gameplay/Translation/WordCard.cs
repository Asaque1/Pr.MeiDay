// 밑에 나오는 단어 카드 한 장 (드래그해서 상자에 넣는 거)
// ScriptableObject 가 아니라 게임 중에 new 로 만드는 일반 클래스
public class WordCard
{
    public WordDefinition word;    // 이 카드가 가진 단어
    public bool isUsed = false;    // 상자에 올려놨는지
    public bool isRemoved = false; // 틀리게 놨다가 떨어져서 사라졌는지
}
