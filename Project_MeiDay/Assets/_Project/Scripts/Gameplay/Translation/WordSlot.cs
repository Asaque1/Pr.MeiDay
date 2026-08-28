// 단어 상자 한 칸 (게임 중에 만들어지는 일반 클래스)
public class WordSlot
{
    public WordCategory category;      // 무슨 색 상자인지
    public WordDefinition correctWord; // 정답 단어
    public WordDefinition placedWord;  // 플레이어가 올려놓은 단어 (없으면 null)

    // 빈칸에 흐리게 보여줄 외계 사투리 (정답 단어의 사투리 중 하나)
    // 민원 시작할 때 뽑아서 담아둠 - 화면을 다시 그려도 안 바뀌게
    public string hintDialect = "";

    // 올려놓은 게 정답이랑 같은지 확인
    public bool IsCorrect()
    {
        return placedWord == correctWord;
    }
}
