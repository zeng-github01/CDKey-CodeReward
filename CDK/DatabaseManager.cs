using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Rocket.Core.Logging;
using Rocket.Unturned.Player;
using Rocket.Unturned.Chat;
using Rocket.Core;
using fr34kyn01535.Uconomy;
using Rocket.API;
//using UnityEngine;

namespace CDK
{
    public class DatabaseManager
    {
        public enum RedeemCDKResult {Success,Redeemed,KeyNotFound,MaxRedeemed,Renewed,Error,PlayerNotMatch }

        //public enum CreateCDKResult { Success,Failure,KeyExist,Error}
        internal DatabaseManager()
        {
            CheckSchema();
        }

        //#region CreateCDK
        //public CreateCDKResult CreateCDK(string CDK, ushort Vehicle, uint Experience, decimal Money,int MaxRedem = 1, string Items = "", string GrantPermissionGroup = "")
        //{
        //    try
        //    {

        //        var result = ExecuteQuery(true, $"SELECT 1 FROM `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` WHERE `CDK` ='{CDK}' LIMIT 1;");

        //        if (result != null) return CreateCDKResult.KeyExist;
        //        else
        //        {
        //            try
        //            {
        //                ExecuteQuery(true, $"INSERT IGNORE INTO `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` (CDK,Items,Vehicle,Experience,Money,GrantPermissionGroup,MaxRedeem) values('{CDK}','{Items}','{Convert.ToUInt16(Vehicle)}','{Convert.ToUInt32(Experience)}','{Convert.ToDecimal(Money)}','{GrantPermissionGroup}','{Convert.ToInt32(MaxRedem)}')");
        //                return CreateCDKResult.Success;

                        
        //            }
        //            catch (Exception ex)
        //            {
        //                Logger.LogException(ex);
        //                return CreateCDKResult.Failure;
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.LogException(ex);
        //    }
        //    return CreateCDKResult.Error;
        //}
        //#endregion

        public RedeemCDKResult RedeemCDK(UnturnedPlayer player,string CDK)
        {
            try
            {
                var cdk = ExecuteQuery(true,
                   $"SELECT 1 FROM `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` WHERE `CDK` ='{CDK}' LIMIT 1;");
                if (cdk == null) return RedeemCDKResult.KeyNotFound;
                var Owner = ExecuteQuery(true, $"select `Owner` from `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` where `CDK` = '{CDK}'");
                if(!Convert.IsDBNull(Owner))
                {
                    if (Owner.ToString() != player.CSteamID.ToString()) return RedeemCDKResult.PlayerNotMatch;
                }

                var EnaleRenew = ExecuteQuery(true, $"select `EnableRenew` from `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` where `CDK` = '{CDK}'");

                if ((int)EnaleRenew == 0)
                {
                    var log = ExecuteQuery(true,
                        $"select `SteamID` from `{Main.Instance.Configuration.Instance.DatabaseRedeemLogTableName}` where `CDK` = '{CDK}';");

                    if (log != null && log.ToString() == player.Id) return RedeemCDKResult.Redeemed;

                    var Items = ExecuteQuery(true,
                    $"select `Items` from `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` where `CDK` = '{CDK}';");
                    var Vehicle = ExecuteQuery(true,
                    $"select `Vehicle` from `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` where `CDK` = '{CDK}';");
                    var Experience = ExecuteQuery(true,
                    $"select `Experience` from `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` where `CDK` = '{CDK}';");
                    var Money = ExecuteQuery(true,
                    $"select `Money` from `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` where `CDK` = '{CDK}';");
                    var GrantPermissionGroup = ExecuteQuery(true,
                    $"select `GrantPermissionGroup` from `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` where `CDK` = '{CDK}';");
                    var MaxRedeem = ExecuteQuery(true,
                    $"select `MaxRedeem` from `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` where `CDK` = '{CDK}';");
                    var RedeemedTimes = ExecuteQuery(true,
                    $"select `RedeemedTimes` from `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` where `CDK` = '{CDK}';");
                    var ValidUntil = ExecuteQuery(true,
                    $"select `ValidUntil` from `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` where `CDK` = '{CDK}'");
                    var Reputation = ExecuteQuery(true,
                    $"select `Reputation` from `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` where `CDK` = '{CDK}'");
                    var Amount = ExecuteQuery(true,
                    $"select `Amount` from `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` where `CDK` = {CDK}");

                    if (!Convert.IsDBNull(MaxRedeem) && (int)RedeemedTimes >= (int)MaxRedeem) return RedeemCDKResult.MaxRedeemed;


                    if (!Convert.IsDBNull(Items) && Convert.IsDBNull(Amount))
                    {
                        string[] _items = Items.ToString().Split(',');
                        foreach (string item in _items)
                        {
                            if (ushort.TryParse(item, out ushort s))
                            {
                                if (!player.GiveItem(s, 1))
                                {
                                    UnturnedChat.Say(player, Main.Instance.Translate("items_give_fail"), UnityEngine.Color.red);
                                }
                            }
                        }
                    }
                    else if(!Convert.IsDBNull(Items) && !Convert.IsDBNull(Amount))
                    {
                        string[] _items = Items.ToString().Split(',');
                        string[] _amount = Amount.ToString().Split(',');
                        foreach (string item in _items)
                        {
                            foreach (string amount in _amount)
                            {
                                if (ushort.TryParse(item, out ushort s))
                                {
                                    if (byte.TryParse(amount, out byte am))
                                    {
                                        if (!player.GiveItem(s,am))
                                        {
                                            UnturnedChat.Say(player, Main.Instance.Translate("items_give_fail"), UnityEngine.Color.red);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (!Convert.IsDBNull(Experience)) player.Experience += Convert.ToUInt32(Experience);
                    if(!Convert.IsDBNull(Reputation)) player.Reputation += Convert.ToInt32(Reputation);
                    if (!Convert.IsDBNull(Vehicle)) player.GiveVehicle(Convert.ToUInt16(Vehicle));
                    if (!Convert.IsDBNull(GrantPermissionGroup))
                    {
                        switch (R.Permissions.AddPlayerToGroup(GrantPermissionGroup.ToString(), player))
                        {
                            case Rocket.API.RocketPermissionsProviderResult.Success:
                                UnturnedChat.Say(player, Main.Instance.Translate("permission_granted", GrantPermissionGroup));
                                break;
                            case Rocket.API.RocketPermissionsProviderResult.DuplicateEntry:
                                UnturnedChat.Say(player, Main.Instance.Translate("permission_duplicate_entry", GrantPermissionGroup), UnityEngine.Color.yellow);
                                break;
                            default:
                                UnturnedChat.Say(player, Main.Instance.Translate("permission_grant_error"), UnityEngine.Color.red);
                                break;
                        }
                    }
                    if (!Convert.IsDBNull(Money))
                    {
                        Main.ExecuteDependencyCode("Uconomy", (IRocketPlugin plugin) =>
                        {
                            if (plugin.State == PluginState.Loaded)
                            {
                                Uconomy.Instance.Database.IncreaseBalance(player.CSteamID.ToString(), Convert.ToDecimal(Money));
                                UnturnedChat.Say(player, Main.Instance.Translate("uconomy_gain", Convert.ToDecimal(Money), Uconomy.Instance.Configuration.Instance.MoneyName));
                            }
                        });
                    }
                    if (Convert.IsDBNull(ValidUntil))
                    {
                        ExecuteQuery(true,
                           $"insert ignore into `{Main.Instance.Configuration.Instance.DatabaseRedeemLogTableName}` (SteamID,CDK) values('{player.Id}','{CDK}');");
                    }
                    else
                    {
                        ExecuteQuery(false,
                           $"insert ignore into `{Main.Instance.Configuration.Instance.DatabaseRedeemLogTableName}` (SteamID,CDK,ValidUntil) values('{player.Id}','{CDK}','{ValidUntil}');");
                    }
                    //write log into mysql
                    if (Convert.ToInt32(MaxRedeem) > 0)
                    {
                        ExecuteQuery(true,
                    $"update `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}`set`RedeemedTimes` = RedeemedTimes +1 where `CDK` = '{CDK}'");
                        return RedeemCDKResult.Success;
                    }
                    // ExecuteQuery(true,
                    //$"DELETE FORM `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` where `CDK` = '{CDK}'");
                    return RedeemCDKResult.Success;
                }
                else if((int)EnaleRenew == 1)
                {
                    var PermissionGroup = ExecuteQuery(true, $"select `GrantPermissionGroup` from `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` where `CDK` = '{CDK}';");
                    var ValidUntil = ExecuteQuery(true, $"select `ValidUntil` from `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` where `CDK` = '{CDK}'");
                    R.Permissions.AddPlayerToGroup(PermissionGroup.ToString(), player);
                    ExecuteQuery(true, $"UPDATE `{Main.Instance.Configuration.Instance.DatabaseRedeemLogTableName}` set `ValidUntil` = '{ValidUntil}' WHERE `CDK` = '{CDK}' AND `SteamID` = '{player.Id}'");
                    ExecuteQuery(true, $"UPDATE `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` set `EnableRenew` = 0 WHERE `CDK` = '{CDK}'");
                    return RedeemCDKResult.Renewed;
                }
            }
            catch(Exception ex)
            {
                Logger.LogException(ex);
            }
            return RedeemCDKResult.Error;
        }

        public void CheckValid(UnturnedPlayer player)
        {
           var result = ExecuteQuery(true, $"select `CDK` from (select * from `{Main.Instance.Configuration.Instance.DatabaseRedeemLogTableName}` where `SteamID` = '{player.Id}') AS ALI where `ValidUntil` < now();");
            if(result != null)
            {
                List<string> cdklist = new List<string> {};
                do
                {
                    string cdk = result.ToString();
                    var PermissionGroup = ExecuteQuery(true, $"select `GrantPermissionGroup` from `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` where `CDK` = '{cdk}';") ;
                    R.Permissions.RemovePlayerFromGroup(PermissionGroup.ToString(), player);
                    cdklist.Add(cdk);
                    result = ExecuteQuery(true, $"select `CDK` from (select * from `{Main.Instance.Configuration.Instance.DatabaseRedeemLogTableName}` where `SteamID` = '{player.Id}') AS ALI where `ValidUntil` < now();");
                } while (result == null);
                foreach(string cdk in cdklist)
                {
                    UnturnedChat.Say(player, Main.Instance.Translate("key_expired",cdk), UnityEngine.Color.yellow);
                }
               
            }
        }

        public bool IsPurchased(UnturnedPlayer player,string CDK) //check player if is first purchase
        {
            bool result;
            var PermissionGroup = ExecuteQuery(true, $"select `GrantPermissionGroup` from `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` where `CDK` = {CDK};");
            var search = ExecuteQuery(true, $"select 1 from {Main.Instance.Configuration.Instance.DatabaseCDKTableName} where `GrantPermissionGroup` = '{PermissionGroup}' and `Owner` = '{player.Id}';");
            if(search == null)
            {
                result = false;
            }
            else
            {
                result = true;
            }
            
            return result;
        }
        internal void CheckSchema() // intial mysql table
        {
            var cdk = ExecuteQuery(true,
               $"show tables like '{Main.Instance.Configuration.Instance.DatabaseCDKTableName}'");

            if (cdk == null)
                ExecuteQuery(false,
                    $"CREATE TABLE `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` (`CDK` varchar(32) NOT NULL,`Items` varchar(32), `Amount` varchar(32), `Vehicle` int(16), `Experience` int(32), `Reputation` int(32), `Money` decimal(15,2) , `GrantPermissionGroup` varchar(32) , `MaxRedeem` int(32), `RedeemedTimes` int(6) NOT NULL DEFAULT '0', `ValidUntil` timestamp, `EnableRenew` BOOLEAN DEFAULT '0', `Owner` varchar(32) ,PRIMARY KEY (`CDK`))");

            var log = ExecuteQuery(true,
               $"show tables like '{Main.Instance.Configuration.Instance.DatabaseRedeemLogTableName}'");

            if(log == null)
                ExecuteQuery(false,
                    $"CREATE TABLE `{Main.Instance.Configuration.Instance.DatabaseRedeemLogTableName}` (`CDK` varchar(32) NOT NULL, `SteamID` varchar(32), `Redeemed Time` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP, `ValidUntil` timestamp,PRIMARY KEY (`CDK`))");

            if(Main.Instance.Configuration.Instance.MySQLTableVer == 1)
            {
                Logger.Log("Updating database table version to 2.");
                ExecuteQuery(false, $"ALTER TABLE `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` ADD `ValidUntil` timestamp;");
                ExecuteQuery(false, $"ALTER TABLE `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` ADD `EnableRenew` int(1) DEFAULT '0';");
                ExecuteQuery(false, $"ALTER TABLE `{Main.Instance.Configuration.Instance.DatabaseRedeemLogTableName}` ADD `ValidUntil` timestamp DEFAULT NULL;");
                Main.Instance.Configuration.Instance.MySQLTableVer = 2;
                Main.Instance.Configuration.Save();
            }
            if(Main.Instance.Configuration.Instance.MySQLTableVer == 2)
            {
                Logger.Log("Updating database table version to 3.");
                ExecuteQuery(false, $"ALTER TABLE `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` ADD `Owner` varchar(32);");
                ExecuteQuery(false, $"ALTER TABLE `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` ADD `Reputation` int(32) AFTER `Experience`;");
                ExecuteQuery(false, $"ALTER TABLE `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` MODIFY `Items` varchar(32);");
                ExecuteQuery(false, $"ALTER TABLE `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` MODIFY `GrantPermissionGroup` varchar(32);");
                ExecuteQuery(false, $"ALTER TABLE `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` MODIFY `Vehicle` int(16);");
                ExecuteQuery(false, $"ALTER TABLE `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` MODIFY `Experience` int(32);");
                ExecuteQuery(false, $"ALTER TABLE `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` MODIFY `Money` decimal(15,2);");
                ExecuteQuery(false, $"ALTER TABLE `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` MODIFY `MaxRedeem` int(32);");
                Main.Instance.Configuration.Instance.MySQLTableVer = 3;
                Main.Instance.Configuration.Save();
            }
            if(Main.Instance.Configuration.Instance.MySQLTableVer == 3)
            {
                Logger.Log("Updating database table version to 4");
                ExecuteQuery(true, $"ALTER TABLE `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` MODIFY `EnableRenew` BOOLEAN DEFALT '0'");
                ExecuteQuery(true, $"ALTER TABLE `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` ADD `Amount` varchar(32) AFTER `Items`");
                Main.Instance.Configuration.Instance.MySQLTableVer = 4;
                Main.Instance.Configuration.Save();
            }
           
        }

        private MySqlConnection CreateConnection()
        {
            MySqlConnection connection = null;
            try
            {
                if (Main.Instance.Configuration.Instance.DatabasePort == 0)
                    Main.Instance.Configuration.Instance.DatabasePort = 3306;
                connection = new MySqlConnection(
                    $"SERVER={Main.Instance.Configuration.Instance.DatabaseAddress};DATABASE={Main.Instance.Configuration.Instance.DatabaseName};UID={Main.Instance.Configuration.Instance.DatabaseUsername};PASSWORD={Main.Instance.Configuration.Instance.DatabasePassword};PORT={Main.Instance.Configuration.Instance.DatabasePort};");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return connection;
        }

        /// <summary>
        /// Executes a MySql query.
        /// </summary>
        /// <param name="isScalar">If the query is expected to return a value.</param>
        /// <param name="query">The query to execute.</param>
        /// <returns>The value if isScalar is true, null otherwise.</returns>
        public object ExecuteQuery(bool isScalar, string query)
        {
            // This method is to reduce the amount of copy paste that there was within this class.
            // Initiate result and connection globally instead of within TryCatch context.
            var connection = CreateConnection();
            object result = null;

            try
            {
                // Initialize command within try context, and execute within it as well.
                var command = connection.CreateCommand();
                command.CommandText = query;
                connection.Open();
                if (isScalar)
                    result = command.ExecuteScalar();
                else
                    command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                // Catch and log any errors during execution, like connection or similar.
                Logger.LogException(ex);
            }
            finally
            {
                // No matter what happens, close the connection at the end of execution.+



                connection.Close();
            }

            return result;
        }
    }
}
