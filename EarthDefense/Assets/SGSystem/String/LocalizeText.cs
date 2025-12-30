using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Components;
using TMPro;

namespace SG.LocaleService
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LocalizeText : MonoBehaviour
    {
        [SerializeField, HideInInspector] private LocalizedString _localizeString;
        private TextMeshProUGUI _textMeshProUGUI;

        public TextMeshProUGUI TextMeshProUGUI
        {
            get { return _textMeshProUGUI; }
        }

        private void Awake()
        {
            _textMeshProUGUI = GetComponent<TextMeshProUGUI>();
            _localizeString.StringChanged += UpdateText;
        }

        private void OnDestroy()
        {
            _localizeString.StringChanged -= UpdateText;
        }

        private void UpdateText(string localizedText)
        {
            if (_textMeshProUGUI != null)
            {
                _textMeshProUGUI.SetText(localizedText);
            }
        }

        public void RefreshText()
        {
            UpdateText(_localizeString.TableEntryReference);
        }

        /// <summary>
        /// 로컬라이즈된 문자열의 테이블 이름과 엔트리 이름을 설정합니다.
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="entryName"></param>
        public void SetEntry(string tableName, string entryName)
        {
            _localizeString.TableReference = tableName;
            _localizeString.TableEntryReference = entryName;
            UpdateText(_localizeString.GetLocalizedString());
        }

        public void SetEntry(string entryName)
        {
            _localizeString.TableEntryReference = entryName;
            UpdateText(_localizeString.GetLocalizedString());
        }

        /// <summary>
        /// 로컬라이즈된 문자열을 직접 설정합니다.
        /// </summary>
        /// <param name="text"></param>
        public void SetText(string text) 
        {
            var findEntryKey = LocalizeService.FindEntryKeyByLocalizedString(text);
            if (!string.IsNullOrEmpty(findEntryKey))
            {
                SetEntry(findEntryKey);
            }
            else
            {
                _localizeString.TableEntryReference = string.Empty;
                UpdateText(text);
            }
        }
    }
}