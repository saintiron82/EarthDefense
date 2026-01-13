#!/usr/bin/env python3
"""
Polar Asset CLI - JSON으로 Unity .asset 파일 자동 생성

사용법:
    python polar_asset_cli.py create --type laser --json weapon.json --output Assets/Polar/RES/
    python polar_asset_cli.py create --type machinegun --json weapon.json
    python polar_asset_cli.py batch --input weapons_folder/ --output Assets/Polar/RES/
    python polar_asset_cli.py update --asset existing.asset --json updates.json
"""

import argparse
import json
import uuid
import os
import sys
from pathlib import Path
from typing import Dict, Any, Optional

# 무기 타입별 스크립트 GUID 및 클래스 정보
WEAPON_TYPES = {
    "laser": {
        "guid": "e9e885dc7d0dbbf47854304fe725e2a9",
        "class": "Assembly-CSharp::Polar.Weapons.PolarLaserWeaponData",
        "fields": [
            # Base fields
            "id", "weaponName", "icon", "weaponBundleId", "projectileBundleId",
            "damage", "knockbackPower", "areaType", "damageRadius",
            "useGaussianFalloff", "woundIntensity", "tickRate", "optionProfile",
            # Laser specific
            "extendSpeed", "retractSpeed", "maxLength", "beamWidth", "beamColor", "duration"
        ],
        "defaults": {
            "id": "",
            "weaponName": "",
            "weaponBundleId": "",
            "projectileBundleId": "Beam",
            "damage": 100,
            "knockbackPower": 0.2,
            "areaType": 0,
            "damageRadius": 0,
            "useGaussianFalloff": 1,
            "woundIntensity": 0.2,
            "tickRate": 10,
            "extendSpeed": 50,
            "retractSpeed": 70,
            "maxLength": 50,
            "beamWidth": 0.1,
            "beamColor": {"r": 0, "g": 1, "b": 1, "a": 1},
            "duration": 2
        }
    },
    "machinegun": {
        "guid": "418388b5bfcd68d40ac6149f18238753",
        "class": "Assembly-CSharp::Polar.Weapons.PolarMachinegunWeaponData",
        "fields": [
            # Base fields
            "id", "weaponName", "icon", "weaponBundleId", "projectileBundleId",
            "damage", "knockbackPower", "areaType", "damageRadius",
            "useGaussianFalloff", "woundIntensity", "tickRate", "optionProfile",
            # Machinegun specific
            "fireRate", "projectileSpeed", "spreadAngle",
            "projectileLifetime", "projectileScale", "projectileColor", "projectileOptions"
        ],
        "defaults": {
            "id": "",
            "weaponName": "",
            "weaponBundleId": "",
            "projectileBundleId": "Bullet",
            "damage": 50,
            "knockbackPower": 0.2,
            "areaType": 2,
            "damageRadius": 1,
            "useGaussianFalloff": 1,
            "woundIntensity": 0.2,
            "tickRate": 10,
            "fireRate": 10,
            "projectileSpeed": 15,
            "spreadAngle": 2,
            "projectileLifetime": 3,
            "projectileScale": 0.3,
            "projectileColor": {"r": 1, "g": 1, "b": 0, "a": 1}
        }
    },
    "missile": {
        "guid": "f81c35f6ab105394d8dbd7cc1c4ffc5d",
        "class": "Assembly-CSharp::Polar.Weapons.Data.PolarMissileWeaponData",
        "fields": [
            # Base fields
            "id", "weaponName", "icon", "weaponBundleId", "projectileBundleId",
            "damage", "knockbackPower", "areaType", "damageRadius",
            "useGaussianFalloff", "woundIntensity", "tickRate", "optionProfile",
            # Missile specific
            "fireRate", "missileSpeed", "missileLifetime",
            "coreRadius", "effectiveRadius", "maxRadius",
            "coreMultiplier", "effectiveMinMultiplier", "maxMinMultiplier",
            "falloffType", "explosionVFXPrefab", "missileScale", "missileColor", "missileOptions"
        ],
        "defaults": {
            "id": "",
            "weaponName": "",
            "weaponBundleId": "",
            "projectileBundleId": "Missile",
            "damage": 500,
            "knockbackPower": 0.2,
            "areaType": 3,
            "damageRadius": 5,
            "useGaussianFalloff": 1,
            "woundIntensity": 0.2,
            "tickRate": 10,
            "fireRate": 0.5,
            "missileSpeed": 12,
            "missileLifetime": 5,
            "coreRadius": 1,
            "effectiveRadius": 5,
            "maxRadius": 8,
            "coreMultiplier": 1.0,
            "effectiveMinMultiplier": 0.8,
            "maxMinMultiplier": 0.1,
            "falloffType": 1,
            "missileScale": 0.5,
            "missileColor": {"r": 1, "g": 0, "b": 0, "a": 1}
        }
    },
    "bullet": {
        "guid": "",  # TODO: PolarBulletWeaponData의 GUID 필요
        "class": "Assembly-CSharp::Polar.Weapons.PolarBulletWeaponData",
        "fields": [
            "id", "weaponName", "icon", "weaponBundleId", "projectileBundleId",
            "damage", "knockbackPower", "areaType", "damageRadius",
            "useGaussianFalloff", "woundIntensity", "tickRate", "optionProfile",
            "bulletColor", "bulletScale", "bulletSpeed",
            "muzzleFlashPrefab", "impactEffectPrefab", "fireSoundId", "impactSoundId"
        ],
        "defaults": {
            "id": "",
            "weaponName": "",
            "weaponBundleId": "",
            "projectileBundleId": "Bullet",
            "damage": 100,
            "knockbackPower": 0.2,
            "areaType": 1,
            "damageRadius": 0,
            "useGaussianFalloff": 0,
            "woundIntensity": 0.2,
            "tickRate": 10,
            "bulletColor": {"r": 1, "g": 0.8, "b": 0.2, "a": 1},
            "bulletScale": 0.15,
            "bulletSpeed": 10,
            "fireSoundId": "weapon_bullet_fire",
            "impactSoundId": "weapon_bullet_impact"
        }
    }
}

# 옵션 프로필 타입
PROFILE_TYPES = {
    "weapon_option": {
        "guid": "",  # TODO: PolarWeaponOptionProfile의 GUID
        "class": "Assembly-CSharp::Polar.Weapons.PolarWeaponOptionProfile",
        "fields": ["id", "damage", "knockbackPower", "areaType", "damageRadius",
                   "useGaussianFalloff", "woundIntensity", "tickRate"]
    },
    "projectile_option": {
        "guid": "",  # TODO: PolarProjectileOptionProfile의 GUID
        "class": "Assembly-CSharp::Polar.Weapons.PolarProjectileOptionProfile",
        "fields": ["id", "speed", "lifetime", "scale", "color"]
    }
}


def generate_guid() -> str:
    """Unity 호환 GUID 생성 (32자 hex, 소문자)"""
    return uuid.uuid4().hex


def format_yaml_value(value: Any, indent: int = 0) -> str:
    """값을 Unity YAML 형식으로 변환"""
    if value is None:
        return "{fileID: 0}"
    elif isinstance(value, bool):
        return "1" if value else "0"
    elif isinstance(value, int):
        return str(value)
    elif isinstance(value, float):
        return str(value)
    elif isinstance(value, str):
        return value
    elif isinstance(value, dict):
        # Color 또는 참조 타입
        if "r" in value and "g" in value and "b" in value:
            # Color
            return "{" + f"r: {value['r']}, g: {value['g']}, b: {value['b']}, a: {value.get('a', 1)}" + "}"
        elif "fileID" in value:
            # 참조
            if "guid" in value:
                return "{" + f"fileID: {value['fileID']}, guid: {value['guid']}, type: {value.get('type', 3)}" + "}"
            else:
                return "{" + f"fileID: {value['fileID']}" + "}"
        else:
            return "{fileID: 0}"
    elif isinstance(value, list):
        # 배열 (Color 배열 등)
        if len(value) == 4 and all(isinstance(v, (int, float)) for v in value):
            return "{" + f"r: {value[0]}, g: {value[1]}, b: {value[2]}, a: {value[3]}" + "}"
        return "{fileID: 0}"
    else:
        return str(value)


def create_asset_content(weapon_type: str, data: Dict[str, Any], asset_name: str) -> str:
    """Unity .asset 파일 내용 생성"""
    type_info = WEAPON_TYPES.get(weapon_type)
    if not type_info:
        raise ValueError(f"Unknown weapon type: {weapon_type}")

    # 기본값과 병합
    merged_data = {**type_info["defaults"], **data}

    # YAML 헤더
    lines = [
        "%YAML 1.1",
        "%TAG !u! tag:unity3d.com,2011:",
        "--- !u!114 &11400000",
        "MonoBehaviour:",
        "  m_ObjectHideFlags: 0",
        "  m_CorrespondingSourceObject: {fileID: 0}",
        "  m_PrefabInstance: {fileID: 0}",
        "  m_PrefabAsset: {fileID: 0}",
        "  m_GameObject: {fileID: 0}",
        "  m_Enabled: 1",
        "  m_EditorHideFlags: 0",
        f"  m_Script: {{fileID: 11500000, guid: {type_info['guid']}, type: 3}}",
        f"  m_Name: {asset_name}",
        f"  m_EditorClassIdentifier: {type_info['class']}",
    ]

    # 데이터 필드들
    for field in type_info["fields"]:
        value = merged_data.get(field)

        # 참조 필드 처리 (icon, optionProfile, etc.)
        if field in ["icon", "optionProfile", "projectileOptions", "missileOptions",
                     "explosionVFXPrefab", "muzzleFlashPrefab", "impactEffectPrefab"]:
            if value is None or value == "":
                lines.append(f"  {field}: {{fileID: 0}}")
            elif isinstance(value, dict) and "guid" in value:
                lines.append(f"  {field}: {{fileID: {value.get('fileID', 11400000)}, guid: {value['guid']}, type: 2}}")
            else:
                lines.append(f"  {field}: {{fileID: 0}}")
        else:
            formatted = format_yaml_value(value)
            lines.append(f"  {field}: {formatted}")

    return "\n".join(lines) + "\n"


def create_meta_content(guid: str) -> str:
    """Unity .meta 파일 내용 생성"""
    return f"""fileFormatVersion: 2
guid: {guid}
NativeFormatImporter:
  externalObjects: {{}}
  mainObjectFileID: 11400000
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def create_asset(weapon_type: str, json_data: Dict[str, Any], output_path: str, asset_name: Optional[str] = None):
    """에셋 파일 생성"""
    if not asset_name:
        asset_name = json_data.get("id", json_data.get("weaponName", "NewWeapon"))

    asset_content = create_asset_content(weapon_type, json_data, asset_name)
    meta_guid = generate_guid()
    meta_content = create_meta_content(meta_guid)

    # 출력 경로 설정
    output_dir = Path(output_path)
    output_dir.mkdir(parents=True, exist_ok=True)

    asset_file = output_dir / f"{asset_name}.asset"
    meta_file = output_dir / f"{asset_name}.asset.meta"

    # 파일 쓰기
    asset_file.write_text(asset_content, encoding="utf-8")
    meta_file.write_text(meta_content, encoding="utf-8")

    print(f"Created: {asset_file}")
    print(f"Created: {meta_file}")
    print(f"Asset GUID: {meta_guid}")

    return str(asset_file), meta_guid


def update_asset(asset_path: str, json_data: Dict[str, Any]):
    """기존 에셋 파일 업데이트"""
    asset_file = Path(asset_path)
    if not asset_file.exists():
        raise FileNotFoundError(f"Asset not found: {asset_path}")

    # 기존 파일 읽기
    content = asset_file.read_text(encoding="utf-8")
    lines = content.split("\n")

    # 필드 업데이트
    for key, value in json_data.items():
        formatted = format_yaml_value(value)

        # 해당 필드 라인 찾아서 교체
        for i, line in enumerate(lines):
            if line.strip().startswith(f"{key}:"):
                indent = len(line) - len(line.lstrip())
                lines[i] = " " * indent + f"{key}: {formatted}"
                break

    # 파일 쓰기
    asset_file.write_text("\n".join(lines), encoding="utf-8")
    print(f"Updated: {asset_file}")


def batch_create(input_dir: str, output_dir: str, weapon_type: Optional[str] = None):
    """폴더의 모든 JSON 파일로 에셋 생성"""
    input_path = Path(input_dir)
    if not input_path.exists():
        raise FileNotFoundError(f"Input directory not found: {input_dir}")

    json_files = list(input_path.glob("*.json"))
    print(f"Found {len(json_files)} JSON files")

    for json_file in json_files:
        try:
            with open(json_file, "r", encoding="utf-8") as f:
                data = json.load(f)

            # 타입 추론 또는 지정된 타입 사용
            wtype = weapon_type or data.get("type", "laser")

            asset_name = json_file.stem
            create_asset(wtype, data, output_dir, asset_name)

        except Exception as e:
            print(f"Error processing {json_file}: {e}")


def main():
    parser = argparse.ArgumentParser(
        description="Polar Asset CLI - JSON으로 Unity .asset 파일 자동 생성",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
예제:
  # 단일 에셋 생성
  python polar_asset_cli.py create --type laser --json laser_weapon.json --output Assets/Polar/RES/

  # CLI에서 직접 JSON 입력
  python polar_asset_cli.py create --type machinegun --data '{"id":"MG1","damage":100}' --output ./

  # 배치 생성
  python polar_asset_cli.py batch --input ./weapons/ --output Assets/Polar/RES/ --type laser

  # 기존 에셋 업데이트
  python polar_asset_cli.py update --asset weapon.asset --json updates.json
        """
    )

    subparsers = parser.add_subparsers(dest="command", help="명령어")

    # create 명령어
    create_parser = subparsers.add_parser("create", help="새 에셋 생성")
    create_parser.add_argument("--type", "-t", required=True,
                               choices=["laser", "machinegun", "missile", "bullet"],
                               help="무기 타입")
    create_parser.add_argument("--json", "-j", help="JSON 파일 경로")
    create_parser.add_argument("--data", "-d", help="JSON 데이터 문자열")
    create_parser.add_argument("--output", "-o", default=".", help="출력 디렉토리")
    create_parser.add_argument("--name", "-n", help="에셋 이름 (기본값: id 필드)")

    # batch 명령어
    batch_parser = subparsers.add_parser("batch", help="폴더의 모든 JSON으로 배치 생성")
    batch_parser.add_argument("--input", "-i", required=True, help="입력 폴더 (JSON 파일들)")
    batch_parser.add_argument("--output", "-o", required=True, help="출력 폴더")
    batch_parser.add_argument("--type", "-t", help="무기 타입 (지정 안하면 JSON의 type 필드 사용)")

    # update 명령어
    update_parser = subparsers.add_parser("update", help="기존 에셋 업데이트")
    update_parser.add_argument("--asset", "-a", required=True, help=".asset 파일 경로")
    update_parser.add_argument("--json", "-j", help="JSON 파일 경로")
    update_parser.add_argument("--data", "-d", help="JSON 데이터 문자열")

    # list 명령어
    list_parser = subparsers.add_parser("list", help="지원되는 무기 타입 및 필드 목록")
    list_parser.add_argument("--type", "-t", help="특정 타입의 상세 정보")

    args = parser.parse_args()

    if args.command == "create":
        # JSON 데이터 로드
        if args.json:
            with open(args.json, "r", encoding="utf-8") as f:
                data = json.load(f)
        elif args.data:
            data = json.loads(args.data)
        else:
            print("Error: --json 또는 --data 옵션이 필요합니다")
            sys.exit(1)

        create_asset(args.type, data, args.output, args.name)

    elif args.command == "batch":
        batch_create(args.input, args.output, args.type)

    elif args.command == "update":
        if args.json:
            with open(args.json, "r", encoding="utf-8") as f:
                data = json.load(f)
        elif args.data:
            data = json.loads(args.data)
        else:
            print("Error: --json 또는 --data 옵션이 필요합니다")
            sys.exit(1)

        update_asset(args.asset, data)

    elif args.command == "list":
        if args.type:
            type_info = WEAPON_TYPES.get(args.type)
            if type_info:
                print(f"\n=== {args.type.upper()} ===")
                print(f"Script GUID: {type_info['guid']}")
                print(f"Class: {type_info['class']}")
                print(f"\nFields:")
                for field in type_info["fields"]:
                    default = type_info["defaults"].get(field, "N/A")
                    print(f"  - {field}: {default}")
            else:
                print(f"Unknown type: {args.type}")
        else:
            print("\n지원되는 무기 타입:")
            for wtype in WEAPON_TYPES:
                print(f"  - {wtype}")
            print("\n상세 정보: python polar_asset_cli.py list --type <type>")
    else:
        parser.print_help()


if __name__ == "__main__":
    main()
