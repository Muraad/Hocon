using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.Framework.Configuration;
using Microsoft.Framework.Configuration.Hocon;

using Nulands.HOCON;
using Nulands.HOCON.OGS;
using Nulands.HOCON.OGS.HOCON;

namespace Test
{
    [TestClass]
    public class HoconConfigTest
    {
        [TestMethod]
        public void Hocon_BasicTest()
        {
            HoconConfig config = Hocon.ParseString(@"ssh {
                connection {
                    host : 127.0.0.1
                    port : 22
                }
                status : on
            }");

            Assert.AreEqual("127.0.0.1", config.GetString("ssh.connection.host"));
            Assert.AreEqual(22, config.GetInt("ssh.connection.port"));
            Assert.AreEqual(true, config.GetBool("ssh.status"));
        }


        [TestMethod]
        public void HoconConfig_BasicTest()
        {
            IConfigurationBuilder configBuilder = new ConfigurationBuilder();

            configBuilder.AddHoconFile(
            @"ssh {
                connection {
                    host : 127.0.0.1
                    port : 22
                }
                status : on
            }");

            IConfiguration config = configBuilder.Build();

            Assert.AreEqual("127.0.0.1", config.Get("ssh:connection:host"));
            Assert.AreEqual("22", config.Get("ssh:connection:port"));
            Assert.AreEqual("True", config.Get("ssh:status"));

            IConfiguration subConfig = config.GetConfigurationSection("ssh:connection");
            Assert.IsNotNull(subConfig);
            Assert.AreEqual("127.0.0.1", subConfig.Get("host"));
            Assert.AreEqual("22", subConfig.Get("port"));
        }
    }
}
