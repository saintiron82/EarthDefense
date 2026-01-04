using System.Collections.Generic;
using UnityEngine;

namespace ShapeDefense.Scripts.Data
{
    /// <summary>
    /// 무기 데이터 테이블 - 모든 무기 데이터를 관리
    /// ScriptableObject로 생성하여 에셋으로 관리
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponDataTable", menuName = "ShapeDefense/Data Table/Weapon Data Table", order = 0)]
    public class WeaponDataTable : ScriptableObject
    {
        [Header("Weapon Data List")]
        [SerializeField] private List<WeaponData> weapons = new List<WeaponData>();
        
        /// <summary>
        /// 모든 무기 데이터 목록
        /// </summary>
        public IReadOnlyList<WeaponData> Weapons => weapons;
        
        /// <summary>
        /// ID로 무기 데이터 찾기
        /// </summary>
        public WeaponData GetById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            
            return weapons.Find(w => w.Id == id);
        }
        
        /// <summary>
        /// 이름으로 무기 데이터 찾기
        /// </summary>
        public WeaponData GetByName(string weaponName)
        {
            if (string.IsNullOrEmpty(weaponName)) return null;
            
            return weapons.Find(w => w.WeaponName == weaponName);
        }
        
        /// <summary>
        /// 레벨로 필터링된 무기 목록
        /// </summary>
        public List<WeaponData> GetUnlockedWeapons(int playerLevel)
        {
            return weapons.FindAll(w => w.UnlockLevel <= playerLevel);
        }
        
        /// <summary>
        /// 무기 추가 (에디터용)
        /// </summary>
        public void AddWeapon(WeaponData weapon)
        {
            if (weapon != null && !weapons.Contains(weapon))
            {
                weapons.Add(weapon);
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
        }
        
        /// <summary>
        /// 무기 제거 (에디터용)
        /// </summary>
        public void RemoveWeapon(WeaponData weapon)
        {
            if (weapons.Remove(weapon))
            {
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
        }
        
        /// <summary>
        /// ID 중복 체크
        /// </summary>
        public bool HasDuplicateIds()
        {
            var ids = new HashSet<string>();
            foreach (var weapon in weapons)
            {
                if (string.IsNullOrEmpty(weapon.Id)) continue;
                if (!ids.Add(weapon.Id))
                {
                    UnityEngine.Debug.LogError($"[WeaponDataTable] 중복 ID 발견: {weapon.Id}");
                    return true;
                }
            }
            return false;
        }
        
#if UNITY_EDITOR
        [ContextMenu("Validate Data")]
        private void ValidateData()
        {
            int errorCount = 0;
            
            // ID 중복 체크
            if (HasDuplicateIds())
            {
                errorCount++;
            }
            
            // 각 무기 데이터 검증
            foreach (var weapon in weapons)
            {
                if (string.IsNullOrEmpty(weapon.Id))
                {
                    UnityEngine.Debug.LogError($"[WeaponDataTable] 빈 ID: {weapon.WeaponName}");
                    errorCount++;
                }
                
                if (string.IsNullOrEmpty(weapon.WeaponBundleId))
                {
                    UnityEngine.Debug.LogError($"[WeaponDataTable] {weapon.WeaponName}: WeaponBundleId가 비어있습니다");
                    errorCount++;
                }
            }
            
            if (errorCount == 0)
            {
                UnityEngine.Debug.Log($"[WeaponDataTable] 검증 완료! 총 {weapons.Count}개 무기 데이터");
            }
            else
            {
                UnityEngine.Debug.LogError($"[WeaponDataTable] 검증 실패! {errorCount}개 오류 발견");
            }
        }
        
        [ContextMenu("Sort By Unlock Level")]
        private void SortByUnlockLevel()
        {
            weapons.Sort((a, b) => a.UnlockLevel.CompareTo(b.UnlockLevel));
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEngine.Debug.Log("[WeaponDataTable] 해금 레벨 순으로 정렬 완료");
        }
#endif
    }
}

