using System;
using System.EnterpriseServices;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;

namespace TKGi.CCG.Plugin
{

    [Guid("f919de1a-efc4-4902-b7e5-56a314a87262")]
    [ProgId("CcgCredentialsProviderProvider")]
    public class CcgCredentialsProviderProvider
#if !TEST
        : ServicedComponent, ICcgDomainAuthCredentials
#endif
    {
        private Action<string> log;

        public CcgCredentialsProviderProvider()
        {
            log = entry => { };
        }
        public CcgCredentialsProviderProvider(Func<HttpClient> httpClientFactory, Action<string> log)
        { 
            this.log = log;
        }

        public void GetPasswordCredentials(
            [MarshalAs(UnmanagedType.LPWStr), In] string pluginInput,
            [MarshalAs(UnmanagedType.LPWStr)] out string domainName,
            [MarshalAs(UnmanagedType.LPWStr)] out string username,
            [MarshalAs(UnmanagedType.LPWStr)] out string password)
        {
            var config = ParseInput(pluginInput);

            if (config.LoggingEnabled)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(config.LogFile));
                log = entry => File.AppendAllText(config.LogFile, entry + "\r\n");
            }

            log("Using domainName: " + config.DomainName);
            log("Using username: " + config.UserName);
            log("Using password: " + config.PassWord);

            domainName = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GMSA_DOMAIN_NAME")) 
                         ? Environment.GetEnvironmentVariable("GMSA_DOMAIN_NAME") 
                         : config.DomainName;

            username = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GMSA_USERNAME")) 
                       ? Environment.GetEnvironmentVariable("GMSA_USERNAME") 
                       : config.UserName;

            password = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GMSA_PASSWORD")) 
                       ? Environment.GetEnvironmentVariable("GMSA_PASSWORD") 
                       : config.PassWord;
            
            log("Got Final Domain: " + domainName);
            log("Got Final Username: " + username);
            log("Got Final Password: " + "".PadLeft(password.Length, '*'));
        }

        public Config ParseInput(string input)
        {
            var entries = input.Split(';').ToDictionary(str => str.Split('=')[0].ToUpper(), str => str.Split('=')[1]);

            if (entries.Count() > 4)
            {
                throw new Exception("Invalid configuration");
            }

            var config = new Config
            {
                DomainName = entries.ContainsKey("DOMAINNAME") ? entries["DOMAINNAME"] : throw new Exception("Missing DomainName config"),
                UserName = entries.ContainsKey("USERNAME") ? entries["USERNAME"] : throw new Exception("Missing UserName config"),
                PassWord = entries.ContainsKey("PASSWORD") ? entries["PASSWORD"] : throw new Exception("Missing PassWord config"),
                LogFile = entries.ContainsKey("LOGFILE") ? entries["LOGFILE"] : null
            };

            return config;
        }


        public class Config
        {
            public string DomainName { get; set; }
            public string UserName { get; set; }
            public string PassWord { get; set; }
            public string LogFile { get; set; }
            public bool LoggingEnabled => !string.IsNullOrEmpty(LogFile);
        }
    }
}