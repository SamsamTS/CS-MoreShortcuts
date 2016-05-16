using UnityEngine;
using ColossalFramework;
using ColossalFramework.UI;

using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Collections.Generic;
using System.Reflection;
using System.Globalization;

namespace MoreShortcuts
{
    public class Shortcut
    {
        public Shortcut() { }

        public Shortcut(Shortcut shortcut)
        {
            this.name = shortcut.name;
            this.component = shortcut.component;
            this.path = shortcut.path.Clone() as string[];
            this.inputKey = shortcut.inputKey;
            this.usePath = shortcut.usePath;
            this.onlyVisible = shortcut.onlyVisible;
        }

        public Shortcut(UIComponent component)
        {
            UITextComponent textComponent = component as UITextComponent;

            if (textComponent != null && !textComponent.text.IsNullOrWhiteSpace())
                this.name = GetUniqueName(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(textComponent.text.ToLower()));
            else
                this.name = GetUniqueName(component.name);
            this.component = component.name;
            this.path = GetUIComponentPath(component);

            if (component.parent != null)
            {
                foreach (UIComponent child in component.parent.components)
                {
                    if (child != component && child.name == component.name)
                    {
                        this.usePath = true;
                        this.onlyVisible = false;
                        return;
                    }
                }
            }

            UIComponent[] components = GameObject.FindObjectsOfType<UIComponent>();
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] != component && components[i].name == component.name)
                {
                    this.usePath = false;
                    this.onlyVisible = true;
                    return;
                }
            }
        }

        [XmlAttribute]
        public string name;

        [XmlElement("UIComponent")]
        public string component;

        [XmlArray("Path"), XmlArrayItem("Item"), DefaultValue(null)]
        public string[] path = null;

        [DefaultValue(0)]
        public int inputKey = 0;

        [XmlAttribute, DefaultValue(false)]
        public bool usePath = false;

        [XmlAttribute, DefaultValue(false)]
        public bool onlyVisible = false;

        #region Static
        private static SavedString m_savedShortcuts = new SavedString("shortcuts", MoreShortcuts.settingsFileName, "", true);
        private static XmlSerializer m_xmlSerializer = new XmlSerializer(typeof(Shortcut[]), new XmlRootAttribute("Shortcuts"));

        public static List<Shortcut> shortcuts = new List<Shortcut>();

        public static void LoadShorcuts()
        {
            DebugUtils.Log("LoadShorcuts");
            Shortcut[] shortcuts = new Shortcut[0];
            try
            {
                // Creating setting file
                if (!((Dictionary<string, SettingsFile>)typeof(GameSettings).GetField("m_SettingsFiles", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(GameSettings.instance)).ContainsKey(MoreShortcuts.settingsFileName))
                    GameSettings.AddSettingsFile(new SettingsFile[] { new SettingsFile() { fileName = MoreShortcuts.settingsFileName } });

                if (!m_savedShortcuts.value.IsNullOrWhiteSpace())
                {
                    shortcuts = m_xmlSerializer.Deserialize(new StringReader(m_savedShortcuts.value)) as Shortcut[];
                    if (shortcuts != null) DebugUtils.Log("Loaded " + shortcuts.Length + " shortcuts");
                    else shortcuts = new Shortcut[0];
                }
            }
            catch (Exception e)
            {
                DebugUtils.Log("Could load/create the setting file.");
                DebugUtils.LogException(e);
            }

            Shortcut.shortcuts = new List<Shortcut>(shortcuts);
        }

        public static void SaveShorcuts()
        {
            if (shortcuts.Count == 0)
            {
                if (m_savedShortcuts.exists) m_savedShortcuts.Delete();
                return;
            }

            StringWriter str = new StringWriter();
            m_xmlSerializer.Serialize(str, shortcuts.ToArray());

            m_savedShortcuts.value = str.ToString();
        }

        public static void AddShortcut(Shortcut shortcut)
        {
            if (shortcuts.Contains(shortcut)) return;

            shortcut.name = GetUniqueName(shortcut.name);
            shortcuts.Add(shortcut);
        }

        public static string GetUniqueName(string name)
        {
            int count = 0;
            string uniqueName = name;
            while (Shortcut.GetShortcut(uniqueName) != null)
                uniqueName = name + ++count;

            return uniqueName;
        }

        public static void RemoveShortcut(Shortcut shortcut)
        {
            if (!shortcuts.Contains(shortcut)) return;

            shortcuts.Remove(shortcut);
        }

        public static Shortcut GetShortcut(string name)
        {
            foreach (Shortcut shortcut in shortcuts)
            {
                if (shortcut.name == name)
                    return shortcut;
            }

            return null;
        }

        public static Shortcut GetShortcut(UIComponent component)
        {
            foreach (Shortcut shortcut in shortcuts)
            {
                if (shortcut.component == component.name)
                {
                    if (string.Join(">", shortcut.path) == string.Join(">", GetUIComponentPath(component)))
                        return shortcut;
                }
            }

            return null;
        }

        private static string[] GetUIComponentPath(UIComponent component)
        {
            if (component == null) return null;

            List<string> path = new List<string>();

            do
            {
                int count = 0;
                int pos = 0;

                Transform parent = component.transform.parent;
                int childCount = parent.childCount;

                for (int i = 0; i < childCount; i++)
                {
                    Transform child = parent.GetChild(i);
                    if (child.name == component.name)
                    {
                        if (child == component.transform)
                        {
                            pos = count;
                            break;
                        }
                        count++;
                    }
                }

                path.Add(pos + ":" + component.name);

                component = component.parent;
            }
            while (component != null);

            path.Reverse();
            return path.ToArray();
        }

        public static bool IsPressed(InputKey inputKey, Event e)
        {
            if (e.type != EventType.KeyDown) return false;

            KeyCode keyCode = (KeyCode)(inputKey & 0x0FFFFFFF);
            return keyCode != KeyCode.None && e.keyCode == keyCode && (e.modifiers & EventModifiers.Control) != EventModifiers.None == ((inputKey & 0x40000000) != 0) && (e.modifiers & EventModifiers.Shift) != EventModifiers.None == ((inputKey & 0x20000000) != 0) && (e.modifiers & EventModifiers.Alt) != EventModifiers.None == ((inputKey & 0x10000000) != 0);
        }

        public static void ParseEvent(Event e)
        {
            List<Shortcut> toTrigger = new List<Shortcut>();

            foreach (Shortcut shortcut in shortcuts)
            {
                if (Shortcut.IsPressed(shortcut.inputKey, e))
                {
                    toTrigger.Add(shortcut);
                }
            }

            if (toTrigger.Count == 0) return;

            UIComponent[] components = GameObject.FindObjectsOfType<UIComponent>();

            for (int i = 0; i < components.Length; i++)
            {
                foreach (Shortcut shortcut in toTrigger)
                {
                    bool isButton = components[i] is UIButton || components[i] is UIMultiStateButton || components[i] is UICheckBox;
                    if (components[i].name != shortcut.component || !isButton || (shortcut.onlyVisible && !components[i].isVisible)) continue;

                    if (shortcut.usePath && (string.Join(">", GetUIComponentPath(components[i])) != string.Join(">", shortcut.path)))
                        continue;

                    SimulateClick(components[i]);
                    e.Use();
                }
            }
        }

        private static void SimulateClick(UIComponent component)
        {
            Camera camera = component.GetCamera();
            Vector3 vector = camera.WorldToScreenPoint(component.center);
            Ray ray = camera.ScreenPointToRay(vector);
            UIMouseEventParameter p = new UIMouseEventParameter(component, UIMouseButton.Left, 1, ray, vector, Vector2.zero, 0f);

            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            if (component.isEnabled)
            {
                component.GetType().GetMethod("OnMouseDown", flags).Invoke(component, new object[] { p });
                component.GetType().GetMethod("OnClick", flags).Invoke(component, new object[] { p });
                component.GetType().GetMethod("OnMouseUp", flags).Invoke(component, new object[] { p });
            }
            else
                component.GetType().GetMethod("OnDisabledClick", flags).Invoke(component, new object[] { p });

        }
        #endregion
    }
}
