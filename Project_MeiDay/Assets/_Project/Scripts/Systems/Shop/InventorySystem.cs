using System.Collections.Generic;
using UnityEngine;

// 내가 가진 아이템들을 관리하는 매니저
public class InventorySystem : MonoBehaviour
{
    public List<string> ownedItems = new List<string>(); // 가진 아이템 id 목록 (1회성 구매 기록용)

    // 영구템 레벨 (id 랑 레벨을 같은 순서로 담음. 저장하기 편하게 리스트 두 개)
    public List<string> permItemIds = new List<string>();
    public List<int> permItemLevels = new List<int>();

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

    // 이 영구템이 몇 레벨인지 (안 샀으면 0)
    public int GetItemLevel(string id)
    {
        for (int i = 0; i < permItemIds.Count; i++)
        {
            if (permItemIds[i] == id)
            {
                return permItemLevels[i];
            }
        }
        return 0;
    }

    // 영구템 레벨 하나 올리기 (처음 사면 1레벨)
    public void LevelUpItem(string id)
    {
        for (int i = 0; i < permItemIds.Count; i++)
        {
            if (permItemIds[i] == id)
            {
                permItemLevels[i]++;
                return;
            }
        }

        permItemIds.Add(id);
        permItemLevels.Add(1);
    }

    // 이 효과를 가진 영구템들의 힘을 다 합침 (레벨 x 세기)
    public float GetEffectPower(ItemEffect effect)
    {
        float total = 0;
        List<ItemDefinition> items = GameManager.instance.content.items;

        for (int i = 0; i < items.Count; i++)
        {
            ItemDefinition item = items[i];
            if (item.kind != ItemKind.Permanent || item.effect != effect)
            {
                continue;
            }

            int level = GetItemLevel(item.id);
            if (level > 0)
            {
                total += level * item.power;
            }
        }

        return total;
    }

    // 이 효과를 가진 영구템들의 부작용 세기 합침 (레벨 x sidePower)
    public float GetSidePower(ItemEffect effect)
    {
        float total = 0;
        List<ItemDefinition> items = GameManager.instance.content.items;

        for (int i = 0; i < items.Count; i++)
        {
            ItemDefinition item = items[i];
            if (item.kind != ItemKind.Permanent || item.effect != effect)
            {
                continue;
            }

            int level = GetItemLevel(item.id);
            if (level > 0)
            {
                total += level * item.sidePower;
            }
        }

        return total;
    }
    // 가진 영구템들이 올려주는 알바 보수 합 (레벨 x arbeitPayBonus)
    public int GetArbeitPayBonus()
    {
        int total = 0;
        List<ItemDefinition> items = GameManager.instance.content.items;

        for (int i = 0; i < items.Count; i++)
        {
            ItemDefinition item = items[i];
            if (item.kind != ItemKind.Permanent || item.arbeitPayBonus == 0)
            {
                continue;
            }

            int level = GetItemLevel(item.id);
            if (level > 0)
            {
                total += level * item.arbeitPayBonus;
            }
        }

        return total;
    }
}
