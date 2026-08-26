using UnityEngine;

// 게임 저장/불러오기 매니저
// PlayerPrefs 라는 유니티 기본 저장 기능을 씀 (간단함)
public class SaveManager : MonoBehaviour
{
    // 밸런스 시뮬레이션 돌릴 때만 켬 (수백 판을 돌리면서 진짜 저장을 망치면 안 되니까)
    public bool skipSave = false;

    // 이어할 저장 데이터가 있는지
    public bool HasSave()
    {
        if (PlayerPrefs.GetInt("saveExists", 0) == 1)
        {
            return true;
        }

        // saveExists 를 넣기 전 버전에서 저장한 데이터도 이어할 수 있게 함
        return PlayerPrefs.HasKey("day");
    }

    // 저장하기
    public void Save()
    {
        if (skipSave == true)
        {
            return; // 시뮬레이션 중엔 저장 안 함
        }

        GameManager gm = GameManager.instance;

        // 기본 숫자들
        PlayerPrefs.SetInt("day", gm.calendar.day);
        PlayerPrefs.SetInt("credits", gm.economy.credits);
        PlayerPrefs.SetInt("rentMiss", gm.economy.rentMissCount);
        PlayerPrefs.SetInt("nextRentDay", gm.economy.nextRentDay);
        PlayerPrefs.SetFloat("suspicion", gm.suspicion.current);

        // 하드모드 상태
        int hardValue = 0;
        if (gm.hardMode == true)
        {
            hardValue = 1;
        }
        PlayerPrefs.SetInt("hardMode", hardValue);
        PlayerPrefs.SetInt("hardModeDay", gm.hardModeDay);
        PlayerPrefs.SetInt("brokerChoice", (int)gm.brokerChoice);

        // 우주선 샀는지 (bool 은 못 넣으니까 1이나 0으로 바꿔서 저장)
        int shipValue = 0;
        if (gm.hasSpaceship == true)
        {
            shipValue = 1;
        }
        PlayerPrefs.SetInt("hasSpaceship", shipValue);

        // 튜토리얼 어디까지 봤는지
        if (TutorialManager.instance != null)
        {
            PlayerPrefs.SetInt("tutorialStep", TutorialManager.instance.currentStep);
        }

        // 밤 상점이 열렸는지 (우주선이랑 같은 방식)
        int shopValue = 0;
        if (gm.shopUnlocked == true)
        {
            shopValue = 1;
        }
        PlayerPrefs.SetInt("shopUnlocked", shopValue);

        // 가진 아이템들 (리스트는 못 넣으니까 개수 + 하나씩 번호 붙여서 저장)
        PlayerPrefs.SetInt("itemCount", gm.inventory.ownedItems.Count);
        for (int i = 0; i < gm.inventory.ownedItems.Count; i++)
        {
            PlayerPrefs.SetString("item" + i, gm.inventory.ownedItems[i]);
        }

        // 이미 본 이벤트들 (아이템이랑 같은 방식)
        PlayerPrefs.SetInt("seenEventCount", gm.events.seenEventIds.Count);
        for (int i = 0; i < gm.events.seenEventIds.Count; i++)
        {
            PlayerPrefs.SetString("seenEvent" + i, gm.events.seenEventIds[i]);
        }

        // 영구템 레벨들 (id 랑 레벨 나란히)
        PlayerPrefs.SetInt("permCount", gm.inventory.permItemIds.Count);
        for (int i = 0; i < gm.inventory.permItemIds.Count; i++)
        {
            PlayerPrefs.SetString("permId" + i, gm.inventory.permItemIds[i]);
            PlayerPrefs.SetInt("permLv" + i, gm.inventory.permItemLevels[i]);
        }

        // 내일 적용될 파티 버프
        PlayerPrefs.SetInt("pendingPayoutBuff", gm.pendingPayoutBuff);
        PlayerPrefs.SetInt("pendingExtraComplaints", gm.pendingExtraComplaints);

        // 상점별 알바 누적 횟수
        for (int i = 0; i < gm.arbeit.arbeitDoneCounts.Count; i++)
        {
            PlayerPrefs.SetInt("arbeitDone" + i, gm.arbeit.arbeitDoneCounts[i]);
        }

        // 안 쓰고 남긴 알바 횟수 (다음 날 이월용)
        PlayerPrefs.SetInt("arbeitLeft", gm.arbeit.arbeitLeftToday);

        // 상점별 경험치
        for (int i = 0; i < gm.shop.shopExp.Count; i++)
        {
            PlayerPrefs.SetInt("shopExp" + i, gm.shop.shopExp[i]);
        }

        // 1회성 물건 값이 얼마나 올라있는지
        PlayerPrefs.SetInt("consumableBuyCount", gm.shop.consumableBuyCount);

        // 모든 값을 다 넣은 뒤 마지막에 저장 있음 표시
        PlayerPrefs.SetInt("saveExists", 1);
        PlayerPrefs.Save();
        Debug.Log("저장 완료");
    }

    // 불러오기. 저장 데이터가 없으면 false
    public bool Load()
    {
        if (HasSave() == false)
        {
            return false;
        }

        GameManager gm = GameManager.instance;

        // 기본 숫자들
        gm.calendar.day = PlayerPrefs.GetInt("day", 1);
        gm.economy.credits = PlayerPrefs.GetInt("credits", gm.config.startingCredits);
        gm.economy.rentMissCount = PlayerPrefs.GetInt("rentMiss", 0);
        gm.economy.nextRentDay = PlayerPrefs.GetInt("nextRentDay", gm.config.firstRentDay);
        gm.suspicion.current = PlayerPrefs.GetFloat("suspicion", 0);

        // 하드모드 상태
        gm.hardMode = (PlayerPrefs.GetInt("hardMode", 0) == 1);
        gm.hardModeDay = PlayerPrefs.GetInt("hardModeDay", 0);
        gm.brokerChoice = (BrokerChoice)PlayerPrefs.GetInt("brokerChoice", 0);

        // 우주선 (1이면 샀던 거)
        gm.hasSpaceship = (PlayerPrefs.GetInt("hasSpaceship", 0) == 1);

        // 밤 상점 해금 (1이면 열렸던 거)
        gm.shopUnlocked = (PlayerPrefs.GetInt("shopUnlocked", 0) == 1);

        // 튜토리얼 이어보기
        if (TutorialManager.instance != null)
        {
            TutorialManager.instance.SetStep(PlayerPrefs.GetInt("tutorialStep", 0));
        }

        // 아이템들 (저장했던 개수만큼 다시 꺼내서 리스트에 넣기)
        gm.inventory.ownedItems.Clear();
        int itemCount = PlayerPrefs.GetInt("itemCount", 0);
        for (int i = 0; i < itemCount; i++)
        {
            string itemId = PlayerPrefs.GetString("item" + i, "");
            gm.inventory.ownedItems.Add(itemId);
        }

        // 이미 본 이벤트들
        gm.events.seenEventIds.Clear();
        int seenCount = PlayerPrefs.GetInt("seenEventCount", 0);
        for (int i = 0; i < seenCount; i++)
        {
            gm.events.seenEventIds.Add(PlayerPrefs.GetString("seenEvent" + i, ""));
        }

        // 영구템 레벨들
        gm.inventory.permItemIds.Clear();
        gm.inventory.permItemLevels.Clear();
        int permCount = PlayerPrefs.GetInt("permCount", 0);
        for (int i = 0; i < permCount; i++)
        {
            gm.inventory.permItemIds.Add(PlayerPrefs.GetString("permId" + i, ""));
            gm.inventory.permItemLevels.Add(PlayerPrefs.GetInt("permLv" + i, 0));
        }

        // 내일 적용될 파티 버프
        gm.pendingPayoutBuff = PlayerPrefs.GetInt("pendingPayoutBuff", 0);
        gm.pendingExtraComplaints = PlayerPrefs.GetInt("pendingExtraComplaints", 0);
        for(int i = 0; i < gm.arbeit.arbeitDoneCounts.Count; i++)
        {
            gm.arbeit.arbeitDoneCounts[i] = PlayerPrefs.GetInt("arbeitDone" + i, 0);
        }

        // 안 쓰고 남긴 알바 횟수
        gm.arbeit.arbeitLeftToday = PlayerPrefs.GetInt("arbeitLeft", 0);

        // 상점별 경험치
        for (int i = 0; i < gm.shop.shopExp.Count; i++)
        {
            gm.shop.shopExp[i] = PlayerPrefs.GetInt("shopExp" + i, 0);
        }

        // 1회성 물건 값이 얼마나 올라있는지
        gm.shop.consumableBuyCount = PlayerPrefs.GetInt("consumableBuyCount", 0);

        Debug.Log("불러오기 완료");
        return true;
    }

    // 저장 지우기 (죽거나 엔딩 봤을 때 부름 - 이어하기 못 하게)
    public void Clear()
    {
        if (skipSave == true)
        {
            return; // 시뮬레이션 중엔 진짜 저장을 지우면 안 됨
        }

        PlayerPrefs.DeleteKey("saveExists");
        PlayerPrefs.DeleteKey("day"); // 이거 안 지우면 HasSave() 가 계속 true 나옴
        PlayerPrefs.DeleteKey("tutorialStep"); // 새 게임이면 튜토리얼도 처음부터
        PlayerPrefs.Save();

        if (TutorialManager.instance != null)
        {
            TutorialManager.instance.SetStep(0);
        }

        Debug.Log("저장 지움");
    }
}
