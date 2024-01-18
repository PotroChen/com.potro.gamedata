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
        /// 数据表述文件
        /// </summary>
        public static string DataDescFile { get { return instance.m_DataDescFile; } }
        /// <summary>
        /// 表格模板文件夹
        /// </summary>
        public static string TableTemplateDirectory { get { return instance.m_TableTemplateDirectory; } }
        /// <summary>
        /// 表格文件夹(csv)
        /// </summary>
        public static string TableDirectory { get { return instance.m_TableDirectory; } }
        /// <summary>
        /// 代码生成文件
        /// </summary>
        public static string GeneratedCodeDirectory { get { return instance.m_GeneratedCodeDirectory; } }

        [SerializeField]
        private string m_DataDescFile = "";
        [SerializeField]
        private string m_TableTemplateDirectory = "";
        [SerializeField]
        private string m_TableDirectory = "";
        [SerializeField]
        private string m_GeneratedCodeDirectory = "";


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