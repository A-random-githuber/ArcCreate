using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArcCreate.Compose.Components
{
    public class ChartFilePathInputField : MonoBehaviour
    {
        private static readonly List<(string Path, string LocalizationKey, bool IsInscribed)> Options
            = new List<(string Path, string LocalizationKey, bool IsInscribed)>
        {
            ("0", "Compose.UI.Project.Options.Past", false),
            ("1", "Compose.UI.Project.Options.Present", false),
            ("2", "Compose.UI.Project.Options.Future", false),
            ("4", "Compose.UI.Project.Options.Eternal", false),
            ("3", "Compose.UI.Project.Options.Beyond", false),
            ("3", "Compose.UI.Project.Options.Inscribed", true),
        };

        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private Toggle toggleCustom;
        [SerializeField] private TMP_Dropdown presets;

        public bool IsDifficultyAliasPreset => !toggleCustom.isOn && Options[presets.value].Path == "3";

        public bool UseInscribedPreset => !toggleCustom.isOn && Options[presets.value].IsInscribed;

        private void Awake()
        {
            presets.onValueChanged.AddListener(OnPresetSelect);
            toggleCustom.onValueChanged.AddListener(OnToggleCustom);
            ReloadOptions();
            I18n.OnLocaleChanged += ReloadOptions;

            presets.SetValueWithoutNotify(2);
            inputField.SetTextWithoutNotify("2");
        }

        private void ReloadOptions()
        {
            presets.options = Options
                .Select(e => new TMP_Dropdown.OptionData(I18n.S(e.LocalizationKey)))
                .ToList();
        }

        private void OnDestroy()
        {
            presets.onValueChanged.RemoveListener(OnPresetSelect);
            toggleCustom.onValueChanged.RemoveListener(OnToggleCustom);
            I18n.OnLocaleChanged -= ReloadOptions;
        }

        private void OnToggleCustom(bool val)
        {
            inputField.gameObject.SetActive(val);
            presets.gameObject.SetActive(!val);
            if (!val)
            {
                OnPresetSelect(presets.value);
            }
        }

        private void OnPresetSelect(int val)
        {
            inputField.text = Options[val].Path;
        }
    }
}
