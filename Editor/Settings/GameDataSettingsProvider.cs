using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using GameFramework.GameData;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UIElements;

namespace GameFramework.GameData
{
    class GameDataSettingsProvider : SettingsProvider
    {
        [SettingsProvider]
        public static SettingsProvider CreateTimelineProjectSettingProvider()
        {
            var provider = new GameDataSettingsProvider("Project/GameData Editor Settings", SettingsScope.Project);
            return provider;
        }

        SerializedObject m_SerializedObject;
        SerializedProperty m_DataDescFile;
        SerializedProperty m_TableTemplateDirectory;
        SerializedProperty m_TableDirectory;
        SerializedProperty m_GeneratedCodeDirectory;

        public GameDataSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
                : base(path, scopes, keywords) { }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            GameDataEditorSettings.instance.Save();
            m_SerializedObject = GameDataEditorSettings.instance.GetSerializedObject();
            m_DataDescFile = m_SerializedObject.FindProperty("m_DataDescFile");
            m_TableTemplateDirectory = m_SerializedObject.FindProperty("m_TableTemplateDirectory");
            m_TableDirectory = m_SerializedObject.FindProperty("m_TableDirectory");
            m_GeneratedCodeDirectory = m_SerializedObject.FindProperty("m_GeneratedCodeDirectory");
        }

        /* ScriptableSingleton故意设置成DontSaveAndHide
         * 故意不希望我们用PropertyField.而是通过调用Save()保存(不知道为什么)
         */
        public override void OnGUI(string searchContext)
        {
            try
            {
                EditorGUI.indentLevel++;
                m_SerializedObject.Update();

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.Space();

                m_DataDescFile.stringValue = RelativeFilePathFieldLayout("数据描述文件(XML)", m_DataDescFile.stringValue,"xml");
                m_TableTemplateDirectory.stringValue = RelativeFolderPathFieldLayout("表格模板文件夹", m_TableTemplateDirectory.stringValue);
                m_TableDirectory.stringValue = RelativeFolderPathFieldLayout("表格文件夹(csv)", m_TableDirectory.stringValue);
                m_GeneratedCodeDirectory.stringValue = RelativeFolderPathFieldLayout("代码生成文件", m_GeneratedCodeDirectory.stringValue);

                if (EditorGUI.EndChangeCheck())
                {
                    m_SerializedObject.ApplyModifiedProperties();
                    GameDataEditorSettings.instance.Save();
                }
            }
            finally
            {
                EditorGUI.indentLevel--;
            }
        }

        string RelativeFolderPathFieldLayout(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            string path = EditorGUILayout.TextField(label, value);
            if (GUILayout.Button("...", UnityEngine.GUILayout.Width(30)))
            {
                string newPath = EditorUtility.OpenFolderPanel(label, value, "");
                if (newPath.Length != 0)
                {
                    path = PathUitls.MakeRelativePath(Application.dataPath, newPath);
                }

            }
            EditorGUILayout.EndHorizontal();
            return path;
        }

        string RelativeFilePathFieldLayout(string label, string value,string extension)
        {
            EditorGUILayout.BeginHorizontal();
            string path = EditorGUILayout.TextField(label, value);
            if (GUILayout.Button("...", UnityEngine.GUILayout.Width(30)))
            {
                string newPath = EditorUtility.OpenFilePanel(label, value, extension);
                if (newPath.Length != 0)
                {
                    path = PathUitls.MakeRelativePath(Application.dataPath, newPath);
                }

            }
            EditorGUILayout.EndHorizontal();
            return path;
        }
    }
}
