using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;

using System.Collections.Generic;
using UnityEngine;

using UIUtils = SamsamTS.UIUtils;

namespace MoreShortcuts
{
    public class OptionsKeymapping : UICustomControl
    {
        private int count = 0;

        private static readonly string kKeyBindingTemplate = "KeyBindingTemplate";
        private static Shortcut m_EditingBinding;
        private static List<UITextComponent> m_components = new List<UITextComponent>();

        public static OptionsKeymapping instance;

        public static bool isCapturing
        {
            get { return m_EditingBinding != null; }
        }

        private void Awake()
        {
            instance = this;
            count = 0;

            foreach (Shortcut shortcut in Shortcut.shortcuts)
            {
                AddKeymapping(shortcut.name, shortcut);
            }
        }

        private void OnDestroy()
        {
            instance = null;
            m_EditingBinding = null;
            m_components.Clear();
        }

        public static void RefreshShortcutsList()
        {
            UIComponent[] components = new UIComponent[instance.component.components.Count];
            instance.component.components.CopyTo(components, 0);

            foreach (UIComponent child in components)
            {
                if (child.name == "ShortcutItem")
                    GameObject.DestroyImmediate(child);
            }

            instance.Awake();
        }

        public static UIButton GetKeymapping(UIComponent parent, Shortcut shortcut)
        {
            UIPanel uIPanel = parent.AttachUIComponent(UITemplateManager.GetAsGameObject(kKeyBindingTemplate)) as UIPanel;

            UIButton uIButton = uIPanel.Find<UIButton>("Binding");

            //uIButton = UnityEngine.Object.Instantiate<GameObject>(uIButton.gameObject).GetComponent<UIButton>();
            m_components.Add(uIButton);
            parent.AttachUIComponent(uIButton.gameObject);

            uIButton.eventKeyDown += new KeyPressHandler(OnBindingKeyDown);
            uIButton.eventMouseDown += new MouseEventHandler(OnBindingMouseDown);

            if (shortcut != null)
                uIButton.text = SavedInputKey.ToLocalizedString("KEYNAME", shortcut.inputKey);
            else
                uIButton.text = Locale.Get("KEYNAME", ((InputKey)0).ToString());
            uIButton.objectUserData = shortcut;

            //GameObject.DestroyImmediate(uIPanel);

            return uIButton;
        }

        private void AddKeymapping(string label, Shortcut shortcut)
        {
            UIPanel uIPanel = component.AttachUIComponent(UITemplateManager.GetAsGameObject(kKeyBindingTemplate)) as UIPanel;
            uIPanel.name = "ShortcutItem";
            if (count++ % 2 == 1) uIPanel.backgroundSprite = null;

            UILabel uILabel = uIPanel.Find<UILabel>("Name");
            UIButton uIButton = uIPanel.Find<UIButton>("Binding");
            uIButton.eventClick += (c, p) =>
            {
                GUI.UIShortcutModal.instance.title = "Edit Shortcut";
                GUI.UIShortcutModal.instance.shortcut = shortcut;

                UIView.PushModal(GUI.UIShortcutModal.instance);
                GUI.UIShortcutModal.instance.Show();

            };

            uILabel.text = label;
            uIButton.text = SavedInputKey.ToLocalizedString("KEYNAME", shortcut.inputKey);
            uIButton.objectUserData = shortcut;
            uIButton.stringUserData = "MoreShortcuts";

            UIButton delete = uIPanel.AddUIComponent<UIButton>();
            delete.atlas = UIUtils.GetAtlas("Ingame");
            delete.normalBgSprite = "buttonclose";
            delete.hoveredBgSprite = "buttonclosehover";
            delete.pressedBgSprite = "buttonclosepressed";
            delete.tooltip = "Delete Shortcut";
            delete.eventClick += (c, p) =>
            {
                ConfirmPanel.ShowModal("Delete Shortcut", "Are you sure you want to delete the [" + shortcut.name + "] shortcut?", (c2, ret) =>
                {
                    if (ret == 1)
                    {
                        Shortcut.RemoveShortcut(shortcut);
                        Shortcut.SaveShorcuts();
                        RefreshShortcutsList();
                    }
                });
            };

            delete.relativePosition = uIButton.relativePosition + new Vector3(uIButton.width + 10, 0);
        }

        public static void EditBinding(UIButton binding)
        {
            m_EditingBinding = (Shortcut)binding.objectUserData;
            binding.buttonsMask = (UIMouseButton.Left | UIMouseButton.Right | UIMouseButton.Middle | UIMouseButton.Special0 | UIMouseButton.Special1 | UIMouseButton.Special2 | UIMouseButton.Special3);
            binding.text = "Press any key";
            binding.Focus();
            UIView.PushModal(binding);
        }

        private void OnEnable()
        {
            LocaleManager.eventLocaleChanged += new LocaleManager.LocaleChangedHandler(OnLocaleChanged);
        }

        private void OnDisable()
        {
            LocaleManager.eventLocaleChanged -= new LocaleManager.LocaleChangedHandler(OnLocaleChanged);
        }

        private void OnLocaleChanged()
        {
            RefreshBindableInputs();
        }

        private static bool IsModifierKey(KeyCode code)
        {
            return code == KeyCode.LeftControl || code == KeyCode.RightControl || code == KeyCode.LeftShift || code == KeyCode.RightShift || code == KeyCode.LeftAlt || code == KeyCode.RightAlt;
        }

        private static bool IsControlDown()
        {
            return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        }

        private static bool IsShiftDown()
        {
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        }

        private static bool IsAltDown()
        {
            return Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        }

        private static bool IsUnbindableMouseButton(UIMouseButton code)
        {
            return code == UIMouseButton.Left || code == UIMouseButton.Right;
        }

        private static bool IsAlreadyBound(Shortcut target, InputKey inputKey, out List<Shortcut> currentAssigned)
        {
            currentAssigned = new List<Shortcut>();
            if (inputKey == 0) return false;

            foreach (Shortcut shortcut in Shortcut.shortcuts)
            {
                if (shortcut.name != target.name && shortcut.inputKey == inputKey)
                    currentAssigned.Add(shortcut);
            }
            return currentAssigned.Count > 0;
        }

        private static KeyCode ButtonToKeycode(UIMouseButton button)
        {
            if (button == UIMouseButton.Left)
            {
                return KeyCode.Mouse0;
            }
            if (button == UIMouseButton.Right)
            {
                return KeyCode.Mouse1;
            }
            if (button == UIMouseButton.Middle)
            {
                return KeyCode.Mouse2;
            }
            if (button == UIMouseButton.Special0)
            {
                return KeyCode.Mouse3;
            }
            if (button == UIMouseButton.Special1)
            {
                return KeyCode.Mouse4;
            }
            if (button == UIMouseButton.Special2)
            {
                return KeyCode.Mouse5;
            }
            if (button == UIMouseButton.Special3)
            {
                return KeyCode.Mouse6;
            }
            return KeyCode.None;
        }

        private static void OnBindingKeyDown(UIComponent comp, UIKeyEventParameter p)
        {
            if (m_EditingBinding != null && !IsModifierKey(p.keycode))
            {
                p.Use();
                UIView.PopModal();
                KeyCode keycode = p.keycode;
                InputKey inputKey = (p.keycode == KeyCode.Escape) ? (InputKey)m_EditingBinding.inputKey : SavedInputKey.Encode(keycode, p.control, p.shift, p.alt);
                if (p.keycode == KeyCode.Backspace)
                {
                    inputKey = 0;
                }
                List<Shortcut> currentAssigned;
                if (!IsAlreadyBound(m_EditingBinding, inputKey, out currentAssigned))
                {
                    if (m_EditingBinding.inputKey != inputKey)
                    {
                        m_EditingBinding.inputKey = inputKey;
                        Shortcut.SaveShorcuts();
                    }

                    UITextComponent uITextComponent = p.source as UITextComponent;
                    uITextComponent.text = SavedInputKey.ToLocalizedString("KEYNAME", inputKey);
                    m_EditingBinding = null;
                }
                else
                {
                    string arg = (currentAssigned.Count <= 1) ? currentAssigned[0].name : Locale.Get("KEYMAPPING_MULTIPLE");
                    string message = string.Format(Locale.Get("CONFIRM_REBINDKEY", "Message"), SavedInputKey.ToLocalizedString("KEYNAME", inputKey), arg);
                    ConfirmPanel.ShowModal(Locale.Get("CONFIRM_REBINDKEY", "Title"), message, delegate(UIComponent c, int ret)
                    {
                        if (ret == 1)
                        {
                            for (int i = 0; i < currentAssigned.Count; i++)
                            {
                                currentAssigned[i].inputKey = 0;
                            }
                        }

                        m_EditingBinding.inputKey = inputKey;
                        Shortcut.SaveShorcuts();
                        RefreshKeyMapping();

                        UITextComponent uITextComponent = p.source as UITextComponent;
                        uITextComponent.text = SavedInputKey.ToLocalizedString("KEYNAME", m_EditingBinding.inputKey);
                        m_EditingBinding = null;
                    });
                }
            }
        }

        private static void OnBindingMouseDown(UIComponent comp, UIMouseEventParameter p)
        {
            if (m_EditingBinding == null)
            {
                p.Use();
                m_EditingBinding = (Shortcut)p.source.objectUserData;
                UIButton uIButton = p.source as UIButton;
                uIButton.buttonsMask = (UIMouseButton.Left | UIMouseButton.Right | UIMouseButton.Middle | UIMouseButton.Special0 | UIMouseButton.Special1 | UIMouseButton.Special2 | UIMouseButton.Special3);
                uIButton.text = "Press any key";
                p.source.Focus();
                UIView.PushModal(p.source);
            }
            else if (!IsUnbindableMouseButton(p.buttons))
            {
                p.Use();
                UIView.PopModal();
                InputKey inputKey = SavedInputKey.Encode(ButtonToKeycode(p.buttons), IsControlDown(), IsShiftDown(), IsAltDown());
                List<Shortcut> currentAssigned;
                if (!IsAlreadyBound(m_EditingBinding, inputKey, out currentAssigned))
                {
                    if (m_EditingBinding.inputKey != inputKey)
                    {
                        m_EditingBinding.inputKey = inputKey;
                        Shortcut.SaveShorcuts();
                    }

                    UIButton uIButton = p.source as UIButton;
                    uIButton.text = SavedInputKey.ToLocalizedString("KEYNAME", m_EditingBinding.inputKey);
                    uIButton.buttonsMask = UIMouseButton.Left;
                    m_EditingBinding = null;
                }
                else
                {
                    string arg = (currentAssigned.Count <= 1) ? Locale.Get("KEYMAPPING", currentAssigned[0].name) : Locale.Get("KEYMAPPING_MULTIPLE");
                    string message = string.Format(Locale.Get("CONFIRM_REBINDKEY", "Message"), SavedInputKey.ToLocalizedString("KEYNAME", inputKey), arg);
                    ConfirmPanel.ShowModal(Locale.Get("CONFIRM_REBINDKEY", "Title"), message, delegate(UIComponent c, int ret)
                    {
                        if (ret == 1)
                        {
                            m_EditingBinding.inputKey = inputKey;

                            for (int i = 0; i < currentAssigned.Count; i++)
                            {
                                currentAssigned[i].inputKey = 0;
                            }
                            Shortcut.SaveShorcuts();
                            RefreshKeyMapping();
                        }
                        UIButton uIButton = p.source as UIButton;
                        uIButton.text = SavedInputKey.ToLocalizedString("KEYNAME", m_EditingBinding.inputKey);
                        uIButton.buttonsMask = UIMouseButton.Left;
                        m_EditingBinding = null;
                    });
                }
            }
        }

        private void RefreshBindableInputs()
        {
            foreach (UIComponent current in component.GetComponentsInChildren<UIComponent>())
            {
                UITextComponent uITextComponent = current.Find<UITextComponent>("Binding");
                if (uITextComponent != null)
                {
                    Shortcut shortcut = uITextComponent.objectUserData as Shortcut;
                    if (shortcut != null)
                    {
                        uITextComponent.text = SavedInputKey.ToLocalizedString("KEYNAME", shortcut.inputKey);
                    }
                }
                UILabel uILabel = current.Find<UILabel>("Name");
                if (uILabel != null)
                {
                    uILabel.text = Locale.Get("KEYMAPPING", uILabel.stringUserData);
                }
            }
        }

        private static void RefreshKeyMapping()
        {
            foreach (UITextComponent current in m_components)
            {
                Shortcut shortcut = (Shortcut)current.objectUserData;
                if (m_EditingBinding != shortcut)
                {
                    current.text = SavedInputKey.ToLocalizedString("KEYNAME", shortcut.inputKey);
                }
            }
        }
    }
}