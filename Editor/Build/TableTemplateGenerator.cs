using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Remoting.Messaging;
using System.Text;
using CsvHelper;

namespace GameFramework.GameData
{
    internal class TableTemplateGenerator 
    {
        private Dictionary<string, DataDescription> nameToDataDesc = new Dictionary<string, DataDescription>();

        public TableTemplateGenerator(string descFilePath)
        {
            var descFile = GameDataSerialization.Deserialize(descFilePath);
            foreach (var dataDesc in descFile.DataDescList)
            {
                if (!nameToDataDesc.ContainsKey(dataDesc.Name))
                {
                    nameToDataDesc[dataDesc.Name] = dataDesc;
                }
                else
                    throw new Exception($"Exist different data with same name:{dataDesc.Name}");
            }
        }

        internal void GenerateTableTemplateFile(TableDescription tableDescription, string rootDirectory)
        {
            string filePath = Path.Combine(rootDirectory, tableDescription.FilePath);
            string directory = Path.GetDirectoryName(filePath);
            if(!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var dataDesc = GetDataDesc(tableDescription);
            using (var writer = new StreamWriter(filePath,false, GameDataSettings.CSVConfiguration.Encoding))
            {
                using (var csv = new CsvWriter(writer, GameDataSettings.CSVConfiguration))
                {
                    //���Ǽ���"sep="��ᵼ��Excel������ȷ�ı����ʽ��csv,��������
                    ////��֪��������ָ�����ʲô����ָExcel��ĳЩ�汾��
                    //csv.WriteField($"sep={GameDataSettings.CSVConfiguration.Delimiter}", true);
                    //csv.NextRecord();
                    //Write �ֶ�����
                    foreach (var variable in dataDesc.Variables)
                    {
                        string comment = !string.IsNullOrEmpty(variable.Comment) ? variable.Comment : variable.Name;
                        csv.WriteField(comment);
                    }
                    csv.NextRecord();
                    //Write �ֶ�����
                    foreach (var variable in dataDesc.Variables)
                    {
                        string name = variable.Name;
                        csv.WriteField(name);
                    }
                }
            }
        }

        DataDescription GetDataDesc(TableDescription tableDescription)
        {
            var dataType = tableDescription.DataType;
            if (nameToDataDesc.TryGetValue(dataType, out var dataDesc))
            {
                return dataDesc;
            }
            else
                throw new Exception($"Table {tableDescription.Name} can not find dataType {dataType}");
        }
    }
}
