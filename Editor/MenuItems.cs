using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using CsvHelper;
using UnityEditor;
using UnityEngine;

namespace GameFramework.GameData
{
    public static class MenuItems
    {
        [UnityEditor.MenuItem("GameData/GenerateCodeFiles(生成代码文件)")]
        public static void GenerateCodeFiles()
        {
            string descPath = Path.Combine(UnityEngine.Application.dataPath, GameDataEditorSettings.DataDescFile);
            var descFile = GameDataSerialization.Deserialize(descPath);

            string rootCodeDirectory = Path.Combine(UnityEngine.Application.dataPath, GameDataEditorSettings.GeneratedCodeDirectory);
            CodeGenerator codeGenerator = new CodeGenerator(descPath);
            foreach (var data in descFile.DataDescList)
            {
                codeGenerator.GenerateDataCodeFile(data, rootCodeDirectory);
            }

            foreach (var table in descFile.TableDescList)
            {
                codeGenerator.GenerateTableCodeFile(table, rootCodeDirectory);
            }

            codeGenerator.GenerateGameDataRuntimeCodeFile(descFile, rootCodeDirectory);

            AssetDatabase.Refresh();
        }

        [UnityEditor.MenuItem("GameData/ClearGeneratedCodeFiles(清空代码文件)")]
        public static void ClearGeneratedCodeFiles()
        {
            string rootCodeDirectory = Path.Combine(UnityEngine.Application.dataPath, GameDataEditorSettings.GeneratedCodeDirectory);
            if (Directory.Exists(rootCodeDirectory))
            {
                Directory.Delete(rootCodeDirectory,true);
                AssetDatabase.Refresh();
            }
        }

        [UnityEditor.MenuItem("GameData/GenerateTableTemplates(生成表格模板文件)")]
        public static void GenerateTableTemplates()
        {
            string descPath = Path.Combine(UnityEngine.Application.dataPath, GameDataEditorSettings.DataDescFile);
            var descFile = GameDataSerialization.Deserialize(descPath);

            string rootTableTemplateDirectory = Path.Combine(UnityEngine.Application.dataPath, GameDataEditorSettings.TableTemplateDirectory);

            TableTemplateGenerator tableTemplateGenerator = new TableTemplateGenerator(descPath);
            foreach (var table in descFile.TableDescList)
            {
                tableTemplateGenerator.GenerateTableTemplateFile(table, rootTableTemplateDirectory);
            }
        }

        //[UnityEditor.MenuItem("Test/Test")]
        //public static void Test()
        //{
        //    var GameDataSettings = AssetDatabase.LoadAssetAtPath<GameDataSettings>("Assets/Settings/GameDataSettings.asset");
        //    string path = Path.Combine(Application.dataPath, GameDataSettings.TableDirectory, "Items/ConsumableItemTable.csv");
        //    using (StreamReader streamReader = new StreamReader(path))
        //    {
        //        using (CsvReader csvReader = new CsvReader(streamReader, GameDataSettings.CSVConfiguration))
        //        {
        //            csvReader.Read();
        //            int row = 0;
        //            while (csvReader.Read())
        //            {
        //                row++;
        //                if (row == 1)//Reader在第二行
        //                {
        //                    csvReader.ReadHeader();
        //                }
        //                else
        //                {
        //                    csvReader.GetRecords<ConsumableItemCfg>
        //                }
        //                Debug.Log($"{csvReader.GetField(0)},{csvReader.GetField(1)},{csvReader.GetField(2)},{csvReader.GetField(3)},{csvReader.GetField(4)}");
        //            }
        //        }
        //    }
        //}
    }

}