using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using SG;

public class LocalizeParticleText : MonoBehaviour
{
    [Header("Particle System Settings")]
    [SerializeField]
    private ParticleSystem targetParticleSystem; // DESC :: 텍스처를 변경할 파티클 시스템

    [Header("Localized Textures")]
    [SerializeField]
    private List<LocalizedTextureData> localizedTextures = new List<LocalizedTextureData>(); // DESC :: 언어별 텍스처 데이터

    [Header("Material Settings")]
    [SerializeField]
    private string texturePropertyName = ConstrantStrings.MainTex; // DESC :: 머티리얼의 텍스처 프로퍼티 이름

    [SerializeField]
    private bool createMaterialInstance = true; // DESC :: 머티리얼 인스턴스를 생성할지 여부

    private ParticleSystemRenderer particleRenderer; // DESC :: 파티클 시스템 렌더러
    private Material originalMaterial; // DESC :: 원본 머티리얼
    private Material materialInstance; // DESC :: 머티리얼 인스턴스
    private bool isInitialized = false; // DESC :: 초기화 여부

    [Serializable]
    public class LocalizedTextureData
    {
        [Tooltip("언어 코드 (예: ko, en, ja)")]
        public string localeCode = string.Empty; // DESC :: 언어 코드
        
        [Tooltip("해당 언어에 대응되는 텍스처")]
        public Texture2D texture = null; // DESC :: 텍스처
    }

    void Start()
    {
        InitializeComponents();
        RegisterLocalizationEvent();
        UpdateTextureForCurrentLocale();
    }

    void OnEnable()
    {
        if (isInitialized)
        {
            RegisterLocalizationEvent();
        }
    }

    void OnDisable()
    {
        UnregisterLocalizationEvent();
    }

    void OnDestroy()
    {
        UnregisterLocalizationEvent();
        CleanupMaterialInstance();
    }

    private void InitializeComponents()
    {
        // DESC :: 파티클 시스템이 지정되지 않았다면 현재 GameObject에서 찾기
        if (targetParticleSystem == null)
        {
            targetParticleSystem = GetComponent<ParticleSystem>();
        }

        if (targetParticleSystem == null)
        {
            Debug.LogError($"LocalizeParticleText: ParticleSystem not found on {gameObject.name}");
            return;
        }

        // DESC :: 파티클 시스템 렌더러 가져오기
        particleRenderer = targetParticleSystem.GetComponent<ParticleSystemRenderer>();
        if (particleRenderer == null)
        {
            Debug.LogError($"LocalizeParticleText: ParticleSystemRenderer not found on {gameObject.name}");
            return;
        }

        // DESC :: 원본 머티리얼 저장
        originalMaterial = particleRenderer.sharedMaterial;
        
        // DESC :: 머티리얼 인스턴스 생성
        if (createMaterialInstance && originalMaterial != null)
        {
            CreateMaterialInstance();
        }

        isInitialized = true;
    }

    private void CreateMaterialInstance()
    {
        if (originalMaterial != null)
        {
            materialInstance = new Material(originalMaterial);
            particleRenderer.material = materialInstance;
        }
    }

    private void CleanupMaterialInstance()
    {
        if (materialInstance != null)
        {
            if (Application.isPlaying)
            {
                Destroy(materialInstance);
            }
            else
            {
                DestroyImmediate(materialInstance);
            }
            materialInstance = null;
        }
    }

    private void RegisterLocalizationEvent()
    {
        if (LocalizationSettings.Instance != null)
        {
            LocalizationSettings.Instance.OnSelectedLocaleChanged -= OnLocaleChanged;
            LocalizationSettings.Instance.OnSelectedLocaleChanged += OnLocaleChanged;
        }
    }

    private void UnregisterLocalizationEvent()
    {
        if (LocalizationSettings.Instance != null)
        {
            LocalizationSettings.Instance.OnSelectedLocaleChanged -= OnLocaleChanged;
        }
    }

    private void OnLocaleChanged(Locale newLocale)
    {
        UpdateTextureForCurrentLocale();
    }

    private void UpdateTextureForCurrentLocale()
    {
        if (!isInitialized || particleRenderer == null)
        {
            return;
        }

        var currentLocale = LocalizationSettings.SelectedLocale;
        if (currentLocale == null)
        {
            Debug.LogWarning($"LocalizeParticleText: No locale selected for {gameObject.name}");
            return;
        }

        string currentLocaleCode = currentLocale.Identifier.Code;
        Texture2D targetTexture = GetTextureForLocale(currentLocaleCode);

        if (targetTexture != null)
        {
            ApplyTextureToMaterial(targetTexture);
        }
        else
        {
            Debug.LogWarning($"LocalizeParticleText: No texture found for locale '{currentLocaleCode}' on {gameObject.name}");
        }
    }

    private Texture2D GetTextureForLocale(string localeCode)
    {
        foreach (var localizedData in localizedTextures)
        {
            if (string.Equals(localizedData.localeCode, localeCode, StringComparison.OrdinalIgnoreCase))
            {
                return localizedData.texture;
            }
        }

        // DESC :: 일치하는 언어 코드가 없으면 첫 번째 텍스처 반환 (fallback)
        if (localizedTextures.Count > 0 && localizedTextures[0].texture != null)
        {
            return localizedTextures[0].texture;
        }

        return null;
    }

    private void ApplyTextureToMaterial(Texture2D texture)
    {
        Material targetMaterial = createMaterialInstance ? materialInstance : originalMaterial;
        
        if (targetMaterial != null && targetMaterial.HasProperty(texturePropertyName))
        {
            targetMaterial.SetTexture(texturePropertyName, texture);
            
            // DESC :: 머티리얼 인스턴스를 사용하지 않는 경우 렌더러에 직접 적용
            if (!createMaterialInstance)
            {
                particleRenderer.material = targetMaterial;
            }
        }
        else
        {
            Debug.LogWarning($"LocalizeParticleText: Material does not have property '{texturePropertyName}' on {gameObject.name}");
        }
    }

    /// <summary>
    /// 런타임에서 특정 언어 코드에 대한 텍스처를 설정하는 메서드
    /// </summary>
    /// <param name="localeCode">언어 코드</param>
    /// <param name="texture">설정할 텍스처</param>
    public void SetTextureForLocale(string localeCode, Texture2D texture)
    {
        // DESC :: 기존 데이터 찾기
        LocalizedTextureData existingData = null;
        foreach (var data in localizedTextures)
        {
            if (string.Equals(data.localeCode, localeCode, StringComparison.OrdinalIgnoreCase))
            {
                existingData = data;
                break;
            }
        }

        // DESC :: 기존 데이터가 있으면 업데이트, 없으면 새로 추가
        if (existingData != null)
        {
            existingData.texture = texture;
        }
        else
        {
            localizedTextures.Add(new LocalizedTextureData
            {
                localeCode = localeCode,
                texture = texture
            });
        }

        // DESC :: 현재 로케일과 일치하면 즉시 적용
        var currentLocale = LocalizationSettings.SelectedLocale;
        if (currentLocale != null && 
            string.Equals(currentLocale.Identifier.Code, localeCode, StringComparison.OrdinalIgnoreCase))
        {
            UpdateTextureForCurrentLocale();
        }
    }

    /// <summary>
    /// 특정 언어 코드의 텍스처 데이터를 제거하는 메서드
    /// </summary>
    /// <param name="localeCode">제거할 언어 코드</param>
    public void RemoveTextureForLocale(string localeCode)
    {
        for (int i = localizedTextures.Count - 1; i >= 0; --i)
        {
            if (string.Equals(localizedTextures[i].localeCode, localeCode, StringComparison.OrdinalIgnoreCase))
            {
                localizedTextures.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// 현재 설정된 모든 언어 코드 목록을 반환하는 메서드
    /// </summary>
    /// <returns>언어 코드 배열</returns>
    public string[] GetAvailableLocaleCodes()
    {
        string[] codes = new string[localizedTextures.Count];
        for (int i = 0; i < localizedTextures.Count; ++i)
        {
            codes[i] = localizedTextures[i].localeCode;
        }
        return codes;
    }

#if UNITY_EDITOR
    // DESC :: 에디터에서 디버깅을 위한 메서드
    [ContextMenu("Test Current Locale")]
    private void TestCurrentLocale()
    {
        if (Application.isPlaying)
        {
            var currentLocale = LocalizationSettings.SelectedLocale;
            if (currentLocale != null)
            {
                Debug.Log($"Current Locale: {currentLocale.Identifier.Code}");
                UpdateTextureForCurrentLocale();
            }
            else
            {
                Debug.Log("No locale is currently selected");
            }
        }
        else
        {
            Debug.Log("This test can only be run in Play Mode");
        }
    }
#endif
}
