using System.Collections.Generic;
using UnityEngine;

// 정해진 단계에서만 보이는 UI 묶음
// 업적 버튼이나 설명서 버튼이 타이틀 화면에까지 떠 있어서 만듦
// 끄고 싶은 오브젝트에 직접 붙이지 말 것 - 꺼지면 자기 Update 가 안 돌아서 다시 못 켬
// 항상 켜져 있는 부모(Canvas 등)에 붙이고 target 에 끄고 켤 것을 꽂기
public class ShowOnPhases : MonoBehaviour
{
    [SerializeField] GameObject target;                // 끄고 켤 대상
    [SerializeField] List<GamePhase> phases = new List<GamePhase>(); // 이 단계들에서만 보임

    void Update()
    {
        if (target == null || GameManager.instance == null)
        {
            return;
        }

        bool show = phases.Contains(GameManager.instance.phase);
        if (target.activeSelf != show)
        {
            target.SetActive(show);
        }
    }
}
