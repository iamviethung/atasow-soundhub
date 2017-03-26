using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Networking;
using Windows.Networking.Connectivity;

namespace HNet
{
    public class HostNameProvider
    {
        static List<ConnectionProfile> hostprofiles;

        public static HostName GetLoopbackHost()
        {
            return new HostName("127.0.0.1");
        }

        public static ulong IPDecompose(string ip)
        {
            var ipStr = ip.Split('.');
            ulong ipLong = 0;

            for (int i = 0; i < 4; i++)
            {
                ipLong <<= 8;
                ipLong |= byte.Parse(ipStr[i]);
            }

            return ipLong;
        }
        public static string IPReconstruct(ulong ip)
        {
            return (byte)(ip >> 24) + "." + (byte)(ip >> 16) + "." + (byte)(ip >> 8) + "." + (byte)ip;
        }

        public async static Task<List<HostName>> GetWirelessHostsAsync()
        {
            return await GetHostWithFilterAsync(
                new ConnectionProfileFilter() { IsConnected = true, IsWlanConnectionProfile = true });
        }
        public async static Task<List<HostName>> GetConnectedHostsAsync()
        {
            return await GetHostWithFilterAsync(
                new ConnectionProfileFilter() { IsConnected = true });
        }

        async static Task<List<HostName>> GetHostWithFilterAsync(ConnectionProfileFilter filter)
        {
            var list = new List<HostName>();
            hostprofiles = new List<ConnectionProfile>();

            var profiles = await NetworkInformation.FindConnectionProfilesAsync(filter);

            var hostnames = NetworkInformation.GetHostNames();

            foreach (var host in hostnames)
            {
                if (host.IPInformation != null && host.IPInformation.NetworkAdapter != null)
                {
                    Guid adapterID = host.IPInformation.NetworkAdapter.NetworkAdapterId;
                    foreach (var profile in profiles)
                        if (adapterID == profile.NetworkAdapter.NetworkAdapterId)
                        {
                            list.Add(host);
                            hostprofiles.Add(profile);
                            break;
                        }
                }

            }

            return list;
        }
        public static List<ConnectionProfile> GetCorrespondingProfiles()
        {
            return hostprofiles;
        }

        public static bool IsIPv4(string ip)
        {
            var quads = ip.Split('.');
            if (!(quads.Length == 4)) return false;
            foreach (var quad in quads)
            {
                int q;
                if (!Int32.TryParse(quad, out q) || !q.ToString().Length.Equals(quad.Length)
                    || q < 0 || q > 255) { return false; }
            }
            return true;
        }
    }
}
