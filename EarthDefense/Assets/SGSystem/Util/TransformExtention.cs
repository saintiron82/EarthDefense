using UnityEngine;

public static class TransformExtention
{
    public static void ExNormalize(this Transform transform)
    {
        transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        transform.localScale = Vector3.one;
    }

    public static void ExRectTransformNormalize( this Transform transform, RectTransform targetCanvasRectTransform )
    {
        if( transform == null || targetCanvasRectTransform == null )
            return;

        ExSetParentWithFit( transform, targetCanvasRectTransform );
        //RectTransform의 SizeDelta를 고려한 위치 계산이 필요 
        var sourceRectTransform = transform as RectTransform;
        if( sourceRectTransform != null )
        {
            //RectTransform 의 Strecthing을 고려한 Normalized 위치를 계산합니다.
            //stretch타입을 알아낸다
            //sourceRectTransform
            var targetSizeDelta = sourceRectTransform.sizeDelta;
            if( sourceRectTransform.anchorMin.x == 0 && sourceRectTransform.anchorMax.x == 1 )
            {
                targetSizeDelta.x = 0;
            }
            if( sourceRectTransform.anchorMin.y == 0 && sourceRectTransform.anchorMax.y == 1 )
            {
                targetSizeDelta.y = 0;
            }
            sourceRectTransform.sizeDelta = targetSizeDelta;    
        }
    }

    public static void ExSetParentWithFit( this Transform transform, Transform parent ) {
        transform.SetParent( parent );
        transform.ExNormalize();
    }

    public static void ExSetParentWithFullStratch( this Transform transform, Transform parent )
    {
        transform.SetParent( parent );
        transform.ExNormalize();
        var rectTransform = transform as RectTransform;
        if( rectTransform != null )
        {
            rectTransform.FullStratch();
        }
    }

    public static void FullStratch( this RectTransform rectTransform )
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}