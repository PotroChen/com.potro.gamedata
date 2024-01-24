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
        SerializedProperty m_GeneratedCodeDirectory;
        SerializedProperty m_DefaultNameSpace;
        SerializedProperty m_RuntimeSettingPath;

        GameDataSettings m_RuntimeSettings;
        SerializedObject m_RuntimeSettingsSO;
        SerializedProperty m_RelativeTableDirectory;

        public GameDataSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
                : base(path, scopes, keywords) { }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            GameDataEditorSettings.instance.Save();
            m_SerializedObject = GameDataEditorSettings.instance.GetSerializedObject();
            m_DataDescFile = m_SerializedObject.FindProperty("m_DataDescFile");
            m_TableTemplateDirectory = m_SerializedObject.FindProperty("m_TableTemplateDirectory");
            m_GeneratedCodeDirectory = m_SerializedObject.FindProperty("m_GeneratedCodeDirectory");
            m_DefaultNameSpace = m_SerializedObject.FindProperty("m_DefaultNameSpace");
            m_RuntimeSettingPath = m_SerializedObject.FindProperty("m_RuntimeSettingPath");

            TryGetRuntimeSettings(m_RuntimeSettingPath.stringValue);
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

                EditorGUILayout.LabelField("Editor Settings", EditorStyles.boldLabel);
                m_DataDescFile.stringValue = RelativeFilePathFieldLayout("数据描述文件(XML)", m_DataDescFile.stringValue,"xml");
                m_TableTemplateDirectory.stringValue = RelativeFolderPathFieldLayout("表格模板文件夹", m_TableTemplateDirectory.stringValue);
                m_GeneratedCodeDirectory.stringValue = RelativeFolderPathFieldLayout("代码生成文件夹", m_GeneratedCodeDirectory.stringValue);
                m_DefaultNameSpace.stringValue = EditorGUILayout.TextField("生成代码默认命名空间", m_DefaultNameSpace.stringValue);
                var newSettingsPath = RelativeFilePathFieldLayout("Runtime Settings文件", m_RuntimeSettingPath.stringValue, "asset");
                if (m_RuntimeSettingPath.stringValue != newSettingsPath)
                {
                    m_RuntimeSettingPath.stringValue = newSettingsPath;
                    //加载Setitngs
                    TryGetRuntimeSettings(m_RuntimeSettingPath.stringValue);
                }
                if (EditorGUI.EndChangeCheck())
                {
                    m_SerializedObject.ApplyModifiedProperties();
                    GameDataEditorSettings.instance.Save();
                }

                if (m_RuntimeSettings == null)
                {
                    EditorGUILayout.HelpBox("Runtime Settings文件无法找到,请在该路径创建或者换路径", MessageType.Error);
                }
                else 
                {
                    EditorGUILayout.LabelField("Runtime Settings", EditorStyles.boldLabel);
                    EditorGUI.BeginChangeCheck();

                    m_RelativeTableDirectory.stringValue = RelativeFolderPathFieldLayout("表格文件夹(csv)", m_RelativeTableDirectory.stringValue);

                    if (EditorGUI.EndChangeCheck())
                    {
                        m_RuntimeSettingsSO.ApplyModifiedProperties();
                    }
                }
            }
            finally
            {
                EditorGUI.indentLevel--;
            }
        }

        void TryGetRuntimeSettings(string relativePath)
        {
            m_RuntimeSettings = AssetDatabase.LoadAssetAtPath<GameDataSettings>(Path.Combine("Assets", relativePath));
            if (m_RuntimeSettings != null)
            {
                m_RuntimeSettingsSO = new SerializedObject(m_RuntimeSettings);
                m_RelativeTableDirectory = m_RuntimeSettingsSO.FindProperty("m_RelativeTableDirectory");
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
