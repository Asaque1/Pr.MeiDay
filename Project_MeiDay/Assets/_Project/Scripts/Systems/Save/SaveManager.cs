using UnityEngine;

// 게임 저장/불러오기 매니저
// PlayerPrefs 라는 유니티 기본 저장 기능을 씀 (간단함)
public class SaveManager : MonoBehaviour
{
    // 저장하기
    public void Save()
    {
        GameManager gm = GameManager.instance;

        // 기본 숫자들
        PlayerPrefs.SetInt("day", gm.calendar.day);
        PlayerPrefs.SetInt("credits", gm.economy.credits);
        PlayerPrefs.SetInt("rentMiss", gm.economy.rentMissCount);
        PlayerPrefs.SetFloat("suspicion", gm.suspicion.current);

        // 우주선 샀는지 (bool 은 못 넣으니까 1이나 0으로 바꿔서 저장)
        int shipValue = 0;
        if (gm.hasSpaceship == true)
        {
            shipValue = 1;
        }
        PlayerPrefs.SetInt("hasSpaceship", shipValue);

        // 가진 아이템들 (리스트는 못 넣으니까 개수 + 하나씩 번호 붙여서 저장)
        PlayerPrefs.SetInt("itemCount", gm.inventory.ownedItems.Count);
        for (int i = 0; i < gm.inventory.ownedItems.Count; i++)
        {
            PlayerPrefs.SetString("item" + i, gm.inventory.ownedItems[i]);
        }

        PlayerPrefs.Save();
        Debug.Log("저장 완료");
    }

    // 불러오기 (저장한 적 없으면 기본값 들어감)
    public void Load()
    {
        GameManager gm = GameManager.instance;

        // 기본 숫자들
        gm.calendar.day = PlayerPrefs.GetInt("day", 1);
        gm.economy.credits = PlayerPrefs.GetInt("credits", 100);
        gm.economy.rentMissCount = PlayerPrefs.GetInt("rentMiss", 0);
        gm.suspicion.current = PlayerPrefs.GetFloat("suspicion", 0);

        // 우주선 (1이면 샀던 거)
        gm.hasSpaceship = (PlayerPrefs.GetInt("hasSpaceship", 0) == 1);

        // 아이템들 (저장했던 개수만큼 다시 꺼내서 리스트에 넣기)
        gm.inventory.ownedItems.Clear();
        int itemCount = PlayerPrefs.GetInt("itemCount", 0);
        for (int i = 0; i < itemCount; i++)
        {
            string itemId = PlayerPrefs.GetString("item" + i, "");
            gm.inventory.ownedItems.Add(itemId);
        }

        Debug.Log("불러오기 완료");
    }
}
