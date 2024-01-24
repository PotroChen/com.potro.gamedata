using System.Collections.Generic;
using System.IO;
using System.CodeDom;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Text;
using System;
using UnityEditor;
using UnityEditorInternal;
using System.Linq;
using UnityEngine;

namespace GameFramework.GameData
{
    internal class CodeGenerator
    {
        private Dictionary<string,CodeTypeReference> typeMapping = new Dictionary<string,CodeTypeReference>();
        private Dictionary<string,DataDescription> nameToDataDesc = new Dictionary<string,DataDescription>();

        public CodeGenerator(string descFilePath) 
        {
            CodeTypeReference stringType = new CodeTypeReference(typeof(string));
            typeMapping["string"] = stringType;

            CodeTypeReference uShortIntType = new CodeTypeReference(typeof(ushort));
            typeMapping["uint16"] = uShortIntType;

            CodeTypeReference uIntType = new CodeTypeReference(typeof(uint));
            typeMapping["uint32"] = uIntType;

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

        #region GenerateTableCodeFile

        internal void GenerateTableCodeFile(TableDescription tableDescription, string rootDirectory)
        {
            string directoryPath = Path.Combine(rootDirectory, tableDescription.CodeDirectory);
            string filePath = Path.Combine(directoryPath, $"{tableDescription.Name}.cs");

            CodeNamespace ns = new CodeNamespace(GameDataEditorSettings.DefaultNameSpace);
            HashSet<string> usingNamespaces = null;
            CodeTypeDeclaration table = TableDescToCodeTypeDeclaration(tableDescription,ref usingNamespaces);
            ns.Types.Add(table);

            CodeCompileUnit compileUnit = new CodeCompileUnit();
            //设置所属Namespace
            compileUnit.Namespaces.Add(ns);
            //添加Using Namespace
            CodeDomUtils.AddUsingNameSapce(compileUnit, usingNamespaces);
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            GenerateCSharpCode(compileUnit, filePath);
        }

        //Table类
        internal CodeTypeDeclaration TableDescToCodeTypeDeclaration(TableDescription tableDescription,ref HashSet<string> usingNamespaces)
        {
            CodeTypeDeclaration tableClass = new CodeTypeDeclaration(tableDescription.Name);
            var keyType = GetKeyType(tableDescription);
            var dataDesc = GetDataDesc(tableDescription);
            //记录需要添加的Using Namespace
            if (usingNamespaces == null)
                usingNamespaces = new HashSet<string>();

            if (!usingNamespaces.Contains("GameFramework.GameData"))
                usingNamespaces.Add("GameFramework.GameData");

            if (!usingNamespaces.Contains("System.Collections.Generic"))
                usingNamespaces.Add("System.Collections.Generic");

            if(!usingNamespaces.Contains(dataDesc.GetNamespace()))
                usingNamespaces.Add(dataDesc.GetNamespace());
            //添加泛型基类
            tableClass.BaseTypes.Add(new CodeTypeReference("TableBase", new CodeTypeReference[] { keyType, new CodeTypeReference(dataDesc.Name) }));

            /* 这部分可以放到基类里面
            //添加key2Data Dictionary
            CodeMemberField dataDic = new CodeMemberField();
            dataDic.Name = "m_DataDic";
            dataDic.Attributes = MemberAttributes.FamilyAndAssembly;//访问等级为Internals
            dataDic.Type = new CodeTypeReference("SortedDictionary", new CodeTypeReference[] { keyType, new CodeTypeReference(dataDesc.Name) });
            tableClass.Members.Add(dataDic);

            //添加GetData方法
            CodeMemberMethod getDataMethod = new CodeMemberMethod();
            getDataMethod.Name = $"Get{dataDesc.Name}";
            getDataMethod.Attributes = MemberAttributes.Public| MemberAttributes.Final;
            getDataMethod.ReturnType = new CodeTypeReference(dataDesc.Name);
            getDataMethod.Parameters.Add(new CodeParameterDeclarationExpression(keyType, tableDescription.Key));
            string intendedString = "            ";
            getDataMethod.Statements.Add(new CodeSnippetStatement(
                $"{intendedString}{dataDesc.Name} rtn;\n" +
                $"{intendedString}m_DataDic.TryGetValue({tableDescription.Key},out rtn);\n" +
                $"{intendedString}return rtn;"));
            tableClass.Members.Add(getDataMethod);

            //添加GetAllData方法
            CodeMemberMethod getAllDataMethod = new CodeMemberMethod();
            getAllDataMethod.Name = $"GetAll{dataDesc.Name}s";
            getAllDataMethod.Attributes = MemberAttributes.Public | MemberAttributes.Final;
            getAllDataMethod.ReturnType = dataDic.Type;
            getAllDataMethod.Statements.Add(new CodeSnippetStatement(
                $"{intendedString}return m_DataDic;"));
            tableClass.Members.Add(getAllDataMethod);
            */

            string intendedString = "            ";
            //添加基类抽象方法LoadRecords
            CodeMemberMethod loadRecords = new CodeMemberMethod();
            loadRecords.Name = "LoadRecords";
            loadRecords.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("IEnumerable", new CodeTypeReference[] { new CodeTypeReference(dataDesc.Name) }),"records"));
            loadRecords.Attributes = MemberAttributes.Override | MemberAttributes.Family;
            loadRecords.Statements.Add(new CodeSnippetStatement(
                $"{intendedString}foreach (var record in records)\n" +
                $"{intendedString}{{\n" +
                $"{intendedString}    m_DataDic[record.{tableDescription.Key}] = record;\n" +
                $"{intendedString}}}"));
            tableClass.Members.Add(loadRecords);

            return tableClass;
        }

        CodeTypeReference GetKeyType(TableDescription tableDescription)
        {
            var dataType = tableDescription.DataType;
            var dataDesc = GetDataDesc(tableDescription);

            foreach (var variable in dataDesc.Variables)
            {
                if (variable.Name == tableDescription.Key)
                {
                    return GetCodeType(variable.Type);
                }
            }
            throw new Exception($"Table {tableDescription.Name} can not find key {tableDescription.Key}");
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
        #endregion

        #region GenerateDataCodeFile
        internal void GenerateDataCodeFile(DataDescription dataDescription,string rootDirectory)
        {
            string directoryPath = Path.Combine(rootDirectory, dataDescription.CodeDirectory);
            string filePath = Path.Combine(directoryPath,$"{dataDescription.Name}.cs");

            CodeNamespace ns = new CodeNamespace(GameDataEditorSettings.DefaultNameSpace);
            CodeTypeDeclaration dataClass = DataDescToCodeTypeDeclaration(dataDescription);
            ns.Types.Add(dataClass);

            CodeCompileUnit compileUnit = new CodeCompileUnit();
            compileUnit.Namespaces.Add(ns);

            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            GenerateCSharpCode(compileUnit, filePath);
        }

        //Data类声明
        internal CodeTypeDeclaration DataDescToCodeTypeDeclaration(DataDescription dataDescription)
        {
            CodeTypeDeclaration dataClass = new CodeTypeDeclaration(dataDescription.Name);
            if (dataDescription.Variables != null && dataDescription.Variables.Count > 0)
            {
                //Data字段声明
                foreach (var variable in dataDescription.Variables)
                {
                    var fieldType = GetCodeType(variable.Type);
                    string fieldName = $"m_{variable.Name}";
                    CodeMemberField field = new CodeMemberField(fieldType, fieldName);
                    field.Attributes = MemberAttributes.FamilyAndAssembly;
                    dataClass.Members.Add(field);

                    CodeMemberProperty property = new CodeMemberProperty();
                    property.Name = variable.Name;
                    property.Attributes = MemberAttributes.Public | MemberAttributes.Final;
                    property.Type = fieldType;
                    //Get
                    property.GetStatements.Add(new CodeMethodReturnStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldName)));
                    //Set
                    property.SetStatements.Add(
                    new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldName),new CodePropertySetValueReferenceExpression()));

                    property.Comments.Add(Comment(variable.Comment));
                    dataClass.Members.Add(property);
                }
            }

            return dataClass;
        }
        #endregion


        public void GenerateCSharpCode(CodeCompileUnit compileunit,string filePath)
        {
            CSharpCodeProvider provider = new CSharpCodeProvider();
            using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                //会有auto-generated的注释，决定先留下(后续如果想要去掉,去掉TextWriter的前几行)
                //用4个空格作为Tab
                IndentedTextWriter tw = new IndentedTextWriter(sw, "    ");
                
                CodeGeneratorOptions options = new CodeGeneratorOptions();
                options.BracingStyle = "C";//左花括号换行
                provider.GenerateCodeFromCompileUnit(compileunit, tw,
                    options);
                tw.Close();
            }
        }

        #region Comdom Utils
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

        private CodeTypeReference GetCodeType(string typeName)
        {
            if (typeMapping.TryGetValue(typeName, out var type))
            {
                return type;
            }
            else
            {
                throw new Exception($"不支持variable type,{typeName}");
            }
        }


        #endregion
    }
}
