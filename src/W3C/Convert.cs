using Nethereum.Util;
using System.Numerics;
using Nethereum.Hex.HexConvertors;
using Nethereum.Hex.HexConvertors.Extensions;
using System.Text;

namespace W3C
{
    public static class ConvertW3C
    {
        public static BigInteger ToWeiFromUnit(string token)
        {
            var weiNum = Nethereum.Util.UnitConversion.Convert.ToWei(token);
            return weiNum;
        }

        public static BigDecimal FromWeiToBigDecimal(BigInteger wei)
        {
            var tokenNum = Nethereum.Util.UnitConversion.Convert.FromWei(wei);
            return tokenNum;
        }

        //public static string ConvertToHex(BigInteger newValue)
        //{
        //    return newValue.ToHex(false);
        //}

        public static BigInteger ConvertFromHex(string hex)
        {
            return hex.HexToBigInteger(false);
        }
    }
}
