using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System;
using System.Reflection;

namespace GameFramework.GameData
{
    [FilePath("ProjectSettings/GameDataEditorSetting.asset", FilePathAttribute.Location.ProjectFolder)]
    public class GameDataEditorSettings : ScriptableSingleton<GameDataEditorSettings>
    {
        /// <summary>
        /// ���ݱ����ļ�
        /// </summary>
        public static string DataDescFile { get { return instance.m_DataDescFile; } }
        /// <summary>
        /// ���ģ���ļ���
        /// </summary>
        public static string TableTemplateDirectory { get { return instance.m_TableTemplateDirectory; } }
        /// <summary>
        /// ���������ļ���
        /// </summary>
        public static string GeneratedCodeDirectory { get { return instance.m_GeneratedCodeDirectory; } }

        /// <summary>
        /// ���ɴ���Ĭ�������ռ�
        /// </summary>
        public static string DefaultNameSpace { get { return instance.m_DefaultNameSpace; } }

        /// <summary>
        /// GameData Runtime Settings Path
        /// </summary>
        public static string RuntimeSettingsPath { get { return instance.m_RuntimeSettingPath; } }

        [SerializeField]
        private string m_DataDescFile = "";
        [SerializeField]
        private string m_TableTemplateDirectory = "";
        [SerializeField]
        private string m_GeneratedCodeDirectory = "";
        [SerializeField]
        private string m_DefaultNameSpace = "";
        [SerializeField]
        private string m_RuntimeSettingPath = "";


        void OnDisable()
        {
            Save();
        }

        public void Save()
        {
            Save(true);
        }

        internal SerializedObject GetSerializedObject()
        {
            return new SerializedObject(this);
        }
    }
}