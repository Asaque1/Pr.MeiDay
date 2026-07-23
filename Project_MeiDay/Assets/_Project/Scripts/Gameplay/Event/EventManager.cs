using System;
using System.Collections.Generic;
using UnityEngine;

// 아침 이벤트를 관리하는 매니저
// 오늘 볼 이벤트 뽑기 + 대사 진행 + 한 번 본 건 다시 안 보여줌
public class EventManager : MonoBehaviour
{
    public List<string> seenEventIds = new List<string>(); // 이미 본 이벤트 id 들 (저장됨)

    public EventDefinition currentEvent; // 지금 보는 중인 이벤트 (없으면 null)
    public int lineIndex = 0;            // 몇 번째 대사인지

    // 대사 상태 바뀌면 알려줌 (UI 가 듣고 다시 그림)
    public event Action OnDialogueUpdated;

    // 오늘 아침에 볼 이벤트 고르기 (없으면 null)
    public EventDefinition PickTodayEvent()
    {
        int day = GameManager.instance.calendar.day;
        List<EventDefinition> all = GameManager.instance.content.events;

        // 1순위: 날짜 고정 이벤트 (같은 날 겹치거나 밀린 게 있으면 이른 날짜 것부터 하나씩)
        EventDefinition fixedPick = null;
        for (int i = 0; i < all.Count; i++)
        {
            EventDefinition ev = all[i];
            if (ev.timing != EventTiming.FixedDay || HasSeen(ev.id) == true)
            {
                continue;
            }
            if (ev.fixedDay > day)
            {
                continue; // 아직 날짜 안 됨
            }
            if (fixedPick == null || ev.fixedDay < fixedPick.fixedDay)
            {
                fixedPick = ev;
            }
        }

        if (fixedPick != null)
        {
            return fixedPick;
        }

        // 일정 아끼기 스위치: 랜덤 이벤트를 확률 없이 목록 순서대로 하나씩 보여줌
        if (GameManager.instance.config.eventsInOrder == true)
        {
            for (int i = 0; i < all.Count; i++)
            {
                EventDefinition ev = all[i];
                if (ev.timing == EventTiming.Random && HasSeen(ev.id) == false)
                {
                    return ev;
                }
            }
            return null;
        }

        // 2순위: 랜덤 이벤트 (안 본 것들을 섞어서 하나씩 확률 굴림)
        List<EventDefinition> candidates = new List<EventDefinition>();
        for (int i = 0; i < all.Count; i++)
        {
            EventDefinition ev = all[i];
            if (ev.timing == EventTiming.Random && HasSeen(ev.id) == false)
            {
                candidates.Add(ev);
            }
        }

        // 순서 섞기 (앞에 적힌 이벤트만 유리하지 않게)
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            EventDefinition temp = candidates[i];
            candidates[i] = candidates[j];
            candidates[j] = temp;
        }

        for (int i = 0; i < candidates.Count; i++)
        {
            if (UnityEngine.Random.value < candidates[i].randomChance)
            {
                return candidates[i];
            }
        }

        return null; // 오늘은 이벤트 없는 아침
    }

    // 이벤트 대사 시작
    public void StartEvent(EventDefinition ev)
    {
        currentEvent = ev;
        lineIndex = 0;

        // 대사가 하나도 없는 이벤트면 바로 끝냄 (데이터 실수 대비)
        if (ev.lines.Count == 0)
        {
            FinishEvent();
            return;
        }

        NotifyUpdated();
    }

    // 지금 나오는 대사 (이벤트 중 아니면 null)
    public EventLine CurrentLine()
    {
        if (currentEvent == null)
        {
            return null;
        }
        if (lineIndex >= currentEvent.lines.Count)
        {
            return null;
        }
        return currentEvent.lines[lineIndex];
    }

    // 다음 대사로 (마지막이었으면 이벤트 끝냄)
    public void AdvanceLine()
    {
        if (currentEvent == null)
        {
            return;
        }

        lineIndex++;

        if (lineIndex >= currentEvent.lines.Count)
        {
            FinishEvent();
            return;
        }

        NotifyUpdated();
    }

    // 이벤트 끝 - 본 걸로 기록하고 게임한테 알려줌
    void FinishEvent()
    {
        EventDefinition done = currentEvent;
        currentEvent = null;

        if (HasSeen(done.id) == false)
        {
            seenEventIds.Add(done.id);
        }

        // 이벤트 내용에 따라 의심도가 오르내림
        if (done.suspicionChange > 0)
        {
            GameManager.instance.suspicion.AddSuspicion(done.suspicionChange);
        }
        else if (done.suspicionChange < 0)
        {
            GameManager.instance.suspicion.ReduceSuspicion(-done.suspicionChange);
        }

        NotifyUpdated();
        GameManager.instance.OnMorningEventDone(done);
    }

    // 이 이벤트 이미 봤는지
    public bool HasSeen(string id)
    {
        for (int i = 0; i < seenEventIds.Count; i++)
        {
            if (seenEventIds[i] == id)
            {
                return true;
            }
        }
        return false;
    }

    // 구독한 UI 한테 알려주기
    void NotifyUpdated()
    {
        if (OnDialogueUpdated != null)
        {
            OnDialogueUpdated();
        }
    }
}
