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
using CDK.Data;
using Steamworks;
//using UnityEngine;
using Rocket.Core.Plugins;

namespace CDK
{
    public class DatabaseManager
    {
        public enum RedeemCDKResult { Success, Redeemed, KeyNotFound, MaxRedeemed, Renewed, Error, PlayerNotMatch }

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

        public RedeemCDKResult RedeemCDK(UnturnedPlayer player, string CDK)
        {
            try
            {
                var cdkdata = GetCDKData(CDK);
                var logdata = GetLogData(player.CSteamID, CDK);
                if (cdkdata != null)
                {
                    if (cdkdata.Owner != null && cdkdata.Owner != player.CSteamID)
                    {
                        return RedeemCDKResult.PlayerNotMatch;
                    }
                    if (cdkdata.MaxRedeem != null && cdkdata.RedeemedTimes >= cdkdata.MaxRedeem.Value)
                    {
                        return RedeemCDKResult.MaxRedeemed;
                    }
                    if (logdata != null && !cdkdata.Renew)
                    {
                        return RedeemCDKResult.Redeemed;
                    }
                    else if (logdata == null && !cdkdata.Renew)
                    {

                        if (cdkdata.Items != string.Empty && cdkdata.Amount == string.Empty)
                        {
                            foreach (string item in cdkdata.Items.Split(','))
                            {
                                player.GiveItem(Convert.ToUInt16(item), 1);
                            }
                        }
                        else if (cdkdata.Items != string.Empty && cdkdata.Amount != string.Empty)
                        {
                            foreach (string item in cdkdata.Items.Split(','))
                            {
                                foreach (string amount in cdkdata.Amount.Split(','))
                                {
                                    player.GiveItem(Convert.ToUInt16(item), Convert.ToByte(amount));
                                }
                            }
                        }

                        if (cdkdata.Vehicle != null)
                        {
                            player.GiveVehicle(cdkdata.Vehicle.Value);
                        }
                        if (cdkdata.Reputation != null)
                        {
                            player.Reputation += cdkdata.Reputation.Value;
                        }
                        if (cdkdata.Experience != null)
                        {
                            player.Experience += cdkdata.Experience.Value;
                        }
                        if (cdkdata.Money != null)
                        {
                            Main.ExecuteDependencyCode("Uconomy", (IRocketPlugin plugin) =>
                            {
                                if (plugin.State == PluginState.Loaded)
                                {
                                    Uconomy.Instance.Database.IncreaseBalance(player.Id, cdkdata.Money.Value);
                                    UnturnedChat.Say(player, Main.Instance.Translate("uconomy_gain", Convert.ToDecimal(cdkdata.Money.Value), Uconomy.Instance.Configuration.Instance.MoneyName));
                                }
                            });
                        }

                        if (cdkdata.GrantPermissionGroup != string.Empty)
                        {
                            switch (R.Permissions.AddPlayerToGroup(cdkdata.GrantPermissionGroup, player))
                            {
                                case Rocket.API.RocketPermissionsProviderResult.Success:
                                    UnturnedChat.Say(player, Main.Instance.Translate("permission_granted", cdkdata.GrantPermissionGroup));
                                    break;
                                case Rocket.API.RocketPermissionsProviderResult.DuplicateEntry:
                                    UnturnedChat.Say(player, Main.Instance.Translate("permission_duplicate_entry", cdkdata.GrantPermissionGroup), UnityEngine.Color.yellow);
                                    break;
                                default:
                                    UnturnedChat.Say(player, Main.Instance.Translate("permission_grant_error"), UnityEngine.Color.red);
                                    break;
                            }
                        }

                        SaveLogToDB(new LogData(CDK, player.CSteamID, DateTime.Now, cdkdata.ValidUntil));
                        IncreaseRedeemedTime(CDK);
                    }
                    else if (logdata != null && cdkdata.Renew)
                    {
                        R.Permissions.AddPlayerToGroup(cdkdata.GrantPermissionGroup, player);
                        UpdateLogInDB(new LogData(CDK, player.CSteamID, DateTime.Now, cdkdata.ValidUntil));
                        UpdateRenew(CDK);
                        return RedeemCDKResult.Renewed;
                    }
                }
                else
                {
                    return RedeemCDKResult.KeyNotFound;
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
            return RedeemCDKResult.Error;
        }

        public void CheckValid(UnturnedPlayer player)
        {
            LogData logData = GetLogData(player.CSteamID);
            if (logData != null)
            {
                do
                {
                    CDKData cDKData = GetCDKData(logData.CDK);
                    R.Permissions.RemovePlayerFromGroup(cDKData.GrantPermissionGroup, player);
                    UnturnedChat.Say(player, Main.Instance.Translate("key_expired", logData.CDK));
                    logData = GetLogData(player.CSteamID);
                } while (logData == null);
            }
        }

        private CDKData BuildCDKData(MySqlDataReader reader)
        {
            return new CDKData(reader.GetString("CDK"), reader.GetString("Items"), reader.GetString("Amount"), reader.GetUInt16("Vehicle"), reader.GetUInt16("Experience"), reader.GetDecimal("Money"), reader.GetInt32("Reputation"), reader.GetString("GrantPermissionGroup"), reader.GetInt32("RedeemedTimes"), reader.GetInt32("MaxRedeem"), reader.GetDateTime("ValidUntil"), new CSteamID(reader.GetUInt64("Owner")), reader.GetBoolean("EnableRenew"));
        }
        private LogData BuildLogData(MySqlDataReader reader)
        {
            return new LogData(reader.GetString("CDK"), new CSteamID(reader.GetUInt64("SteamID")), reader.GetDateTime("Redeemed Time"), reader.GetDateTime("ValidUntil"));
        }

        public CDKData GetCDKData(string cdk)
        {
            CDKData data = null;
            try
            {
                MySqlConnection connection = CreateConnection();
                MySqlCommand command = connection.CreateCommand();
                command.CommandText = $"SELECT * FROM `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` WHERE `CDK` = '{cdk}'";
                connection.Open();
                MySqlDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    data = BuildCDKData(reader);
                }
                connection.Close();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
            return data;
        }

        public LogData GetLogData(CSteamID steamID, string cdk = null)
        {
            LogData logData = null;
            try
            {
                MySqlConnection connection = CreateConnection();
                MySqlCommand command = connection.CreateCommand();
                if (cdk != null)
                {
                    command.CommandText = $"SELECT * FROM `{Main.Instance.Configuration.Instance.DatabaseRedeemLogTableName}` WHERE `SteamID` = '{steamID} AND `CDK` = {cdk}'";
                }
                else
                {
                    command.CommandText = $"select 1 from `{ Main.Instance.Configuration.Instance.DatabaseRedeemLogTableName}` where `SteamID` = '{steamID} and `ValidUntil` < now()";
                }
                connection.Open();
                MySqlDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    logData = BuildLogData(reader);
                }
                connection.Close();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
            return logData;
        }

        internal void SaveLogToDB(LogData logData)
        {
            ExecuteQuery(true, $"INSERT INTO `{Main.Instance.Configuration.Instance.DatabaseRedeemLogTableName}` (CDK,SteamID,Redeemed Time,ValidUntil) VALUES('{logData.CDK}','{logData.SteamID}','{logData.RedeemTime}','{logData.ValidUntil}')");
        }

        internal void UpdateLogInDB(LogData logData)
        {
            ExecuteQuery(true, $"UPDATE `{Main.Instance.Configuration.Instance.DatabaseRedeemLogTableName}` SET `ValidUntil` = '{logData.ValidUntil}',`Redeemed Time` = {logData.RedeemTime} WHERE `SteamID` = '{logData.SteamID}' AND `CDK` = '{logData.CDK}'");
        }
        internal void UpdateRenew(string cdk)
        {
            ExecuteQuery(true, $"UPDATE `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` SET `EnableRenew` = 0 WHERE `CDK` = {cdk}");
        }

        internal void IncreaseRedeemedTime(string cdk)
        {
            ExecuteQuery(true, $"UPDATE `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` SET `RedeemedTimes` = RedeemedTimes +1 where `CDK` = '{cdk}' ");
        }

        public bool IsPurchased(UnturnedPlayer player, string CDK) //check player if is first purchase
        {
            bool result;
            var PermissionGroup = ExecuteQuery(true, $"select `GrantPermissionGroup` from `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` where `CDK` = {CDK};");
            var search = ExecuteQuery(true, $"select 1 from {Main.Instance.Configuration.Instance.DatabaseCDKTableName} where `GrantPermissionGroup` = '{PermissionGroup}' and `Owner` = '{player.Id}';");
            if (search == null)
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

            if (log == null)
                ExecuteQuery(false,
                    $"CREATE TABLE `{Main.Instance.Configuration.Instance.DatabaseRedeemLogTableName}` (`CDK` varchar(32) NOT NULL, `SteamID` varchar(32), `Redeemed Time` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP, `ValidUntil` timestamp)");

            if (Main.Instance.Configuration.Instance.MySQLTableVer == 1)
            {
                Logger.Log("Updating database table version to 2.");
                ExecuteQuery(false, $"ALTER TABLE `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` ADD `ValidUntil` timestamp;");
                ExecuteQuery(false, $"ALTER TABLE `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` ADD `EnableRenew` int(1) DEFAULT '0';");
                ExecuteQuery(false, $"ALTER TABLE `{Main.Instance.Configuration.Instance.DatabaseRedeemLogTableName}` ADD `ValidUntil` timestamp DEFAULT NULL;");
                Main.Instance.Configuration.Instance.MySQLTableVer = 2;
                Main.Instance.Configuration.Save();
            }
            if (Main.Instance.Configuration.Instance.MySQLTableVer == 2)
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
            if (Main.Instance.Configuration.Instance.MySQLTableVer == 3)
            {
                Logger.Log("Updating database table version to 4");
                ExecuteQuery(true, $"ALTER TABLE `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` MODIFY `EnableRenew` BOOLEAN DEFALT '0'");
                ExecuteQuery(true, $"ALTER TABLE `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` ADD `Amount` varchar(32) AFTER `Items`");
                Main.Instance.Configuration.Instance.MySQLTableVer = 4;
                Main.Instance.Configuration.Save();
            }
            if (Main.Instance.Configuration.Instance.MySQLTableVer == 4)
            {
                Logger.Log("Updating database table version to 5");
                ExecuteQuery(true, $"ALTER TABLE `{Main.Instance.Configuration.Instance.DatabaseRedeemLogTableName}` DROP PRIMARY KEY ");
                Main.Instance.Configuration.Instance.MySQLTableVer = 5;
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