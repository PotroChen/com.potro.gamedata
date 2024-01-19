using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

namespace GameFramework.GameData
{
    [XmlRoot("root")]
    public struct DescriptionFile
    {
        [XmlElement("data")]
        public List<DataDescription> DataDescList;
        [XmlElement("table")]
        public List<TableDescription> TableDescList;
    }

    public struct DataDescription
    {
        [XmlAttribute("name")]
        public string Name;

        //最初的设想里是有的，但是有感觉不是特别有必要，需要的时候再支持
        //[XmlAttribute("namespace")]
        //public string NameSpace;

        [XmlAttribute("codedirectory")]
        public string CodeDirectory;

        [XmlElement("variable")]
        public List<VariableDescription> Variables;

        public string GetNamespace()
        {
            return GameDataEditorSettings.DefaultNameSpace;
        }
    }

    public struct VariableDescription
    {
        [XmlAttribute("name")]
        public string Name;
        [XmlAttribute("comment")]
        public string Comment;
        [XmlAttribute("type")]
        public string Type;
    }

    public struct TableDescription
    {
        [XmlAttribute("name")]
        public string Name;
        [XmlAttribute("datatype")]
        public string DataType;
        [XmlAttribute("codedirectory")]
        public string CodeDirectory;
        [XmlAttribute("filepath")]
        public string FilePath;
        [XmlAttribute("key")]
        public string Key;
    }


}
