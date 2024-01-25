using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CsvHelper;
using System.IO;
using System.Globalization;

namespace GameFramework.GameData
{
    public interface ITable
    {
        void Init(string relativeFilePath);

        bool Load();

        void Unload();
    }

    public abstract class TableBase<TKey,TValue>: ITable
    {
        public string FullPath { get { return Path.Combine(GameDataSettings.TableDirectory, RelativeFilePath); } }
        public string RelativeFilePath { get; private set; }
        public bool IsLoaded { get; private set; }

        protected SortedDictionary<TKey, TValue> m_DataDic = new SortedDictionary<TKey, TValue>();

        public void Init(string relativeFilePath)
        {
            RelativeFilePath = relativeFilePath;
        }

        public TValue GetData(TKey key)
        {
            TValue rtn;
            m_DataDic.TryGetValue(key, out rtn);
            return rtn;
        }

        public SortedDictionary<TKey, TValue> GetAllData()
        {
            return m_DataDic;
        }

        public bool Load()
        {
            if (IsLoaded)
                return true;


            using (FileStream fileStream = new FileStream(FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (StreamReader streamReader = new StreamReader(fileStream))
                {
                    using (CsvReader csvReader = new CsvReader(streamReader, GameDataSettings.CSVConfiguration))
                    {

                        csvReader.Read();//进入第一行，是描述
                        csvReader.Read();//进入第二行，真正的表头
                        csvReader.ReadHeader();//读取表头

                        var records = csvReader.GetRecords<TValue>();

                        LoadRecords(records);
                    }
                }
            }
            IsLoaded = true;
            return IsLoaded;
        }

        protected abstract void LoadRecords(IEnumerable<TValue> records);

        public void Unload()
        {
            m_DataDic.Clear();
        }
    }
}
