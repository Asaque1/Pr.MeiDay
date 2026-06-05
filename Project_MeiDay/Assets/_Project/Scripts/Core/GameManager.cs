using UnityEngine;

// 게임 전체를 관리하는 대장 클래스
// 다른 데서 GameManager.instance 로 불러서 씀
public class GameManager : MonoBehaviour
{
    public static GameManager instance; // 어디서든 쓰는 나 자신

    // 게임 데이터 (인스펙터에서 연결)
    public GameConfig config;
    public ContentDatabase content;

    // 매니저들 (인스펙터에서 연결)
    public EconomyManager economy;
    public SuspicionManager suspicion;
    public CalendarManager calendar;
    public InventorySystem inventory;
    public ShopManager shop;
    public SaveManager save;
    public ComplaintManager complaint;

    // 게임 상태
    public GamePhase phase = GamePhase.Intro; // 지금 어느 단계인지
    public bool hasSpaceship = false;         // 우주선 샀는지

    void Awake()
    {
        instance = this; // 게임 켜질 때 instance 에 나를 넣어줌
    }

    // 단계 바꾸기
    public void ChangePhase(GamePhase newPhase)
    {
        phase = newPhase;
        Debug.Log("단계 바뀜: " + newPhase);
    }

    // 게임 오버
    public void GameOver(GameOverReason reason)
    {
        phase = GamePhase.GameOver;
        Debug.Log("게임 오버! 이유: " + reason);
    }

    // 엔딩
    public void Ending(EndingType type)
    {
        phase = GamePhase.Ending;
        Debug.Log("엔딩! " + type);
    }
}
