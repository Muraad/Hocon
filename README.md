# Hocon
A Hocon configuration source for aspnet configuration (Microsoft.Framework.Configuration)


The hocon implementation is a fork of https://github.com/4agenda/OGS.HOCON where everything is packed into one 
big file (Hocon.cs).

This repository provides a Hocon IConfigurationSource for https://github.com/aspnet/Configuration


Usage 

```c#

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

```
