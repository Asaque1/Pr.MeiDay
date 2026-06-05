// 단어 상자 한 칸 (게임 중에 만들어지는 일반 클래스)
public class WordSlot
{
    public WordCategory category;      // 무슨 색 상자인지
    public WordDefinition correctWord; // 정답 단어
    public WordDefinition placedWord;  // 플레이어가 올려놓은 단어 (없으면 null)

    // 올려놓은 게 정답이랑 같은지 확인
    public bool IsCorrect()
    {
        return placedWord == correctWord;
    }
}
