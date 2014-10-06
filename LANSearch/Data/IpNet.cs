using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace LANSearch.Data
{
    public class IpNet
    {
        public IpNet(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentNullException("address");
            var splitCidr = address.Split('/');
            if (splitCidr.Length > 2)
                throw new ArgumentException("Invalid address argument", "address");
            IPAddress ip;
            if (!IPAddress.TryParse(splitCidr[0], out ip) || ip.AddressFamily != AddressFamily.InterNetwork)
                throw new ArgumentException("Invalid address argument", "address");
            if (splitCidr.Length == 1)
            {
                NetMask = uint.MaxValue;
                IpStartAddress = IpEndAddress = ip.ToInt();
                return;
            }
            int maskedBits;
            if (!int.TryParse(splitCidr[1], out maskedBits) || maskedBits < 0 || maskedBits > 32)
                throw new ArgumentException("Invalid address argument", "address");
            ParseCidr(ip, maskedBits);
        }

        private void ParseCidr(IPAddress ip, int maskedBits)
        {
            NetMask = maskedBits == 32 ? uint.MaxValue : ~(uint.MaxValue >> maskedBits);
            var maskBytes = BitConverter.GetBytes(NetMask).Reverse().ToArray();

            var ipBytes = ip.GetAddressBytes();

            var startIpBytes = new byte[4];
            var endIpBytes = new byte[4];

            for (int i = 0; i < 4; i++)
            {
                startIpBytes[i] = (byte)(ipBytes[i] & maskBytes[i]);
                endIpBytes[i] = (byte)(ipBytes[i] | ~maskBytes[i]);
            }
            IpStartAddress = startIpBytes[0] << 24 | startIpBytes[1] << 16 | startIpBytes[2] << 8 | startIpBytes[3];
            IpEndAddress = endIpBytes[0] << 24 | endIpBytes[1] << 16 | endIpBytes[2] << 8 | endIpBytes[3];
        }

        public uint NetMask { get; private set; }

        public int IpStartAddress { get; private set; }

        public int IpEndAddress { get; private set; }

        public override string ToString()
        {
            int mask = GetCidrMask();
            var startBytes = BitConverter.GetBytes(IpStartAddress);
            var strIp = string.Format("{0}.{1}.{2}.{3}", startBytes[3], startBytes[2], startBytes[1], startBytes[0]);
            if (mask == 32)
                return strIp;
            return string.Format("{0}/{1}", strIp, mask);
        }

        private int GetCidrMask()
        {
            int masked = 0;
            for (int i = 31; i >= 0; i--)
            {
                if ((NetMask & (1 << i)) == 0)
                    break;
                masked++;
            }
            return masked;
        }

        public bool IsInRange(string value)
        {
            IPAddress ip;
            if (!IPAddress.TryParse(value, out ip) || ip.AddressFamily != AddressFamily.InterNetwork)
                return false;
            return IsInRange(ip);
        }

        public bool IsInRange(IPAddress ip)
        {
            return IsInRange(ip.ToInt());
        }

        public bool IsInRange(int ip)
        {
            return ip >= IpStartAddress && ip <= IpEndAddress;
        }
    }

    public static class IpNetExtensions
    {
        public static int ToInt(this IPAddress ip)
        {
            if (ip.AddressFamily != AddressFamily.InterNetwork)
                throw new ArgumentException("Only IPv4 addresses are allowed", "ip");
            var bytes = ip.GetAddressBytes();
            return bytes[0] << 24 | bytes[1] << 16 | bytes[2] << 8 | bytes[3];
        }
    }
}