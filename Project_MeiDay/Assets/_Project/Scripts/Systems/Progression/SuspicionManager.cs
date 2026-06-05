using UnityEngine;

// 의심도를 관리하는 매니저
// 의심도가 꽉 차면 지구인인 게 들켜서 게임오버
public class SuspicionManager : MonoBehaviour
{
    public float current = 0; // 지금 의심도

    // 의심도 올리기 (민원 틀렸을 때 등)
    public void AddSuspicion(float amount)
    {
        current += amount;

        float max = GameManager.instance.config.maxSuspicion;
        if (current >= max)
        {
            current = max;
            // 의심도 꽉 참 -> 들킴 -> 게임오버
            GameManager.instance.GameOver(GameOverReason.SuspicionExceeded);
        }
    }

    // 의심도 내리기 (변장 용품 같은 거 썼을 때)
    public void ReduceSuspicion(float amount)
    {
        current -= amount;
        if (current < 0)
        {
            current = 0; // 0 아래로는 안 내려감
        }
    }
}
