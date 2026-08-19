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
        // 후보를 여러 개 적어놨으면 그 중 하나를 뽑아서 이번 판 정답으로 씀
        for (int i = 0; i < complaint.slots.Count; i++)
        {
            WordSlot slot = new WordSlot();
            slot.category = complaint.slots[i].category;
            slot.correctWord = complaint.slots[i].PickAnswer();
            slot.placedWord = null;
            slots.Add(slot);
            _slotCardIndex.Add(-1);
        }

        // 카드 만들기: 정답 + 오답 모아서 랜덤 순서로 깔아줌
        List<WordDefinition> cardWords = new List<WordDefinition>();

        // 정답 단어들 (상자마다 하나씩)
        for (int i = 0; i < slots.Count; i++)
        {
            cardWords.Add(slots[i].correctWord);
        }

        // 오답 단어들 (상자 종류별로 정해진 개수만큼 랜덤으로 뽑음)
        PickWrongWords(complaint, cardWords);

        // 순서 섞기
        Shuffle(cardWords);

        for (int i = 0; i < cardWords.Count; i++)
        {
            WordCard card = new WordCard();
            card.word = cardWords[i];
            card.isUsed = false;
            cards.Add(card);
        }

        // 번역서: 상자 하나를 미리 풀어줌 (한 단어 정답 공개)
        if (GameManager.instance.inventory.GetEffectPower(ItemEffect.FreeAnswerWord) > 0)
        {
            RevealOneAnswer();
        }
    }

    // 랜덤 상자 하나에 정답 카드를 미리 꽂아줌
    void RevealOneAnswer()
    {
        if (slots.Count == 0)
        {
            return;
        }

        int slotIndex = Random.Range(0, slots.Count);
        WordDefinition answer = slots[slotIndex].correctWord;

        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i].word == answer && cards[i].isUsed == false)
            {
                PlaceCard(i, slotIndex);
                return;
            }
        }
    }

    // 오답 후보에서 상자 종류에 맞는 걸 랜덤으로 뽑아서 결과에 넣어줌
    void PickWrongWords(ComplaintDefinition complaint, List<WordDefinition> result)
    {
        // 오답 후보 모으기
        // 따로 적어둔 wrongWords 에다가, 상자에서 안 뽑힌 후보들까지 같이 씀
        // 안 뽑힌 후보는 같은 종류의 다른 서류라 제일 그럴싸한 오답이 됨
        // (덕분에 데이터에 오답을 따로 안 적어도 카드가 채워짐)
        List<WordDefinition> raw = new List<WordDefinition>();
        raw.AddRange(complaint.wrongWords);
        for (int s = 0; s < complaint.slots.Count; s++)
        {
            raw.AddRange(complaint.slots[s].candidateWords);
        }

        // 후보 정리 (데이터 실수 대비: 빈 칸, 정답이랑 같은 단어, 두 번 적힌 단어는 걸러냄)
        List<WordDefinition> pool = new List<WordDefinition>();
        for (int i = 0; i < raw.Count; i++)
        {
            WordDefinition w = raw[i];
            if (w == null)
            {
                continue; // 인스펙터에서 빈 칸으로 남은 것
            }
            if (result.Contains(w) == true)
            {
                continue; // 정답이랑 같은 단어는 오답이 될 수 없음
            }
            if (pool.Contains(w) == true)
            {
                continue; // 같은 단어 두 번 적힌 것
            }
            pool.Add(w);
        }

        // 서점 영구템 레벨만큼 오답 카드가 줄어듬 (사전이 좋아질수록 헷갈릴 게 없음)
        int perSlot = GameManager.instance.config.wrongCardsPerSlot
            - (int)GameManager.instance.inventory.GetEffectPower(ItemEffect.FewerWrongCards);
        if (perSlot < 0)
        {
            perSlot = 0;
        }

        for (int s = 0; s < slots.Count; s++)
        {
            for (int n = 0; n < perSlot; n++)
            {
                int found = FindRandomByCategory(pool, slots[s].category);
                if (found < 0)
                {
                    break; // 이 종류 오답이 더 없음
                }

                result.Add(pool[found]);
                pool.RemoveAt(found);
            }
        }
    }

    // 풀에서 그 종류인 것 중 하나를 랜덤으로 골라 번호 돌려줌 (없으면 -1)
    int FindRandomByCategory(List<WordDefinition> pool, WordCategory category)
    {
        List<int> matches = new List<int>();
        for (int i = 0; i < pool.Count; i++)
        {
            if (pool[i].category == category)
            {
                matches.Add(i);
            }
        }

        if (matches.Count == 0)
        {
            return -1;
        }

        return matches[Random.Range(0, matches.Count)];
    }

    // 리스트 순서 섞기
    void Shuffle(List<WordDefinition> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            WordDefinition temp = list[i];
            list[i] = list[j];
            list[j] = temp;
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
        if (card.isRemoved == true)
        {
            return; // 떨어져 사라진 카드
        }

        // 그 상자에 이미 카드가 있으면 먼저 꺼내줌
        ClearSlot(slotIndex);

        slots[slotIndex].placedWord = card.word;
        card.isUsed = true;
        _slotCardIndex[slotIndex] = cardIndex;

        // 카드 놓는 소리
        if (SoundManager.instance != null)
        {
            SoundManager.instance.PlaySfx(SoundManager.instance.cardPlace);
        }
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

    // 빈칸을 하나도 안 남기고 다 채웠는지 (맞았는지는 안 봄)
    // 이게 true 가 돼야 확인 버튼이 나옴
    public bool IsAllFilled()
    {
        if (slots.Count == 0)
        {
            return false; // 상자가 없는 민원은 제출할 것도 없음
        }

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].placedWord == null)
            {
                return false;
            }
        }

        return true;
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

    // 틀렸을 때 - 의심도 오르고 틀린 횟수 세고 틀린 카드는 떨어짐
    public void AddWrong()
    {
        wrongCount++;
        GameManager.instance.suspicion.AddSuspicion(GameManager.instance.config.suspicionWrongTranslation);
        DropWrongCards();
    }

    // 틀리게 놓은 오답 카드들이 떨어져 사라짐 (다시 풀 때는 좀 쉬워짐)
    void DropWrongCards()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            WordSlot slot = slots[i];
            if (slot.placedWord == null || slot.IsCorrect() == true)
            {
                continue; // 빈 상자거나 맞게 놓은 건 그대로
            }

            // 다른 상자의 정답 단어면 없애면 안 됨 (그거 없으면 못 푸니까)
            if (IsAnswerWord(slot.placedWord) == true)
            {
                continue;
            }

            int cardIndex = _slotCardIndex[i];
            if (cardIndex >= 0)
            {
                cards[cardIndex].isRemoved = true; // 카드가 떨어져 사라짐
            }
            slot.placedWord = null;
            _slotCardIndex[i] = -1;
        }
    }

    // 이 단어가 어느 상자든 정답인지
    bool IsAnswerWord(WordDefinition word)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].correctWord == word)
            {
                return true;
            }
        }
        return false;
    }

    // 너무 많이 틀렸는지 (민원인 나가는 기준)
    public bool IsOverLimit()
    {
        return wrongCount >= GameManager.instance.config.maxWrongTries;
    }
}
