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
        public string CDK { get; internal set; }
        public CSteamID SteamID { get; internal set; }
        public DateTime RedeemTime { get; internal set; }
        public DateTime ValidUntil { get; internal set; }
        public string GrantPermissionGroup { get; internal set; }

        public LogData(string cdk,CSteamID steamID,DateTime redeemtime,DateTime validtime,string PermissionGroup)
        {
            CDK = cdk;
            SteamID = steamID;
            RedeemTime = redeemtime;
            ValidUntil = validtime;
            GrantPermissionGroup = PermissionGroup;
        }
    }
}
