using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 첫날 낮 튜토리얼
// 안 풀린 기능은 어둡게 해서 못 누르게 막아두고, 순서대로 하나씩 풀어줌
// 기존 코드에 콜백을 심지 않고, 지금 단계의 조건만 매 프레임 확인함
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager instance;

    [SerializeField] List<TutorialStep> steps = new List<TutorialStep>();

    [Header("안내 UI")]
    // 안내문은 빼기로 해서 지금은 인스펙터에서 비워둠 (비어 있으면 아래 코드가 전부 그냥 넘어감)
    // 되살리려면 씬의 TutorialRoot/Space/HintBox 를 다시 끼우면 됨
    [SerializeField] GameObject hintBox;   // 안내 묶음
    [SerializeField] TMP_Text hintText;    // 안내 글
    [SerializeField] RectTransform ring;   // 가리키는 테두리
    [SerializeField] GameObject skipButton;

    [Header("조건 확인에 쓰는 것들")]
    [SerializeField] GameObject pdaPanel;  // PdaOpened 확인용
    [SerializeField] GameObject printPart; // PrintOpened 확인용

    // 안내가 여기는 안 가리게 피해줌 (대사판, PDA 버튼줄)
    [SerializeField] RectTransform[] avoid;

    // 이게 떠 있는 동안은 안내를 아예 접어둠 (수배자 노트처럼 화면을 덮는 창)
    [SerializeField] GameObject[] hideHintWhile;

    [Header("잠금 모양")]
    // 어둡게만 하면 밝은 배경에선 오히려 도드라지고, 어두운 배경(PDA 안)에선 아예 안 보임
    // 그래서 회색 쪽으로 빼고 흐리게 함 - 밝은 데서도 어두운 데서도 "꺼진 것"으로 보임
    [SerializeField] float grayAmount = 0.75f; // 회색에 얼마나 가깝게 (1이면 완전 회색)
    [SerializeField] float fade = 0.6f;        // 얼마나 흐리게
    [SerializeField] float ringPadding = 6f;  // 테두리를 대상보다 얼마나 크게
    [SerializeField] float hintGap = 14f;     // 테두리랑 안내 사이 틈
    [SerializeField] Vector2 homeSpot = new Vector2(0f, -10f); // 가리킬 게 없을 때 안내가 서는 자리

    int _step = 0;
    bool _ready = false;   // 잠금까지 끝났는지
    bool _finished = false;

    // 단계가 시작될 때의 값. 이걸 안 잡아두면 "다음 민원" 같은 조건이
    // 시작하자마자 이미 참이라 안내를 못 읽고 넘어가버림
    int _baseCount = 0;
    int _baseIndex = 0;
    float _shownFor = 0f;
    const float MinShowTime = 1.2f; // 안내는 최소 이만큼은 떠 있음

    // 잠글 때 원래 색을 기억해뒀다가 풀 때 되돌림
    Dictionary<Graphic, Color> _originalColors = new Dictionary<Graphic, Color>();
    List<GameObject> _locked = new List<GameObject>();
    float _rescanTimer = 0f;

    public int currentStep { get { return _step; } }

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        SetObject(hintBox, false);
        SetObject(skipButton, false);
    }

    // 저장에서 불러온 단계로 맞추기 (SaveManager 가 불러줌)
    public void SetStep(int step)
    {
        _step = step;
        _ready = false; // 다음 프레임에 다시 잠그고 다시 그림
    }

    void Update()
    {
        if (GameManager.instance == null)
        {
            return; // 아직 게임 준비 전
        }

        if (_finished == true)
        {
            return;
        }

        // 첫날 낮에만 함 (이어하기로 둘째 날부터 들어오면 튜토리얼 없음)
        if (GameManager.instance.calendar.day > 1 || _step >= steps.Count)
        {
            Finish();
            return;
        }

        // 아직 낮이 시작되기 전(제목 화면 등)이면 안내를 안 띄움
        // 안 그러면 제목 화면 위에 안내랑 건너뛰기가 떠서 새 게임 버튼을 가림
        GamePhase phase = GameManager.instance.phase;
        if (phase != GamePhase.DayWork && phase != GamePhase.DayResult)
        {
            SetObject(hintBox, false);
            SetObject(skipButton, false);
            SetObject(ring == null ? null : ring.gameObject, false);
            return;
        }

        SetObject(skipButton, true);

        if (_ready == false)
        {
            LockAll();
            for (int i = 0; i <= _step && i < steps.Count; i++)
            {
                Unlock(steps[i]);
            }
            ShowStep();
            _ready = true;
        }

        // 잠긴 것 안에 나중에 생긴 게 있으면 그것도 어둡게 (카드처럼 도중에 만들어지는 것들)
        _rescanTimer -= Time.deltaTime;
        if (_rescanTimer <= 0f)
        {
            _rescanTimer = 0.25f;
            for (int i = 0; i < _locked.Count; i++)
            {
                DarkenTree(_locked[i]);
            }
        }

        FollowTarget();

        _shownFor += Time.deltaTime;
        if (_shownFor < MinShowTime)
        {
            return; // 안내를 읽을 틈은 주고 넘김
        }

        if (IsStepDone(steps[_step]) == true)
        {
            _step++;
            SaveStep();

            if (_step >= steps.Count)
            {
                Finish();
                return;
            }

            Unlock(steps[_step]);
            ShowStep();
        }
    }

    // 지금 단계 조건이 채워졌는지
    bool IsStepDone(TutorialStep step)
    {
        GameManager gm = GameManager.instance;

        if (step.doneWhen == TutorialCheck.CardPlaced)
        {
            List<WordSlot> slots = gm.complaint.translation.slots;
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].placedWord != null)
                {
                    return true;
                }
            }
            return false;
        }
        if (step.doneWhen == TutorialCheck.TranslationDone)
        {
            return gm.complaint.stage == ComplaintStage.Resolve;
        }
        if (step.doneWhen == TutorialCheck.PdaOpened)
        {
            return pdaPanel != null && pdaPanel.activeInHierarchy == true;
        }
        if (step.doneWhen == TutorialCheck.PrintOpened)
        {
            return printPart != null && printPart.activeInHierarchy == true;
        }
        if (step.doneWhen == TutorialCheck.ComplaintDone)
        {
            return gm.complaint.successCount + gm.complaint.failCount > _baseCount;
        }
        if (step.doneWhen == TutorialCheck.NextComplaint)
        {
            return gm.complaint.currentIndex > _baseIndex;
        }
        if (step.doneWhen == TutorialCheck.DayResult)
        {
            return gm.phase == GamePhase.DayResult;
        }

        return false;
    }

    // 안내 글이랑 테두리 다시 그리기
    void ShowStep()
    {
        if (_step >= steps.Count)
        {
            return;
        }

        TutorialStep step = steps[_step];

        // 이 단계가 시작될 때의 값을 기준으로 잡아둠
        if (GameManager.instance != null)
        {
            _baseCount = GameManager.instance.complaint.successCount + GameManager.instance.complaint.failCount;
            _baseIndex = GameManager.instance.complaint.currentIndex;
        }
        _shownFor = 0f;

        SetObject(hintBox, string.IsNullOrEmpty(step.hint) == false);
        SetObject(skipButton, true);

        if (hintText != null)
        {
            hintText.text = step.hint;
        }

        SetObject(ring == null ? null : ring.gameObject, step.target != null);
        FollowTarget();
    }

    // 테두리랑 안내를 대상 자리로 옮김 (대상이 움직여도 따라가게 매 프레임)
    void FollowTarget()
    {
        if (_step >= steps.Count || ring == null)
        {
            return;
        }

        // 화면을 덮는 창이 떠 있으면 안내를 통째로 접어둠 (그 위에 떠서 창을 가림)
        if (IsCoveredByWindow() == true)
        {
            SetObject(hintBox, false);
            SetObject(ring.gameObject, false);
            return;
        }

        SetObject(hintBox, true);

        RectTransform target = steps[_step].target;

        // 가리킬 게 없거나 아직 안 보이면 테두리는 접고 안내만 정해진 자리에 둠
        // (여기서 그냥 돌아가면 안내가 직전 단계 자리에 그대로 남음)
        if (target == null || target.gameObject.activeInHierarchy == false)
        {
            SetObject(ring.gameObject, false);
            if (hintBox != null)
            {
                RectTransform idle = hintBox.GetComponent<RectTransform>();
                if (idle != null)
                {
                    idle.anchoredPosition = homeSpot;
                }
            }
            return;
        }

        SetObject(ring.gameObject, true);

        RectTransform space = ring.parent as RectTransform;
        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);

        Vector2 min = space.InverseTransformPoint(corners[0]);
        Vector2 max = space.InverseTransformPoint(corners[2]);

        // 자기 그림이 없는 빈 묶음(손패처럼)이면 테두리가 헐렁해짐 - 안에 든 것만 감싸게 좁힘
        // 버튼처럼 자기 그림이 있는 건 그대로 둠 (아이콘만 감싸버리면 안 됨)
        Vector2 innerMin = Vector2.zero;
        Vector2 innerMax = Vector2.zero;
        bool hasInner = false;
        bool isContainer = target.GetComponent<Graphic>() == null;
        for (int i = 0; isContainer == true && i < target.childCount; i++)
        {
            RectTransform child = target.GetChild(i) as RectTransform;
            if (child == null || child.gameObject.activeInHierarchy == false)
            {
                continue;
            }

            // 지금 끌고 있는 카드는 빼고 셈
            // 안 그러면 카드를 집어서 위로 올리는 동안 테두리가 그쪽까지 쭉 늘어남
            // (끄는 동안 WordCardUI 가 blocksRaycasts 를 꺼두는 걸 표시로 씀)
            CanvasGroup group = child.GetComponent<CanvasGroup>();
            if (group != null && group.blocksRaycasts == false)
            {
                continue;
            }

            child.GetWorldCorners(corners);
            for (int c = 0; c < 4; c++)
            {
                Vector2 p = space.InverseTransformPoint(corners[c]);
                if (hasInner == false)
                {
                    innerMin = p;
                    innerMax = p;
                    hasInner = true;
                    continue;
                }
                innerMin = Vector2.Min(innerMin, p);
                innerMax = Vector2.Max(innerMax, p);
            }
        }

        if (hasInner == true)
        {
            // 자식이 묶음 밖으로 삐져나온 경우(글자칸이 버튼보다 큰 것 등)는 묶음 안으로 잘라둠
            min = Vector2.Max(min, Vector2.Min(innerMin, innerMax));
            max = Vector2.Min(max, Vector2.Max(innerMin, innerMax));
        }

        Vector2 size = max - min;
        Vector2 center = (max + min) * 0.5f;

        ring.sizeDelta = size + Vector2.one * (ringPadding * 2f);
        ring.anchoredPosition = center;

        // 안내는 대상 위/아래/옆 중에 다른 걸 제일 덜 가리는 자리로
        if (hintBox != null)
        {
            RectTransform hintRt = hintBox.GetComponent<RectTransform>();
            if (hintRt != null)
            {
                float gapY = ring.sizeDelta.y * 0.5f + hintGap + hintRt.sizeDelta.y * 0.5f;
                float gapX = ring.sizeDelta.x * 0.5f + hintGap + hintRt.sizeDelta.x * 0.5f;

                float sideLimit = space.rect.width * 0.5f - hintRt.sizeDelta.x * 0.5f - 10f;
                float topLimit = space.rect.height * 0.5f - hintRt.sizeDelta.y * 0.5f - 10f;

                float midX = Mathf.Clamp(center.x, -sideLimit, sideLimit);
                float midY = Mathf.Clamp(center.y, -topLimit, topLimit);

                // 위, 아래, 왼쪽, 오른쪽 순서로 봄 (같으면 앞쪽이 이김)
                // 네 자리가 다 뭔가를 덮으면 마지막으로 빈 자리(homeSpot)까지 물러남
                // 대상에서 멀어지지만 테두리가 가리키고 있으니 카드를 덮는 것보단 나음
                Vector2[] spots = new Vector2[]
                {
                    new Vector2(midX, center.y + gapY),
                    new Vector2(midX, center.y - gapY),
                    new Vector2(center.x - gapX, midY),
                    new Vector2(center.x + gapX, midY),
                    homeSpot,
                };

                Vector2 best = spots[0];
                float bestBad = float.MaxValue;

                for (int i = 0; i < spots.Length; i++)
                {
                    Vector2 spot = spots[i];
                    if (Mathf.Abs(spot.x) > sideLimit || Mathf.Abs(spot.y) > topLimit)
                    {
                        continue; // 화면 밖
                    }

                    float bad = Covered(space, spot.x, spot.y, hintRt);
                    if (bad < bestBad)
                    {
                        bestBad = bad;
                        best = spot;
                    }
                }

                hintRt.anchoredPosition = best;
            }
        }
    }

    // 수배자 노트 같은 창이 떠 있는지
    bool IsCoveredByWindow()
    {
        if (hideHintWhile == null)
        {
            return false;
        }

        for (int i = 0; i < hideHintWhile.Length; i++)
        {
            if (hideHintWhile[i] != null && hideHintWhile[i].activeInHierarchy == true)
            {
                return true;
            }
        }

        return false;
    }

    // 안내를 이 자리에 두면 가리면 안 되는 것(대사판, PDA 버튼줄)을 얼마나 덮는지
    float Covered(RectTransform space, float x, float y, RectTransform hintRt)
    {
        if (avoid == null)
        {
            return 0f;
        }

        Vector2 half = hintRt.sizeDelta * 0.5f;
        float left = x - half.x;
        float right = x + half.x;
        float bottom = y - half.y;
        float top = y + half.y;

        Vector3[] corners = new Vector3[4];
        float sum = 0f;

        for (int i = 0; i < avoid.Length; i++)
        {
            RectTransform one = avoid[i];
            if (one == null || one.gameObject.activeInHierarchy == false)
            {
                continue;
            }

            one.GetWorldCorners(corners);
            Vector2 aMin = space.InverseTransformPoint(corners[0]);
            Vector2 aMax = space.InverseTransformPoint(corners[2]);

            float w = Mathf.Min(right, aMax.x) - Mathf.Max(left, aMin.x);
            float h = Mathf.Min(top, aMax.y) - Mathf.Max(bottom, aMin.y);
            if (w > 0f && h > 0f)
            {
                sum += w * h;
            }
        }

        return sum;
    }

    // 잠긴 것의 색
    Color LockedColor(Color c)
    {
        float gray = 0.5f;
        return new Color(
            Mathf.Lerp(c.r, gray, grayAmount),
            Mathf.Lerp(c.g, gray, grayAmount),
            Mathf.Lerp(c.b, gray, grayAmount),
            c.a * fade);
    }

    // ---- 잠그고 풀기 ----

    void LockAll()
    {
        _locked.Clear();
        for (int i = 0; i < steps.Count; i++)
        {
            GameObject[] targets = steps[i].unlocks;
            if (targets == null)
            {
                continue;
            }
            for (int j = 0; j < targets.Length; j++)
            {
                Lock(targets[j]);
            }
        }
    }

    void Lock(GameObject go)
    {
        if (go == null || _locked.Contains(go) == true)
        {
            return;
        }

        _locked.Add(go);

        CanvasGroup group = go.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = go.AddComponent<CanvasGroup>();
        }
        group.interactable = false;
        group.blocksRaycasts = false;

        DarkenTree(go);
    }

    void Unlock(TutorialStep step)
    {
        if (step == null || step.unlocks == null)
        {
            return;
        }

        for (int i = 0; i < step.unlocks.Length; i++)
        {
            Unlock(step.unlocks[i]);
        }
    }

    void Unlock(GameObject go)
    {
        if (go == null)
        {
            return;
        }

        _locked.Remove(go);

        CanvasGroup group = go.GetComponent<CanvasGroup>();
        if (group != null)
        {
            group.interactable = true;
            group.blocksRaycasts = true;
        }

        // 기억해둔 원래 색으로 되돌림
        Graphic[] graphics = go.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            Color original;
            if (_originalColors.TryGetValue(graphics[i], out original) == true)
            {
                graphics[i].color = original;
                _originalColors.Remove(graphics[i]);
            }
        }
    }

    // 안 풀린 것들 어둡게 (색만 낮춤 - 알파를 내리면 밝은 배경에선 오히려 밝아짐)
    void DarkenTree(GameObject go)
    {
        if (go == null)
        {
            return;
        }

        Graphic[] graphics = go.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic g = graphics[i];
            if (_originalColors.ContainsKey(g) == true)
            {
                continue; // 이미 어둡게 해둔 것
            }

            Color c = g.color;
            _originalColors.Add(g, c);
            g.color = LockedColor(c);
        }
    }

    // ---- 끝내기 ----

    // 건너뛰기 버튼 OnClick 에 연결
    public void OnClickSkip()
    {
        Finish();
    }

    void Finish()
    {
        if (_finished == true)
        {
            return;
        }
        _finished = true;

        // 남은 것 전부 풀어줌
        for (int i = 0; i < steps.Count; i++)
        {
            Unlock(steps[i]);
        }

        _step = steps.Count;
        SaveStep();

        SetObject(hintBox, false);
        SetObject(skipButton, false);
        SetObject(ring == null ? null : ring.gameObject, false);
    }

    void SaveStep()
    {
        PlayerPrefs.SetInt("tutorialStep", _step);
    }

    void SetObject(GameObject go, bool on)
    {
        if (go != null && go.activeSelf != on)
        {
            go.SetActive(on);
        }
    }
}
