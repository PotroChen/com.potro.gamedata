using Microsoft.CSharp;
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace GameFramework.GameData
{
    internal class CodeGenerator
    {
        const string GameDataRuntimeClassName = "GameDataRuntime";
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
            typeMapping["uint"] = uIntType;

            CodeTypeReference singleType = new CodeTypeReference(typeof(Single));
            typeMapping["single"] = singleType;
            typeMapping["float"] = singleType;

            CodeTypeReference doubleType = new CodeTypeReference(typeof(Double));
            typeMapping["double"] = doubleType;

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

        internal void GenerateGameDataRuntimeCodeFile(DescriptionFile descriptionFile, string rootDirectory)
        {
            string filePath = Path.Combine(rootDirectory,$"{GameDataRuntimeClassName}.cs");
            
            CodeNamespace ns = new CodeNamespace(GameDataEditorSettings.DefaultNameSpace);
            HashSet<string> usingNamespaces = null;
            CodeTypeDeclaration descFile = DescriptionFileToCodeTypeDeclaration(descriptionFile, ref usingNamespaces);
            ns.Types.Add(descFile);

            CodeCompileUnit compileUnit = new CodeCompileUnit();
            //设置所属Namespace
            compileUnit.Namespaces.Add(ns);
            //添加Using Namespace
            CodeDomUtils.AddUsingNameSapce(compileUnit, usingNamespaces);
            if (!Directory.Exists(rootDirectory))
                Directory.CreateDirectory(rootDirectory);

            GenerateCSharpCode(compileUnit, filePath);
        }

        internal CodeTypeDeclaration DescriptionFileToCodeTypeDeclaration(DescriptionFile descriptionFile, ref HashSet<string> usingNamespaces)
        {
            string tabString = "    ";
            CodeTypeDeclaration descFileClass = new CodeTypeDeclaration(GameDataRuntimeClassName);
            descFileClass.BaseTypes.Add(new CodeTypeReference("GameDataRuntimeBase"));
            //记录需要添加的Using Namespace
            if (usingNamespaces == null)
                usingNamespaces = new HashSet<string>();

            if (!usingNamespaces.Contains("GameFramework.GameData"))
                usingNamespaces.Add("GameFramework.GameData");

            CodeMemberField instanceField = new CodeMemberField();
            instanceField.Type = new CodeTypeReference(GameDataRuntimeClassName);
            instanceField.Name = "m_Instance";
            instanceField.Attributes = MemberAttributes.Private | MemberAttributes.Static;
            descFileClass.Members.Add(instanceField);
            CodeMemberProperty instanceProperty = new CodeMemberProperty();
            instanceProperty.Type = new CodeTypeReference(GameDataRuntimeClassName);
            instanceProperty.Name = "Instance";
            instanceProperty.Attributes = MemberAttributes.Public | MemberAttributes.Static;
            instanceProperty.GetStatements.Add(new CodeSnippetExpression(
                $"if(m_Instance == null)\n" +
                $"{tabString}{tabString}{tabString}{tabString}    m_Instance = new GameDataRuntime();\n" +
                $"{tabString}{tabString}{tabString}{tabString}return m_Instance"));
            descFileClass.Members.Add(instanceProperty);

            foreach (var tableDesc in descriptionFile.TableDescList)
            {
                string fieldName = $"m_{tableDesc.Name}";
                //添加Table字段
                CodeMemberField tableField = new CodeMemberField();
                tableField.Type = new CodeTypeReference(tableDesc.Name);
                tableField.Name = fieldName;
                tableField.Attributes = MemberAttributes.Family | MemberAttributes.Final;
                tableField.InitExpression = new CodeObjectCreateExpression(new CodeTypeReference(tableDesc.Name));
                descFileClass.Members.Add(tableField);
                //添加Table属性
                CodeMemberProperty tableProperty = new CodeMemberProperty();
                tableProperty.Type = new CodeTypeReference(tableDesc.Name);
                tableProperty.Name = tableDesc.Name;
                tableProperty.Attributes = MemberAttributes.Public | MemberAttributes.Final;
                tableProperty.GetStatements.Add(new CodeMethodReturnStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldName)));
                descFileClass.Members.Add(tableProperty);
            }

            //添加初始化方法
            CodeMemberMethod initMethod = new CodeMemberMethod();
            initMethod.Attributes = MemberAttributes.Public | MemberAttributes.Final;
            initMethod.Name = "Init";
            initMethod.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(bool)), "loadAtBegin"));
            foreach (var tableDesc in descriptionFile.TableDescList)
            {
                string fieldName = $"m_{tableDesc.Name}";
                initMethod.Statements.Add(new CodeSnippetStatement(
                    $"{tabString}{tabString}{tabString}{fieldName}.Init(\"{tableDesc.FilePath}\");\n" +
                    $"{tabString}{tabString}{tabString}if(loadAtBegin)\n" +
                    $"{tabString}{tabString}{tabString}    LoadTable({fieldName});"));
            }
            descFileClass.Members.Add(initMethod);

            //添加Unload方法
            CodeMemberMethod unloadMethod = new CodeMemberMethod();
            unloadMethod.Attributes = MemberAttributes.Public | MemberAttributes.Final;
            unloadMethod.Name = "Unload";
            foreach (var tableDesc in descriptionFile.TableDescList)
            {
                string fieldName = $"m_{tableDesc.Name}";
                unloadMethod.Statements.Add(new CodeSnippetStatement(
                    $"{tabString}{tabString}{tabString}UnloadTable({fieldName});"));
            }
            descFileClass.Members.Add(unloadMethod);
            return descFileClass;
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
                    if (!TryGetCodeType(variable.Type, out var codeReference))
                    {
                        throw new Exception($"Table:{tableDescription.Name} Variable:{variable.Name},不支持variable type,{variable.Type}");
                    }
                    return codeReference;
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
                    if (!TryGetCodeType(variable.Type, out var fieldType))
                    {
                        throw new Exception($"Data:{dataDescription.Name} Variable:{variable.Name},不支持variable type {variable.Type}");
                    }

                    string fieldName = $"m_{variable.Name}";
                    CodeMemberField field = new CodeMemberField(fieldType, fieldName);
                    field.Attributes = MemberAttributes.FamilyAndAssembly;
                    dataClass.Members.Add(field);

                    CodeMemberProperty property = new CodeMemberProperty();
                    property.Name = variable.Name;
                    property.Attributes = MemberAttributes.Public | MemberAttributes.Final;
                    // 检查是否为 List<T> 类型，如果是则添加 CsvHelper.Configuration.Attributes.TypeConverter 特性
                    if (fieldType.BaseType != null && fieldType.BaseType.StartsWith("System.Collections.Generic.List"))
                    {
                        if (fieldType.TypeArguments.Count == 1)
                        {
                            var elementType = fieldType.TypeArguments[0];
                            var typeConverterAttr = new CodeAttributeDeclaration(
                                "CsvHelper.Configuration.Attributes.TypeConverter",
                                new CodeAttributeArgument(
                                    new CodeTypeOfExpression(
                                        new CodeTypeReference($"GameFramework.GameData.GenericListTypeConverter<{elementType.BaseType}>")
                                    )
                                )
                            );
                            property.CustomAttributes.Add(typeConverterAttr);
                        }
                    }
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
            Encoding encoding = Encoding.UTF8;
            CSharpCodeProvider provider = new CSharpCodeProvider();
            using (StreamWriter sw = new StreamWriter(filePath, false, encoding))
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

            var content = File.ReadAllText(filePath, encoding);
            content = SetLineEndings(content, EditorSettings.lineEndingsForNewScripts);
            File.WriteAllText(filePath, content, encoding);
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

        private bool TryGetCodeType(string typeName, out CodeTypeReference type)
        {
            if (typeMapping.TryGetValue(typeName, out type))
            {
                return true;
            }

            // 处理 list(xxx)，不区分大小写
            var match = Regex.Match(typeName, @"^list\(\s*(.+?)\s*\)$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                string innerTypeName = match.Groups[1].Value;
                if (TryGetCodeType(innerTypeName, out var innerType))
                {
                    type = new CodeTypeReference("System.Collections.Generic.List", innerType);
                    return true;
                }
                return false;
            }

            return false;
        }

        //从源码拷贝出来的
        internal static string SetLineEndings(string content, LineEndingsMode lineEndingsMode)
        {
            const string windowsLineEndings = "\r\n";
            const string unixLineEndings = "\n";

            string preferredLineEndings;

            switch (lineEndingsMode)
            {
                case LineEndingsMode.OSNative:
                    if (Application.platform == RuntimePlatform.WindowsEditor)
                        preferredLineEndings = windowsLineEndings;
                    else
                        preferredLineEndings = unixLineEndings;
                    break;
                case LineEndingsMode.Unix:
                    preferredLineEndings = unixLineEndings;
                    break;
                case LineEndingsMode.Windows:
                    preferredLineEndings = windowsLineEndings;
                    break;
                default:
                    preferredLineEndings = unixLineEndings;
                    break;
            }

            content = Regex.Replace(content, @"\r\n?|\n", preferredLineEndings);

            return content;
        }


        #endregion
    }
}
