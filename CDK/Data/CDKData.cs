using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Steamworks;

namespace CDK.Data
{
    public class CDKData
    {
        public string CDK;
        public string Items;
        public string Amount;
        public ushort? Vehicle;
        public ushort? Experience;
        public decimal? Money;
        public int? Reputation;
        public string GrantPermissionGroup;
        public int RedeemedTimes;
        public int? MaxRedeem;
        public DateTime ValidUntil;
        public bool Renew;
        public CSteamID Owner;
        public CDKData(string cdk,string items,string amount,ushort? vehicle,ushort? exp,decimal? money,int? reputation,string permissonGroup,int redeemedTimes,int? maxredeem,DateTime validuntill,CSteamID steamID,bool renew)
        {
            CDK = cdk;
            Items = items;
            Amount = amount;
            Vehicle = vehicle;
            Experience = exp;
            Money = money;
            Reputation = reputation;
            GrantPermissionGroup = permissonGroup;
            RedeemedTimes = redeemedTimes;
            MaxRedeem = maxredeem;
            ValidUntil = validuntill;
            Owner = steamID;
            Renew = renew;
        }

    }
}
