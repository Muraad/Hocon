using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using Microsoft.Framework.Configuration;
using Nulands.HOCON;

namespace Microsoft.Framework.Configuration.Hocon
{
    public class HoconConfigurationSource : ConfigurationSource
    {
        DictionaryReader hoconDictReader = new DictionaryReader(null);

        public Stream InputStream { get; set; }
        public String InputText { get; set; }

        public HoconConfigurationSource()
        {

        }

        public HoconConfigurationSource(Stream inputStream)
        {
            InputStream = inputStream;
        }

        public HoconConfigurationSource(String str)
        {
            InputText = str;
        }

        public override void Load()
        {
            if (InputStream == null && String.IsNullOrEmpty(InputText))
                throw new NullReferenceException("HoconConfigurationSource: InputStream and InputText is null or empty. Unable to load a hocon configuration");

            Data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (InputStream != null)
                Load(InputStream);
            else
                Load(InputText);
        }

        public void Load(Stream stream)
        {
            hoconDictReader.ReadFromStream(stream);
            foreach(var item in hoconDictReader.Source)
            {
                Data[item.Key] = item.Value.ToString();
            }
        }

        public void Load(String str)
        {
            hoconDictReader.ReadFromString(str);
            foreach (var item in hoconDictReader.Source)
            {
                Data[item.Key.Replace(".", Constants.KeyDelimiter)] = item.Value.ToString();
            }
        }
    }
}
