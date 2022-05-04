using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Steamworks;

namespace CDK.Data
{
    public class LogData
    {
        public string CDK;
        public CSteamID SteamID;
        public DateTime RedeemTime;
        public DateTime ValidUntil;

        public LogData(string cdk,CSteamID steamID,DateTime redeemtime,DateTime validtime)
        {
            CDK = cdk;
            SteamID = steamID;
            RedeemTime = redeemtime;
            ValidUntil = validtime;
        }
    }
}
