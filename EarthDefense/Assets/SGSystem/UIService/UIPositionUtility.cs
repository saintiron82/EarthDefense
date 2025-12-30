using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG.UI
{
    public static class UIPositionUtility
    {
        /// <summary>
        /// 주어진 targetRect가 canvasRect의 화면을 벗어나지 않도록 위치를 보정합니다.
        /// </summary>
        public static Vector2 TargetPositionClamp( RectTransform canvasRect, RectTransform targetRect, Vector2 targetPos )
        {
            // DESC :: Canvas 컴포넌트 가져오기
            Canvas canvas = canvasRect.GetComponent<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning($"[UIPositionUtility] Canvas component not found on {canvasRect.name}");
                return targetPos;
            }

            // DESC :: 디버그 정보 출력
            Debug.Log($"[UIPositionUtility] TargetPositionClamp - Canvas: {canvasRect.name}, RenderMode: {canvas.renderMode}, " +
                     $"CanvasSize: {canvasRect.rect.size}, Target: {targetRect.name}, TargetSize: {targetRect.rect.size}, TargetPos: {targetPos}");
            
            Vector2 clampedPos = targetPos;
            
            switch (canvas.renderMode)
            {
                case RenderMode.ScreenSpaceOverlay:
                    clampedPos = ClampForScreenSpaceOverlay(canvasRect, targetRect, targetPos);
                    break;
                    
                case RenderMode.ScreenSpaceCamera:
                    clampedPos = ClampForScreenSpaceCamera(canvasRect, targetRect, targetPos, canvas.worldCamera);
                    break;
                    
                case RenderMode.WorldSpace:
                    clampedPos = ClampForWorldSpace(canvasRect, targetRect, targetPos, canvas.worldCamera);
                    break;
            }
            
            Debug.Log($"[UIPositionUtility] Final position: {clampedPos} (Delta: {clampedPos - targetPos})");
            
            return clampedPos;
        }

        /// <summary>
        /// ScreenSpaceOverlay 모드를 위한 클램핑
        /// </summary>
        private static Vector2 ClampForScreenSpaceOverlay(RectTransform canvasRect, RectTransform targetRect, Vector2 targetPos)
        {
            var canvasSize = canvasRect.rect.size;
            var targetSize = targetRect.rect.size;
            var pivot = targetRect.pivot;
            
            // DESC :: 캔버스 경계 계산 (중심 기준)
            var halfCanvasSize = canvasSize * 0.5f;
            var canvasMin = -halfCanvasSize;
            var canvasMax = halfCanvasSize;
            
            // DESC :: 타겟의 실제 경계 계산 (피벗 고려)
            var targetMin = targetPos - pivot * targetSize;
            var targetMax = targetMin + targetSize;
            
            Vector2 delta = Vector2.zero;
            
            // DESC :: X축 클램핑
            if (targetMin.x < canvasMin.x)
                delta.x = canvasMin.x - targetMin.x;
            else if (targetMax.x > canvasMax.x)
                delta.x = canvasMax.x - targetMax.x;
                
            // DESC :: Y축 클램핑
            if (targetMin.y < canvasMin.y)
                delta.y = canvasMin.y - targetMin.y;
            else if (targetMax.y > canvasMax.y)
                delta.y = canvasMax.y - targetMax.y;
                
            return targetPos + delta;
        }

        /// <summary>
        /// ScreenSpaceCamera 모드를 위한 클램핑
        /// </summary>
        private static Vector2 ClampForScreenSpaceCamera(RectTransform canvasRect, RectTransform targetRect, Vector2 targetPos, Camera camera)
        {
            // DESC :: ScreenSpaceCamera 모드에서는 기본적으로 ScreenSpaceOverlay와 유사하지만 카메라 설정을 고려
            return ClampForScreenSpaceOverlay(canvasRect, targetRect, targetPos);
        }

        /// <summary>
        /// WorldSpace 모드를 위한 클램핑
        /// </summary>
        private static Vector2 ClampForWorldSpace(RectTransform canvasRect, RectTransform targetRect, Vector2 targetPos, Camera camera)
        {
            // DESC :: WorldSpace에서는 실제 월드 좌표계에서 클램핑
            var canvasSize = canvasRect.rect.size;
            var targetSize = targetRect.rect.size;
            var pivot = targetRect.pivot;
            
            // DESC :: 캔버스의 실제 월드 스케일 고려
            var canvasScale = canvasRect.lossyScale;
            var scaledCanvasSize = new Vector2(canvasSize.x * canvasScale.x, canvasSize.y * canvasScale.y);
            var scaledTargetSize = new Vector2(targetSize.x * targetRect.lossyScale.x, targetSize.y * targetRect.lossyScale.y);
            
            var halfCanvasSize = scaledCanvasSize * 0.5f;
            var canvasMin = -halfCanvasSize;
            var canvasMax = halfCanvasSize;
            
            var targetMin = targetPos - pivot * scaledTargetSize;
            var targetMax = targetMin + scaledTargetSize;
            
            Vector2 delta = Vector2.zero;
            
            if (targetMin.x < canvasMin.x)
                delta.x = canvasMin.x - targetMin.x;
            else if (targetMax.x > canvasMax.x)
                delta.x = canvasMax.x - targetMax.x;
                
            if (targetMin.y < canvasMin.y)
                delta.y = canvasMin.y - targetMin.y;
            else if (targetMax.y > canvasMax.y)
                delta.y = canvasMax.y - targetMax.y;
                
            return targetPos + delta;
        }
    }
}