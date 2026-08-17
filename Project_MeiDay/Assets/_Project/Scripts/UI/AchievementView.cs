using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 업적 화면 - 딸 때 뜨는 알림이랑 목록 창을 그려줌
// 지금까지는 업적을 따도 Debug.Log 만 찍히고 화면엔 아무것도 안 나왔음
public class AchievementView : MonoBehaviour
{
    [SerializeField] GameObject popUp;         // 땄을 때 잠깐 뜨는 알림
    [SerializeField] TMP_Text popUpNameText;   // 알림에 뜨는 업적 이름
    [SerializeField] float popUpSeconds = 3f;  // 알림 보여주는 시간

    [SerializeField] GameObject window;        // 업적 목록 창
    [SerializeField] Transform listParent;     // 목록 줄들이 놓이는 자리
    [SerializeField] GameObject linePrefab;    // 목록 한 줄 프리팹

    [SerializeField] GameObject newTag;        // 새로 딴 게 있을 때 뜨는 표시
    [SerializeField] TMP_Text newCountText;    // 새로 딴 개수

    // 기록 초기화 확인창 (한 번 지우면 되돌릴 수 없어서 꼭 물어봄)
    [SerializeField] GameObject resetConfirmPanel;

    // 아직 안 딴 업적은 글자를 흐리게 (딴 것만 눈에 들어오게)
    // 줄 전체를 CanvasGroup 으로 흐리게 하면 배경까지 투명해져서 뒤 화면이 비침 - 글자만 건드림
    static readonly Color GotTextColor = new Color(0.1f, 0.1f, 0.1f, 1f);
    static readonly Color LockedTextColor = new Color(0.62f, 0.62f, 0.62f, 1f);

    List<GameObject> _lines = new List<GameObject>();
    float _popUpLeft = 0f;
    int _newCount = 0;
    bool _windowWasOpen = false;

    void Start()
    {
        SetOn(popUp, false);
        SetOn(resetConfirmPanel, false);
        RefreshNewTag();
        RebuildList();

        if (GameManager.instance != null && GameManager.instance.achievements != null)
        {
            GameManager.instance.achievements.OnUnlocked += OnUnlocked;
        }
    }

    void OnDestroy()
    {
        if (GameManager.instance != null && GameManager.instance.achievements != null)
        {
            GameManager.instance.achievements.OnUnlocked -= OnUnlocked;
        }
    }

    void Update()
    {
        // 알림 시간 다 되면 끔
        if (_popUpLeft > 0f)
        {
            _popUpLeft -= Time.deltaTime;
            if (_popUpLeft <= 0f)
            {
                SetOn(popUp, false);
            }
        }

        // 목록 창은 버튼(QuickButton)이 켜주니까, 방금 열렸는지는 여기서 알아챔
        bool open = (window != null && window.activeSelf == true);
        if (open == true && _windowWasOpen == false)
        {
            _newCount = 0;
            RefreshNewTag();
        }
        _windowWasOpen = open;
    }

    // 업적 땄을 때 (매니저가 불러줌)
    void OnUnlocked(AchievementDefinition ach)
    {
        if (ach == null)
        {
            return;
        }

        if (popUpNameText != null)
        {
            popUpNameText.text = ach.title;
        }
        SetOn(popUp, true);
        _popUpLeft = popUpSeconds;

        _newCount++;
        RefreshNewTag();
        RebuildList(); // 목록도 같이 갱신해두면 나중에 열었을 때 최신
    }

    // 새로 딴 개수 표시
    void RefreshNewTag()
    {
        SetOn(newTag, _newCount > 0);

        if (newCountText != null)
        {
            newCountText.text = _newCount.ToString();
        }
    }

    // 업적 목록 다시 만들기
    void RebuildList()
    {
        for (int i = 0; i < _lines.Count; i++)
        {
            if (_lines[i] != null)
            {
                Destroy(_lines[i]);
            }
        }
        _lines.Clear();

        if (listParent == null || linePrefab == null)
        {
            return; // 아직 안 꽂아둠
        }

        GameManager gm = GameManager.instance;
        if (gm == null || gm.content == null || gm.achievements == null)
        {
            return;
        }

        List<AchievementDefinition> all = gm.content.achievements;
        for (int i = 0; i < all.Count; i++)
        {
            AchievementDefinition ach = all[i];
            if (ach == null)
            {
                continue;
            }

            GameObject line = Instantiate(linePrefab, listParent);
            _lines.Add(line);
            FillLine(line, ach, gm.achievements.HasUnlocked(ach.id));
        }
    }

    // 목록 한 줄 채우기
    void FillLine(GameObject line, AchievementDefinition ach, bool got)
    {
        // 숨은 업적은 따기 전엔 뭔지 안 알려줌
        string title = ach.title;
        string desc = ach.description;
        if (ach.hidden == true && got == false)
        {
            title = "???";
            desc = "숨은 업적";
        }

        Color textColor = LockedTextColor;
        if (got == true)
        {
            textColor = GotTextColor;
        }
        SetText(line, "Background/AchieveName", title, textColor);
        SetText(line, "Background/AchieveDesc", desc, textColor);

        // 업적 그림이랑 그 뒷판 - 그림이 아직 안 들어왔으면 둘 다 꺼서 빈 사각형이 안 보이게
        bool hasIcon = (ach.icon != null && got == true);
        Transform iconT = line.transform.Find("Background/AchiveImage");
        if (iconT != null)
        {
            Image icon = iconT.GetComponent<Image>();
            if (icon != null)
            {
                icon.sprite = ach.icon;
                icon.enabled = hasIcon;
            }
        }
        SetGraphicOn(line, "Background/AchivementBackground", hasIcon);

        // 줄마다 붙어있는 NewTag 는 목록에선 안 씀 (버튼 쪽에만 띄움)
        Transform tagT = line.transform.Find("NewTag");
        if (tagT != null)
        {
            tagT.gameObject.SetActive(false);
        }
    }

    // 줄 안에 있는 그림칸 켜고 끄기
    void SetGraphicOn(GameObject line, string path, bool on)
    {
        Transform t = line.transform.Find(path);
        if (t == null)
        {
            return;
        }

        Graphic g = t.GetComponent<Graphic>();
        if (g != null)
        {
            g.enabled = on;
        }
    }

    // 줄 안에 있는 글자칸 채우기 (없으면 그냥 넘어감)
    void SetText(GameObject line, string path, string text, Color color)
    {
        Transform t = line.transform.Find(path);
        if (t == null)
        {
            return;
        }

        TMP_Text tm = t.GetComponent<TMP_Text>();
        if (tm != null)
        {
            tm.text = text;
            tm.color = color;
        }
    }

    // ---- 아래는 버튼 OnClick 에 연결해서 씀 ----

    // 기록 초기화 버튼 - 바로 지우지 않고 확인창을 띄움
    // 업적은 회차를 넘겨 남는 유일한 기록이라 실수로 한 번 누르면 복구가 안 됨
    public void OnClickResetAchievements()
    {
        if (resetConfirmPanel == null)
        {
            // 확인창을 안 꽂아뒀으면 아무것도 안 함
            // (여기서 그냥 지워버리면 물어보지도 않고 기록이 날아감)
            Debug.LogWarning("업적 초기화 확인창이 연결 안 됨 - 안전하게 아무것도 안 함");
            return;
        }

        SetOn(resetConfirmPanel, true);
    }

    // 확인창에서 "예" - 여기서 진짜로 지움
    public void OnClickConfirmReset()
    {
        SetOn(resetConfirmPanel, false);

        if (GameManager.instance == null || GameManager.instance.achievements == null)
        {
            return;
        }

        GameManager.instance.achievements.ResetAll();

        // 지운 걸 화면에도 바로 반영 (목록은 다시 만들고 새 표시는 없앰)
        _newCount = 0;
        RefreshNewTag();
        SetOn(popUp, false);
        _popUpLeft = 0f;
        RebuildList();
    }

    // 확인창에서 "아니오"
    public void OnClickCancelReset()
    {
        SetOn(resetConfirmPanel, false);
    }

    // 켜고 끄기 (아직 안 꽂아뒀어도 안 터지게)
    void SetOn(GameObject target, bool on)
    {
        if (target == null)
        {
            return;
        }

        target.SetActive(on);
    }
}
