using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using UnityEngine;

namespace GameFramework.GameData
{
    internal static class GameDataSerialization
    {
        public static void Serialize(string filePath, DescriptionFile content)
        {
            string xmlContent = "";
            using (Utf8StringWriter stringWriter = new Utf8StringWriter())
            {
                XmlSerializer serializer = new XmlSerializer(typeof(DescriptionFile));
                XmlSerializerNamespaces xmlNamespaces = new XmlSerializerNamespaces();
                xmlNamespaces.Add("", "");
                serializer.Serialize(stringWriter, content, xmlNamespaces);
                xmlContent = stringWriter.ToString();
            }
            File.WriteAllText(filePath, xmlContent);
        }

        public static DescriptionFile Deserialize(string filePath)
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Open))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(DescriptionFile));
                var descriptionFile = (DescriptionFile)serializer.Deserialize(stream);
                return descriptionFile;
            }
        }

        private class Utf8StringWriter : StringWriter
        {
            public override Encoding Encoding
            {
                get { return Encoding.UTF8; }
            }
        }
    }
}
