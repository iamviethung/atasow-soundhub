using HNet;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking;

namespace TEngine
{
    public class AdapterProfile
    {
        public AdapterProfile(HostName host, string name)
        {
            Host = host;
            Name = name;
        }

        public HostName Host { get; private set; }
        public string HostName
        {
            get
            {
                if (Host != null)
                    return Host.CanonicalName;
                else return "";
            }
        }
        public string Name { get; private set; }

        public static async Task<List<AdapterProfile>> GetListOfAdaptersAsync()
        {
            var hostnames = await HostNameProvider.GetConnectedHostsAsync();
            var profiles = HostNameProvider.GetCorrespondingProfiles();

            List<AdapterProfile> adapters = new List<AdapterProfile>();
            for (int i = 0; i < hostnames.Count; i++)
                adapters.Add(new AdapterProfile(hostnames[i], profiles[i].ProfileName));

            return adapters;
        }
    }
}
