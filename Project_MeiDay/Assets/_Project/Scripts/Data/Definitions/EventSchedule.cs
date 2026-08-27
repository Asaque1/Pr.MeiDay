using System.Collections.Generic;
using UnityEngine;

// 일차 고정 이벤트 한 줄 ("3일차 아침에 이 이벤트")
[System.Serializable]
public class ScheduledEvent
{
    public int day = 1;                  // 며칠째에 나올지
    public EventSlot slot;               // 하루 중 어느 자리에 낄지
    public EventDefinition eventDef;     // 무슨 이벤트를 띄울지
}

// 일차 고정 이벤트만 모아놓은 표
// 이벤트 파일을 하나씩 열어보지 않아도 "며칠에 뭐가 나오는지" 를 여기서 한눈에 보고 고침
// (확률로 나오는 것, 조건 맞으면 나오는 것은 예전처럼 이벤트 파일 쪽에 그대로 둠)
[CreateAssetMenu(fileName = "EventSchedule", menuName = "MeiDay/Event Schedule")]
public class EventSchedule : ScriptableObject
{
    public List<ScheduledEvent> days = new List<ScheduledEvent>();

    // 오늘 이 자리에서 틀 고정 이벤트 고르기 (없으면 null)
    // 지난 날짜인데 아직 안 본 게 있으면 그것부터 (하루에 여러 개가 몰리면 밀려서 나옴)
    public EventDefinition Pick(int day, EventSlot slot, EventManager events)
    {
        ScheduledEvent best = null;

        for (int i = 0; i < days.Count; i++)
        {
            ScheduledEvent row = days[i];
            if (row == null || row.eventDef == null)
            {
                continue; // 아직 안 채운 줄
            }
            if (row.slot != slot)
            {
                continue;
            }
            if (row.day > day)
            {
                continue; // 아직 날짜 안 됨
            }
            if (events != null && events.HasSeen(row.eventDef.id) == true)
            {
                continue; // 이미 본 것
            }

            if (best == null || row.day < best.day)
            {
                best = row;
            }
        }

        if (best == null)
        {
            return null;
        }

        return best.eventDef;
    }

    // 표에 적어둔 이벤트인지 (이벤트 파일 쪽 고정 설정이랑 겹쳐서 두 번 나오지 않게 씀)
    public bool Contains(EventDefinition ev)
    {
        if (ev == null)
        {
            return false;
        }

        for (int i = 0; i < days.Count; i++)
        {
            if (days[i] != null && days[i].eventDef == ev)
            {
                return true;
            }
        }
        return false;
    }
}
