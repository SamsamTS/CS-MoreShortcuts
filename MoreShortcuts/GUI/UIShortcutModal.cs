using ColossalFramework;
using ColossalFramework.UI;

using UnityEngine;

using UIUtils = SamsamTS.UIUtils;

namespace MoreShortcuts.GUI
{
    public class UIShortcutModal : UIPanel
    {
        private UITitleBar m_title;
        private UITextField m_name;
        private UITextField m_componentName;
        private UIButton m_binding;
        private UICheckBox m_usePath;
        private UICheckBox m_onlyVisible;
        private UIButton m_ok;
        private UIButton m_cancel;

        private static UIShortcutModal _instance;

        public static UIShortcutModal instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = UIView.GetAView().AddUIComponent(typeof(UIShortcutModal)) as UIShortcutModal;
                }
                return _instance;
            }
        }

        public string title;
        public Shortcut shortcut;

        public UIButton binding
        {
            get { return m_binding; }
        }

        public override void Start()
        {
            base.Start();

            atlas = UIUtils.GetAtlas("Ingame");
            backgroundSprite = "MenuPanel";
            color = new Color32(58, 88, 104, 255);
            isVisible = false;
            canFocus = true;
            isInteractive = true;
            clipChildren = true;
            width = 400;
            height = 380;
            relativePosition = new Vector3(Mathf.Floor((GetUIView().fixedWidth - width) / 2), Mathf.Floor((GetUIView().fixedHeight - height) / 2));

            // Title Bar
            m_title = AddUIComponent<UITitleBar>();
            m_title.title = "New Shortcut";
            m_title.isModal = true;

            // Name
            UILabel label = AddUIComponent<UILabel>();
            label.text = "Shortcut name:";
            label.autoHeight = true;
            label.relativePosition = new Vector2(20, 60);

            m_name = UIUtils.CreateTextField(this);
            m_name.size = new Vector2(width - 40, 30);
            m_name.relativePosition = label.relativePosition + new Vector3(0, label.height + 5);
            m_name.textScale = 1.1f;
            m_name.padding.top = 6;
            m_name.useDropShadow = true;

            m_name.eventTextChanged += (c, s) =>
            {
                m_ok.isEnabled = !s.IsNullOrWhiteSpace();
            };

            // Component Name
            label = AddUIComponent<UILabel>();
            label.text = "Button name:";
            label.autoHeight = true;
            label.relativePosition = m_name.relativePosition + new Vector3(0, m_name.height + 15);

            m_componentName = UIUtils.CreateTextField(this);
            m_componentName.size = new Vector2(width - 40, 30);
            m_componentName.relativePosition = label.relativePosition + new Vector3(0, label.height + 5);
            m_componentName.textScale = 1.1f;
            m_componentName.padding.top = 6;
            m_componentName.useDropShadow = true;

            m_componentName.isEnabled = false;

            // Binding
            label = AddUIComponent<UILabel>();
            label.text = "Key binding:";
            label.autoHeight = true;
            label.relativePosition = m_componentName.relativePosition + new Vector3(0, m_componentName.height + 15);

            m_binding = OptionsKeymapping.GetKeymapping(this, null);
            m_binding.width = width - 40;
            m_binding.relativePosition = label.relativePosition + new Vector3(0, label.height + 5);

            // Use Path
            m_usePath = UIUtils.CreateCheckBox(this);
            m_usePath.label.text = "Use button's full path";
            m_usePath.tooltip = "If checked, the full path to the button is used rather than the name alone.\nThis ensure the button is unique.";
            m_usePath.relativePosition = m_binding.relativePosition + new Vector3(0, m_binding.height + 15);

            m_usePath.eventCheckChanged += (c, state) =>
            {
                shortcut.usePath = state;
            };

            // Only visible
            m_onlyVisible = UIUtils.CreateCheckBox(this);
            m_onlyVisible.label.text = "Trigger only if visible";
            m_onlyVisible.tooltip = "If checked, the button is only triggered if visible.\nUseful for buttons with the same name but only one visible at a time.";
            m_onlyVisible.relativePosition = m_usePath.relativePosition + new Vector3(0, m_usePath.height + 10);

            m_onlyVisible.eventCheckChanged += (c, state) =>
            {
                shortcut.onlyVisible = state;
            };

            // Ok
            m_ok = UIUtils.CreateButton(this);
            m_ok.text = "OK";
            m_ok.relativePosition = new Vector2(20, height - m_ok.height - 20);
            m_ok.isEnabled = false;

            m_ok.eventClick += (c, p) =>
            {
                if (isVisible)
                {
                    if (m_name.text != shortcut.name)
                        shortcut.name = Shortcut.GetUniqueName(m_name.text);
                    shortcut.usePath = m_usePath.isChecked;
                    shortcut.onlyVisible = m_onlyVisible.isChecked;
                    shortcut.inputKey = ((Shortcut)m_binding.objectUserData).inputKey;

                    Shortcut.AddShortcut(shortcut);
                    Shortcut.SaveShorcuts();
                    OptionsKeymapping.RefreshShortcutsList();

                    UIView.PopModal();
                    Hide();
                }
            };

            // Cancel
            m_cancel = UIUtils.CreateButton(this);
            m_cancel.text = "Cancel";
            m_cancel.relativePosition = new Vector2(width - m_cancel.width - 20, height - m_cancel.height - 20);

            m_cancel.eventClick += (c, p) =>
            {
                if (isVisible)
                {
                    UIView.PopModal();
                    Hide();
                }
            };

            isVisible = false;
        }

        private void Init()
        {
            if (shortcut == null) return;

            m_title.title = title;
            m_name.text = shortcut.name;
            m_componentName.text = shortcut.component;
            m_componentName.tooltip = string.Join(">", shortcut.path);
            m_binding.objectUserData = new Shortcut(shortcut);
            m_binding.text = SavedInputKey.ToLocalizedString("KEYNAME", shortcut.inputKey);
            m_binding.relativePosition = m_usePath.relativePosition - new Vector3(0, m_binding.height + 15);
            m_usePath.isChecked = shortcut.usePath;
            m_onlyVisible.isChecked = shortcut.onlyVisible;

            OptionsKeymapping.EditBinding(m_binding);
        }

        protected override void OnVisibilityChanged()
        {
            base.OnVisibilityChanged();

            UIComponent modalEffect = GetUIView().panelsLibraryModalEffect;

            if (isVisible)
            {
                Init();

                if (modalEffect != null)
                {
                    modalEffect.Show(true);
                    modalEffect.opacity = 0;
                    BringToFront();
                    ValueAnimator.Animate("NewThemeModalEffect", delegate(float val)
                    {
                        modalEffect.opacity = val;
                    }, new AnimatedFloat(0f, 1f, 0.7f, EasingType.CubicEaseOut));
                }
            }
            else if (modalEffect != null)
            {
                ValueAnimator.Animate("NewThemeModalEffect", delegate(float val)
                {
                    modalEffect.opacity = val;
                }, new AnimatedFloat(1f, 0f, 0.7f, EasingType.CubicEaseOut), delegate
                {
                    modalEffect.Hide();
                });
            }
        }

        protected override void OnKeyDown(UIKeyEventParameter p)
        {
            if (p.used || OptionsKeymapping.isCapturing) return;

            if (p.keycode == KeyCode.Escape)
            {
                p.Use();
                m_cancel.SimulateClick();
            }
            else if (p.keycode == KeyCode.Return)
            {
                p.Use();
                m_ok.SimulateClick();
            }
        }
    }
}
