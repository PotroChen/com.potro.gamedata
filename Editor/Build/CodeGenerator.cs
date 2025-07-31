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
using System.Text.RegularExpressions;
using Mono.Cecil;

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
            //��������Namespace
            compileUnit.Namespaces.Add(ns);
            //���Using Namespace
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
            //��¼��Ҫ��ӵ�Using Namespace
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
                //���Table�ֶ�
                CodeMemberField tableField = new CodeMemberField();
                tableField.Type = new CodeTypeReference(tableDesc.Name);
                tableField.Name = fieldName;
                tableField.Attributes = MemberAttributes.Family | MemberAttributes.Final;
                tableField.InitExpression = new CodeObjectCreateExpression(new CodeTypeReference(tableDesc.Name));
                descFileClass.Members.Add(tableField);
                //���Table����
                CodeMemberProperty tableProperty = new CodeMemberProperty();
                tableProperty.Type = new CodeTypeReference(tableDesc.Name);
                tableProperty.Name = tableDesc.Name;
                tableProperty.Attributes = MemberAttributes.Public | MemberAttributes.Final;
                tableProperty.GetStatements.Add(new CodeMethodReturnStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldName)));
                descFileClass.Members.Add(tableProperty);
            }

            //��ӳ�ʼ������
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

            //���Unload����
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
            //��������Namespace
            compileUnit.Namespaces.Add(ns);
            //���Using Namespace
            CodeDomUtils.AddUsingNameSapce(compileUnit, usingNamespaces);
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            GenerateCSharpCode(compileUnit, filePath);
        }

        //Table��
        internal CodeTypeDeclaration TableDescToCodeTypeDeclaration(TableDescription tableDescription,ref HashSet<string> usingNamespaces)
        {
            CodeTypeDeclaration tableClass = new CodeTypeDeclaration(tableDescription.Name);
            var keyType = GetKeyType(tableDescription);
            var dataDesc = GetDataDesc(tableDescription);
            //��¼��Ҫ��ӵ�Using Namespace
            if (usingNamespaces == null)
                usingNamespaces = new HashSet<string>();

            if (!usingNamespaces.Contains("GameFramework.GameData"))
                usingNamespaces.Add("GameFramework.GameData");

            if (!usingNamespaces.Contains("System.Collections.Generic"))
                usingNamespaces.Add("System.Collections.Generic");

            if(!usingNamespaces.Contains(dataDesc.GetNamespace()))
                usingNamespaces.Add(dataDesc.GetNamespace());
            //��ӷ��ͻ���
            tableClass.BaseTypes.Add(new CodeTypeReference("TableBase", new CodeTypeReference[] { keyType, new CodeTypeReference(dataDesc.Name) }));

            /* �ⲿ�ֿ��Էŵ���������
            //���key2Data Dictionary
            CodeMemberField dataDic = new CodeMemberField();
            dataDic.Name = "m_DataDic";
            dataDic.Attributes = MemberAttributes.FamilyAndAssembly;//���ʵȼ�ΪInternals
            dataDic.Type = new CodeTypeReference("SortedDictionary", new CodeTypeReference[] { keyType, new CodeTypeReference(dataDesc.Name) });
            tableClass.Members.Add(dataDic);

            //���GetData����
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

            //���GetAllData����
            CodeMemberMethod getAllDataMethod = new CodeMemberMethod();
            getAllDataMethod.Name = $"GetAll{dataDesc.Name}s";
            getAllDataMethod.Attributes = MemberAttributes.Public | MemberAttributes.Final;
            getAllDataMethod.ReturnType = dataDic.Type;
            getAllDataMethod.Statements.Add(new CodeSnippetStatement(
                $"{intendedString}return m_DataDic;"));
            tableClass.Members.Add(getAllDataMethod);
            */

            string intendedString = "            ";
            //��ӻ�����󷽷�LoadRecords
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
                        throw new Exception($"Table:{tableDescription.Name} Variable:{variable.Name},��֧��variable type,{variable.Type}");
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

        //Data������
        internal CodeTypeDeclaration DataDescToCodeTypeDeclaration(DataDescription dataDescription)
        {
            CodeTypeDeclaration dataClass = new CodeTypeDeclaration(dataDescription.Name);
            if (dataDescription.Variables != null && dataDescription.Variables.Count > 0)
            {
                //Data�ֶ�����
                foreach (var variable in dataDescription.Variables)
                {
                    if (!TryGetCodeType(variable.Type, out var fieldType))
                    {
                        throw new Exception($"Data:{dataDescription.Name} Variable:{variable.Name},��֧��variable type {variable.Type}");
                    }

                    string fieldName = $"m_{variable.Name}";
                    CodeMemberField field = new CodeMemberField(fieldType, fieldName);
                    field.Attributes = MemberAttributes.FamilyAndAssembly;
                    dataClass.Members.Add(field);

                    CodeMemberProperty property = new CodeMemberProperty();
                    property.Name = variable.Name;
                    property.Attributes = MemberAttributes.Public | MemberAttributes.Final;
                    // ����Ƿ�Ϊ List<T> ���ͣ����������� CsvHelper.Configuration.Attributes.TypeConverter ����
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
                //����auto-generated��ע�ͣ�����������(���������Ҫȥ��,ȥ��TextWriter��ǰ����)
                //��4���ո���ΪTab
                IndentedTextWriter tw = new IndentedTextWriter(sw, "    ");
                CodeGeneratorOptions options = new CodeGeneratorOptions();
                options.BracingStyle = "C";//�����Ż���

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
        /// ע��
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

            // ���� list(xxx)�������ִ�Сд
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

        //��Դ�뿽��������
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
