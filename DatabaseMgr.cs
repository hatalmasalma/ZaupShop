using System;
using System.Collections.Generic;
using System.Data;
using fr34kyn01535.Uconomy;
using I18N.West;
using MySql.Data.MySqlClient;
using Rocket.Core.Logging;
using ZaupShop.Groups;

namespace ZaupShop
{
    public class DatabaseMgr
    {
        internal DatabaseMgr()
        {
            var cP1250 = new CP1250();
            CheckSchema();
        }

        private void CheckSchema()
        {
            string itemShopTableName = ZaupShop.Instance.ItemShopTableName;
            string vehicleShopTableName = ZaupShop.Instance.VehicleShopTableName;
            string groupsTableName = ZaupShop.Instance.GroupListTableName;
            
            var res = ExecuteQuery(true,
                $"show tables like '{itemShopTableName}'");

            if (res == null)
                ExecuteQuery(false,
                    $"CREATE TABLE `{itemShopTableName}` (`id` int(6) NOT NULL,`itemname` varchar(32) NOT NULL,`cost` decimal(15,2) NOT NULL DEFAULT '20.00',`buyback` decimal(15,2) NOT NULL DEFAULT '0.00',PRIMARY KEY (`id`))");

            res = ExecuteQuery(true,
                $"show tables like '{vehicleShopTableName}'");

            if (res == null)
                ExecuteQuery(false,
                    $"CREATE TABLE `{vehicleShopTableName}` (`id` int(6) NOT NULL,`vehiclename` varchar(32) NOT NULL,`cost` decimal(15,2) NOT NULL DEFAULT '100.00',PRIMARY KEY (`id`))");

            res = ExecuteQuery(true,
                $"show columns from `{itemShopTableName}` like 'buyback'");

            if (res == null)
                ExecuteQuery(false,
                    $"ALTER TABLE `{itemShopTableName}` ADD `buyback` decimal(15,2) NOT NULL DEFAULT '0.00'");

            res = ExecuteQuery(true, $"show tables like '{groupsTableName}'");

            if (res == null)
                ExecuteQuery(false,
                    $"CREATE TABLE `{groupsTableName}` (`name` varchar(32) NOT NULL,`whitelist` tinyint NOT NULL,PRIMARY KEY (`name`))");
        }

        private MySqlConnection CreateConnection()
        {
            MySqlConnection mySqlConnection = null;
            try
            {
                if (Uconomy.Instance.Configuration.Instance.DatabasePort == 0)
                    Uconomy.Instance.Configuration.Instance.DatabasePort = 3306;
                mySqlConnection = new MySqlConnection(
                    $"SERVER={Uconomy.Instance.Configuration.Instance.DatabaseAddress};DATABASE={Uconomy.Instance.Configuration.Instance.DatabaseName};UID={Uconomy.Instance.Configuration.Instance.DatabaseUsername};PASSWORD={Uconomy.Instance.Configuration.Instance.DatabasePassword};PORT={Uconomy.Instance.Configuration.Instance.DatabasePort};");
            }
            catch (Exception exception)
            {
                Logger.LogException(exception);
            }

            return mySqlConnection;
        }

        public bool AddItem(int id, string name, decimal cost, bool change)
        {
            var affected = ExecuteQuery(false,
                change
                    ? $"update `{ZaupShop.Instance.ItemShopTableName}` set itemname=@name, cost='{cost}' where id='{id}';"
                    : $"Insert into `{ZaupShop.Instance.ItemShopTableName}` (`id`, `itemname`, `cost`) VALUES ('{id}', @name, '{cost}');",
                new MySqlParameter("@name", name));

            if (affected == null) return false;

            int.TryParse(affected.ToString(), out var rows);

            return rows > 0;
        }

        public bool AddVehicle(int id, string name, decimal cost, bool change)
        {
            var affected = ExecuteQuery(false,
                change
                    ? $"update `{ZaupShop.Instance.VehicleShopTableName}` set vehiclename=@name, cost='{cost}' where id='{id}';"
                    : $"Insert into `{ZaupShop.Instance.VehicleShopTableName}` (`id`, `vehiclename`, `cost`) VALUES ('{id}', @name, '{cost}');",
                new MySqlParameter("@name", name));

            if (affected == null) return false;

            int.TryParse(affected.ToString(), out var rows);

            return rows > 0;
        }

        public decimal GetItemCost(int id)
        {
            var num = new decimal(0);
            var obj = ExecuteQuery(true,
                $"select `cost` from `{ZaupShop.Instance.ItemShopTableName}` where `id` = '{id}';");

            if (obj != null) decimal.TryParse(obj.ToString(), out num);

            return num;
        }

        public decimal GetVehicleCost(int id)
        {
            var num = new decimal(0);
            var obj = ExecuteQuery(true,
                $"select `cost` from `{ZaupShop.Instance.VehicleShopTableName}` where `id` = '{id}';");

            if (obj != null) decimal.TryParse(obj.ToString(), out num);

            return num;
        }

        public bool DeleteItem(int id)
        {
            var affected = ExecuteQuery(false,
                $"delete from `{ZaupShop.Instance.ItemShopTableName}` where id='{id}';");

            if (affected == null) return false;

            int.TryParse(affected.ToString(), out var rows);

            return rows > 0;
        }

        public bool DeleteVehicle(int id)
        {
            var affected = ExecuteQuery(false,
                $"delete from `{ZaupShop.Instance.VehicleShopTableName}` where id='{id}';");

            if (affected == null) return false;

            int.TryParse(affected.ToString(), out var rows);

            return rows > 0;
        }

        public bool SetBuyPrice(int id, decimal cost)
        {
            var affected = ExecuteQuery(false,
                $"update `{ZaupShop.Instance.ItemShopTableName}` set `buyback`='{cost}' where id='{id}';");

            if (affected == null) return false;

            int.TryParse(affected.ToString(), out var rows);

            return rows > 0;
        }

        public decimal GetItemBuyPrice(int id)
        {
            var num = new decimal(0);
            var obj = ExecuteQuery(true,
                $"select `buyback` from `{ZaupShop.Instance.ItemShopTableName}` where `id` = '{id}';");

            if (obj != null) decimal.TryParse(obj.ToString(), out num);

            return num;
        }

        public bool AddGroup(ZaupGroup group)
        {
            byte mySQLBool = group.Whitelist ? (byte) 1 : (byte) 0;
            string commandText =
                $"Insert into `{ZaupShop.Instance.GroupListTableName}` (`name`, `whitelist`) VALUES (@name, '{mySQLBool}');";

            var rowsObject = ExecuteQuery(false, commandText, new MySqlParameter("@name", group.Name));

            if (rowsObject == null)
                return false;

            byte newRows = byte.Parse(rowsObject.ToString());

            commandText =
                $"CREATE TABLE `{group.Name}` (`id` smallint UNSIGNED NOT NULL AUTO_INCREMENT, `assetid` smallint UNSIGNED NOT NULL, `vehicle` tinyint NOT NULL, PRIMARY KEY (`id`))";

            ExecuteQuery(false, commandText);

            return newRows == 1;
        }

        public bool DelGroup(string groupName)
        {
            string commandText =
                $"DELETE FROM `{ZaupShop.Instance.GroupListTableName}` WHERE `name` = @name;";

            var rowsObject = ExecuteQuery(false, commandText, new MySqlParameter("@name", groupName));

            if (rowsObject == null)
                return false;

            byte goneRows = byte.Parse(rowsObject.ToString());

            commandText = $"DROP TABLE `{groupName}`;";
            
            ExecuteQuery(false, commandText);

            return goneRows == 1;
        }

        public bool AddIDToGroup(ZaupGroup group, ZaupGroupElement element)
        {
            byte mySQLBool = element.Vehicle ? (byte) 1 : (byte) 0;
            string commandText = $"INSERT INTO `{group.Name}` (`assetid`, `vehicle`) VALUES ('{element.ID}', '{mySQLBool}');";

            var rowsObject = ExecuteQuery(false, commandText);

            if (rowsObject == null)
                return false;
            
            byte newRows = byte.Parse(rowsObject.ToString());

            return newRows == 1;
        }

        public bool RemoveIDFromGroup(ZaupGroup group, ZaupGroupElement element)
        {
            byte mySQLBool = element.Vehicle ? (byte) 1 : (byte) 0;
            string commandText = $"DELETE FROM `{group.Name}` WHERE `assetid` = '{element.ID}' AND `vehicle` = '{mySQLBool}';";

            var rowsObject = ExecuteQuery(false, commandText);

            if (rowsObject == null)
                return false;
            
            byte goneRows = byte.Parse(rowsObject.ToString());

            return goneRows == 1;
        }

        public HashSet<ZaupGroup> GetGroups()
        {
            string query = $"SELECT * FROM `{ZaupShop.Instance.GroupListTableName}`;";
            return _GetGroups(query);
        }

        private HashSet<ZaupGroup> _GetGroups(string query)
        {
            HashSet<ZaupGroup> groups = new HashSet<ZaupGroup>();

            using MySqlConnection connection = CreateConnection();
            try
            {
                using MySqlCommand command = connection.CreateCommand();
                command.CommandText = query;

                connection.Open();
                using MySqlDataReader reader = command.ExecuteReader();
                
                while (reader.Read())
                {
                    string groupName = reader.GetString(0);
                    bool wlist = reader.GetBoolean(1);
                    groups.Add(new ZaupGroup(groupName, wlist, new HashSet<ZaupGroupElement>()));
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return groups;
        }

        public HashSet<ZaupGroupElement> GetGroupElements(string groupName)
        {
            string query = $"SELECT * FROM `{groupName}`;";
            return _GetGroupElements(query);
        }

        private HashSet<ZaupGroupElement> _GetGroupElements(string query)
        {
            HashSet<ZaupGroupElement> groupElements = new HashSet<ZaupGroupElement>();

            using MySqlConnection connection = CreateConnection();
            try
            {
                using MySqlCommand command = connection.CreateCommand();
                command.CommandText = query;

                connection.Open();
                using MySqlDataReader reader = command.ExecuteReader();
                
                while (reader.Read())
                {
                    ushort ID = reader.GetUInt16(1);
                    bool vehicle = reader.GetBoolean(2);
                    groupElements.Add(new ZaupGroupElement(ID, vehicle));
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return groupElements;
        }

        /// <summary>
        /// Executes a MySql query.
        /// </summary>
        /// <param name="isScalar">If the query is expected to return a value.</param>
        /// <param name="query">The query to execute.</param>
        /// <param name="parameters">The MySqlParameters to be added to the command.</param>
        /// <returns>The value if isScalar is true, null otherwise.</returns>
        public object ExecuteQuery(bool isScalar, string query, params MySqlParameter[] parameters)
        {
            object result = null;

            using (var connection = CreateConnection())
            {
                try
                {
                    using var command = connection.CreateCommand();
                    command.CommandText = query;

                    foreach (var parameter in parameters)
                        command.Parameters.Add(parameter);

                    connection.Open();
                    result = isScalar ? command.ExecuteScalar() : command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                }
                finally
                {
                    connection.Close();
                }
            }

            return result;
        }
    }
}