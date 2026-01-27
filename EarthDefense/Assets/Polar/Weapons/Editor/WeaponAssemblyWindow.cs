using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Polar.Weapons.Effects;
using Script.SystemCore.Pool;
using Polar.Weapons.Projectiles;

namespace Polar.Weapons.Editor
{
    /// <summary>
    /// 무기 조립 에디터 윈도우
    /// 플랫폼: Projectile (투사체), Beam (빔), Deployable (설치형)
    /// </summary>
    public class WeaponAssemblyWindow : EditorWindow
    {
        private int _selectedTab;
        private readonly string[] _tabNames = { "무기 조립", "Effect 조합", "프리셋", "테스트" };
        
        private Vector2 _scrollPos;
        private Vector2 _effectScrollPos;
        
        // 무기 설정 (공통)
        private WeaponPlatform _weaponPlatform = WeaponPlatform.Projectile;
        private string _weaponId = "new_weapon";
        private string _weaponName = "New Weapon";
        private float _damage = 50f;
        private float _knockbackPower = 0.5f;
        private float _fireRate = 1f;
        private PolarAreaType _areaType = PolarAreaType.Explosion;
        private int _explosionRadius = 5;
        
        // Projectile 전용
        private float _projectileSpeed = 10f;
        private float _spreadAngle;
        private float _projectileLifetime = 5f;
        
        // Beam 전용
        private float _beamWidth = 0.5f;
        private int _reflectCount;
        private float _tickRate = 10f;
        private float _maxLength = 50f;
        
        // Deployable 전용
        private DeployableType _deployableType = DeployableType.Mine;
        private float _triggerRadius = 2f;
        private float _lifetime = 10f;
        private float _activationDelay = 0.5f;
        private int _maxDeployCount = 3;
        
        // 타겟팅 설정
        private TargetingMode _targetingMode = TargetingMode.MouseDirection;
        private TargetPriority _targetPriority = TargetPriority.Nearest;
        private float _fixedAngle;
        private float _trackingSpeed = 5f;
        private float _detectionRange = 10f;
        private bool _leadTarget;
        
        // Effect 설정
        private List<EffectSlot> _effectSlots = new List<EffectSlot>();
        private PolarEffectBase _effectToAdd;
        
        // 프리셋
        private List<PolarWeaponData> _savedPresets = new List<PolarWeaponData>();
        
        // 테스트용 프리팹
        private GameObject _testProjectilePrefab;
        private GameObject _testBeamPrefab;
        private GameObject _testWeaponPrefab;
        private float _testAngle;
        
        // 런타임 무기 등록 기능 제거 (테스트 목적 단순화)

        // Projectile Impact Policy
        private ProjectileHitResponse _hitResponse = ProjectileHitResponse.StopAndApplyDamage;

        // (구버전) 런타임 풀 등록 변수 제거: 현재는 EnsureProjectileBundleReadyForEquip에서 바로 번들ID를 만들고 등록함

        [MenuItem("EarthDefense/Weapon Assembly Tool &W")]
        public static void ShowWindow()
        {
            var window = GetWindow<WeaponAssemblyWindow>("무기 조립 도구");
            window.minSize = new Vector2(500, 600);
        }
        
        private void OnEnable() => LoadPresets();
        
        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            DrawHeader();
            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabNames);
            EditorGUILayout.Space(10);
            
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            switch (_selectedTab)
            {
                case 0: DrawWeaponAssemblyTab(); break;
                case 1: DrawEffectCombineTab(); break;
                case 2: DrawPresetTab(); break;
                case 3: DrawTestTab(); break;
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("🔧 무기 조립 도구", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("새로 만들기", EditorStyles.toolbarButton)) ResetAll();
            EditorGUILayout.EndHorizontal();
        }
        
        private void ResetAll()
        {
            _weaponPlatform = WeaponPlatform.Projectile;
            _weaponId = "new_weapon";
            _weaponName = "New Weapon";
            _damage = 50f;
            _knockbackPower = 0.5f;
            _fireRate = 1f;
            _areaType = PolarAreaType.Explosion;
            _explosionRadius = 5;

            _projectileSpeed = 10f;
            _spreadAngle = 0f;
            _projectileLifetime = 5f;

            _beamWidth = 0.5f;
            _reflectCount = 0;
            _tickRate = 10f;
            _maxLength = 50f;

            _deployableType = DeployableType.Mine;
            _triggerRadius = 2f;
            _lifetime = 10f;
            _activationDelay = 0.5f;
            _maxDeployCount = 3;

            _targetingMode = TargetingMode.MouseDirection;
            _targetPriority = TargetPriority.Nearest;
            _fixedAngle = 0f;
            _trackingSpeed = 5f;
            _detectionRange = 10f;
            _leadTarget = false;

            _effectSlots.Clear();
            _effectToAdd = null;
            _hitResponse = ProjectileHitResponse.StopAndApplyDamage;

            _testProjectilePrefab = null;
            _testBeamPrefab = null;
            _testWeaponPrefab = null;
            _testAngle = 0f;
        }
        
        private void CreateWeaponData()
        {
            string path = EditorUtility.SaveFilePanelInProject("Save Weapon", _weaponId, "asset", "");
            if (string.IsNullOrEmpty(path)) return;

            // 현재 구조: 플랫폼별 세부 데이터 타입이 아직 정리 중이므로 기본 PolarWeaponData로 저장
            PolarWeaponData weaponData;
            if (_weaponPlatform == WeaponPlatform.Projectile)
            {
                weaponData = CreateTempMissileWeaponData();
            }
            else
            {
                weaponData = CreateTempWeaponData();
            }

            AssetDatabase.CreateAsset(weaponData, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = weaponData;
            EditorGUIUtility.PingObject(weaponData);
        }
        
        private void SaveAsPreset()
        {
            CreateWeaponData();
            LoadPresets();
        }
        
        private void LoadPresets()
        {
            _savedPresets.Clear();
            foreach (var guid in AssetDatabase.FindAssets("t:PolarWeaponData"))
            {
                var data = AssetDatabase.LoadAssetAtPath<PolarWeaponData>(AssetDatabase.GUIDToAssetPath(guid));
                if (data != null) _savedPresets.Add(data);
            }
        }
        
        private void LoadPreset(PolarWeaponData preset)
        {
            if (preset == null) return;

            _weaponId = string.IsNullOrEmpty(preset.Id) ? "loaded" : preset.Id;
            _weaponName = string.IsNullOrEmpty(preset.WeaponName) ? "Loaded" : preset.WeaponName;
            _damage = preset.Damage;
            _knockbackPower = preset.KnockbackPower;
            // _fireRate = preset.FireRate;
            // PolarWeaponData는 FireRate를 가지지 않습니다(무기 타입별 데이터에만 있음).
            // 에디터 내부 _fireRate는 조립 탭 값 유지(혹은 템플릿/수동 입력)로 사용합니다.
            // 필요하면 추후 (preset is PolarMissileWeaponData / PolarMachinegunWeaponData)로 분기해서 로드하세요.
            _areaType = preset.AreaType;
            _explosionRadius = preset.DamageRadius;
            _hitResponse = preset.ImpactPolicy.hitResponse;

            _effectSlots.Clear();
            if (preset.ImpactEffects != null)
            {
                foreach (var e in preset.ImpactEffects)
                {
                    if (e is PolarEffectBase eff)
                    {
                        _effectSlots.Add(new EffectSlot
                        {
                            effect = eff,
                            triggerType = eff.TriggerCondition.triggerType,
                            probability = eff.TriggerCondition.probability
                        });
                    }
                }
            }
        }
        
        private void AddEffect(PolarEffectBase effect)
        {
            if (effect == null) return;
            _effectSlots.Add(new EffectSlot
            {
                effect = effect,
                triggerType = effect.TriggerCondition.triggerType,
                probability = effect.TriggerCondition.probability
            });
        }
        
        private void CreateQuickEffect(EffectType type)
        {
            if (type == EffectType.Gravity)
            {
                var effect = ScriptableObject.CreateInstance<PolarGravityFieldEffect>();
                string path = EditorUtility.SaveFilePanelInProject("Save Effect", $"GravityEffect_{System.DateTime.Now:HHmmss}", "asset", "");
                if (string.IsNullOrEmpty(path)) return;
                AssetDatabase.CreateAsset(effect, path);
                AssetDatabase.SaveAssets();
                AddEffect(effect);
                return;
            }

            EditorUtility.DisplayDialog("알림", $"{type} Effect는 아직 구현되지 않았습니다.", "OK");
        }
        
        private void ApplyTemplate(WeaponTemplate t)
        {
            ResetAll();
            switch (t)
            {
                case WeaponTemplate.Missile:
                    _weaponPlatform = WeaponPlatform.Projectile;
                    _weaponId = "missile"; _weaponName = "Missile";
                    _damage = 50f; _fireRate = 1f; _projectileSpeed = 12f;
                    _areaType = PolarAreaType.Explosion; _explosionRadius = 5;
                    _targetingMode = TargetingMode.MouseDirection;
                    break;
                case WeaponTemplate.Machinegun:
                    _weaponPlatform = WeaponPlatform.Projectile;
                    _weaponId = "machinegun"; _weaponName = "Machinegun";
                    _damage = 10f; _fireRate = 10f; _projectileSpeed = 20f;
                    _spreadAngle = 3f; _areaType = PolarAreaType.Fixed;
                    _targetingMode = TargetingMode.MouseDirection;
                    break;
                case WeaponTemplate.Shotgun:
                    _weaponPlatform = WeaponPlatform.Projectile;
                    _weaponId = "shotgun"; _weaponName = "Shotgun";
                    _damage = 15f; _fireRate = 1f; _projectileSpeed = 25f;
                    _spreadAngle = 20f; _areaType = PolarAreaType.Fixed;
                    _targetingMode = TargetingMode.MouseDirection;
                    break;
                case WeaponTemplate.HomingMissile:
                    _weaponPlatform = WeaponPlatform.Projectile;
                    _weaponId = "homing_missile"; _weaponName = "Homing Missile";
                    _damage = 60f; _fireRate = 0.5f; _projectileSpeed = 8f;
                    _areaType = PolarAreaType.Explosion; _explosionRadius = 4;
                    _targetingMode = TargetingMode.Homing;
                    _trackingSpeed = 8f; _detectionRange = 15f; _leadTarget = true;
                    break;
                case WeaponTemplate.Laser:
                    _weaponPlatform = WeaponPlatform.Beam;
                    _weaponId = "laser"; _weaponName = "Laser";
                    _damage = 30f; _tickRate = 10f; _beamWidth = 0.3f;
                    _reflectCount = 2; _maxLength = 50f;
                    _targetingMode = TargetingMode.MouseDirection;
                    break;
                case WeaponTemplate.Flamethrower:
                    _weaponPlatform = WeaponPlatform.Beam;
                    _weaponId = "flamethrower"; _weaponName = "Flamethrower";
                    _damage = 20f; _tickRate = 15f; _beamWidth = 2f;
                    _reflectCount = 0; _maxLength = 15f;
                    _targetingMode = TargetingMode.MouseDirection;
                    break;
                case WeaponTemplate.Mine:
                    _weaponPlatform = WeaponPlatform.Deployable;
                    _deployableType = DeployableType.Mine;
                    _weaponId = "mine"; _weaponName = "Mine";
                    _damage = 100f; _triggerRadius = 2f; _lifetime = 30f;
                    _activationDelay = 1f; _areaType = PolarAreaType.Explosion; _explosionRadius = 8;
                    _targetingMode = TargetingMode.AutoNearest;
                    _detectionRange = 2f;
                    break;
                case WeaponTemplate.Turret:
                    _weaponPlatform = WeaponPlatform.Deployable;
                    _deployableType = DeployableType.Turret;
                    _weaponId = "turret"; _weaponName = "Auto Turret";
                    _damage = 15f; _triggerRadius = 8f; _lifetime = 20f;
                    _fireRate = 5f; _areaType = PolarAreaType.Fixed;
                    _targetingMode = TargetingMode.AutoNearest;
                    _detectionRange = 10f;
                    break;
                case WeaponTemplate.Trap:
                    _weaponPlatform = WeaponPlatform.Deployable;
                    _deployableType = DeployableType.Trap;
                    _weaponId = "trap"; _weaponName = "Slow Trap";
                    _damage = 5f; _triggerRadius = 3f; _lifetime = 15f;
                    _activationDelay = 0f;
                    _targetingMode = TargetingMode.AutoNearest;
                    _detectionRange = 3f;
                    break;
            }
        }
        
        private void AutoFindWeaponPrefab()
        {
            var guids = AssetDatabase.FindAssets("t:Prefab PolarProjectileWeapon");
            if (guids.Length == 0) guids = AssetDatabase.FindAssets("t:Prefab PolarLaserWeapon");
            if (guids.Length == 0) guids = AssetDatabase.FindAssets("t:Prefab Weapon");

            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _testWeaponPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                return;
            }
        }
        
        private void AutoFindProjectilePrefab()
        {
            var guids = AssetDatabase.FindAssets("t:Prefab Bullet");
            if (guids.Length == 0) guids = AssetDatabase.FindAssets("t:Prefab Projectile");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _testProjectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
        }
        
        private void AutoFindBeamPrefab()
        {
            var guids = AssetDatabase.FindAssets("t:Prefab Beam");
            if (guids.Length == 0) guids = AssetDatabase.FindAssets("t:Prefab Laser");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _testBeamPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
        }
        
        private void CopyAsJson()
        {
            EditorGUIUtility.systemCopyBuffer = CreateJson();
            Debug.Log("[WeaponAssembly] JSON copied");
        }
        
        private void SaveAsJsonFile()
        {
            string path = EditorUtility.SaveFilePanel("Save JSON", "", $"{_weaponId}.json", "json");
            if (string.IsNullOrEmpty(path)) return;
            System.IO.File.WriteAllText(path, CreateJson());
            AssetDatabase.Refresh();
        }
        
        private string CreateJson()
        {
            var data = new WeaponJson
            {
                id = _weaponId,
                weaponName = _weaponName,
                platform = _weaponPlatform.ToString(),
                damage = _damage,
                fireRate = _fireRate,
                projectileSpeed = _projectileSpeed,
                effects = new string[_effectSlots.Count]
            };
            for (int i = 0; i < _effectSlots.Count; i++)
                data.effects[i] = _effectSlots[i].effect?.EffectId ?? "";
            return JsonUtility.ToJson(data, true);
        }
        
        #region 무기 조립 탭
        private void DrawWeaponAssemblyTab()
        {
            // 플랫폼 선택
            EditorGUILayout.LabelField("플랫폼 선택", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _weaponPlatform = (WeaponPlatform)EditorGUILayout.EnumPopup("무기 플랫폼", _weaponPlatform);
            EditorGUILayout.HelpBox(GetPlatformDescription(), MessageType.Info);
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
            
            // 기본 설정
            EditorGUILayout.LabelField("기본 설정", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _weaponId = EditorGUILayout.TextField("무기 ID", _weaponId);
            _weaponName = EditorGUILayout.TextField("무기 이름", _weaponName);
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
            
            // 전투 설정
            EditorGUILayout.LabelField("전투 설정", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _damage = EditorGUILayout.FloatField("데미지", _damage);
            _knockbackPower = EditorGUILayout.Slider("넉백 파워", _knockbackPower, 0f, 2f);
            
            if (_weaponPlatform != WeaponPlatform.Deployable || _deployableType == DeployableType.Turret)
            {
                _fireRate = EditorGUILayout.Slider("발사 속도 (발/초)", _fireRate, 0.1f, 30f);
            }
            
            _areaType = (PolarAreaType)EditorGUILayout.EnumPopup("피해 타입", _areaType);
            if (_areaType == PolarAreaType.Explosion || _areaType == PolarAreaType.Gaussian)
            {
                _explosionRadius = EditorGUILayout.IntSlider("피해 반경", _explosionRadius, 1, 20);
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
            
            // 플랫폼별 설정
            DrawPlatformSpecificSettings();
            
            EditorGUILayout.Space(10);
            
            // 타겟팅 설정 (모든 플랫폼)
            DrawTargetingSettings();
            
            EditorGUILayout.Space(10);
            DrawEffectSummary();
            
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("📄 무기 데이터 생성", GUILayout.Height(35))) CreateWeaponData();
            if (GUILayout.Button("📦 프리셋으로 저장", GUILayout.Height(35))) SaveAsPreset();
            EditorGUILayout.EndHorizontal();
        }
        
        private string GetPlatformDescription()
        {
            return _weaponPlatform switch
            {
                WeaponPlatform.Projectile => "투사체: 발사 → 이동 → 충돌 (미사일, 머신건, 샷건)",
                WeaponPlatform.Beam => "빔: 즉시 발사 → 지속 데미지 (레이저, 화염방사기)",
                WeaponPlatform.Deployable => "설치형: 설치 → 대기 → 발동 (지뢰, 터렛, 함정)",
                _ => ""
            };
        }
        
        private void DrawPlatformSpecificSettings()
        {
            switch (_weaponPlatform)
            {
                case WeaponPlatform.Projectile:
                    DrawProjectileSettings();
                    break;
                case WeaponPlatform.Beam:
                    DrawBeamSettings();
                    break;
                case WeaponPlatform.Deployable:
                    DrawDeployableSettings();
                    break;
            }
        }
        
        private void DrawProjectileSettings()
        {
            EditorGUILayout.LabelField("투사체 설정", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _projectileSpeed = EditorGUILayout.Slider("투사체 속도", _projectileSpeed, 1f, 50f);
            _spreadAngle = EditorGUILayout.Slider("산포각", _spreadAngle, 0f, 30f);
            _projectileLifetime = EditorGUILayout.Slider("수명 (초)", _projectileLifetime, 0.5f, 10f);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("충돌 반응(탄 성질)", EditorStyles.miniBoldLabel);
            _hitResponse = (ProjectileHitResponse)EditorGUILayout.EnumPopup("Hit Response", _hitResponse);
            EditorGUILayout.EndVertical();
        }
        
        private void DrawBeamSettings()
        {
            EditorGUILayout.LabelField("빔 설정", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _beamWidth = EditorGUILayout.Slider("빔 폭", _beamWidth, 0.1f, 3f);
            _maxLength = EditorGUILayout.Slider("최대 길이", _maxLength, 5f, 100f);
            _tickRate = EditorGUILayout.Slider("틱 레이트 (회/초)", _tickRate, 1f, 30f);
            _reflectCount = EditorGUILayout.IntSlider("반사 횟수", _reflectCount, 0, 5);
            EditorGUILayout.EndVertical();
        }
        
        private void DrawDeployableSettings()
        {
            EditorGUILayout.LabelField("설치형 설정", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            _deployableType = (DeployableType)EditorGUILayout.EnumPopup("설치물 타입", _deployableType);
            EditorGUILayout.LabelField(GetDeployableTypeDescription(), EditorStyles.miniLabel);
            EditorGUILayout.Space(5);
            
            _triggerRadius = EditorGUILayout.Slider("감지 반경", _triggerRadius, 0.5f, 10f);
            _lifetime = EditorGUILayout.Slider("지속 시간 (초)", _lifetime, 1f, 60f);
            _activationDelay = EditorGUILayout.Slider("활성화 지연 (초)", _activationDelay, 0f, 3f);
            _maxDeployCount = EditorGUILayout.IntSlider("최대 설치 개수", _maxDeployCount, 1, 10);
            
            EditorGUILayout.EndVertical();
        }
        
        private string GetDeployableTypeDescription()
        {
            return _deployableType switch
            {
                DeployableType.Mine => "💣 지뢰: 적 접근 시 폭발",
                DeployableType.Turret => "🔫 터렛: 범위 내 적 자동 공격",
                DeployableType.Trap => "🕸️ 함정: 적 둔화/속박",
                DeployableType.Shield => "🛡️ 방어막: 특정 섹터 보호",
                DeployableType.Beacon => "📡 비콘: 버프/디버프 영역",
                _ => ""
            };
        }
        
        private void DrawTargetingSettings()
        {
            EditorGUILayout.LabelField("타겟팅 설정", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            _targetingMode = (TargetingMode)EditorGUILayout.EnumPopup("타겟팅 모드", _targetingMode);
            EditorGUILayout.LabelField(GetTargetingModeDescription(), EditorStyles.miniLabel);
            EditorGUILayout.Space(5);
            
            // 모드별 추가 옵션
            switch (_targetingMode)
            {
                case TargetingMode.Fixed:
                    _fixedAngle = EditorGUILayout.Slider("고정 각도", _fixedAngle, 0f, 360f);
                    break;
                    
                case TargetingMode.AutoNearest:
                case TargetingMode.AutoFarthest:
                case TargetingMode.AutoWeakest:
                case TargetingMode.AutoStrongest:
                case TargetingMode.AutoRandom:
                    _detectionRange = EditorGUILayout.Slider("탐지 범위", _detectionRange, 1f, 50f);
                    break;
                    
                case TargetingMode.Homing:
                    _targetPriority = (TargetPriority)EditorGUILayout.EnumPopup("타겟 우선순위", _targetPriority);
                    _detectionRange = EditorGUILayout.Slider("탐지 범위", _detectionRange, 1f, 50f);
                    _trackingSpeed = EditorGUILayout.Slider("추적 속도", _trackingSpeed, 1f, 20f);
                    _leadTarget = EditorGUILayout.Toggle("선행 조준 (예측)", _leadTarget);
                    break;
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private string GetTargetingModeDescription()
        {
            return _targetingMode switch
            {
                TargetingMode.MouseDirection => "🖱️ 마우스 방향으로 발사",
                TargetingMode.Fixed => "📐 지정된 고정 각도로 발사",
                TargetingMode.AutoNearest => "🎯 가장 가까운 적을 자동 타겟",
                TargetingMode.AutoFarthest => "🎯 가장 먼 적을 자동 타겟",
                TargetingMode.AutoWeakest => "💔 체력이 낮은 적을 자동 타겟",
                TargetingMode.AutoStrongest => "💪 체력이 높은 적을 자동 타겟",
                TargetingMode.AutoRandom => "🎲 무작위 적을 자동 타겟",
                TargetingMode.Homing => "🚀 발사 후 적을 추적 (유도탄)",
                _ => ""
            };
        }
        
        private void DrawEffectSummary()
        {
            EditorGUILayout.LabelField($"Effect ({_effectSlots.Count}개)", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (_effectSlots.Count == 0)
            {
                EditorGUILayout.HelpBox("Effect가 없습니다. Effect 조합 탭에서 추가하세요.", MessageType.Info);
            }
            else
            {
                for (int i = _effectSlots.Count - 1; i >= 0; i--)
                {
                    var slot = _effectSlots[i];
                    if (slot.effect == null) continue;
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"• {slot.effect.EffectName} ({slot.triggerType})");
                    if (GUILayout.Button("×", GUILayout.Width(20))) _effectSlots.RemoveAt(i);
                    EditorGUILayout.EndHorizontal();
                }
            }
            if (GUILayout.Button("+ Effect 추가")) _selectedTab = 1;
            EditorGUILayout.EndVertical();
        }
        #endregion
        
        #region Effect 조합 탭
        private void DrawEffectCombineTab()
        {
            EditorGUILayout.LabelField("Effect 추가", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _effectToAdd = (PolarEffectBase)EditorGUILayout.ObjectField("Effect 선택", _effectToAdd, typeof(PolarEffectBase), false);
            if (_effectToAdd != null)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField($"ID: {_effectToAdd.EffectId}");
                EditorGUILayout.LabelField($"Trigger: {_effectToAdd.TriggerCondition.triggerType}");
                if (GUILayout.Button("이 Effect 추가", GUILayout.Height(25)))
                {
                    AddEffect(_effectToAdd);
                    _effectToAdd = null;
                }
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("빠른 Effect 생성", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🌀 중력장", GUILayout.Height(30))) CreateQuickEffect(EffectType.Gravity);
            if (GUILayout.Button("🔥 화염", GUILayout.Height(30))) CreateQuickEffect(EffectType.Fire);
            if (GUILayout.Button("☠️ 독", GUILayout.Height(30))) CreateQuickEffect(EffectType.Poison);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField($"현재 Effect 목록 ({_effectSlots.Count}개)", EditorStyles.boldLabel);
            _effectScrollPos = EditorGUILayout.BeginScrollView(_effectScrollPos, EditorStyles.helpBox, GUILayout.Height(200));
            for (int i = 0; i < _effectSlots.Count; i++)
            {
                var slot = _effectSlots[i];
                if (slot.effect == null) continue;
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"{i + 1}. {slot.effect.EffectName}", GUILayout.Width(150));
                slot.triggerType = (EffectTriggerType)EditorGUILayout.EnumPopup(slot.triggerType, GUILayout.Width(100));
                slot.probability = EditorGUILayout.Slider(slot.probability, 0f, 1f, GUILayout.Width(100));
                if (GUILayout.Button("×", GUILayout.Width(25))) { _effectSlots.RemoveAt(i); break; }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
            if (_effectSlots.Count > 0 && GUILayout.Button("모든 Effect 제거")) _effectSlots.Clear();
        }
        #endregion
        
        #region 프리셋 탭
        private void DrawPresetTab()
        {
            EditorGUILayout.LabelField("저장된 프리셋", EditorStyles.boldLabel);
            if (_savedPresets.Count == 0)
            {
                EditorGUILayout.HelpBox("저장된 프리셋이 없습니다.", MessageType.Info);
            }
            else
            {
                foreach (var preset in _savedPresets)
                {
                    if (preset == null) continue;
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    EditorGUILayout.LabelField(preset.WeaponName, GUILayout.Width(150));
                    EditorGUILayout.LabelField($"DMG: {preset.Damage}", GUILayout.Width(80));
                    if (GUILayout.Button("불러오기", GUILayout.Width(70))) LoadPreset(preset);
                    if (GUILayout.Button("선택", GUILayout.Width(50)))
                    {
                        Selection.activeObject = preset;
                        EditorGUIUtility.PingObject(preset);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            
            EditorGUILayout.Space(10);
            if (GUILayout.Button("프리셋 새로고침")) LoadPresets();
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("빠른 프리셋", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("투사체", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🚀 미사일", GUILayout.Height(25))) ApplyTemplate(WeaponTemplate.Missile);
            if (GUILayout.Button("🔫 머신건", GUILayout.Height(25))) ApplyTemplate(WeaponTemplate.Machinegun);
            if (GUILayout.Button("💥 샷건", GUILayout.Height(25))) ApplyTemplate(WeaponTemplate.Shotgun);
            if (GUILayout.Button("🎯 유도탄", GUILayout.Height(25))) ApplyTemplate(WeaponTemplate.HomingMissile);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("빔", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("⚡ 레이저", GUILayout.Height(25))) ApplyTemplate(WeaponTemplate.Laser);
            if (GUILayout.Button("🔥 화염방사", GUILayout.Height(25))) ApplyTemplate(WeaponTemplate.Flamethrower);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("설치형", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("💣 지뢰", GUILayout.Height(25))) ApplyTemplate(WeaponTemplate.Mine);
            if (GUILayout.Button("🔫 터렛", GUILayout.Height(25))) ApplyTemplate(WeaponTemplate.Turret);
            if (GUILayout.Button("🕸️ 함정", GUILayout.Height(25))) ApplyTemplate(WeaponTemplate.Trap);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        #endregion
        
        #region 테스트 탭
        private void DrawTestTab()
        {
            EditorGUILayout.LabelField("무기 테스트", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("테스트 기능은 Play 모드에서만 사용 가능합니다.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Play 모드에서 (1) 테스트 발사(장착 없이) 또는 (2) 즉시 장착(입력으로 실전 테스트) 둘 다 가능합니다.", MessageType.None);
            }

            EditorGUILayout.Space(10);

            // 무기 프리팹(장착용) 설정
            EditorGUILayout.LabelField("무기 프리팹 (장착/등록용)", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            _testWeaponPrefab = (GameObject)EditorGUILayout.ObjectField(
                "무기 프리팹", _testWeaponPrefab, typeof(GameObject), false);
            if (GUILayout.Button("자동 찾기", GUILayout.Width(70)))
            {
                AutoFindWeaponPrefab();
            }
            EditorGUILayout.EndHorizontal();

            if (_testWeaponPrefab == null)
            {
                EditorGUILayout.HelpBox("무기 프리팹이 없으면, '무기 인스턴스 생성/등록' 경로에서 최소 컴포넌트 추가로 보완합니다(정식 프리팹이 있으면 그걸 쓰는 게 가장 안정적).", MessageType.Info);
            }

            // 프리팹 설정
            EditorGUILayout.LabelField("프리팹 설정 (필수)", EditorStyles.miniBoldLabel);
            if (_weaponPlatform == WeaponPlatform.Projectile)
            {
                EditorGUILayout.BeginHorizontal();
                _testProjectilePrefab = (GameObject)EditorGUILayout.ObjectField(
                    "투사체 프리팹", _testProjectilePrefab, typeof(GameObject), false);
                if (GUILayout.Button("자동 찾기", GUILayout.Width(70)))
                {
                    AutoFindProjectilePrefab();
                }
                EditorGUILayout.EndHorizontal();

                if (_testProjectilePrefab == null)
                {
                    EditorGUILayout.HelpBox("⚠️ 투사체 프리팹 필수! '자동 찾기' 버튼을 누르거나 직접 지정하세요.", MessageType.Error);
                }
            }
            else if (_weaponPlatform == WeaponPlatform.Beam)
            {
                EditorGUILayout.BeginHorizontal();
                _testBeamPrefab = (GameObject)EditorGUILayout.ObjectField(
                    "빔 프리팹", _testBeamPrefab, typeof(GameObject), false);
                if (GUILayout.Button("자동 찾기", GUILayout.Width(70)))
                {
                    AutoFindBeamPrefab();
                }
                EditorGUILayout.EndHorizontal();

                if (_testBeamPrefab == null)
                {
                    EditorGUILayout.HelpBox("⚠️ 빔 프리팹 필수! '자동 찾기' 버튼을 누르거나 직접 지정하세요.", MessageType.Error);
                }
            }

            EditorGUILayout.Space(5);

            // 발사 각도
            _testAngle = EditorGUILayout.Slider("발사 각도", _testAngle, 0f, 360f);

            EditorGUILayout.Space(10);

            // 현재 설정 요약
            EditorGUILayout.LabelField("현재 설정 요약", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField($"  이름: {_weaponName}");
            EditorGUILayout.LabelField($"  플랫폼: {_weaponPlatform}");
            EditorGUILayout.LabelField($"  타겟팅: {_targetingMode}");
            EditorGUILayout.LabelField($"  데미지: {_damage}");
            EditorGUILayout.LabelField($"  속도: {_projectileSpeed}");
            EditorGUILayout.LabelField($"  Effect 수: {_effectSlots.Count}");

            EditorGUILayout.Space(10);

            // 1) 테스트 발사: 장착 없이 즉시 발사(입력/장착과 무관하게 전달수단만 검증)
            EditorGUILayout.LabelField("테스트 발사(장착 없이)", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            bool canTestFire = Application.isPlaying &&
                              ((_weaponPlatform == WeaponPlatform.Projectile && _testProjectilePrefab != null) ||
                               (_weaponPlatform == WeaponPlatform.Beam && _testBeamPrefab != null) ||
                               _weaponPlatform == WeaponPlatform.Deployable);
            
            EditorGUI.BeginDisabledGroup(!canTestFire);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("▶ 테스트 발사", GUILayout.Height(34)))
            {
                TestFireWithoutEquip(_testAngle);
            }
            if (GUILayout.Button("▶▶ 연속 발사 (5)", GUILayout.Height(34)))
            {
                for (int i = 0; i < 5; i++) TestFireWithoutEquip(_testAngle);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();
            
            if (Application.isPlaying && !canTestFire)
            {
                EditorGUILayout.HelpBox("전달수단 프리팹(투사체/빔)을 지정해야 테스트 발사가 가능합니다.", MessageType.Warning);
            }
            
            EditorGUILayout.EndVertical();
            
             // 즉시장착 - 프리팹이 있어야만 활성화 (Projectile/Beam은 전달수단 프리팹 필수)
            bool canEquip = canTestFire;

             EditorGUILayout.LabelField("런타임 장착(실전 테스트)", EditorStyles.boldLabel);
             EditorGUILayout.BeginVertical(EditorStyles.helpBox);

             EditorGUI.BeginDisabledGroup(!canEquip);
             if (GUILayout.Button("🧩 현재 조립 무기를 즉시 장착(=원래 무기처럼 입력으로 사용)", GUILayout.Height(34)))
             {
                 EquipToPlayerFromEditor();
             }
             EditorGUI.EndDisabledGroup();

             if (Application.isPlaying)
             {
                EditorGUILayout.HelpBox("장착 후에는 게임에서 원래 하던 대로 Attack 입력으로 발사합니다. (테스트 발사는 별도 기능으로 그대로 유지)", MessageType.None);
             }
             else
             {
                 EditorGUILayout.HelpBox("Play 모드로 들어간 뒤 장착하세요.", MessageType.Info);
             }
             EditorGUILayout.EndVertical();
 
             EditorGUILayout.EndVertical();
             
             EditorGUILayout.Space(10);
             EditorGUILayout.LabelField("데이터 내보내기", EditorStyles.boldLabel);
             EditorGUILayout.BeginVertical(EditorStyles.helpBox);
             if (GUILayout.Button("📋 JSON으로 복사", GUILayout.Height(25))) CopyAsJson();
             if (GUILayout.Button("💾 JSON 파일로 저장", GUILayout.Height(25))) SaveAsJsonFile();
             EditorGUILayout.EndVertical();
        }
        #endregion
        
        private enum WeaponPlatform { Projectile, Beam, Deployable }
        private enum DeployableType { Mine, Turret, Trap, Shield, Beacon }
        private enum TargetingMode 
        { 
            MouseDirection,     // 마우스 방향
            Fixed,              // 고정 각도
            AutoNearest,        // 가장 가까운 적
            AutoFarthest,       // 가장 먼 적
            AutoWeakest,        // 체력 낮은 적
            AutoStrongest,      // 체력 높은 적
            AutoRandom,         // 무작위 적
            Homing              // 유도 (추적)
        }
        private enum TargetPriority { Nearest }
        private enum EffectType { Gravity, Fire, Poison }
        private enum WeaponTemplate { Missile, Machinegun, Shotgun, HomingMissile, Laser, Flamethrower, Mine, Turret, Trap }
        
        [System.Serializable]
        private class EffectSlot
        {
            public PolarEffectBase effect;
            public EffectTriggerType triggerType;
            public float probability = 1f;
        }
        
#pragma warning disable 0414
        [System.Serializable]
        private class WeaponJson
        {
            // JSON 내보내기 전용 DTO (JsonUtility가 public field만 직렬화)
            public string id;
            public string weaponName;
            public string platform;
            public float damage;
            public float fireRate;
            public float projectileSpeed;
            public string[] effects;
        }
#pragma warning restore 0414
        
        private void SetFieldViaReflection(object obj, string fieldName, object value)
        {
            if (obj == null || string.IsNullOrEmpty(fieldName)) return;

            var type = obj.GetType();
            while (type != null)
            {
                var field = type.GetField(fieldName,
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Instance);

                if (field != null)
                {
                    field.SetValue(obj, value);
                    return;
                }

                type = type.BaseType;
            }
        }

        private void EquipToPlayerFromEditor()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[WeaponAssembly] Play 모드에서만 장착할 수 있습니다.");
                return;
            }

            var pwm = Object.FindFirstObjectByType<PlayerWeaponManager>();
            if (pwm == null)
            {
                Debug.LogWarning("[WeaponAssembly] PlayerWeaponManager를 찾을 수 없습니다.");
                return;
            }

            // IPolarField 확보 (씬에서 검색)
            IPolarField field = null;
            var allBehaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var mb in allBehaviours)
            {
                if (mb is IPolarField pf)
                {
                    field = pf;
                    break;
                }
            }

            if (field == null)
            {
                Debug.LogWarning("[WeaponAssembly] IPolarField를 찾을 수 없어 무기를 장착할 수 없습니다.");
                return;
            }

            // 현재 조립 데이터 생성
            PolarWeaponData tempData = _weaponPlatform == WeaponPlatform.Projectile
                ? CreateTempMissileWeaponData()
                : this.CreateTempWeaponData();

            // ✅ 투사체 플랫폼이면: ProjectileBundleId가 비어있으면, 테스트 프리팹 기반으로 즉시 발사 가능 상태를 만든다.
            // PolarProjectileWeapon은 PoolService + ProjectileBundleId가 필수이므로, 여기에서 보장한다.
            if (_weaponPlatform == WeaponPlatform.Projectile && string.IsNullOrEmpty(tempData.ProjectileBundleId))
            {
                // '장착/정규 발사' 경로는 PoolService + ProjectileBundleId가 필수
                EnsureProjectileBundleReadyForEquip(tempData);
            }

            // 1) 현재 무기 인스턴스가 있으면: 데이터 스왑만으로 즉시 사용 가능
            if (pwm.CurrentWeapon != null)
            {
                pwm.SwapRuntimeWeaponData(tempData, field);
                Debug.Log($"[WeaponAssembly] ✅ 장착 완료(데이터 스왑): weapon={pwm.CurrentWeapon.GetType().Name}, dataId={pwm.CurrentWeaponData?.Id}, weaponBundle={pwm.CurrentWeaponData?.WeaponBundleId}, projectileBundle={pwm.CurrentWeaponData?.ProjectileBundleId}");
                return;
            }

            // 2) 현재 무기 인스턴스가 없으면:
            //    - WeaponBundleId가 있으면: PlayerWeaponManager 공식 장착 로직 사용
            //    - WeaponBundleId가 없으면: 에디터에서 지정한 무기 프리팹(또는 최소 프리팹)을 인스턴스화해 '정식 등록'

            if (!string.IsNullOrEmpty(tempData.WeaponBundleId))
            {
                pwm.EquipRuntimeWeapon(tempData);
                Debug.Log($"[WeaponAssembly] ✅ 장착 완료(로드): dataId={tempData.Id}, weaponBundle={tempData.WeaponBundleId}");
                return;
            }

            // WeaponBundleId가 비어있고 현재 무기도 없으면, 인스턴스 생성이 필요
            GameObject weaponGo;

            // 우선: 테스트 탭에서 지정한 무기 프리팹이 있으면 그것으로 생성
            if (_testWeaponPrefab != null)
            {
                weaponGo = Object.Instantiate(_testWeaponPrefab);
                weaponGo.name = $"RuntimeWeapon_{tempData.WeaponName}";
            }
            else
            {
                // 마지막 수단: 최소 무기 오브젝트 생성(프리팹 없이)
                weaponGo = new GameObject($"RuntimeWeapon_{tempData.WeaponName}");
            }

            // 무기 컴포넌트 확보 (프리팹에 이미 붙어있어야 정상)
            var weapon = weaponGo.GetComponent<PolarWeaponBase>();
            if (weapon == null)
            {
                // 프리팹에 없으면 플랫폼에 맞춰 최소 컴포넌트를 추가
                weapon = _weaponPlatform == WeaponPlatform.Beam
                    ? weaponGo.AddComponent<PolarLaserWeapon>()
                    : weaponGo.AddComponent<PolarProjectileWeapon>();
            }

            pwm.RegisterRuntimeWeaponInstance(weapon, field, tempData);

            if (pwm.CurrentWeapon == null)
            {
                Debug.LogWarning("[WeaponAssembly] ❌ 장착 실패: RegisterRuntimeWeaponInstance 이후 CurrentWeapon이 null 입니다.");
                return;
            }

            Debug.Log($"[WeaponAssembly] ✅ 장착 완료(인스턴스 등록): weapon={pwm.CurrentWeapon.GetType().Name}, dataId={pwm.CurrentWeaponData?.Id}, weaponBundle={pwm.CurrentWeaponData?.WeaponBundleId}, projectileBundle={pwm.CurrentWeaponData?.ProjectileBundleId}");
        }
        
        private void EnsureProjectileBundleReadyForEquip(PolarWeaponData tempData)
        {
            if (_testProjectilePrefab == null)
            {
                Debug.LogWarning("[WeaponAssembly] (장착) 투사체 프리팹이 없어 ProjectileBundleId를 자동 구성할 수 없습니다.");
                return;
            }

            var pool = PoolService.Instance;
            if (pool == null)
            {
                Debug.LogWarning("[WeaponAssembly] (장착) PoolService.Instance가 없어 정규 발사를 구성할 수 없습니다.");
                return;
            }

            // 에셋 경로를 bundleId로 사용(에디터 테스트 전용)
            // 같은 프로젝트 내에서는 유일하고, 별도 등록 UI 없이 즉시 사용 가능.
            var bundleId = AssetDatabase.GetAssetPath(_testProjectilePrefab);
            if (string.IsNullOrEmpty(bundleId))
            {
                // 혹시 런타임 생성된 오브젝트 등 경로가 없으면 이름 기반 fallback
                bundleId = $"runtime://projectile/{_testProjectilePrefab.name}";
            }

            // Pool 등록 (덮어쓰기 허용) → 어떤 프리팹을 골라도 즉시 발사 가능
            var ok = pool.RegisterPrefab(bundleId, _testProjectilePrefab, overwrite: true);
            if (!ok)
            {
                Debug.LogWarning($"[WeaponAssembly] PoolService.RegisterPrefab 실패: {bundleId}");
                return;
            }

            SetFieldViaReflection(tempData, "projectileBundleId", bundleId);

            // (디버그) bundleId 로그만 출력
            Debug.Log($"[WeaponAssembly] ✅ (장착) ProjectileBundleId 준비 완료: {bundleId}");
        }

        private void TestFireWithoutEquip(float angleDeg)
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[WeaponAssembly] Play 모드에서만 테스트 발사가 가능합니다.");
                return;
            }

            // Field는 PlayerWeaponManager와 무관하게 씬에서 검색
            IPolarField field = null;
            var allBehaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var mb in allBehaviours)
            {
                if (mb is IPolarField pf) { field = pf; break; }
            }

            if (field == null)
            {
                Debug.LogWarning("[WeaponAssembly] IPolarField를 찾을 수 없어 테스트 발사를 할 수 없습니다.");
                return;
            }

            // ⚠️ 중요: 테스트 발사는 무기(플랫폼)와 무관하지만,
            //         '투사체 스크립트가 요구하는 WeaponData 타입'과는 무관하지 않습니다.
            //         (예: PolarMachinegunProjectile은 PolarMachinegunWeaponData 요구)
            // Projectile: '즉석 생성 발사'가 기본(풀/번들은 등록/장착용 메커니즘)
            if (_weaponPlatform == WeaponPlatform.Projectile)
            {
                PolarWeaponData tempData;
                if (_testProjectilePrefab == null)
                {
                    Debug.LogWarning("[WeaponAssembly] 투사체 프리팹이 없어 테스트 발사를 할 수 없습니다.");
                    return;
                }

                var origin = field.CenterPosition;

                PolarProjectileBase projectile = null;

                // ✅ 기본 루트: Instantiate로 즉석 생성
                var go = Object.Instantiate(_testProjectilePrefab, origin, Quaternion.identity);
                if (go != null)
                {
                    go.TryGetComponent(out projectile);
                }

                if (projectile == null)
                {
                    Debug.LogWarning($"[WeaponAssembly] 선택한 프리팹에 PolarProjectileBase 컴포넌트가 없습니다: {_testProjectilePrefab.name}");
                    if (go != null) Object.Destroy(go);
                    return;
                }
                
                // 투사체 타입에 맞는 데이터 생성
                tempData = CreateTempWeaponDataForProjectile(projectile);

                // 테스트 발사 시작 반경: 벽과 충분히 떨어진 내부에서 시작해야 "이동"이 보장됨
                float startRadius = Mathf.Max(0.5f, field.InitialRadius * 0.25f);

                 // Launch(field, data, angle, radius) 오버로드 우선
                 var launchMethod = projectile.GetType().GetMethod("Launch",
                     System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic,
                     binder: null,
                     types: new[] { typeof(IPolarField), typeof(PolarWeaponData), typeof(float), typeof(float) },
                     modifiers: null);
                 try
                 {
                     if (launchMethod != null)
                     {
                         launchMethod.Invoke(projectile, new object[] { field, tempData, angleDeg, startRadius });
                      }
                      else
                      {
                          projectile.Launch(field, tempData);

                         // ⚠️ 일부 투사체는 기본 Launch에서 angle/radius/speed가 세팅되지 않으면 이동이 시작되지 않음.
                         // 테스트 발사 전용으로 최소 초기값을 강제한다.
                         ForceStartMoveForTest(projectile, field, tempData, angleDeg, startRadius);
                       }
 
                     LogProjectileStateForTest("AfterLaunch", projectile, field);
                      Debug.Log($"[WeaponAssembly] ✅ 테스트 발사 성공 (즉석 생성): {_testProjectilePrefab.name}");
                 }
                 catch (System.Exception ex)
                 {
                     Debug.LogError($"[WeaponAssembly] 테스트 발사 실패 (즉석 생성): {ex}");
                 }

                 return;
             }

            // Beam: 프리팹에서 직접 발사체 생성
            if (_weaponPlatform == WeaponPlatform.Beam)
            {
                if (_testBeamPrefab == null)
                {
                    Debug.LogWarning("[WeaponAssembly] 빔 프리팹이 없어 테스트 발사를 할 수 없습니다.");
                    return;
                }

                var origin = field.CenterPosition;

                // ✅ 기본 루트: Instantiate로 즉석 생성
                Object.Instantiate(_testBeamPrefab, origin, Quaternion.identity);
                Debug.Log($"[WeaponAssembly] ✅ 테스트 발사 성공 (즉석 생성): {_testBeamPrefab.name}");

                return;
            }

            Debug.LogWarning("[WeaponAssembly] 알 수 없는 무기 플랫폼입니다. 테스트 발사 실패.");
        }
        
        private PolarWeaponData CreateTempWeaponDataForProjectile(PolarProjectileBase projectile)
        {
            if (projectile == null) return CreateTempWeaponData();

            // 투사체가 머신건 전용이면 머신건 데이터로 생성
            if (projectile is PolarMachinegunProjectile)
             {
                 var mg = ScriptableObject.CreateInstance<PolarMachinegunWeaponData>();
                 FillCommonWeaponData(mg);
                 // machinegun-specific
                 SetFieldViaReflection(mg, "projectileSpeed", _projectileSpeed);
                 SetFieldViaReflection(mg, "projectileLifetime", _projectileLifetime);
                 SetFieldViaReflection(mg, "spreadAngle", _spreadAngle);
                 SetFieldViaReflection(mg, "fireRate", _fireRate);
                 return mg;
             }

            // 기본은 범용 데이터
            return CreateTempMissileWeaponData();
        }
        
        private void FillCommonWeaponData(PolarWeaponData data)
        {
            if (data == null) return;
            SetFieldViaReflection(data, "id", string.IsNullOrWhiteSpace(_weaponId) ? "runtime_weapon" : _weaponId);
            SetFieldViaReflection(data, "weaponName", string.IsNullOrWhiteSpace(_weaponName) ? "Runtime Weapon" : _weaponName);
            SetFieldViaReflection(data, "damage", _damage);
            SetFieldViaReflection(data, "knockbackPower", _knockbackPower);
            SetFieldViaReflection(data, "fireRate", _fireRate);
            SetFieldViaReflection(data, "areaType", _areaType);
            SetFieldViaReflection(data, "damageRadius", _explosionRadius);

            if (_effectSlots != null && _effectSlots.Count > 0)
            {
                var effects = new ScriptableObject[_effectSlots.Count];
                for (int i = 0; i < _effectSlots.Count; i++) effects[i] = _effectSlots[i].effect;
                SetFieldViaReflection(data, "impactEffects", effects);
            }

            SetFieldViaReflection(data, "projectileImpactPolicy", new ProjectileImpactPolicy
            {
                hitResponse = _hitResponse,
                penetrationCount = 0
            });
        }
        
        private PolarWeaponData CreateTempWeaponData()
        {
            var data = ScriptableObject.CreateInstance<PolarWeaponData>();
            FillCommonWeaponData(data);
            return data;
        }
        
        private PolarWeaponData CreateTempMissileWeaponData()
        {
            var data = CreateTempWeaponData();
            SetFieldViaReflection(data, "projectileSpeed", _projectileSpeed);
            SetFieldViaReflection(data, "projectileLifetime", _projectileLifetime);
            SetFieldViaReflection(data, "spreadAngle", _spreadAngle);
            return data;
        }
        
        private void ForceStartMoveForTest(PolarProjectileBase projectile, IPolarField field, PolarWeaponData data, float angleDeg, float startRadius)
        {
            if (projectile == null || field == null) return;

             // 공통 필드(_angleDeg/_radius/_speed)를 베이스에 맞춰 강제 세팅
             // - radius는 0이면 벽 충돌 판정/데미지 처리만 발생할 수 있으니 InitialRadius 근처에서 시작
             // - speed는 데이터에 있으면 사용
            // - speed는 데이터에 있으면 사용
            startRadius = Mathf.Max(0.5f, startRadius);

             float speed = 0f;
             try
             {
                // PolarWeaponData가 케이스별로 필드/프로퍼티가 섞여 있을 수 있으므로 둘 다 시도
                var prop = data?.GetType().GetProperty("ProjectileSpeed");
                if (prop != null && prop.PropertyType == typeof(float)) speed = (float)prop.GetValue(data);

                if (speed <= 0f)
                {
                    var fieldInfo = data?.GetType().GetField("projectileSpeed",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                    if (fieldInfo != null && fieldInfo.FieldType == typeof(float)) speed = (float)fieldInfo.GetValue(data);
                }
             }
             catch { /* ignore */ }

             if (speed <= 0f)
             {
                 // WeaponData에 못 찾으면 에디터 설정값 사용
                 speed = Mathf.Max(0.1f, _projectileSpeed);
             }

             SetFieldViaReflection(projectile, "_field", field);
             SetFieldViaReflection(projectile, "_weaponData", data);
             SetFieldViaReflection(projectile, "_isActive", true);
             SetFieldViaReflection(projectile, "_hasReachedWall", false);
             SetFieldViaReflection(projectile, "_angleDeg", angleDeg);
             SetFieldViaReflection(projectile, "_radius", startRadius);
             SetFieldViaReflection(projectile, "_speed", speed);

             // 위치 즉시 반영
             var updatePos = projectile.GetType().BaseType?.GetMethod("UpdatePolarPosition",
                 System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
             updatePos?.Invoke(projectile, null);
             LogProjectileStateForTest("ForceStart", projectile, field);
          }
        
        private void LogProjectileStateForTest(string tag, PolarProjectileBase projectile, IPolarField field)
        {
            if (projectile == null) return;
            float angle = ReadFloatField(projectile, "_angleDeg");
            float radius = ReadFloatField(projectile, "_radius");
            float speed = ReadFloatField(projectile, "_speed");
            bool isActive = ReadBoolField(projectile, "_isActive");
            bool reached = ReadBoolField(projectile, "_hasReachedWall");

            int sector = -1;
            float sectorRadius = -1f;
            try
            {
                if (field != null)
                {
                    sector = field.AngleToSectorIndex(angle);
                    sectorRadius = field.GetSectorRadius(sector);
                }
            }
            catch { /* ignore */ }

            Debug.Log($"[WeaponAssembly][TestFire:{tag}] go={projectile.gameObject.name} active={isActive} angle={angle:F1} radius={radius:F2} speed={speed:F2} reachedWall={reached} sector={sector} sectorRadius={sectorRadius:F2} pos={projectile.transform.position}");
        }
        
        private float ReadFloatField(object obj, string fieldName)
        {
            var f = obj?.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            return (f != null && f.FieldType == typeof(float)) ? (float)f.GetValue(obj) : 0f;
        }
        
        private bool ReadBoolField(object obj, string fieldName)
        {
            var f = obj?.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            return (f != null && f.FieldType == typeof(bool)) ? (bool)f.GetValue(obj) : false;
        }
    }
}
