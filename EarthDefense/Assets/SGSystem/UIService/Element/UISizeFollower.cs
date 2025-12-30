using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class UISizeFollower : MonoBehaviour
{
    public RectTransform target; // 추적할 대상
    public bool followWidth = true;
    public bool followHeight = true;

    public Vector2 offset = Vector2.zero; // 추가적인 보정값
    public float scaleFactor = 1f;        // 비율 조정 (예: 0.5배 사이즈 등)

    private RectTransform selfRect;

    void Awake()
    {
        selfRect = GetComponent<RectTransform>();
    }

    void LateUpdate()
    {
        if( target == null || selfRect == null )
            return;

        Vector2 newSize = selfRect.sizeDelta;

        if( followWidth )
            newSize.x = target.rect.width * scaleFactor + offset.x;

        if( followHeight )
            newSize.y = target.rect.height * scaleFactor + offset.y;

        selfRect.sizeDelta = newSize;
    }
}