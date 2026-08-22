using UnityEngine;
using UnityEngine.EventSystems;

public class LobbyButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] RectTransform target;

    [Header("Position")]
    [SerializeField] Vector2 movingTransform;
    [SerializeField] float positionSmoothTime = 0.15f;

    [Header("Scale")]
    [SerializeField] float hoverScale = 1.05f;
    [SerializeField] float scaleSmoothTime = 0.15f;

    Vector2 baseTransform;
    Vector2 targetPosition;
    Vector2 positionVelocity;

    Vector3 baseScale;
    Vector3 targetScale;
    Vector3 scaleVelocity;

    void Start()
    {
        target = GetComponent<RectTransform>();

        baseTransform = target.anchoredPosition;
        targetPosition = baseTransform;

        baseScale = target.localScale;
        targetScale = baseScale;
    }

    void Update()
    {
        // 위치
        target.anchoredPosition = Vector2.SmoothDamp(
            target.anchoredPosition,
            targetPosition,
            ref positionVelocity,
            positionSmoothTime
        );

        // 스케일
        target.localScale = Vector3.SmoothDamp(
            target.localScale,
            targetScale,
            ref scaleVelocity,
            scaleSmoothTime
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetPosition = baseTransform + movingTransform;
        targetScale = baseScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetPosition = baseTransform;
        targetScale = baseScale;
    }
}