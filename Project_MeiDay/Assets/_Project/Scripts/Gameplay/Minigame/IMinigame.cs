// 미니게임들이 따라야 하는 약속(인터페이스)
// 미니게임을 새로 만들 때 이걸 implements 하면 됨
public interface IMinigame
{
    void StartMinigame(); // 미니게임 시작
    bool IsCleared();     // 클리어 했는지 확인
}
