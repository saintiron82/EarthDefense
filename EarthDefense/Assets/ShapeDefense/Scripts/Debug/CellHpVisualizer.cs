using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ShapeDefense.Scripts.Debug
{
    /// <summary>
    /// RingSectorDamageMask의 셀 HP를 시각화하는 에디터 전용 유틸리티
    /// 
    /// 사용법:
    /// 1. ChunkEnemy에 이 컴포넌트 추가
    /// 2. "Show Cell HP Gizmos" 체크
    /// 3. 씬 뷰에서 각 셀의 HP가 표시됨
    /// </summary>
    [ExecuteAlways]
    public class CellHpVisualizer : MonoBehaviour
    {
        [Header("Visualization Settings")]
        [SerializeField] private bool showInSceneView = true;
        [SerializeField] private bool showInGameView = false;
        [SerializeField] private bool onlyShowDamagedCells = true;
        
        [Header("Display Options")]
        [SerializeField] private float textSize = 0.15f;
        [SerializeField] private bool showPercentage = false;
        [SerializeField] private bool showCellBorders = true;
        [SerializeField] private float borderSize = 0.08f;
        
        [Header("Color Settings")]
        [SerializeField] private Color fullHpColor = Color.green;
        [SerializeField] private Color halfHpColor = Color.yellow;
        [SerializeField] private Color lowHpColor = Color.red;
        [SerializeField] private Color destroyedColor = Color.black;

        private RingSectorDamageMask _damageMask;

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!showInSceneView) return;
            DrawCellHp();
        }
        
        private void OnGUI()
        {
            if (!showInGameView || !Application.isPlaying) return;
            DrawCellHpGUI();
        }

        private void DrawCellHp()
        {
            if (_damageMask == null)
            {
                _damageMask = GetComponent<RingSectorDamageMask>();
                if (_damageMask == null) return;
            }

            var sector = GetComponent<RingSectorMesh>();
            if (sector == null) return;

            for (int rIdx = 0; rIdx < _damageMask.RadialCells; rIdx++)
            {
                for (int aIdx = 0; aIdx < _damageMask.AngleCells; aIdx++)
                {
                    int cellIdx = rIdx * _damageMask.AngleCells + aIdx;
                    float damage = _damageMask.GetCellDamage(cellIdx);
                    bool isDestroyed = _damageMask.IsCellDestroyed(cellIdx);
                    
                    if (onlyShowDamagedCells && damage <= 0.001f && !isDestroyed) continue;

                    var cellPos = GetCellWorldPosition(sector, aIdx, rIdx);
                    Color color = GetColorByDamage(damage, isDestroyed);
                    
                    // 텍스트 표시
                    Handles.color = color;
                    var style = new GUIStyle();
                    style.normal.textColor = color;
                    style.fontSize = Mathf.RoundToInt(textSize * 100f);
                    style.alignment = TextAnchor.MiddleCenter;
                    style.fontStyle = FontStyle.Bold;
                    
                    string text = GetCellText(damage, isDestroyed);
                    Handles.Label(cellPos, text, style);
                    
                    // 셀 경계 표시
                    if (showCellBorders)
                    {
                        Gizmos.color = new Color(color.r, color.g, color.b, 0.5f);
                        Gizmos.DrawWireSphere(cellPos, borderSize);
                    }
                }
            }
        }

        private void DrawCellHpGUI()
        {
            if (_damageMask == null) return;

            var cam = Camera.main;
            if (cam == null) return;

            var sector = GetComponent<RingSectorMesh>();
            if (sector == null) return;

            for (int rIdx = 0; rIdx < _damageMask.RadialCells; rIdx++)
            {
                for (int aIdx = 0; aIdx < _damageMask.AngleCells; aIdx++)
                {
                    int cellIdx = rIdx * _damageMask.AngleCells + aIdx;
                    float damage = _damageMask.GetCellDamage(cellIdx);
                    bool isDestroyed = _damageMask.IsCellDestroyed(cellIdx);
                    
                    if (onlyShowDamagedCells && damage <= 0.001f && !isDestroyed) continue;

                    var cellPos = GetCellWorldPosition(sector, aIdx, rIdx);
                    var screenPos = cam.WorldToScreenPoint(cellPos);
                    
                    if (screenPos.z > 0)
                    {
                        screenPos.y = Screen.height - screenPos.y;
                        Color color = GetColorByDamage(damage, isDestroyed);
                        
                        var style = new GUIStyle();
                        style.normal.textColor = color;
                        style.fontSize = 12;
                        style.alignment = TextAnchor.MiddleCenter;
                        style.fontStyle = FontStyle.Bold;
                        
                        string text = GetCellText(damage, isDestroyed);
                        GUI.Label(new Rect(screenPos.x - 30, screenPos.y - 10, 60, 20), text, style);
                    }
                }
            }
        }

        private Vector3 GetCellWorldPosition(RingSectorMesh sector, int angleIndex, int radiusIndex)
        {
            float sectorStart = sector.StartAngleDeg;
            float sectorArc = sector.ArcAngleDeg;
            float cellArcSize = sectorArc / Mathf.Max(1, _damageMask.AngleCells);
            float cellAngle = sectorStart + (angleIndex + 0.5f) * cellArcSize;
            
            float innerR = sector.InnerRadius;
            float cellRadialSize = sector.Thickness / Mathf.Max(1, _damageMask.RadialCells);
            float cellRadius = innerR + (radiusIndex + 0.5f) * cellRadialSize;
            
            float angleRad = cellAngle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(
                Mathf.Cos(angleRad) * cellRadius,
                Mathf.Sin(angleRad) * cellRadius,
                0f
            );
            
            return transform.position + offset;
        }

        private Color GetColorByDamage(float damage, bool isDestroyed)
        {
            if (isDestroyed) return destroyedColor;
            
            // 정확한 HP 비율 계산
            float cellHp = _damageMask.CellHp;
            float hpRemaining = cellHp - damage;
            float hpRatio = hpRemaining / cellHp;
            
            if (hpRatio > 0.66f) return fullHpColor;
            if (hpRatio > 0.33f) return Color.Lerp(halfHpColor, fullHpColor, (hpRatio - 0.33f) / 0.33f);
            return Color.Lerp(lowHpColor, halfHpColor, hpRatio / 0.33f);
        }

        private string GetCellText(float damage, bool isDestroyed)
        {
            if (isDestroyed) return "✗";
            if (damage <= 0.001f) return "○";
            
            float cellHp = _damageMask.CellHp;
            float hpRemaining = cellHp - damage;
            
            if (showPercentage)
            {
                float percent = (hpRemaining / cellHp) * 100f;
                return $"{percent:F0}%";
            }
            else
            {
                return $"{hpRemaining:F0}/{cellHp:F0}";
            }
        }
#endif
    }
}

