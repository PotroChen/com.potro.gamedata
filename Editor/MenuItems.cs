using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GameFramework.GameData
{
    public static class MenuItems
    {
        [UnityEditor.MenuItem("GameData/GenerateCodeFiles(���ɴ����ļ�)")]
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
            AssetDatabase.Refresh();
        }

        [UnityEditor.MenuItem("GameData/ClearGeneratedCodeFiles(��մ����ļ�)")]
        public static void ClearGeneratedCodeFiles()
        {
            string rootCodeDirectory = Path.Combine(UnityEngine.Application.dataPath, GameDataEditorSettings.GeneratedCodeDirectory);
            if (Directory.Exists(rootCodeDirectory))
            {
                Directory.Delete(rootCodeDirectory,true);
                AssetDatabase.Refresh();
            }
        }

        [UnityEditor.MenuItem("GameData/GenerateTableTemplates(���ɱ��ģ���ļ�)")]
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
    }

}