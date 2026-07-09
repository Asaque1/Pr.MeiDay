using System.Collections.Generic;
using UnityEngine;

// 외계어 해석(단어 상자 맞추기)을 담당하는 매니저
public class TranslationController : MonoBehaviour
{
    public ComplaintDefinition currentComplaint;         // 지금 푸는 민원
    public List<WordSlot> slots = new List<WordSlot>();  // 지금 화면의 상자들
    public List<WordCard> cards = new List<WordCard>();  // 아래에서 뽑아준 단어 카드들
    List<int> _slotCardIndex = new List<int>();          // 각 상자에 놓인 카드 번호 (-1이면 빈 상자)
    public int wrongCount = 0;                           // 몇 번 틀렸는지

    // 민원 해석 시작 (상자 + 카드 만들어줌)
    public void StartComplaint(ComplaintDefinition complaint)
    {
        currentComplaint = complaint;
        wrongCount = 0;
        slots.Clear();
        cards.Clear();
        _slotCardIndex.Clear();

        // 상자 만들기 (민원에 정해진 자리들)
        for (int i = 0; i < complaint.slots.Count; i++)
        {
            WordSlot slot = new WordSlot();
            slot.category = complaint.slots[i].category;
            slot.correctWord = complaint.slots[i].correctWord;
            slot.placedWord = null;
            slots.Add(slot);
            _slotCardIndex.Add(-1);
        }

        // 카드 만들기 (정답 + 오답 섞인 cardPool 로)
        for (int i = 0; i < complaint.cardPool.Count; i++)
        {
            WordCard card = new WordCard();
            card.word = complaint.cardPool[i];
            card.isUsed = false;
            cards.Add(card);
        }
    }

    // 카드를 상자에 놓기
    public void PlaceCard(int cardIndex, int slotIndex)
    {
        if (cardIndex < 0 || cardIndex >= cards.Count)
        {
            return;
        }
        if (slotIndex < 0 || slotIndex >= slots.Count)
        {
            return;
        }

        WordCard card = cards[cardIndex];
        if (card.isUsed == true)
        {
            return; // 이미 쓴 카드
        }

        // 그 상자에 이미 카드가 있으면 먼저 꺼내줌
        ClearSlot(slotIndex);

        slots[slotIndex].placedWord = card.word;
        card.isUsed = true;
        _slotCardIndex[slotIndex] = cardIndex;
    }

    // 상자에서 카드 빼기 (되돌리기)
    public void ClearSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Count)
        {
            return;
        }

        int cardIndex = _slotCardIndex[slotIndex];
        if (cardIndex < 0)
        {
            return; // 빈 상자
        }

        cards[cardIndex].isUsed = false; // 그 카드 다시 쓸 수 있게
        slots[slotIndex].placedWord = null;
        _slotCardIndex[slotIndex] = -1;
    }

    // 확인. 다 맞으면 true (여기선 판정만 함)
    public bool CheckAnswer()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].IsCorrect() == false)
            {
                return false;
            }
        }

        return true;
    }

    // 틀렸을 때 - 의심도 오르고 틀린 횟수 셈
    public void AddWrong()
    {
        wrongCount++;
        GameManager.instance.suspicion.AddSuspicion(GameManager.instance.config.suspicionWrongTranslation);
    }

    // 너무 많이 틀렸는지 (민원인 나가는 기준)
    public bool IsOverLimit()
    {
        return wrongCount >= GameManager.instance.config.maxWrongTries;
    }
}
