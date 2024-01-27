using CsvHelper.Configuration;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace GameFramework.GameData
{
    /*
     * Runtime游戏初始化时需要加载GameDataSettings,否则会出错
     */
    public class GameDataSettings : ScriptableObject
    {
        protected static GameDataSettings m_Instance;

        /*
         * 关于CSV分割符和编码格式的选择
         * 本人使用Excel版本2019
         * 1.分隔符使用了"\t",分隔符如果选择逗号,怎么都会被Excel保存后改成\t,所以就直接自己用了\t
         * 这个问题其实可以用csv开头写入"sep=,"解决，但是会出现另一个问题,只要用了"sep="Excel会用本地设置的语言编码去打开文件(我不想每个用这个package的人都去设置一遍)
         * 2.Encoding使用了UTF16 littleEndian带bom
         * 分隔符\t 和 UTF16 bigEndian (或者UTF8)的组合在excel打开无法识别分隔符,会导致所有东西显示在一列里面（我不知道为啥WPS就是好的）
         * 带bom是因为windows自身设计问题不带bom无法识别编码格式，会显示不了中文(其他操作系统不会这样,如果要跨平台要注意一下这个问题了)
         */
        public static CsvConfiguration CSVConfiguration { get; set; } = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = "\t",
            Encoding = new UnicodeEncoding(false,true)//不带bom的话,Excel打开无法正确显示中文
        };

        /// <summary>
        /// 表格文件夹(csv)
        /// </summary>
        public static string TableDirectory { get { return m_Instance.GetTableDirectory(); } }

        [SerializeField]
        protected string m_RelativeTableDirectory = "";

        protected virtual void OnEnable()
        {
            m_Instance = this;
        }

        protected virtual void OnDisable()
        { 
            m_Instance = null; 
        }

        protected virtual string GetTableDirectory()
        {
            return Path.Combine(Application.dataPath, m_RelativeTableDirectory);
        }
    }
}