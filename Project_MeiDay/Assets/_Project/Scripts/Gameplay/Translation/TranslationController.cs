using System.Collections.Generic;
using UnityEngine;

// 외계어 해석(단어 상자 맞추기)을 담당하는 매니저
public class TranslationController : MonoBehaviour
{
    public ComplaintDefinition currentComplaint; // 지금 푸는 민원
    public List<WordSlot> slots = new List<WordSlot>(); // 지금 화면의 상자들
    public int wrongCount = 0; // 몇 번 틀렸는지

    // 민원 해석 시작 (상자들 만들어줌)
    public void StartComplaint(ComplaintDefinition complaint)
    {
        currentComplaint = complaint;
        wrongCount = 0;
        slots.Clear();

        // 민원 데이터에 있는 상자들을 게임용 상자로 만들어서 넣기
        for (int i = 0; i < complaint.slots.Count; i++)
        {
            WordSlot slot = new WordSlot();
            slot.category = complaint.slots[i].category;
            slot.correctWord = complaint.slots[i].correctWord;
            slot.placedWord = null;
            slots.Add(slot);
        }
    }

    // 확인 버튼 눌렀을 때. 다 맞으면 true
    public bool CheckAnswer()
    {
        // 상자 하나라도 틀리면 틀린 처리
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].IsCorrect() == false)
            {
                Wrong();
                return false;
            }
        }

        return true; // 다 맞음
    }

    // 틀렸을 때 처리
    void Wrong()
    {
        GameConfig config = GameManager.instance.config; // 설정 꺼내옴

        wrongCount++;
        GameManager.instance.suspicion.AddSuspicion(config.suspicionWrongTranslation); // 틀리면 의심도 오름

        // 너무 많이 틀리면 민원인 화내고 감 -> 벌금
        if (wrongCount >= config.maxWrongTries)
        {
            GameManager.instance.economy.credits -= config.wrongAnswerFine;
        }
    }
}
