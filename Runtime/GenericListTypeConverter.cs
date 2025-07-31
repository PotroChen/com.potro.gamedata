using CsvHelper;
using CsvHelper.Configuration;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace GameFramework.GameData
{
    public class GenericListTypeConverter<T> : CsvHelper.TypeConversion.TypeConverter
    {
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            if (string.IsNullOrEmpty(text)) return new List<T>();
            string[] allElements = text.Split(',');
            var converter = TypeDescriptor.GetConverter(typeof(T));
            var elements = allElements.Select(s => (T)converter.ConvertFromString(s)).ToArray();
            return new List<T>(elements);
        }

        public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
        {
            return string.Join(",", ((List<T>)value).Select(x => x?.ToString()));
        }
    }

}