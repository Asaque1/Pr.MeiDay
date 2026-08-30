using System;
using UnityEngine;

// 의심도를 관리하는 매니저
// 의심도가 꽉 차면 지구인인 게 들켜서 게임오버
public class SuspicionManager : MonoBehaviour
{
    public float current = 0; // 지금 의심도

    // 의심도 바뀌면 알려줌 (연출 UI 가 듣고 씀. 바뀐 양이랑 바뀐 뒤 값)
    public event Action<float, float> OnChanged;

    // 의심도 올리기 (민원 틀렸을 때 등)
    public void AddSuspicion(float amount)
    {
        // 연극단 영구템: 의심도 오르는 양을 줄여줌 (최대 절반까지만)
        float resist = GameManager.instance.inventory.GetEffectPower(ItemEffect.SuspicionResist);
        if (resist > 50)
        {
            resist = 50;
        }
        amount -= amount * resist / 100f;

        current += amount;

        float max = MaxSuspicion();
        if (current > max)
        {
            current = max;
        }

        NotifyChanged(amount);

        // 의심도 꽉 참 -> 들킴 -> 게임오버
        if (current >= max)
        {
            GameManager.instance.GameOver(GameOverReason.SuspicionExceeded);
        }
    }

    // 의심도 내리기 (변장 용품 같은 거 썼을 때)
    // 지금 의심도 최대치
    // 설정값에 향수 같은 아이템이 올려준 만큼 더함 - 화면이랑 게임오버 판정이 같은 값을 써야 해서 여기서만 계산함
    public float MaxSuspicion()
    {
        float bonus = 0f;
        if (GameManager.instance.inventory != null)
        {
            bonus = GameManager.instance.inventory.GetEffectPower(ItemEffect.MaxSuspicionUp);
        }

        return GameManager.instance.config.maxSuspicion + bonus;
    }

    public void ReduceSuspicion(float amount)
    {
        current -= amount;
        if (current < 0)
        {
            current = 0; // 0 아래로는 안 내려감
        }

        NotifyChanged(-amount);
    }

    // 구독한 연출한테 알려주기
    void NotifyChanged(float delta)
    {
        if (OnChanged != null)
        {
            OnChanged(delta, current);
        }
    }
}
