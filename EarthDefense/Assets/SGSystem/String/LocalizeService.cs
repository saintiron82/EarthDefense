using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;  
using System.Globalization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using Cysharp.Threading.Tasks;
using SG.UI;

namespace SG.LocaleService
{ 
    public class LocalizeService : ServiceBase
    {
        public const string STR_LOCALIZE_STRING_TABLE = "Localization"; // DESC :: 불러올 테이블이름, Unity Localization 패키지에서 설정한 테이블 이름과 일치해야 한다 패키지에서는 여러가지 테이블을 지원할 수 있도록 테이블 - 키 쌍을 입력받게함.

        public static int CurrentLanguageIndex
        {
            get
            {
                // DESC :: 현재 선택된 로케일의 인덱스를 반환한다.
                var currentLocale = LocalizationSettings.SelectedLocale;
                if (currentLocale == null)
                {
                    Debug.LogWarning("No locale is currently selected. Returning -1 as index.");
                    return -1;
                }
                var availableLocales = LocalizationSettings.Instance.GetAvailableLocales().Locales;
                return availableLocales.FindIndex(locale => locale.Identifier.Code == currentLocale.Identifier.Code);
            }
        }

        public override UniTask<bool> Init()
        {

            return base.Init();
        }

        public static async UniTask InitializeAsync()
        {
            // DESC :: 로컬라이제이션 시스템을 초기화한다.
            if (LocalizationSettings.InitializationOperation.IsDone == false)
            {
                await LocalizationSettings.InitializationOperation.ToUniTask();
            }
            else
            {
                Debug.Log("LocalizationSettings is already initialized.");
            }
        }

        public static void SetLcaleChangeCallback(Action<Locale> callback)
        {
            LocalizationSettings.Instance.OnSelectedLocaleChanged -= callback; // DESC :: 기존 콜백 제거 (중복 방지)
            LocalizationSettings.Instance.OnSelectedLocaleChanged += callback;
        }

        public static string GetLocalizedString(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }
            var localizedString = new LocalizedString { TableReference = STR_LOCALIZE_STRING_TABLE, TableEntryReference = key };
            // DESC :: 로컬라이즈된 문자열을 가져오고, 만약 로컬라이즈된 문자열이 비어있다면 키를 반환
            if (localizedString.IsEmpty == true)
            {
                Debug.LogWarning($"Localized string for key '{key}' not found in table '{STR_LOCALIZE_STRING_TABLE}'. Returning key as fallback.");
                return key;
            }
            else
            {
                return localizedString.GetLocalizedString();
            }
        }

        /// <summary>
        /// 로컬라이즈 문자열이 ex: tail1 , tail2, tail3 등과 같이 키가 포맷된 경우에 사용한다. (GetLozlizedStringWithFormat와 다름)
        /// </summary>
        /// <param name="key">로컬라이즈 키를 만들기 위한 string format</param>
        /// <param name="args">로컬라이즈 키를 만들기 위해 들어가야할 args</param>
        /// <returns></returns>
        public static string GetLocalizedStringWithKeyFormat(string key, params object[] args)
        {
            // DESC :: 로컬라이즈된 문자열을 가져오고, 만약 로컬라이즈된 문자열이 비어있다면 키를 반환
            var localizeKey = string.Format(key, args);
            return GetLocalizedString(localizeKey);
        }

        /// <summary>
        /// 불러온 로컬라이즈된 문자열에 포맷을 적용하여 반환한다.
        /// </summary>
        /// <param name="key">로컬라이즈 키</param>
        /// <param name="args">로컬라이즈 키로 불러온 string에 format형식에 들어갈 args</param>
        /// <returns></returns>
        public static string GetLocalizedStringWithFormat(string key, params object[] args)
        {
            var localizedString = GetLocalizedString(key);
            if (string.IsNullOrEmpty(localizedString))
            {
                return key;
            }
            return string.Format(localizedString, args);
        }

        public static List<Locale> GetPossibleLocaleInfo()
        {
            // DESC :: 현재 프로젝트에서 지원하는 로케일 정보를 가져온다.
            return LocalizationSettings.Instance.GetAvailableLocales().Locales;
        }

        public static CultureInfo GetCurrentCultureInfo()
        {
            // DESC :: 현재 로케일의 CultureInfo를 반환한다.
            var currentLocale = LocalizationSettings.SelectedLocale;
            if (currentLocale == null)
            {
                Debug.LogWarning("No locale is currently selected. Returning invariant culture.");
                return CultureInfo.InvariantCulture;
            }

            return new CultureInfo(currentLocale.Identifier.Code);
        }
        
        public static string GetLanguageCode()
        {
            // DESC :: 현재 로케일의 언어 코드를 반환한다.
            var currentLocale = LocalizationSettings.SelectedLocale;
            if (currentLocale == null)
            {
                Debug.LogWarning("No locale is currently selected. Returning empty language code.");
                return string.Empty;
            }
            return currentLocale.Identifier.CultureInfo.TwoLetterISOLanguageName;
        }

        public static string GetCurrentLocaleName()
        {  
            var culture = CultureInfo.CurrentCulture;
            return new RegionInfo(culture.Name).TwoLetterISORegionName;
        }

        public static void SetCurrentLocale(Locale locale)
        {
            // DESC :: 현재 로케일을 설정한다.
            if (locale != null)
            {
                if (LocalizationSettings.Instance.GetAvailableLocales().Locales.Contains(locale) == false)
                {
                    Debug.LogWarning($"Locale '{locale.Identifier}' is not available in the project. Cannot set as current locale.");
                    return;
                }

                LocalizationSettings.SelectedLocale = locale;
                UIService.OpendUIPresenterChangeLanguage(locale); // DESC :: 언어 변경 UI 프리젠터 호출 만약 Localize가 아니라 런타임에 Text 지정하는거면 별도지정을 위해 콜백달아논거에 호출
            }
            else
            {
                Debug.LogWarning("Attempted to set a null locale.");
            }
        }

        public static List<Locale> GetAvailableLanguages()
        {
            return LocalizationSettings.Instance.GetAvailableLocales().Locales;
        }

        /// <summary>
        /// localizedString에 해당하는 키를 찾습니다. (Localiazation 테이블에서) , format된 문자열은 지원 X
        /// </summary>
        /// <param name="localizedString">이미 로컬라이즈화된 문자열</param>
        /// <returns></returns>
        public static string FindEntryKeyByLocalizedString(string localizedString)
        {
            if (string.IsNullOrEmpty(localizedString))
            {
                return null;
            }

            if (LocalizationSettings.AvailableLocales == null || LocalizationSettings.StringDatabase == null)
            {
                Debug.LogWarning("Localization system is not initialized.");
                return null; // DESC :: 로컬라이제이션 시스템이 초기화되지 않음
            }

            var currentLocale = LocalizationSettings.SelectedLocale;
            if (currentLocale == null)
            {
                Debug.LogWarning("No locale is currently selected.");
                return null;
            }

            var stringTable = LocalizationSettings.StringDatabase.GetTable(STR_LOCALIZE_STRING_TABLE, currentLocale);
            
            if (stringTable == null)
            {
                Debug.LogWarning($"Could not find localization table '{STR_LOCALIZE_STRING_TABLE}' for locale '{currentLocale.Identifier}'.");
                return null;
            }
            
            // DESC :: 테이블의 모든 엔트리에서 값이 일치하는 키 찾기
            foreach (var entry in stringTable.Values)
            {
                if (entry != null && string.Equals(entry.LocalizedValue, localizedString, System.StringComparison.Ordinal))
                {
                    return entry.Key;
                }
            }

            Debug.LogWarning($"Could not find key for localized string: '{localizedString}'");
            return null;
        }
    }
}