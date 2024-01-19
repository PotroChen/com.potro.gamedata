using System.Collections.Generic;
using System.IO;
using System.CodeDom;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Text;
using System;
using UnityEditor;
using UnityEditorInternal;

namespace GameFramework.GameData
{
    internal class CodeGenerator
    {
        private Dictionary<string,CodeTypeReference> typeMapping = new Dictionary<string,CodeTypeReference>();
        public CodeGenerator() 
        {
            CodeTypeReference stringType = new CodeTypeReference(typeof(string));
            typeMapping["string"] = stringType;

            CodeTypeReference uShortIntType = new CodeTypeReference(typeof(ushort));
            typeMapping["uint16"] = uShortIntType;

            CodeTypeReference uIntType = new CodeTypeReference(typeof(uint));
            typeMapping["uint32"] = uIntType;
        }

        [UnityEditor.MenuItem("Test/Test")]
        public static void GenerateDataClass()
        {
            string descPath = Path.Combine(UnityEngine.Application.dataPath,GameDataEditorSettings.DataDescFile);
            var descFile = GameDataSerialization.Deserialize(descPath);

            CodeGenerator codeGenerator = new CodeGenerator();
            foreach (var data in descFile.DataDescList)
            {
                string rootDirectory = Path.Combine(UnityEngine.Application.dataPath, GameDataEditorSettings.GeneratedCodeDirectory);
                codeGenerator.GenerateDataCodeFile(data, rootDirectory);
            }
            AssetDatabase.Refresh();
        }

        internal void GenerateDataCodeFile(DataDescription dataDescription,string rootDirectory)
        {
            string directoryPath = Path.Combine(rootDirectory, dataDescription.CodeDirectory);
            string filePath = Path.Combine(directoryPath,$"{dataDescription.Name}.cs");

            CodeNamespace ns = new CodeNamespace(GameDataEditorSettings.DefaultNameSpace);
            CodeTypeDeclaration dataClass = DataDescToCodeTypeDeclaration(dataDescription);
            ns.Types.Add(dataClass);

            CodeCompileUnit compileUnit = new CodeCompileUnit();
            compileUnit.Namespaces.Add(ns);

            if(!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            GenerateCSharpCode(compileUnit, filePath);
        }

        internal CodeTypeDeclaration DataDescToCodeTypeDeclaration(DataDescription dataDescription)
        {
            CodeTypeDeclaration dataClass = new CodeTypeDeclaration(dataDescription.Name);

            if (dataDescription.Variables != null && dataDescription.Variables.Count > 0)
            {
                foreach (var variable in dataDescription.Variables)
                {
                    if (typeMapping.TryGetValue(variable.Type, out var type))
                    {
                        string fieldName = $"m_{variable.Name}";
                        CodeMemberField field = new CodeMemberField(type, fieldName);
                        field.Attributes = MemberAttributes.FamilyAndAssembly;
                        dataClass.Members.Add(field);

                        CodeMemberProperty property = new CodeMemberProperty();
                        property.Name = variable.Name;
                        property.Attributes = MemberAttributes.Public;
                        property.Type = type;
                        property.GetStatements.Add(new CodeMethodReturnStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldName)));
                        property.Comments.Add(Comment(variable.Comment));
                        dataClass.Members.Add(property);
                    }
                    else
                        throw new Exception($"{dataDescription.Name}生成失败,不支持variable type,{variable.Type}");
                }
            }
            return dataClass;
        }



        public void GenerateCSharpCode(CodeCompileUnit compileunit,string filePath)
        {
            // Generate the code with the C# code provider.
            CSharpCodeProvider provider = new CSharpCodeProvider();

            // Create a TextWriter to a StreamWriter to the output file.
            using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                IndentedTextWriter tw = new IndentedTextWriter(sw, "    ");

                // Generate source code using the code provider.
                provider.GenerateCodeFromCompileUnit(compileunit, tw,
                    new CodeGeneratorOptions());
                tw.Close();
            }
        }


        #region Utils
        /// <summary>
        /// 注释
        /// </summary>
        /// <param name="content"></param>
        public static CodeCommentStatement Comment(string content)
        {
            CodeComment comment = new CodeComment(content,false);
            CodeCommentStatement commentStatement = new CodeCommentStatement(comment);
            return commentStatement;
        }
        #endregion
    }
}
