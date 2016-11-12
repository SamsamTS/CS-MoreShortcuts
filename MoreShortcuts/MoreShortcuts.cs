using UnityEngine;

using System;

using ColossalFramework;
using ColossalFramework.UI;

namespace MoreShortcuts
{
    public class MoreShortcuts : MonoBehaviour
    {
        public const string settingsFileName = "MoreShortcuts";

        private UIPanel m_panel;
        private UIComponent m_component;
        private bool m_panelIsModal = false;

        public static SavedBool disableCapture = new SavedBool("disableCapture", MoreShortcuts.settingsFileName, false, true);

        public static MoreShortcuts instance;

        public MoreShortcuts()
        {
            instance = this;
        }

        private void Init()
        {
            if (UIView.GetAView() == null) return;

            m_panel = UIView.GetAView().AddUIComponent(typeof(UIPanel)) as UIPanel;
            m_panel.name = "MoreShortcuts_Highlight";
            m_panel.backgroundSprite = "GenericPanelWhite";
            m_panel.size = new Vector2(10, 10);
            m_panel.color = new Color32(0, 255, 0, 255);
            m_panel.opacity = 0.25f;
            m_panel.isVisible = false;

            m_panel.eventClick += AddShortcut;

            m_panel.tooltipBox = UIView.GetAView().defaultTooltipBox;

            DebugUtils.Log("Initialized");
        }

        private void AddShortcut(UIComponent c, UIMouseEventParameter p)
        {
            if (m_component == null) return;
            DebugUtils.Log("Adding shortcut to " + m_component.name);

            Shortcut shortcut = Shortcut.GetShortcut(m_component);

            if (shortcut == null)
            {
                shortcut = new Shortcut(m_component);
                GUI.UIShortcutModal.instance.title = "New Shortcut";
            }
            else
                GUI.UIShortcutModal.instance.title = "Edit Shortcut";

            HidePanel();

            GUI.UIShortcutModal.instance.shortcut = shortcut;
            UIView.PushModal(GUI.UIShortcutModal.instance);
            GUI.UIShortcutModal.instance.Show();
        }

        public void OnGUI()
        {
            try
            {
                if (m_panel == null)
                {
                    Init();
                    if (m_panel == null) return;
                }

                Event e = Event.current;

                if (!GUI.UIShortcutModal.instance.isVisible &&
                    !(UIView.activeComponent is UITextField && UIView.activeComponent.isEnabled) &&
                    (UIView.activeComponent == null || UIView.activeComponent.name != "Binding"))
                {
                    Shortcut.ParseEvent(e);
                }

                UIComponent hovered = UIInput.hoveredComponent;

                if (disableCapture ||
                    GUI.UIShortcutModal.instance.isVisible ||
                    !e.alt ||
                    hovered == null)
                {
                    HidePanel();
                    return;
                }

                if (m_panel.isVisible && !m_panelIsModal && UIView.ModalInputCount() != 0)
                {
                    UIView.PushModal(m_panel);
                    m_panelIsModal = true;
                }

                if (hovered == m_panel) return;

                UIComponent component = hovered;

                while (component != null)
                {
                    if (component is UIButton || component is UIMultiStateButton || component is UICheckBox)
                    {
                        m_panel.absolutePosition = component.absolutePosition;
                        m_panel.size = component.size;
                        ShowPanel();

                        m_panel.tooltip = "Click to add a shortcut to\n" + component.name;

                        m_component = component;
                        return;
                    }
                    component = component.parent;
                }

                HidePanel();
            }
            catch (Exception e)
            {
                DebugUtils.Log("OnGUI failed");
                DebugUtils.LogException(e);
            }
        }


        private void ShowPanel()
        {
            if(!m_panel.isVisible)
            {
                m_panel.isVisible = true;
                m_panel.BringToFront();
            }
        }

        private void HidePanel()
        {
            if (m_panel.isVisible)
            {
                m_panel.isVisible = false;
                if(m_panelIsModal)
                {
                    UIView.PopModal();
                    m_panelIsModal = false;
                }
            }
        }
    }
}
