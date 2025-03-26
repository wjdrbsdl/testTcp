using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;


public class EndianChanger
{
    public static ushort NetToHost(byte[] _bigEndians)
    {
        return (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(_bigEndians));
    }

    public static byte[] HostToNet(ushort _length)
    {
        byte[] lengthByte = BitConverter.GetBytes((ushort)IPAddress.HostToNetworkOrder((short)_length));
        return lengthByte;
    }

}
