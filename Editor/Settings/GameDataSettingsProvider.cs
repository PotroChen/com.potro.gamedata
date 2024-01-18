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
            var provider = new GameDataSettingsProvider("Project/GameData", SettingsScope.Project);
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
            GameDataSettings.instance.Save();
            m_SerializedObject = GameDataSettings.instance.GetSerializedObject();
            m_DataDescFile = m_SerializedObject.FindProperty("m_DataDescFile");
            m_TableTemplateDirectory = m_SerializedObject.FindProperty("m_TableTemplateDirectory");
            m_TableDirectory = m_SerializedObject.FindProperty("m_TableDirectory");
            m_GeneratedCodeDirectory = m_SerializedObject.FindProperty("m_GeneratedCodeDirectory");
        }

        /* ScriptableSingleton�������ó�DontSaveAndHide
         * ���ⲻϣ��������PropertyField.����ͨ������Save()����(��֪��Ϊʲô)
         */
        public override void OnGUI(string searchContext)
        {
            try
            {
                EditorGUI.indentLevel++;
                m_SerializedObject.Update();

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.Space();

                m_DataDescFile.stringValue = RelativeFilePathFieldLayout("���������ļ�(XML)", m_DataDescFile.stringValue,"xml");
                m_TableTemplateDirectory.stringValue = RelativeFolderPathFieldLayout("����ģ���ļ���", m_TableTemplateDirectory.stringValue);
                m_TableDirectory.stringValue = RelativeFolderPathFieldLayout("�����ļ���(csv)", m_TableDirectory.stringValue);
                m_GeneratedCodeDirectory.stringValue = RelativeFolderPathFieldLayout("���������ļ�", m_GeneratedCodeDirectory.stringValue);

                if (EditorGUI.EndChangeCheck())
                {
                    m_SerializedObject.ApplyModifiedProperties();
                    GameDataSettings.instance.Save();
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