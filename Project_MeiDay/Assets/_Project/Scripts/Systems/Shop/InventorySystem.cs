using System.Collections.Generic;
using UnityEngine;

// 내가 가진 아이템들을 관리하는 매니저
public class InventorySystem : MonoBehaviour
{
    public List<string> ownedItems = new List<string>(); // 가진 아이템 id 목록

    // 아이템 추가
    public void AddItem(string id)
    {
        ownedItems.Add(id);
    }

    // 이 아이템 가지고 있는지 확인
    public bool HasItem(string id)
    {
        for (int i = 0; i < ownedItems.Count; i++)
        {
            if (ownedItems[i] == id)
            {
                return true; // 찾음
            }
        }
        return false; // 없음
    }
}
