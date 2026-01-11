# Polar 무기 데이터 JSON 내보내기/가져오기 기능

**작성일:** 2026-01-11  
**버전:** 1.0

## 개요

모든 Polar 무기 데이터를 JSON 형식으로 내보내고 가져올 수 있는 기능이 추가되었습니다.

---

## 기능

### 1. JSON 내보내기 (Export)

무기 데이터를 JSON 파일로 저장:
- 모든 무기 파라미터 포함
- 사람이 읽기 쉬운 형식 (Pretty Print)
- 색상은 RGBA 배열로 변환

### 2. JSON 가져오기 (Import)

JSON 파일에서 무기 데이터 로드:
- 기존 무기 데이터 덮어쓰기
- Undo 지원 (Ctrl+Z로 복구 가능)
- 타입 안전성 (Enum 자동 파싱)

### 3. 일괄 내보내기

프로젝트의 모든 무기 데이터를 한 번에 내보내기

---

## 사용 방법

### Unity Inspector에서

1. **무기 데이터 선택**
   - Project 창에서 PolarWeaponData 에셋 선택

2. **Inspector 하단 버튼 사용**
   - `Export to JSON`: JSON 파일로 저장
   - `Import from JSON`: JSON 파일에서 로드
   - `Copy JSON to Clipboard`: JSON을 클립보드에 복사

### Assets 메뉴에서

1. **단일 무기 내보내기/가져오기**
   ```
   Assets → Polar → Export Weapon Data to JSON
   Assets → Polar → Import Weapon Data from JSON
   ```

2. **전체 무기 일괄 내보내기**
   ```
   Assets → Polar → Export All Weapons to JSON
   ```

---

## JSON 형식

### 기본 무기 데이터 (PolarWeaponData)

```json
{
    "id": "weapon_laser_01",
    "weaponName": "Basic Laser",
    "weaponBundleId": "Weapons/Laser",
    "projectileBundleId": "Projectiles/LaserBeam",
    "damage": 100.0,
    "knockbackPower": 0.2,
    "areaType": "Fixed",
    "damageRadius": 0,
    "useGaussianFalloff": true,
    "woundIntensity": 0.2,
    "tickRate": 10.0
}
```

### 레이저 무기 데이터 (PolarLaserWeaponData)

```json
{
    "baseData": "{...기본 데이터...}",
    "extendSpeed": 50.0,
    "retractSpeed": 70.0,
    "maxLength": 50.0,
    "beamWidth": 0.5,
    "beamColor": [0.0, 1.0, 1.0, 1.0],
    "duration": 2.0
}
```

### 머신건 무기 데이터 (PolarMachinegunWeaponData)

```json
{
    "baseData": "{...기본 데이터...}",
    "fireRate": 10.0,
    "projectileSpeed": 15.0,
    "spreadAngle": 2.0,
    "projectileLifetime": 3.0,
    "projectileScale": 0.3,
    "projectileColor": [1.0, 1.0, 0.0, 1.0]
}
```

### 미사일 무기 데이터 (PolarMissileWeaponData)

```json
{
    "baseData": "{...기본 데이터...}",
    "fireRate": 0.5,
    "missileSpeed": 12.0,
    "missileLifetime": 5.0,
    "coreRadius": 1,
    "effectiveRadius": 5,
    "maxRadius": 8,
    "coreMultiplier": 1.0,
    "effectiveMinMultiplier": 0.8,
    "maxMinMultiplier": 0.1,
    "falloffType": "Smooth",
    "missileScale": 0.5,
    "missileColor": [1.0, 0.0, 0.0, 1.0]
}
```

---

## 코드 API

### 내보내기

```csharp
// 무기 데이터를 JSON 문자열로 변환
PolarWeaponData weaponData = ...;
string json = weaponData.ToJson(prettyPrint: true);

// 파일로 저장
File.WriteAllText("weapon.json", json);
```

### 가져오기

```csharp
// JSON 파일에서 로드
string json = File.ReadAllText("weapon.json");
PolarWeaponData weaponData = ...;
weaponData.FromJson(json);
```

### 프로그래밍 방식 사용

```csharp
// 런타임에서 무기 데이터 복제
var originalWeapon = ...;
string json = originalWeapon.ToJson(false);

var newWeapon = ScriptableObject.CreateInstance<PolarWeaponData>();
newWeapon.FromJson(json);
```

---

## 활용 사례

### 1. 무기 밸런싱

```
1. 무기 데이터를 JSON으로 내보내기
2. 외부 툴(Excel, 스프레드시트)에서 편집
3. JSON으로 다시 가져오기
4. 게임에서 테스트
```

### 2. 버전 관리

```
- JSON 파일을 Git에 커밋
- 변경 사항 추적 용이
- 병합 충돌 해결 간편
```

### 3. 데이터 공유

```
- 팀원 간 무기 설정 공유
- 커뮤니티 모드 지원
- 패치 노트 생성
```

### 4. 자동화

```csharp
// 빌드 시 모든 무기 데이터 백업
[MenuItem("Build/Backup Weapon Data")]
static void BackupWeaponData()
{
    var weapons = AssetDatabase.FindAssets("t:PolarWeaponData");
    foreach (var guid in weapons)
    {
        var path = AssetDatabase.GUIDToAssetPath(guid);
        var weapon = AssetDatabase.LoadAssetAtPath<PolarWeaponData>(path);
        var json = weapon.ToJson(true);
        File.WriteAllText($"Backup/{weapon.name}.json", json);
    }
}
```

### 5. 런타임 로딩

```csharp
// 외부 파일에서 무기 데이터 로드
public class WeaponLoader : MonoBehaviour
{
    public PolarWeaponData weaponTemplate;
    
    void Start()
    {
        string json = File.ReadAllText("custom_weapon.json");
        var customWeapon = Instantiate(weaponTemplate);
        customWeapon.FromJson(json);
        // customWeapon 사용...
    }
}
```

---

## 주의사항

### 1. GameObject/Sprite 참조

JSON은 GameObject나 Sprite 같은 Unity 에셋 참조를 포함하지 않습니다:
- `icon` (Sprite) → 저장 안 됨
- `explosionVFXPrefab` (GameObject) → 저장 안 됨

**해결 방법:**
- 에셋 번들 ID나 경로만 저장
- 로드 시 ResourceService로 다시 로드

### 2. 타입 안전성

```csharp
// ❌ 잘못된 타입으로 가져오기
PolarLaserWeaponData laserData = ...;
string missileJson = ...; // 미사일 JSON
laserData.FromJson(missileJson); // 레이저 전용 필드가 비어있음!

// ✅ 올바른 방법
if (laserData.GetType() == typeof(PolarLaserWeaponData))
{
    laserData.FromJson(json);
}
```

### 3. 버전 호환성

JSON 형식이 변경되면 구버전과 호환되지 않을 수 있습니다:
- 새 필드 추가 시 기본값 사용
- 필드 제거 시 무시됨
- 필드명 변경 시 데이터 손실

---

## 확장 가능성

### 커스텀 직렬화 추가

```csharp
public class MyCustomWeaponData : PolarWeaponData
{
    [SerializeField] private int customValue;
    
    public override string ToJson(bool prettyPrint = true)
    {
        var baseJson = base.ToJson(false);
        var data = new CustomWeaponJson
        {
            baseData = baseJson,
            customValue = this.customValue
        };
        return JsonUtility.ToJson(data, prettyPrint);
    }
    
    [System.Serializable]
    private class CustomWeaponJson
    {
        public string baseData;
        public int customValue;
    }
}
```

### JSON 검증

```csharp
public static bool ValidateWeaponJson(string json)
{
    try
    {
        var data = JsonUtility.FromJson<PolarWeaponData.WeaponDataJson>(json);
        return !string.IsNullOrEmpty(data.id);
    }
    catch
    {
        return false;
    }
}
```

---

## 문제 해결

### Q: JSON 내보내기가 작동하지 않습니다.
**A:** Editor 폴더가 올바른 위치에 있는지 확인하세요:
```
Assets/Polar/Weapons/Editor/PolarWeaponDataJsonUtility.cs
```

### Q: 가져온 데이터가 반영되지 않습니다.
**A:** `EditorUtility.SetDirty()`가 호출되었는지 확인하고, 에셋을 다시 저장하세요.

### Q: 색상이 제대로 저장/로드되지 않습니다.
**A:** RGBA 배열이 4개 요소를 가지는지 확인하세요.

---

## 요약

### 추가된 파일

1. **PolarWeaponData.cs** - ToJson/FromJson 메서드
2. **PolarLaserWeaponData.cs** - 레이저 전용 JSON
3. **PolarMachinegunWeaponData.cs** - 머신건 전용 JSON
4. **PolarMissileWeaponData.cs** - 미사일 전용 JSON
5. **PolarWeaponDataJsonUtility.cs** (Editor) - Unity 에디터 통합

### 주요 기능

- ✅ JSON 내보내기/가져오기
- ✅ Inspector 버튼 통합
- ✅ 일괄 내보내기
- ✅ 클립보드 복사
- ✅ Undo 지원
- ✅ 타입별 직렬화

### 사용법

```
1. 무기 데이터 선택
2. Inspector에서 "Export to JSON" 클릭
3. 파일 저장 위치 선택
4. JSON 파일 편집 (선택)
5. "Import from JSON"으로 다시 로드
```

---

**모든 Polar 무기 데이터를 이제 JSON으로 쉽게 관리할 수 있습니다!**

