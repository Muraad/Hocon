using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Framework.Configuration;
using Microsoft.Framework.Configuration.Internal;
using Microsoft.Framework.Configuration.Hocon;

namespace Microsoft.Framework.Configuration
{
    public static class HoconConfigurationExtension
    {
        public static IConfigurationBuilder AddHoconFile(
            this IConfigurationBuilder configuration,
            String str)
        {
            HoconConfigurationSource hoconConfigSource = new HoconConfigurationSource(str);
            configuration.Add(hoconConfigSource);
            return configuration;
        }

        public static IConfigurationBuilder AddHoconFile(
            this IConfigurationBuilder configuration,
            Stream inputStream)
        {
            HoconConfigurationSource hoconConfigSource = new HoconConfigurationSource(inputStream);
            configuration.Add(hoconConfigSource);
            return configuration;
        }
    }
}
