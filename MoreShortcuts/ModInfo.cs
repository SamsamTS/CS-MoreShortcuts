using ICities;
using UnityEngine;

using System;

using ColossalFramework.UI;

namespace MoreShortcuts
{
    public class ModInfo : IUserMod
    {
        public ModInfo()
        {
            GameObject gameObject = GameObject.Find("MoreShortcuts");
            if (gameObject != null) GameObject.DestroyImmediate(gameObject);

            GameObject.DontDestroyOnLoad(new GameObject("MoreShortcuts").AddComponent<MoreShortcuts>());
            Shortcut.LoadShorcuts();
        }

        public string Name
        {
            get { return "More Shortcuts " + version; }
        }

        public string Description
        {
            get { return "Attach custom key bindings to buttons."; }
        }

        public void OnSettingsUI(UIHelperBase helper)
        {
            try
            {
                UIHelper group = helper.AddGroup(Name) as UIHelper;

                UICheckBox checkBox = (UICheckBox)group.AddCheckbox("Disable button capture (Alt key)", MoreShortcuts.disableCapture.value, (b) =>
                {
                    MoreShortcuts.disableCapture.value = b;
                });
                checkBox.tooltip = "If checked, you will not be able to add new shortcuts.\nThe Alt key will no longer highlight buttons.\n";

                group.AddSpace(10);

                UIPanel panel = group.self as UIPanel;
                UILabel label = panel.AddUIComponent<UILabel>();
                label.textScale = 1.125f;
                label.text = "Shortcuts:";
                panel.gameObject.AddComponent<OptionsKeymapping>();
            }
            catch (Exception e)
            {
                DebugUtils.Log("OnSettingsUI failed");
                DebugUtils.LogException(e);
            }
        }

        public const string version = "1.0.2";
    }
}
