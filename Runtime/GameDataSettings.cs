using CsvHelper.Configuration;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace GameFramework.GameData
{
    /*
     * Runtime��Ϸ��ʼ��ʱ��Ҫ����GameDataSettings,��������
     */
    public class GameDataSettings : ScriptableObject
    {
        protected static GameDataSettings m_Instance;

        /*
         * ����CSV�ָ���ͱ����ʽ��ѡ��
         * ����ʹ��Excel�汾2019
         * 1.�ָ���ʹ����"\t",�ָ������ѡ�񶺺�,��ô���ᱻExcel�����ĳ�\t,���Ծ�ֱ���Լ�����\t
         * ���������ʵ������csv��ͷд��"sep=,"��������ǻ������һ������,ֻҪ����"sep="Excel���ñ������õ����Ա���ȥ���ļ�(�Ҳ���ÿ�������package���˶�ȥ����һ��)
         * 2.Encodingʹ����UTF16 littleEndian��bom
         * �ָ���\t �� UTF16 bigEndian (����UTF8)�������excel���޷�ʶ��ָ���,�ᵼ�����ж�����ʾ��һ�����棨�Ҳ�֪��ΪɶWPS���Ǻõģ�
         * ��bom����Ϊwindows����������ⲻ��bom�޷�ʶ������ʽ������ʾ��������(��������ϵͳ��������,���Ҫ��ƽ̨Ҫע��һ�����������)
         */
        public static CsvConfiguration CSVConfiguration { get; set; } = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = "\t",
            Encoding = new UnicodeEncoding(false,true)//����bom�Ļ�,Excel���޷���ȷ��ʾ����
        };

        /// <summary>
        /// ����ļ���(csv)
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