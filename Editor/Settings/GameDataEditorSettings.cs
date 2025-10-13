using UnityEditor;
using UnityEngine;

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
        /// 代码生成文件夹
        /// </summary>
        public static string GeneratedCodeDirectory { get { return instance.m_GeneratedCodeDirectory; } }

        /// <summary>
        /// 生成代码默认命名空间
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