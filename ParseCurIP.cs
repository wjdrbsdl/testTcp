using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public class ParseCurIP
{
    public static string GetLocalIP()
    {
        string result = string.Empty;

        var host = Dns.GetHostEntry(Dns.GetHostName());

        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                result = ip.ToString();
                return result;
            }

           
        }
        return null;
    }
}
