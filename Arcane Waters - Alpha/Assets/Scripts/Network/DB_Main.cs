using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Xml;
using MapCreationTool;
using MapCreationTool.Serialization;
using System.IO;
using SimpleJSON;
using MapCustomization;
using Newtonsoft.Json;
using System.Threading.Tasks;

#if IS_SERVER_BUILD || NUBIS
using MySql.Data.MySqlClient;

public class DatabaseCredentials
{

   [JsonProperty("AW_DB_SERVER")]
   public string server;

   [JsonProperty("AW_DB_NAME")]
   public string database;

   [JsonProperty("AW_DB_USER")]
   public string user;

   [JsonProperty("AW_DB_PASS")]
   public string password;

}

public class DB_Main : DB_MainStub
{
   #region NubisFeatures

   public static new string fetchSingleBlueprint (string bpIdStr, string usrIdStr) {
      int bpId = int.Parse(bpIdStr);
      int usrId = int.Parse(usrIdStr);
      try {
         using (MySqlConnection connection = getConnection()) {
            connection.Open();
            string query = "SELECT itmId, itmCategory, itmType, itmData " +
                           "FROM arcane.items " +
                           "where (itmCategory = 7 and itmId = @itmId) and items.usrId = @usrId";
            using (MySqlCommand command = new MySqlCommand(query, connection)) {
               command.Parameters.AddWithValue("@itmId", bpId);
               command.Parameters.AddWithValue("@usrId", usrId);

               StringBuilder builder = new StringBuilder();
               using (MySqlDataReader reader = command.ExecuteReader()) {
                  while (reader.Read()) {
                     int itmId = reader.GetInt32("itmId");
                     int itmCategory = reader.GetInt32("itmCategory");
                     int itmType = reader.GetInt32("itmType");
                     string itmData = reader.GetString("itmData");

                     string result = $"[next]{itmId}[space]{itmCategory}[space]{itmType}[space]{itmData}[space]";
                     builder.AppendLine(result);
                  }
               }
               return builder.ToString();
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
         return "";
      }
   }

   public static new string fetchZipRawData (string slotStr) {
      int slot = int.Parse(slotStr);
      UInt32 FileSize;
      byte[] rawData;

      try {
         using (MySqlConnection connection = getConnectionToDevGlobal()) {
            connection.Open();

            string query = "SELECT * FROM global.xml_status where id = " + slot;
            using (MySqlCommand command = new MySqlCommand(query, connection)) {
               using (MySqlDataReader dataReader = command.ExecuteReader()) {
                  while (dataReader.Read()) {
                     FileSize = dataReader.GetUInt32(dataReader.GetOrdinal("dataSize"));
                     rawData = new byte[FileSize];

                     dataReader.GetBytes(dataReader.GetOrdinal("xmlZipData"), 0, rawData, 0, (int) FileSize);

                     string textData = Convert.ToBase64String(rawData);
                     return textData;
                  }
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
         return "";
      }
      return "";
   }

   public static new string userInventory (string usrIdStr, string categoryFilterJSON, string itemIdsToExcludeJSON,
      string mustExcludeEquippedItemsStr, string currentPageStr, string itemsPerPageStr) {
      int usrId = int.Parse(usrIdStr);
      int currentPage = int.Parse(currentPageStr);
      int itemsPerPage = int.Parse(itemsPerPageStr);
      int offset = currentPage * itemsPerPage;

      int[] categoryFilter = categoryFilterJSON.Length > 1 ?
         JsonConvert.DeserializeObject<int[]>(categoryFilterJSON) :
         new int[0];
      int[] itemIdsToExclude = itemIdsToExcludeJSON.Length > 1 ?
         JsonConvert.DeserializeObject<int[]>(itemIdsToExcludeJSON) :
         new int[0];

      string whereClause = getUserInventoryWhereClause(usrId, categoryFilter, itemIdsToExclude,
         mustExcludeEquippedItemsStr == "1");

      try {
         using (MySqlConnection connection = getConnection()) {
            connection.Open();
            using (MySqlCommand command = new MySqlCommand(
            "SELECT * FROM arcane.items " + whereClause + " order by itmCategory limit " + itemsPerPage +
            " offset " + offset, connection)) {
               D.editorLog(command.CommandText);
               StringBuilder stringBuilder = new StringBuilder();
               using (MySqlDataReader reader = command.ExecuteReader()) {
                  while (reader.Read()) {
                     int itmId = reader.GetInt32("itmId");
                     int itmCategory = reader.GetInt32("itmCategory");
                     int itmType = reader.GetInt32("itmType");
                     int itmCount = reader.GetInt32("itmCount");
                     string itmData = "";
                     string itmPalettes = "";

                     try {
                        itmData = reader.GetString("itmData");
                     } catch {
                        //D.editorLog("Blank item data");
                     }
                     try {
                        itmPalettes = reader.GetString("itmPalettes");
                     } catch {
                        D.editorLog("Blank Palette 1");
                     }

                     string result = $"[next]{itmId}[space]{itmCategory}[space]{itmType}[space]{itmCount}[space]{itmData}[space]{itmPalettes}";
                     stringBuilder.AppendLine(result);
                  }
               }
               return stringBuilder.ToString();
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
      return "Failed to Query";
   }

   public static new string userInventoryCount (string usrIdStr, string categoryFilterJSON, string itemIdsToExcludeJSON,
      string mustExcludeEquippedItemsStr) {
      int usrId = int.Parse(usrIdStr);

      int[] categoryFilter = categoryFilterJSON.Length > 1 ?
         JsonConvert.DeserializeObject<int[]>(categoryFilterJSON) :
         new int[0];
      int[] itemIdsToExclude = itemIdsToExcludeJSON.Length > 1 ?
         JsonConvert.DeserializeObject<int[]>(itemIdsToExcludeJSON) :
         new int[0];

      int count = 0;
      string whereClause = getUserInventoryWhereClause(usrId, categoryFilter, itemIdsToExclude,
         mustExcludeEquippedItemsStr == "1");

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT COUNT(*) AS itemCount FROM arcane.items " + whereClause
            , conn)) {
            conn.Open();
            cmd.Prepare();
            D.editorLog(cmd.CommandText);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  count = dataReader.GetInt32("itemCount");
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return count.ToString();
   }

   private static string getUserInventoryWhereClause (int userId, int[] categoryFilter,
      int[] itemIdsToExclude, bool mustExcludeEquippedItems) {
      StringBuilder clause = new StringBuilder();
      clause.Append(" WHERE usrId = " + userId + " ");

      // If the category filter only contains 'none', this is skipped and all the categories are selected
      if ((categoryFilter.Length > 0 && categoryFilter[0] != 0) || categoryFilter.Length > 1) {
         // Setup multiple categories
         clause.Append("AND (itmCategory=" + (int) categoryFilter[0]);
         for (int i = 1; i < categoryFilter.Length; i++) {
            clause.Append(" OR itmCategory=" + +(int) categoryFilter[i]);
         }
         clause.Append(") ");
      }

      // Exclude item ids (not necesarily equipped items)
      if (itemIdsToExclude.Length > 0) {
         clause.Append("AND itmId NOT IN (");
         for (int i = 0; i < itemIdsToExclude.Length; i++) {
            clause.Append(itemIdsToExclude[i] + ", ");
         }

         // Delete the last ", "
         clause.Length = clause.Length - 2;

         clause.Append(") ");
      }

      // Exclude equipped item ids
      if (mustExcludeEquippedItems) {
         clause.Append("AND itmId NOT IN (" +
            "SELECT itmId FROM items RIGHT JOIN users ON " +
            "(items.itmId = users.armId OR items.itmId = users.wpnId OR items.itmId = users.hatId) " +
            "WHERE items.usrId = " + userId + ") ");
      }

      return clause.ToString();
   }

   public static new string fetchXmlVersion (string slotstr) {
      int slot = int.Parse(slotstr);
      try {
         using (MySqlConnection connection = getConnectionToDevGlobal()) {
            connection.Open();
            using (MySqlCommand command = new MySqlCommand(
               "SELECT version FROM global.xml_status where id = " + slot,
               connection)) {

               using (MySqlDataReader reader = command.ExecuteReader()) {
                  while (reader.Read()) {
                     string version = reader.GetString("version");
                     return version;
                  }
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
         return "0";
      }
      return "0";
   }

   public static new string fetchCraftableHats (string usrIdStr) {
      int usrId = int.Parse(usrIdStr);
      try {
         // Connect to the server.
         using (MySqlConnection connection = getConnection()) {
            connection.Open();
            using (MySqlCommand command = new MySqlCommand(
               "SELECT itmId, itmCategory, itmType " +
               "FROM arcane.items " +
               "WHERE(itmCategory = 7 AND itmData LIKE '%blueprintType=hat%') AND items.usrId = @usrId",
               connection)) {
               command.Parameters.AddWithValue("@usrId", usrId);

               StringBuilder stringBuilder = new StringBuilder();
               using (MySqlDataReader reader = command.ExecuteReader()) {
                  while (reader.Read()) {
                     int itmId = reader.GetInt32("itmId");
                     int itmCategory = reader.GetInt32("itmCategory");
                     int itmType = reader.GetInt32("itmType");
                     string result = $"[next]{itmId}[space]{itmCategory}[space]{itmType}[space]";
                     stringBuilder.AppendLine(result);
                  }
               }
               return stringBuilder.ToString();
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
         return "";
      }
   }

   public static new string fetchCraftableArmors (string usrIdStr) {
      int usrId = int.Parse(usrIdStr);
      try {
         // Connect to the server.
         using (MySqlConnection connection = getConnection()) {
            connection.Open();
            using (MySqlCommand command = new MySqlCommand(
               "SELECT itmId, itmCategory, itmType " +
               "FROM items " +
               "WHERE(itmCategory = 7 AND itmData LIKE '%blueprintType=armor%') AND items.usrId = @usrId",
               connection)) {

               command.Parameters.AddWithValue("@usrId", usrId);

               StringBuilder stringBuilder = new StringBuilder();
               using (MySqlDataReader reader = command.ExecuteReader()) {
                  while (reader.Read()) {
                     int itmId = reader.GetInt32("itmId");
                     int itmCategory = reader.GetInt32("itmCategory");
                     int itmType = reader.GetInt32("itmType");
                     string result = $"[next]{itmId}[space]{itmCategory}[space]{itmType}[space]";
                     stringBuilder.AppendLine(result);
                  }
               }
               return stringBuilder.ToString();
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
         return "";
      }
   }

   public static new string fetchCraftableWeapons (string usrIdStr) {
      int usrId = int.Parse(usrIdStr);
      try {
         // Connect to the server.
         using (MySqlConnection connection = getConnection()) {
            connection.Open();
            using (MySqlCommand command = new MySqlCommand(
               "SELECT itmId, itmCategory, itmType " +
               "FROM items " +
               "WHERE(itmCategory = 7 AND itmData LIKE '%blueprintType=weapon%') AND items.usrId = @usrId",
               connection)) {
               command.Parameters.AddWithValue("@usrId", usrId);

               StringBuilder stringBuilder = new StringBuilder();
               using (MySqlDataReader reader = command.ExecuteReader()) {
                  while (reader.Read()) {
                     int itmId = reader.GetInt32("itmId");
                     int itmCategory = reader.GetInt32("itmCategory");
                     int itmType = reader.GetInt32("itmType");
                     string result = $"[next]{itmId}[space]{itmCategory}[space]{itmType}[space]";
                     stringBuilder.AppendLine(result);
                  }
               }
               return stringBuilder.ToString();
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
         return "";
      }
   }

   public static new string fetchCraftingIngredients (string usrIdStr) {
      int usrId = int.Parse(usrIdStr);
      try {
         using (MySqlConnection connection = getConnection()) {
            connection.Open();
            string result = "";
            using (MySqlCommand command = new MySqlCommand(
               "SELECT itmId, itmCategory, itmType, itmCount " +
               "FROM items " +
               "WHERE usrId = @usrId and itmCategory = 6",
               connection)) {
               command.Parameters.AddWithValue("@usrId", usrId);

               using (MySqlDataReader reader = command.ExecuteReader()) {
                  while (reader.Read()) {
                     int itmId = reader.GetInt32("itmId");
                     int itmCategory = reader.GetInt32("itmCategory");
                     int itmType = reader.GetInt32("itmType");
                     int itmCount = reader.GetInt32("itmCount");

                     result += $"[next]{itmId}[space]{itmCategory}[space]{itmType}[space]{itmCount}[space]";
                  }
                  return result;
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
         return "";
      }
   }

   public static new string fetchEquippedItems (string usrIdStr) {
      int usrId = int.Parse(usrIdStr);
      try {
         using (MySqlConnection connection = getConnection()) {
            connection.Open();
            using (MySqlCommand command = new MySqlCommand(
               "SELECT itmId, itmCategory, itmType, itmPalettes " +
               "FROM arcane.items " +
               "left join arcane.users on armId = itmId or wpnId = itmId or hatId = itmId " +
               "where(armId = itmId or wpnId = itmId or hatId = itmId) and items.usrId = @usrId",
               connection)) {
               command.Parameters.AddWithValue("@usrId", usrId);

               StringBuilder stringBuilder = new StringBuilder();
               using (MySqlDataReader reader = command.ExecuteReader()) {
                  while (reader.Read()) {
                     int itmId = reader.GetInt32("itmId");
                     int itmCategory = reader.GetInt32("itmCategory");
                     int itmType = reader.GetInt32("itmType");
                     string itemPalette = reader.GetString("itmPalettes");
                     string result = $"[next]{itmId}[space]{itmCategory}[space]{itmType}[space]{itemPalette}[space]";
                     stringBuilder.AppendLine(result);
                  }
               }
               return stringBuilder.ToString();
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
         return "";
      }
   }

   public static new string fetchMapData (string mapName, string versionStr) {
      int version = int.Parse(versionStr);
      try {
         using (MySqlConnection connection = getConnection()) {
            connection.Open();
            using (MySqlCommand command = new MySqlCommand(
               "SELECT gameData FROM map_versions_v2 left join maps_v2 on mapid = id WHERE (name = @mapName and version=@mapVersion)",
               connection)) {
               command.Parameters.AddWithValue("@mapName", mapName);
               command.Parameters.AddWithValue("@mapVersion", version);

               using (MySqlDataReader reader = command.ExecuteReader()) {
                  while (reader.Read()) {
                     string result = reader.GetString("gameData");
                     return result;
                  }
               }
               return "";
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
         return "";
      }
   }

   public static new string userAbilities (string userIdString, string abilityEquipStatusString) {
      AbilityEquipStatus abilityEquipStatus = (AbilityEquipStatus) int.Parse(abilityEquipStatusString);
      int usrId = int.Parse(userIdString);
      string addedCondition = "";

      if (abilityEquipStatus == AbilityEquipStatus.Equipped) {
         addedCondition = " and abilityEquipSlot != -1";
      } else if (abilityEquipStatus == AbilityEquipStatus.Unequipped) {
         addedCondition = " and abilityEquipSlot = -1";
      }
      List<AbilitySQLData> abilityList = new List<AbilitySQLData>();
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM ability_table_v2 WHERE (userID=@userID" + addedCondition + ")", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@userID", usrId);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  AbilitySQLData abilityData = new AbilitySQLData(dataReader);
                  abilityList.Add(abilityData);
               }
            }

            return JsonConvert.SerializeObject(abilityList);
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
         return "";
      }
   }

   #endregion

   #region Public Variables

   public static string RemoteServer
   {
      get { return _remoteServer; }
   }

   #endregion

   #region Cloud Changesets

   public static new void addNewCloudData (CloudBuildData cloudData) {
      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand(
            "INSERT INTO global.cloud_changesets (buildId, message, lastUserUpdate) " +
            "VALUES(@buildId, @message, @lastUserUpdate) ", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@buildId", cloudData.buildId);
            cmd.Parameters.AddWithValue("@message", cloudData.buildMessage);
            cmd.Parameters.AddWithValue("@lastUserUpdate", DateTime.UtcNow.ToString());

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new CloudBuildData getCloudData () {
      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM global.cloud_changesets order by buildId DESC limit 1", conn)) {
            conn.Open();
            cmd.Prepare();

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  CloudBuildData newBuildData = new CloudBuildData();
                  int buildId = dataReader.GetInt32("buildId");
                  string message = dataReader.GetString("message");
                  try {
                     string lastUpdated = dataReader.GetString("lastUserUpdate");
                     newBuildData.buildDateTime = lastUpdated;
                  } catch {
                     newBuildData.buildDateTime = "";
                  }

                  newBuildData.buildId = buildId;
                  newBuildData.buildMessage = message;

                  return newBuildData;
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return null;
   }

   #endregion

   #region XML Content Handling

   public static new void writeZipData (byte[] bytes, int slot) {
      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand(
            // Declaration of table elements
            "UPDATE global.xml_status SET xmlZipData = @xmlZipData, version = version + 1, dataSize = @dataSize WHERE id = " + slot, conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.Add("@xmlZipData", MySqlDbType.MediumBlob).Value = bytes;
            cmd.Parameters.AddWithValue("@dataSize", bytes.Length);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new string getXmlContent (string tableName, EditorSQLManager.EditorToolType toolType = EditorSQLManager.EditorToolType.None) {
      string content = "";
      string addedFields = "";
      string contentToFetch = "xml_id, xmlContent ";

      if (toolType == EditorSQLManager.EditorToolType.BattlerAbility) {
         addedFields = ", ability_type";
      } else if (toolType == EditorSQLManager.EditorToolType.Palette) {
         contentToFetch = "id, xmlContent ";
      } else if (toolType == EditorSQLManager.EditorToolType.Treasure_Drops || toolType == EditorSQLManager.EditorToolType.Quest) {
         contentToFetch = "xmlId, xmlContent ";
      } else if (toolType == EditorSQLManager.EditorToolType.ItemDefinitions) {
         contentToFetch = "id, serializedData ";
         addedFields = ", category";
      }

      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT " + contentToFetch + addedFields + " FROM global." + tableName, conn)) {

            conn.Open();
            cmd.Prepare();

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  string xmlContent = "";
                  int xmlId = 0;
                  string addedContent = "";

                  if (toolType == EditorSQLManager.EditorToolType.BattlerAbility) {
                     addedContent = dataReader.GetInt32("ability_type") + "[space]";
                  } else if (toolType == EditorSQLManager.EditorToolType.Palette) {
                     xmlId = dataReader.GetInt32("id");
                     xmlContent = dataReader.GetString("xmlContent");
                  } else if (toolType == EditorSQLManager.EditorToolType.Treasure_Drops || toolType == EditorSQLManager.EditorToolType.Quest) {
                     xmlId = dataReader.GetInt32("xmlId");
                     xmlContent = dataReader.GetString("xmlContent");
                  } else if (toolType == EditorSQLManager.EditorToolType.ItemDefinitions) {
                     xmlId = dataReader.GetInt32("id");
                     xmlContent = dataReader.GetString("serializedData");
                     addedContent = dataReader.GetInt32("category") + "[space]";
                  } else {
                     xmlId = dataReader.GetInt32("xml_id");
                     xmlContent = dataReader.GetString("xmlContent"); ;
                  }

                  content += xmlId + "[space]" + addedContent + xmlContent + "[next]\n";
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + toolType + " : " + tableName + " : " + e.ToString());
      }

      return content;
   }

   public static new List<RawPaletteToolData> getPaletteXmlContent (string tableName) {
      List<RawPaletteToolData> newPaletteDataList = new List<RawPaletteToolData>();

      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM global." + tableName, conn)) {

            conn.Open();
            cmd.Prepare();

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  string xmlContent = dataReader.GetString("xmlContent");
                  int xmlId = dataReader.GetInt32("id");
                  string subcategory = dataReader.GetString("subcategory");
                  int tagId = dataReader.GetInt32("tagId");

                  newPaletteDataList.Add(new RawPaletteToolData {
                     xmlData = xmlContent,
                     subcategory = subcategory,
                     tagId = tagId,
                     xmlId = xmlId
                  });
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + tableName + " : " + e.ToString());
      }

      return newPaletteDataList;
   }

   public static new int getLatestXmlVersion () {
      int latestVersion = 0;

      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT version FROM global.xml_status where id = 1", conn)) {

            conn.Open();
            cmd.Prepare();

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  latestVersion = dataReader.GetInt32("version");
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return latestVersion;
   }

   public static new string getLastUpdate (EditorSQLManager.EditorToolType editorType) {
      string updateContent = "";
      string tableName = EditorSQLManager.getSqlTable(editorType);
      string lastUserUpdateKey = editorType == EditorSQLManager.EditorToolType.Palette ? "lastUpdate" : "lastUserUpdate";

      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT " + lastUserUpdateKey + " FROM global." + tableName + " order by " + lastUserUpdateKey + " DESC limit 1", conn)) {

            conn.Open();
            cmd.Prepare();

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  string lastUserUpdate = dataReader.GetString(lastUserUpdateKey);
                  updateContent = tableName + "[space]" + lastUserUpdate + "[next]\n";
               }
            }
         }
      } catch (Exception e) {
         D.error("Request Data was: " + editorType);
         D.error("MySQL Error: " + e.ToString());
      }

      return updateContent;
   }

   #endregion

   #region Server Communications

   public static new ChatInfo getLatestChatInfo () {
      ChatInfo latestChatInfo = new ChatInfo();
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT chtId, time FROM arcane.chat_log order by time desc limit 1", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@chatType", 12);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  int chatId = dataReader.GetInt32("chtId");
                  DateTime time = dataReader.GetDateTime("time");
                  ChatInfo info = new ChatInfo(chatId, "", time, ChatInfo.Type.Global);
                  latestChatInfo = info;
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
      return latestChatInfo;
   }

   #endregion

   #region Abilities

   public static new bool hasAbility (int userId, int abilityId) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT count(*) as itemCount FROM ability_table_v2 WHERE (userID=@userID and abilityId=@abilityId)", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@userID", userId);
            cmd.Parameters.AddWithValue("@abilityId", abilityId);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  int itemCount = DataUtil.getInt(dataReader, "itemCount");
                  if (itemCount < 1) {
                     return false;
                  } else {
                     return true;
                  }
               }
            }
            return true;
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
         return false;
      }
   }

   public static new void updateAbilitySlot (int userID, int abilityId, int slotNumber) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "UPDATE ability_table_v2 SET abilityEquipSlot = @abilityEquipSlot WHERE abilityId = @abilityId and userID = @userID", conn)) {
            conn.Open();
            cmd.Prepare();

            cmd.Parameters.AddWithValue("@userID", userID);
            cmd.Parameters.AddWithValue("@abilityId", abilityId);
            cmd.Parameters.AddWithValue("@abilityEquipSlot", slotNumber);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new void updateAbilitiesData (int userID, AbilitySQLData abilityData) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "INSERT INTO ability_table_v2 (userID, abilityName, abilityId, abilityLevel, abilityDescription, abilityEquipSlot, abilityType) " +
            "VALUES(@userID, @abilityName, @abilityId, @abilityLevel, @abilityDescription, @abilityEquipSlot, @abilityType) " +
            "ON DUPLICATE KEY UPDATE abilityLevel = @abilityLevel, abilityEquipSlot = @abilityEquipSlot", conn)) {

            conn.Open();
            cmd.Prepare();

            cmd.Parameters.AddWithValue("@userID", userID);
            cmd.Parameters.AddWithValue("@abilityName", abilityData.name);
            cmd.Parameters.AddWithValue("@abilityId", abilityData.abilityID);
            cmd.Parameters.AddWithValue("@abilityLevel", abilityData.abilityLevel);
            cmd.Parameters.AddWithValue("@abilityDescription", abilityData.description);
            cmd.Parameters.AddWithValue("@abilityEquipSlot", abilityData.equipSlotIndex);
            cmd.Parameters.AddWithValue("@abilityType", abilityData.abilityType);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   #endregion

   #region Achievements

   public static new List<AchievementData> getAchievementData (int userID, ActionType actionType) {
      List<AchievementData> achievementTypeList = new List<AchievementData>();
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM achievement_data WHERE (userID=@userID AND actionTypeId=@actionTypeId)", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@userID", userID);
            cmd.Parameters.AddWithValue("@actionTypeId", (int) actionType);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  AchievementData achievement = new AchievementData(dataReader);
                  achievementTypeList.Add(achievement);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return achievementTypeList;
   }

   public static new void updateAchievementData (AchievementData achievementData, int userID, bool isCompleted, int addedCount = 0) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "INSERT INTO achievement_data (userID, tier, actionTypeId, achievementName, achievementUniqueID, achievementDescription, achievementCount, achievementItemTypeID, achievementItemCategoryID, isCompleted) " +
            "VALUES(@userID, @tier, @actionTypeId, @achievementName, @achievementUniqueID, @achievementDescription, @achievementCount, @achievementItemTypeID, @achievementItemCategoryID, @isCompleted) " +
            "ON DUPLICATE KEY UPDATE achievementCount = achievementCount + " + addedCount + ", isCompleted = @isCompleted", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@userID", userID);
            cmd.Parameters.AddWithValue("@actionTypeId", (int) achievementData.actionType);
            cmd.Parameters.AddWithValue("@achievementName", achievementData.achievementName);
            cmd.Parameters.AddWithValue("@achievementUniqueID", achievementData.achievementUniqueID);
            cmd.Parameters.AddWithValue("@achievementDescription", achievementData.achievementDescription);
            cmd.Parameters.AddWithValue("@achievementCount", achievementData.count);
            cmd.Parameters.AddWithValue("@achievementItemTypeID", (int) achievementData.itemType);
            cmd.Parameters.AddWithValue("@achievementItemCategoryID", (int) achievementData.itemCategory);
            cmd.Parameters.AddWithValue("@isCompleted", isCompleted == true ? 1 : 0);
            cmd.Parameters.AddWithValue("@tier", achievementData.tier);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new List<AchievementData> getAchievementDataList (int userID) {
      List<AchievementData> achievementList = new List<AchievementData>();

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM achievement_data WHERE userID=@userID", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@userID", userID);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  AchievementData quest = new AchievementData(dataReader);
                  achievementList.Add(quest);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return achievementList;
   }

   #endregion

   #region Battler Abilities XML

   public static new void updateBattleAbilities (int skillId, string abilityName, string abilityXML, int abilityType) {
      string skillIdKey = "xml_id, ";
      string skillIdValue = "@xml_id, ";
      if (skillId < 0) {
         skillIdKey = "";
         skillIdValue = "";
      }

      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand(
            // Declaration of table elements
            "INSERT INTO global.ability_xml_v2 (" + skillIdKey + "xml_name, xmlContent, ability_type, creator_userID, default_ability, lastUserUpdate) " +
            "VALUES(" + skillIdValue + "@xml_name, @xmlContent, @ability_type, @creator_userID, @default_ability, NOW()) " +
            "ON DUPLICATE KEY UPDATE xmlContent = @xmlContent, ability_type = @ability_type, xmlContent = @xmlContent, xml_name = @xml_name, lastUserUpdate = NOW()", conn)) {

            conn.Open();
            cmd.Prepare();

            cmd.Parameters.AddWithValue("@xml_id", skillId);
            cmd.Parameters.AddWithValue("@xml_name", abilityName);
            cmd.Parameters.AddWithValue("@xmlContent", abilityXML);
            cmd.Parameters.AddWithValue("@ability_type", abilityType);
            cmd.Parameters.AddWithValue("@creator_userID", MasterToolAccountManager.self.currentAccountID);
            cmd.Parameters.AddWithValue("@default_ability", 0);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new void deleteBattleAbilityXML (int skillId) {
      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand("DELETE FROM global.ability_xml_v2 WHERE xml_id=@xml_id", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@xml_id", skillId);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new List<AbilityXMLContent> getBattleAbilityXML () {
      List<AbilityXMLContent> xmlContent = new List<AbilityXMLContent>();
      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM global.ability_xml_v2", conn)) {

            conn.Open();
            cmd.Prepare();

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  AbilityXMLContent newXML = new AbilityXMLContent();
                  newXML.abilityXML = dataReader.GetString("xmlContent");
                  newXML.abilityType = dataReader.GetInt32("ability_type");
                  newXML.abilityId = dataReader.GetInt32("xml_id");
                  newXML.ownderId = dataReader.GetInt32("creator_userID");
                  xmlContent.Add(newXML);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return xmlContent;
   }

   public static new List<AbilityXMLContent> getDefaultAbilities () {
      List<AbilityXMLContent> xmlContent = new List<AbilityXMLContent>();
      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM global.ability_xml_v2 WHERE (default_ability=@default_ability)", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@default_ability", 1);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  AbilityXMLContent newXML = new AbilityXMLContent();
                  newXML.abilityXML = dataReader.GetString("xmlContent");
                  newXML.abilityType = dataReader.GetInt32("ability_type");
                  newXML.abilityId = dataReader.GetInt32("xml_id");
                  newXML.ownderId = dataReader.GetInt32("creator_userID");
                  xmlContent.Add(newXML);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return xmlContent;
   }

   #endregion

   #region Treasure Drops XML

   public static new void updateBiomeTreasureDrops (int xmlId, string rawXmlContent, Biome.Type biomeType) {
      string xmlIdKey = "xmlId, ";
      string xmlIdValue = "@xmlId, ";
      if (xmlId < 0) {
         xmlIdKey = "";
         xmlIdValue = "";
      }
      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand(
            // Declaration of table elements
            "INSERT INTO global.treasure_drops_xml_v2 (" + xmlIdKey + "biomeType, xmlContent, lastUserUpdate) " +
            "VALUES(" + xmlIdValue + "@biomeType, @xmlContent, NOW()) " +
            "ON DUPLICATE KEY UPDATE biomeType = @biomeType, xmlContent = @xmlContent, lastUserUpdate = NOW()", conn)) {

            conn.Open();
            cmd.Prepare();

            cmd.Parameters.AddWithValue("@xmlId", xmlId);
            cmd.Parameters.AddWithValue("@biomeType", (int) biomeType);
            cmd.Parameters.AddWithValue("@xmlContent", rawXmlContent);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new List<XMLPair> getBiomeTreasureDrops () {
      List<XMLPair> xmlContent = new List<XMLPair>();
      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM global.treasure_drops_xml_v2", conn)) {
            conn.Open();
            cmd.Prepare();

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  XMLPair newXML = new XMLPair();
                  newXML.xmlId = dataReader.GetInt32("xmlId");
                  newXML.rawXmlData = dataReader.GetString("xmlContent");
                  xmlContent.Add(newXML);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return xmlContent;
   }

   #endregion

   #region Sound Effects

   public static new List<SoundEffect> getSoundEffects () {
      List<SoundEffect> effects = new List<SoundEffect>();

      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM global.soundeffects_v2", conn)) {

            conn.Open();
            cmd.Prepare();

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  SoundEffect newEffect = new SoundEffect();
                  newEffect.id = dataReader.GetInt32("id");
                  newEffect.name = dataReader.GetString("name");
                  newEffect.clipName = dataReader.GetString("clipName");
                  newEffect.minVolume = dataReader.GetFloat("minVolume");
                  newEffect.maxVolume = dataReader.GetFloat("maxVolume");
                  newEffect.minPitch = dataReader.GetFloat("minPitch");
                  newEffect.maxPitch = dataReader.GetFloat("maxPitch");
                  newEffect.offset = dataReader.GetFloat("offset");
                  effects.Add(newEffect);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return effects;
   }

   public static new void updateSoundEffect (SoundEffect effect) {
      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand(
            // Declaration of table elements
            "INSERT INTO global.soundeffects_v2 (id, name, clipName, minVolume, maxVolume, minPitch, maxPitch, offset, lastUserUpdate) " +
            "VALUES(@id, @name, @clipName, @minVolume, @maxVolume, @minPitch, @maxPitch, @offset, NOW()) " +
            "ON DUPLICATE KEY UPDATE id = @id, name = @name, clipName = @clipName, minVolume = @minVolume, maxVolume = @maxVolume, minPitch = @minPitch, maxPitch = @maxPitch, offset = @offset, lastUserUpdate = NOW()", conn)) {

            conn.Open();
            cmd.Prepare();

            cmd.Parameters.AddWithValue("@id", effect.id);
            cmd.Parameters.AddWithValue("@name", effect.name);
            cmd.Parameters.AddWithValue("@clipName", effect.clipName);
            cmd.Parameters.AddWithValue("@minVolume", effect.minVolume);
            cmd.Parameters.AddWithValue("@maxVolume", effect.maxVolume);
            cmd.Parameters.AddWithValue("@minPitch", effect.minPitch);
            cmd.Parameters.AddWithValue("@maxPitch", effect.maxPitch);
            cmd.Parameters.AddWithValue("@offset", effect.offset);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         UnityEngine.Debug.LogError("Error is: " + e.ToString());
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new void deleteSoundEffect (SoundEffect effect) {
      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand("DELETE FROM global.soundeffects_v2 WHERE id=@id", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@id", effect.id);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   #endregion

   #region NPC Quest and Relationship

   public static new void createNPCRelationship (int npcId, int userId, int friendshipLevel) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "INSERT INTO npc_relationship (npcId, usrId, friendshipLevel) " +
            "VALUES (@npcId, @usrId, @friendshipLevel)", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@npcId", npcId);
            cmd.Parameters.AddWithValue("@usrId", userId);
            cmd.Parameters.AddWithValue("@friendshipLevel", friendshipLevel);

            // Execute the command
            cmd.ExecuteNonQuery();

         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new int getFriendshipLevel (int npcId, int userId) {

      int friendshipLevel = -1;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT friendshipLevel FROM npc_relationship WHERE npcId=@npcId AND usrId=@usrId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@npcId", npcId);
            cmd.Parameters.AddWithValue("@usrId", userId);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  friendshipLevel = DataUtil.getInt(dataReader, "friendshipLevel");
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return friendshipLevel;
   }

   public static new void updateNPCRelationship (int npcId, int userId, int friendshipLevel) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "UPDATE npc_relationship SET friendshipLevel=@friendshipLevel WHERE npcId=@npcId AND usrId=@usrId", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@npcId", npcId);
            cmd.Parameters.AddWithValue("@usrId", userId);
            cmd.Parameters.AddWithValue("@friendshipLevel", friendshipLevel);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new void createQuestStatus (int npcId, int userId, int questId, int questNodeId) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "INSERT INTO quest_status_v3 (npcId, usrId, questId, questNodeId) " +
            "VALUES (@npcId, @usrId, @questId, @questNodeId)", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@npcId", npcId);
            cmd.Parameters.AddWithValue("@usrId", userId);
            cmd.Parameters.AddWithValue("@questId", questId);
            cmd.Parameters.AddWithValue("@questNodeId", questNodeId);

            // Execute the command
            cmd.ExecuteNonQuery();

         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new List<Item> getRequiredItems (List<Item> itemList, int usrId) {
      List<Item> newItemList = new List<Item>();
      List<string> categoryList = new List<string>();
      List<string> typeList = new List<string>();

      string itemGroup = "";
      int index = 0;
      foreach (Item itemData in itemList) {
         if (itemData.category == Item.Category.Blueprint) {
            CraftableItemRequirements craftingData = CraftingManager.self.getCraftableData(itemData.itemTypeId);
            categoryList.Add(((int) itemData.category).ToString());
            typeList.Add((craftingData.resultItem.itemTypeId).ToString());
         } else {
            categoryList.Add(((int) itemData.category).ToString());
            typeList.Add((itemData.itemTypeId).ToString());
         }
         if (index > 0) {
            itemGroup += " or ";
         }
         itemGroup += "(itmCategory = @itmCategory_" + index + " AND itmType = @itmType_" + index + ")";
         index++;
      }

      string cmdText = "SELECT * FROM items WHERE usrId=@usrId AND (" + itemGroup + ")";
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(cmdText, conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrId", usrId);
            for (int i = 0; i < itemList.Count; i++) {
               cmd.Parameters.AddWithValue("@itmCategory_" + i, categoryList[i]);
               cmd.Parameters.AddWithValue("@itmType_" + i, typeList[i]);
            }

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  int itemId = dataReader.GetInt32("itmId");
                  Item.Category itemCategory = (Item.Category) dataReader.GetInt32("itmCategory");
                  int itemTypeId = dataReader.GetInt32("itmType");
                  string palettes = dataReader.GetString("itmPalettes");
                  string data = dataReader.GetString("itmData");
                  int count = dataReader.GetInt32("itmCount");

                  // Create an Item instance of the proper class, and then add it to the list
                  Item item = new Item(itemId, itemCategory, itemTypeId, count, palettes, data);
                  newItemList.Add(item.getCastItem());
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
      return newItemList;
   }

   public static new void updateQuestStatus (int npcId, int userId, int questId, int questNodeId, int dialogueId) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "INSERT INTO quest_status_v3 (npcId, usrId, questId, questNodeId, questDialogueId) " +
            "VALUES(@npcId, @usrId, @questId, @questNodeId, @questDialogueId) " +
            "ON DUPLICATE KEY UPDATE questNodeId=@questNodeId, questDialogueId=@questDialogueId", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@npcId", npcId);
            cmd.Parameters.AddWithValue("@usrId", userId);
            cmd.Parameters.AddWithValue("@questId", questId);
            cmd.Parameters.AddWithValue("@questNodeId", questNodeId);
            cmd.Parameters.AddWithValue("@questDialogueId", dialogueId);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new QuestStatusInfo getQuestStatus (int npcId, int userId, int questId, int questNodeId) {
      QuestStatusInfo questStatus = null;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM quest_status_v3 WHERE npcId=@npcId AND usrId=@usrId AND questId=@questId and questNodeId=@questNodeId", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@npcId", npcId);
            cmd.Parameters.AddWithValue("@usrId", userId);
            cmd.Parameters.AddWithValue("@questId", questId);
            cmd.Parameters.AddWithValue("@questNodeId", questNodeId);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  questStatus = new QuestStatusInfo(dataReader);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return questStatus;
   }

   public static new List<QuestStatusInfo> getQuestStatuses (int npcId, int userId) {
      List<QuestStatusInfo> questList = new List<QuestStatusInfo>();

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM quest_status_v3 WHERE npcId=@npcId AND usrId=@usrId ORDER BY questId", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@npcId", npcId);
            cmd.Parameters.AddWithValue("@usrId", userId);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  QuestStatusInfo quest = new QuestStatusInfo(dataReader);
                  questList.Add(quest);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return questList;
   }

   #endregion

   public static new List<SQLEntryNameClass> getSQLDataByName (EditorSQLManager.EditorToolType editorType) {
      List<SQLEntryNameClass> rawDataList = new List<SQLEntryNameClass>();

      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM global." + EditorSQLManager.getSQLTableByName(editorType), conn)) {

            conn.Open();
            cmd.Prepare();

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  SQLEntryNameClass newEntry = new SQLEntryNameClass(dataReader);
                  rawDataList.Add(newEntry);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
      return rawDataList;
   }

   public static new List<SQLEntryIDClass> getSQLDataByID (EditorSQLManager.EditorToolType editorType, EquipmentType equipmentType = EquipmentType.None) {
      List<SQLEntryIDClass> rawDataList = new List<SQLEntryIDClass>();

      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM global." + EditorSQLManager.getSQLTableByID(editorType, equipmentType), conn)) {

            conn.Open();
            cmd.Prepare();

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  if (editorType == EditorSQLManager.EditorToolType.Quest || editorType == EditorSQLManager.EditorToolType.Treasure_Drops) {
                     SQLEntryIDClass newEntry = new SQLEntryIDClass(dataReader, true);
                     rawDataList.Add(newEntry);
                  } else {
                     SQLEntryIDClass newEntry = new SQLEntryIDClass(dataReader, false);
                     rawDataList.Add(newEntry);
                  }
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
      return rawDataList;
   }

   #region Crops XML Data

   public static new void updateCropsXML (string rawData, int xmlId, int cropsType, bool isEnabled, string cropsName) {
      string xmlIdKey = "xml_id, ";
      string xmlIdValue = "@xml_id, ";

      // If this is a newly created data, let sql table auto generate id
      if (xmlId < 0) {
         xmlIdKey = "";
         xmlIdValue = "";
      }

      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand(
            // Declaration of table elements
            "INSERT INTO global.crops_xml_v1 (" + xmlIdKey + "xml_name, xmlContent, creator_userID, is_enabled, crops_type, lastUserUpdate) " +
            "VALUES(" + xmlIdValue + "@xml_name, @xmlContent, @creator_userID, @is_enabled, @crops_type, NOW()) " +
            "ON DUPLICATE KEY UPDATE xmlContent = @xmlContent, crops_type = @crops_type, is_enabled = @is_enabled, xml_name = @xml_name, lastUserUpdate = NOW()", conn)) {

            conn.Open();
            cmd.Prepare();

            cmd.Parameters.AddWithValue("@xml_id", xmlId);
            cmd.Parameters.AddWithValue("@xml_name", cropsName);
            cmd.Parameters.AddWithValue("@xmlContent", rawData);
            cmd.Parameters.AddWithValue("@crops_type", cropsType);
            cmd.Parameters.AddWithValue("@is_enabled", isEnabled);
            cmd.Parameters.AddWithValue("@creator_userID", MasterToolAccountManager.self.currentAccountID);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new List<XMLPair> getCropsXML () {
      List<XMLPair> rawDataList = new List<XMLPair>();
      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM global.crops_xml_v1", conn)) {

            conn.Open();
            cmd.Prepare();

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  XMLPair newPair = new XMLPair {
                     isEnabled = dataReader.GetInt32("is_enabled") == 0 ? false : true,
                     xmlId = dataReader.GetInt32("xml_id"),
                     rawXmlData = dataReader.GetString("xmlContent"),
                     xmlOwnerId = dataReader.GetInt32("creator_userID")
                  };
                  rawDataList.Add(newPair);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
      return new List<XMLPair>(rawDataList);
   }

   public static new void deleteCropsXML (int xmlId) {
      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand("DELETE FROM global.crops_xml_v1 WHERE xml_id=@xml_id", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@xml_id", xmlId);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   #endregion

   #region Ship Ability XML Data

   public static new void updateShipAbilityXML (string rawData, string shipAbilityName, int xmlId) {
      string xml_id_key = "xml_id, ";
      string xml_id_value = "@xml_id, ";

      if (xmlId < 0) {
         xml_id_key = "";
         xml_id_value = "";
      }

      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand(
            // Declaration of table elements
            "INSERT INTO global.ship_ability_xml_v2 (" + xml_id_key + "xml_name, xmlContent, creator_userID, lastUserUpdate) " +
            "VALUES(" + xml_id_value + "@xml_name, @xmlContent, @creator_userID, NOW()) " +
            "ON DUPLICATE KEY UPDATE xml_name = @xml_name, xmlContent = @xmlContent, lastUserUpdate = NOW()", conn)) {

            conn.Open();
            cmd.Prepare();

            cmd.Parameters.AddWithValue("@xml_name", shipAbilityName);
            cmd.Parameters.AddWithValue("@xmlContent", rawData);
            cmd.Parameters.AddWithValue("@xml_id", xmlId);
            cmd.Parameters.AddWithValue("@creator_userID", MasterToolAccountManager.self.currentAccountID);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new List<XMLPair> getShipAbilityXML () {
      List<XMLPair> rawDataList = new List<XMLPair>();
      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM global.ship_ability_xml_v2", conn)) {

            conn.Open();
            cmd.Prepare();

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  XMLPair xmlPair = new XMLPair {
                     rawXmlData = dataReader.GetString("xmlContent"),
                     xmlId = dataReader.GetInt32("xml_id"),
                     isEnabled = dataReader.GetInt32("isActive") == 0 ? false : true,
                     xmlOwnerId = dataReader.GetInt32("creator_userID"),
                  };
                  rawDataList.Add(xmlPair);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
      return rawDataList;
   }

   public static new void deleteShipAbilityXML (int xmlId) {
      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand("DELETE FROM global.ship_ability_xml_v2 WHERE xml_id=@xml_id", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@xml_id", xmlId);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   #endregion

   #region Monster XML Data

   public static new void updateLandMonsterXML (string rawData, int typeIndex, Enemy.Type enemyType, string battlerName, bool isActive) {
      string xml_id_key = "xml_id, ";
      string xml_id_value = "@xml_id, ";

      // If this is a newly created data, let sql table auto generate id
      if (typeIndex < 0) {
         xml_id_key = "";
         xml_id_value = "";
      }

      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand(
            // Declaration of table elements
            "INSERT INTO global.land_monster_xml_v3 (" + xml_id_key + "xmlContent, creator_userID, monster_type, monster_name, isActive, lastUserUpdate) " +
            "VALUES(" + xml_id_value + "@xmlContent, @creator_userID, @monster_type, @monster_name, @isActive, NOW()) " +
            "ON DUPLICATE KEY UPDATE xmlContent = @xmlContent, monster_type = @monster_type, monster_name = @monster_name, isActive = @isActive, lastUserUpdate = NOW()", conn)) {

            conn.Open();
            cmd.Prepare();

            cmd.Parameters.AddWithValue("@xml_id", typeIndex);
            cmd.Parameters.AddWithValue("@xmlContent", rawData);
            cmd.Parameters.AddWithValue("@monster_type", enemyType.ToString());
            cmd.Parameters.AddWithValue("@monster_name", battlerName);
            cmd.Parameters.AddWithValue("@isActive", isActive);
            cmd.Parameters.AddWithValue("@creator_userID", MasterToolAccountManager.self.currentAccountID);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new List<XMLPair> getLandMonsterXML () {
      List<XMLPair> rawDataList = new List<XMLPair>();
      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM global.land_monster_xml_v3", conn)) {

            conn.Open();
            cmd.Prepare();

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  XMLPair newXMLPair = new XMLPair {
                     xmlId = dataReader.GetInt32("xml_id"),
                     rawXmlData = dataReader.GetString("xmlContent"),
                     isEnabled = dataReader.GetInt32("isActive") == 0 ? false : true
                  };

                  rawDataList.Add(newXMLPair);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
      return new List<XMLPair>(rawDataList);
   }

   public static new void deleteLandmonsterXML (int typeID) {
      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand("DELETE FROM global.land_monster_xml_v3 WHERE xml_id=@xml_id", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@xml_id", typeID);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   #endregion

   #region SeaMonster XML Data

   public static new void updateSeaMonsterXML (string rawData, int typeIndex, SeaMonsterEntity.Type enemyType, string battlerName, bool isActive) {
      string xml_id_key = "xml_id, ";
      string xml_id_value = "@xml_id, ";

      // If this is a newly created data, let sql table auto generate id
      if (typeIndex < 0) {
         xml_id_key = "";
         xml_id_value = "";
      }

      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand(
            // Declaration of table elements
            "INSERT INTO global.sea_monster_xml_v2 (" + xml_id_key + "xmlContent, creator_userID, monster_type, monster_name, isActive, lastUserUpdate) " +
            "VALUES(" + xml_id_value + "@xmlContent, @creator_userID, @monster_type, @monster_name, @isActive, NOW()) " +
            "ON DUPLICATE KEY UPDATE xmlContent = @xmlContent, monster_type = @monster_type, monster_name = @monster_name, isActive = @isActive, lastUserUpdate = NOW()", conn)) {

            conn.Open();
            cmd.Prepare();

            cmd.Parameters.AddWithValue("@xml_id", typeIndex);
            cmd.Parameters.AddWithValue("@xmlContent", rawData);
            cmd.Parameters.AddWithValue("@monster_type", enemyType.ToString());
            cmd.Parameters.AddWithValue("@monster_name", battlerName);
            cmd.Parameters.AddWithValue("@isActive", isActive);
            cmd.Parameters.AddWithValue("@creator_userID", MasterToolAccountManager.self.currentAccountID);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new List<XMLPair> getSeaMonsterXML () {
      List<XMLPair> rawDataList = new List<XMLPair>();
      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM global.sea_monster_xml_v2", conn)) {

            conn.Open();
            cmd.Prepare();

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  XMLPair newXMLPair = new XMLPair {
                     xmlId = dataReader.GetInt32("xml_id"),
                     rawXmlData = dataReader.GetString("xmlContent"),
                     isEnabled = dataReader.GetInt32("isActive") == 0 ? false : true
                  };

                  rawDataList.Add(newXMLPair);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
      return new List<XMLPair>(rawDataList);
   }

   public static new void deleteSeamonsterXML (int typeID) {
      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand("DELETE FROM global.sea_monster_xml_v2 WHERE xml_id=@xml_id", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@xml_id", typeID);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   #endregion

   #region NPC Quest XML Data

   public static new void updateNPCQuestXML (string rawData, int typeIndex, string xmlName, int isActive) {
      string xmlIdKey = "xmlId, ";
      string xmlIdValue = "@xmlId, ";

      // If this is a newly created data, let sql table auto generate id
      if (typeIndex < 0) {
         xmlIdKey = "";
         xmlIdValue = "";
      }

      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand(
            // Declaration of table elements
            "INSERT INTO global.quest_data_xml_v1 (" + xmlIdKey + "xmlContent, creatorUserID, lastUserUpdate, xmlName, isActive) " +
            "VALUES(" + xmlIdValue + "@xmlContent, @creatorUserID, lastUserUpdate = NOW(), @xmlName, @isActive) " +
            "ON DUPLICATE KEY UPDATE xmlContent = @xmlContent, lastUserUpdate = NOW(), xmlName = @xmlName, isActive = @isActive", conn)) {

            conn.Open();
            cmd.Prepare();

            cmd.Parameters.AddWithValue("@xmlId", typeIndex);
            cmd.Parameters.AddWithValue("@xmlContent", rawData);
            cmd.Parameters.AddWithValue("@xmlName", xmlName);
            cmd.Parameters.AddWithValue("@isActive", isActive);
            if (MasterToolAccountManager.self != null) {
               cmd.Parameters.AddWithValue("@creatorUserID", MasterToolAccountManager.self.currentAccountID);
            } else {
               cmd.Parameters.AddWithValue("@creatorUserID", 0);
            }

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new List<XMLPair> getNPCQuestXML () {
      List<XMLPair> rawDataList = new List<XMLPair>();
      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM global.quest_data_xml_v1", conn)) {

            conn.Open();
            cmd.Prepare();

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  XMLPair newPair = new XMLPair {
                     isEnabled = dataReader.GetInt32("isActive") == 0 ? false : true,
                     rawXmlData = dataReader.GetString("xmlContent"),
                     xmlId = dataReader.GetInt32("xmlId"),
                     xmlOwnerId = dataReader.GetInt32("creatorUserID")
                  };
                  rawDataList.Add(newPair);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
      return rawDataList;
   }

   public static new void deleteNPCQuestXML (int typeID) {
      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand("DELETE FROM global.quest_data_xml_v1 WHERE xmlId=@xmlId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@xmlId", typeID);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   #endregion

   #region NPC XML Data

   public static new void updateNPCXML (string rawData, int typeIndex) {
      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand(
            // Declaration of table elements
            "INSERT INTO global.npc_xml (xml_id, xmlContent, creator_userID, lastUserUpdate) " +
            "VALUES(@xml_id, @xmlContent, @creator_userID, lastUserUpdate = NOW()) " +
            "ON DUPLICATE KEY UPDATE xmlContent = @xmlContent, lastUserUpdate = NOW()", conn)) {

            conn.Open();
            cmd.Prepare();

            cmd.Parameters.AddWithValue("@xml_id", typeIndex);
            cmd.Parameters.AddWithValue("@xmlContent", rawData);
            cmd.Parameters.AddWithValue("@creator_userID", MasterToolAccountManager.self.currentAccountID);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new List<string> getNPCXML () {
      List<string> rawDataList = new List<string>();
      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM global.npc_xml", conn)) {

            conn.Open();
            cmd.Prepare();

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  rawDataList.Add(dataReader.GetString("xmlContent"));
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
      return new List<string>(rawDataList);
   }

   public static new void deleteNPCXML (int typeID) {
      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand("DELETE FROM global.npc_xml WHERE xml_id=@xml_id", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@xml_id", typeID);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   #endregion

   #region Custom Maps

   public static new void setCustomHouseBase (object command, int userId, int baseMapId) {
      MySqlCommand cmd = command as MySqlCommand;
      cmd.CommandText = "UPDATE users SET customHouseBase = @baseMapId WHERE usrId = @userId;";
      cmd.Parameters.AddWithValue("@userId", userId);
      cmd.Parameters.AddWithValue("@baseMapId", baseMapId);
      cmd.ExecuteNonQuery();
   }

   public static new void setCustomFarmBase (object command, int userId, int baseMapId) {
      MySqlCommand cmd = command as MySqlCommand;
      cmd.CommandText = "UPDATE users SET customFarmBase = @baseMapId WHERE usrId = @userId;";
      cmd.Parameters.AddWithValue("@userId", userId);
      cmd.Parameters.AddWithValue("@baseMapId", baseMapId);
      cmd.ExecuteNonQuery();
   }

   #endregion

   #region Map Customization

   public static new MapCustomizationData getMapCustomizationData (object command, int mapId, int userId) {
      MySqlCommand cmd = command as MySqlCommand;
      cmd.CommandText = "SELECT data FROM map_customization_changes WHERE map_id = @map_id AND user_id = @user_id;";
      cmd.Parameters.AddWithValue("@map_id", mapId);
      cmd.Parameters.AddWithValue("@user_id", userId);

      List<PrefabState> changes = new List<PrefabState>();
      using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
         while (dataReader.Read()) {
            changes.Add(PrefabState.deserialize(dataReader.GetString("data")));
         }
      }

      return new MapCustomizationData {
         mapId = mapId,
         userId = userId,
         prefabChanges = changes.ToArray()
      };
   }

   public static new PrefabState getMapCustomizationChanges (object command, int mapId, int userId, int prefabId) {
      MySqlCommand cmd = command as MySqlCommand;
      cmd.CommandText = "SELECT data FROM map_customization_changes WHERE map_id = @map_id AND user_id = @user_id AND prefab_id = @prefab_id;";
      cmd.Parameters.AddWithValue("@map_id", mapId);
      cmd.Parameters.AddWithValue("@user_id", userId);
      cmd.Parameters.AddWithValue("@prefab_id", prefabId);

      using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
         if (dataReader.Read()) {
            return PrefabState.deserialize(dataReader.GetString("data"));
         }
      }

      return new PrefabState { id = -1 };
   }

   public static new void setMapCustomizationChanges (object command, int mapId, int userId, PrefabState changes) {
      MySqlCommand cmd = command as MySqlCommand;
      cmd.CommandText = "INSERT INTO map_customization_changes(user_id, map_id, prefab_id, data) Values(@user_id, @map_id, @prefab_id, @data) " +
         "ON DUPLICATE KEY UPDATE data = @data;";
      cmd.Parameters.AddWithValue("@map_id", mapId);
      cmd.Parameters.AddWithValue("@user_id", userId);
      cmd.Parameters.AddWithValue("@prefab_id", changes.id);
      cmd.Parameters.AddWithValue("@data", changes.serialize());

      cmd.ExecuteNonQuery();
   }

   #endregion

   #region Map Editor Data

   public static new int getMapId (string areaKey) {
      string cmdText = "SELECT id FROM maps_v2 WHERE name = @areaKey;";
      using (MySqlConnection conn = getConnection())
      using (MySqlCommand cmd = new MySqlCommand(cmdText, conn)) {
         cmd.Parameters.AddWithValue("@areaKey", areaKey);
         conn.Open();
         cmd.Prepare();

         using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
            if (dataReader.Read()) {
               return dataReader.GetInt32("id");
            }
         }
      }

      return -1;
   }

   public static new List<Map> getMaps (object command) {
      MySqlCommand cmd = command as MySqlCommand;

      cmd.CommandText =
            "SELECT id, name, displayName, createdAt, creatorUserId, publishedVersion, sourceMapId, notes, " +
               "editorType, biome, specialType, accName, weatherEffectType " +
            "FROM maps_v2 " +
               "LEFT JOIN global.accounts ON maps_v2.creatorUserId = accId " +
            "ORDER BY name;";

      List<Map> result = new List<Map>();

      using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
         while (dataReader.Read()) {
            result.Add(new Map {
               id = dataReader.GetInt32("id"),
               name = dataReader.GetString("name"),
               displayName = dataReader.GetString("displayName"),
               createdAt = dataReader.GetDateTime("createdAt"),
               publishedVersion = dataReader.IsDBNull(dataReader.GetOrdinal("publishedVersion"))
                  ? -1
                  : dataReader.GetInt32("publishedVersion"),
               creatorID = dataReader.GetInt32("creatorUserId"),
               creatorName = dataReader.GetString("accName"),
               sourceMapId = dataReader.GetInt32("sourceMapId"),
               notes = dataReader.GetString("notes"),
               editorType = (EditorType) dataReader.GetInt32("editorType"),
               biome = (Biome.Type) dataReader.GetInt32("biome"),
               specialType = (Area.SpecialType) dataReader.GetInt32("specialType"),
               weatherEffectType = (WeatherEffectType) dataReader.GetInt32("weatherEffectType")
            });
         }
      }

      return result;
   }

   public static new string getMapInfo (string areaKey) {
      MapInfo mapInfo = null;

      string cmdText = "SELECT * FROM maps_v2 JOIN map_versions_v2 ON (maps_v2.id=map_versions_v2.mapId) WHERE (maps_v2.publishedVersion=map_versions_v2.version) AND maps_v2.name=@mapName";
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(cmdText, conn)) {
            cmd.Parameters.AddWithValue("@mapName", areaKey);
            conn.Open();
            cmd.Prepare();

            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               if (dataReader.Read()) {
                  string mapName = dataReader.GetString("name");
                  string gameData = dataReader.GetString("gameData");
                  int version = dataReader.GetInt32("publishedVersion");
                  mapInfo = new MapInfo(mapName, gameData, version);
               }
            }
         }
      } catch (Exception e) {
         D.debug("Failed to get Map info for: " + areaKey);
         D.error("MySQL Error: " + e.ToString());
      }

      return JsonUtility.ToJson(mapInfo);
   }

   public static new Dictionary<string, MapInfo> getLiveMaps () {
      Dictionary<string, MapInfo> maps = new Dictionary<string, MapInfo>();
      string cmdText = "SELECT * FROM maps_v2 JOIN map_versions_v2 ON (maps_v2.id=map_versions_v2.mapId) WHERE (maps_v2.publishedVersion=map_versions_v2.version)";

      using (MySqlConnection conn = getConnection())
      using (MySqlCommand cmd = new MySqlCommand(cmdText, conn)) {
         conn.Open();
         cmd.Prepare();

         using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
            while (dataReader.Read()) {
               string mapName = dataReader.GetString("name");
               string gameData = dataReader.GetString("gameData");
               int version = dataReader.GetInt32("publishedVersion");
               maps[mapName] = new MapInfo(mapName, gameData, version);
            }
         }
      }

      return maps;
   }

   public static new List<MapVersion> getMapVersions (Map map) {
      List<MapVersion> result = new List<MapVersion>();

      string cmdText = "SELECT version, createdAt, updatedAt " +
         "FROM map_versions_v2 " +
         "WHERE mapId = @id " +
         "ORDER BY updatedAt DESC;";

      using (MySqlConnection conn = getConnection())
      using (MySqlCommand cmd = new MySqlCommand(cmdText, conn)) {
         cmd.Parameters.AddWithValue("@id", map.id);
         conn.Open();
         cmd.Prepare();

         using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
            while (dataReader.Read()) {
               result.Add(new MapVersion {
                  mapId = map.id,
                  version = dataReader.GetInt32("version"),
                  createdAt = dataReader.GetDateTime("createdAt"),
                  updatedAt = dataReader.GetDateTime("updatedAt"),
                  map = map
               });
            }
         }
      }

      return result;
   }

   public static new string getMapVersionEditorData (MapVersion version) {
      string cmdText = "SELECT editorData from map_versions_v2 WHERE mapId = @id AND version = @version;";

      using (MySqlConnection conn = getConnection())
      using (MySqlCommand cmd = new MySqlCommand(cmdText, conn)) {
         conn.Open();
         cmd.Prepare();

         cmd.Parameters.AddWithValue("@id", version.mapId);
         cmd.Parameters.AddWithValue("@version", version.version);

         using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
            if (!dataReader.HasRows) {
               return null;
            } else {
               dataReader.Read();
               return dataReader.GetString("editorData");
            }
         }
      }
   }

   public static new MapVersion getLatestMapVersionEditor (Map map, bool infiniteCommandTimeout = false) {
      string cmdText = "SELECT version, createdAt, updatedAt, editorData " +
         "FROM map_versions_v2 WHERE mapId = @id AND version = (SELECT max(version) FROM map_versions_v2 WHERE mapId = @id);";

      using (MySqlConnection conn = getConnection())
      using (MySqlCommand cmd = new MySqlCommand(cmdText, conn)) {
         if (infiniteCommandTimeout) {
            cmd.CommandTimeout = 0;
         }

         conn.Open();
         cmd.Prepare();
         cmd.Parameters.AddWithValue("@id", map.id);

         using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
            if (!dataReader.HasRows) {
               return null;
            } else {
               dataReader.Read();
               return new MapVersion {
                  mapId = map.id,
                  version = dataReader.GetInt32("version"),
                  createdAt = dataReader.GetDateTime("createdAt"),
                  updatedAt = dataReader.GetDateTime("updatedAt"),
                  editorData = dataReader.GetString("editorData"),
                  map = map
               };
            }
         }
      }
   }

   public static new List<MapSpawn> getMapSpawns () {
      List<MapSpawn> result = new List<MapSpawn>();

      string cmdText = "SELECT mapid, maps_v2.name as mapName, map_spawns_v2.name as spawnName, mapVersion, posX, posY " +
         "FROM map_spawns_v2 JOIN maps_v2 ON maps_v2.id = map_spawns_v2.mapid " +
         "WHERE mapVersion = publishedVersion;";
      using (MySqlConnection conn = getConnection())
      using (MySqlCommand cmd = new MySqlCommand(cmdText, conn)) {
         conn.Open();
         cmd.Prepare();

         using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
            while (dataReader.Read()) {
               result.Add(new MapSpawn {
                  mapId = dataReader.GetInt32("mapid"),
                  mapName = dataReader.GetString("mapName"),
                  mapVersion = dataReader.GetInt32("mapVersion"),
                  name = dataReader.GetString("spawnName"),
                  posX = dataReader.GetFloat("posX"),
                  posY = dataReader.GetFloat("posY")
               });
            }
         }
      }

      return result;
   }

   public static new void createMap (MapVersion mapVersion) {
      using (MySqlConnection conn = getConnection())
      using (MySqlCommand cmd = conn.CreateCommand()) {
         conn.Open();
         MySqlTransaction transaction = conn.BeginTransaction();
         cmd.Transaction = transaction;
         cmd.Connection = conn;

         try {
            // Insert entry to maps
            cmd.CommandText = "INSERT INTO maps_v2(name, createdAt, creatorUserId, publishedVersion, editorType, biome, displayName) " +
               "VALUES(@name, @createdAt, @creatorID, @publishedVersion, @editorType, @biome, @name);";
            cmd.Parameters.AddWithValue("@name", mapVersion.map.name);
            cmd.Parameters.AddWithValue("@createdAt", mapVersion.map.createdAt);
            cmd.Parameters.AddWithValue("@creatorID", mapVersion.map.creatorID);
            cmd.Parameters.AddWithValue("@publishedVersion", mapVersion.map.publishedVersion);
            cmd.Parameters.AddWithValue("@editorType", (int) mapVersion.map.editorType);
            cmd.Parameters.AddWithValue("@biome", (int) mapVersion.map.biome);
            cmd.ExecuteNonQuery();

            long mapId = cmd.LastInsertedId;
            mapVersion.mapId = (int) mapId;
            mapVersion.map.id = (int) mapId;

            // Insert entry to map versions
            cmd.CommandText = "INSERT INTO map_versions_v2(mapId, version, createdAt, updatedAt, editorData, gameData) " +
               "VALUES(@mapId, @version, @createdAt, @updatedAt, @editorData, @gameData);";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@mapId", mapId);
            cmd.Parameters.AddWithValue("@version", mapVersion.version);
            cmd.Parameters.AddWithValue("@createdAt", mapVersion.createdAt);
            cmd.Parameters.AddWithValue("@updatedAt", mapVersion.updatedAt);
            cmd.Parameters.AddWithValue("@editorData", mapVersion.editorData);
            cmd.Parameters.AddWithValue("@gameData", mapVersion.gameData);
            cmd.ExecuteNonQuery();

            // Insert spawns
            cmd.CommandText = "INSERT INTO map_spawns_v2(mapId, mapVersion, name, posX, posY) " +
               "Values(@mapId, @mapVersion, @name, @posX, @posY);";
            foreach (MapSpawn spawn in mapVersion.spawns) {
               cmd.Parameters.Clear();
               cmd.Parameters.AddWithValue("@mapId", mapId);
               cmd.Parameters.AddWithValue("@mapVersion", spawn.mapVersion);
               cmd.Parameters.AddWithValue("@name", spawn.name);
               cmd.Parameters.AddWithValue("@posX", spawn.posX);
               cmd.Parameters.AddWithValue("@posY", spawn.posY);
               cmd.ExecuteNonQuery();
            }

            transaction.Commit();
         } catch (Exception e) {
            transaction.Rollback();
            throw e;
         }
      }
   }

   public static new void duplicateMapGroup (int mapId, int newCreatorId) {
      using (MySqlConnection conn = getConnection())
      using (MySqlCommand cmd = conn.CreateCommand()) {
         conn.Open();
         MySqlTransaction transaction = conn.BeginTransaction();
         cmd.Transaction = transaction;
         cmd.Connection = conn;

         try {
            // Find all children of the map
            List<int> childrenIds = getChildMapIds(cmd, mapId);
            cmd.Parameters.Clear();

            // Duplicate parent map
            int newParentId = duplicateMap(cmd, mapId, newCreatorId, 0);

            // Duplicate all child maps, attaching new parent to them
            foreach (int childId in childrenIds) {
               duplicateMap(cmd, childId, newCreatorId, newParentId);
            }

            transaction.Commit();
         } catch (Exception e) {
            transaction.Rollback();
            throw e;
         }
      }
   }

   private static List<int> getChildMapIds (MySqlCommand cmd, int parentMapId) {
      List<int> childrenIds = new List<int>();

      cmd.Parameters.Clear();
      cmd.CommandText = "SELECT id FROM maps_v2 WHERE sourceMapId = @mapID;";
      cmd.Parameters.AddWithValue("@mapID", parentMapId);

      using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
         while (dataReader.Read()) {
            childrenIds.Add(dataReader.GetInt32("id"));
         }
      }

      return childrenIds;
   }

   private static int duplicateMap (MySqlCommand cmd, int mapId, int newCreatorId, int newSourceMapId) {
      int resultId = -1;

      // Create a new map entry with a random name ending
      cmd.CommandText = "INSERT INTO maps_v2(name, createdAt, creatorUserId, publishedVersion, editorType, biome, displayName, notes, specialType, sourceMapId) " +
         "SELECT CONCAT(LEFT(name, 24), ' ', FLOOR(RAND() * (99999 - 10001)) + 10000), @nowDate, @creatorID, @publishedVersion, editorType, biome, name, notes, specialType, @sourceMapID " +
            "FROM maps_v2 WHERE id = @mapID;";
      cmd.Parameters.Clear();
      cmd.Parameters.AddWithValue("@mapID", mapId);
      cmd.Parameters.AddWithValue("@nowDate", DateTime.Now);
      cmd.Parameters.AddWithValue("@creatorID", newCreatorId);
      cmd.Parameters.AddWithValue("@sourceMapID", newSourceMapId);
      cmd.Parameters.AddWithValue("@publishedVersion", -1);
      cmd.ExecuteNonQuery();

      resultId = (int) cmd.LastInsertedId;

      // Insert entry to map versions
      cmd.CommandText = "INSERT INTO map_versions_v2(mapId, version, createdAt, updatedAt, editorData, gameData) " +
         "SELECT @resultID, version, @nowDate, @nowDate, editorData, gameData " +
            "FROM map_versions_v2 WHERE mapId = @mapID;";
      cmd.Parameters.AddWithValue("@resultID", resultId);
      cmd.ExecuteNonQuery();

      // Insert spawns
      cmd.CommandText = "INSERT INTO map_spawns_v2(mapId, mapVersion, name, posX, posY) " +
         "SELECT @resultID, mapVersion, name, posX, posY " +
            "FROM map_spawns_v2 WHERE mapId = @mapID;";
      cmd.ExecuteNonQuery();

      return resultId;
   }

   public static new void updateMapDetails (Map map) {
      string cmdText = "UPDATE maps_v2 " +
         "SET name = @name, sourceMapId = @sourceId, notes = @notes, specialType = @specialType, displayName = @displayName, weatherEffectType = @weatherEffect " +
         "WHERE id = @mapId;";
      using (MySqlConnection conn = getConnection())
      using (MySqlCommand cmd = new MySqlCommand(cmdText, conn)) {
         conn.Open();
         cmd.Prepare();

         cmd.Parameters.AddWithValue("@mapId", map.id);
         cmd.Parameters.AddWithValue("@name", map.name);
         cmd.Parameters.AddWithValue("@sourceId", map.sourceMapId);
         cmd.Parameters.AddWithValue("@notes", map.notes);
         cmd.Parameters.AddWithValue("@specialType", map.specialType);
         cmd.Parameters.AddWithValue("@displayName", map.displayName);
         cmd.Parameters.AddWithValue("@weatherEffect", map.weatherEffectType);

         // Execute the command
         cmd.ExecuteNonQuery();
      }
   }

   public static new MapVersion createNewMapVersion (MapVersion mapVersion) {
      using (MySqlConnection conn = getConnection())
      using (MySqlCommand cmd = conn.CreateCommand()) {
         conn.Open();
         MySqlTransaction transaction = conn.BeginTransaction();
         cmd.Transaction = transaction;
         cmd.Connection = conn;

         try {
            // Update biome of map entry
            cmd.Parameters.AddWithValue("@mapId", mapVersion.mapId);
            cmd.Parameters.AddWithValue("@biome", (int) mapVersion.map.biome);
            cmd.CommandText = "UPDATE maps_v2 SET biome = @biome WHERE id = @mapId;";
            cmd.ExecuteNonQuery();

            // Fetch latest version of a map
            cmd.CommandText = "SELECT IFNULL(MAX(version), -1) as latestVersion FROM map_versions_v2 WHERE mapId = @mapId;";

            int latestVersion = -1;
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               dataReader.Read();
               latestVersion = dataReader.GetInt32("latestVersion");
            }

            // Clone map into the a new map, setting new version number
            MapVersion result = new MapVersion {
               map = mapVersion.map,
               spawns = mapVersion.spawns,
               mapId = mapVersion.mapId,
               version = latestVersion + 1,
               createdAt = mapVersion.createdAt,
               updatedAt = mapVersion.updatedAt,
               editorData = mapVersion.editorData,
               gameData = mapVersion.gameData
            };

            // Insert the new version 
            cmd.CommandText = "INSERT INTO map_versions_v2(mapId, version, createdAt, updatedAt, editorData, gameData) " +
            "VALUES(@mapId, @version, @createdAt, @updatedAt, @editorData, @gameData);";

            cmd.Parameters.AddWithValue("@version", result.version);
            cmd.Parameters.AddWithValue("@createdAt", result.createdAt);
            cmd.Parameters.AddWithValue("@updatedAt", result.updatedAt);
            cmd.Parameters.AddWithValue("@editorData", result.editorData);
            cmd.Parameters.AddWithValue("@gameData", result.gameData);
            cmd.ExecuteNonQuery();

            // Insert spawns
            cmd.CommandText = "INSERT INTO map_spawns_v2(mapId, mapVersion, name, posX, posY) " +
               "Values(@mapId, @mapVersion, @name, @posX, @posY);";
            foreach (MapSpawn spawn in result.spawns) {
               cmd.Parameters.Clear();
               cmd.Parameters.AddWithValue("@mapId", spawn.mapId);
               cmd.Parameters.AddWithValue("@mapVersion", result.version);
               cmd.Parameters.AddWithValue("@name", spawn.name);
               cmd.Parameters.AddWithValue("@posX", spawn.posX);
               cmd.Parameters.AddWithValue("@posY", spawn.posY);
               cmd.ExecuteNonQuery();
            }

            transaction.Commit();
            return result;
         } catch (Exception e) {
            transaction.Rollback();
            throw e;
         }
      }
   }

   public static new void updateMapVersion (MapVersion mapVersion, bool infiniteCommandTimeout = false) {
      using (MySqlConnection conn = getConnection())
      using (MySqlCommand cmd = conn.CreateCommand()) {
         if (infiniteCommandTimeout) {
            cmd.CommandTimeout = 0;
         }

         conn.Open();
         MySqlTransaction transaction = conn.BeginTransaction();
         cmd.Transaction = transaction;
         cmd.Connection = conn;

         try {
            // Update editor type and biome
            cmd.Parameters.AddWithValue("@mapId", mapVersion.mapId);
            cmd.Parameters.AddWithValue("@editorType", (int) mapVersion.map.editorType);
            cmd.Parameters.AddWithValue("@biome", (int) mapVersion.map.biome);
            cmd.CommandText = "UPDATE maps_v2 SET editorType = @editorType, biome = @biome WHERE id = @mapId;";
            cmd.ExecuteNonQuery();

            // Update entry in map versions
            cmd.CommandText = "UPDATE map_versions_v2 SET " +
               "createdAt = @createdAt, " +
               "updatedAt = @updatedAt, " +
               "editorData = @editorData, " +
               "gameData = @gameData " +
               "WHERE mapId = @mapId AND version = @version;";

            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@mapId", mapVersion.mapId);
            cmd.Parameters.AddWithValue("@version", mapVersion.version);
            cmd.Parameters.AddWithValue("@createdAt", mapVersion.createdAt);
            cmd.Parameters.AddWithValue("@updatedAt", mapVersion.updatedAt);
            cmd.Parameters.AddWithValue("@editorData", mapVersion.editorData);
            cmd.Parameters.AddWithValue("@gameData", mapVersion.gameData);
            cmd.ExecuteNonQuery();

            // Delete old spawns
            cmd.CommandText = "DELETE FROM map_spawns_v2 WHERE mapId = @mapId AND mapVersion = @version;";
            cmd.ExecuteNonQuery();

            // Insert spawns
            cmd.CommandText = "INSERT INTO map_spawns_v2(mapId, mapVersion, name, posX, posY) " +
               "Values(@mapId, @mapVersion, @name, @posX, @posY);";
            foreach (MapSpawn spawn in mapVersion.spawns) {
               cmd.Parameters.Clear();
               cmd.Parameters.AddWithValue("@mapId", spawn.mapId);
               cmd.Parameters.AddWithValue("@mapVersion", spawn.mapVersion);
               cmd.Parameters.AddWithValue("@name", spawn.name);
               cmd.Parameters.AddWithValue("@posX", spawn.posX);
               cmd.Parameters.AddWithValue("@posY", spawn.posY);
               cmd.ExecuteNonQuery();
            }

            transaction.Commit();
         } catch (Exception e) {
            transaction.Rollback();
            throw e;
         }
      }
   }

   public static new void deleteMap (int id) {
      using (MySqlConnection conn = getConnection())
      using (MySqlCommand cmd = conn.CreateCommand()) {
         conn.Open();
         MySqlTransaction transaction = conn.BeginTransaction();
         cmd.Transaction = transaction;
         cmd.Connection = conn;

         try {
            deleteMap(cmd, id);

            transaction.Commit();
         } catch (Exception e) {
            transaction.Rollback();
            throw e;
         }
      }
   }

   public static new void deleteMapGroup (int mapId) {
      using (MySqlConnection conn = getConnection())
      using (MySqlCommand cmd = conn.CreateCommand()) {
         conn.Open();
         MySqlTransaction transaction = conn.BeginTransaction();
         cmd.Transaction = transaction;
         cmd.Connection = conn;

         try {
            List<int> ids = new List<int> { mapId };

            // Find all children of a map
            ids.AddRange(getChildMapIds(cmd, mapId));

            // Delete parent and children
            foreach (int id in ids) {
               deleteMap(cmd, id);
            }

            transaction.Commit();
         } catch (Exception e) {
            transaction.Rollback();
            throw e;
         }
      }
   }

   private static void deleteMap (MySqlCommand cmd, int mapId) {
      cmd.Parameters.Clear();
      cmd.Parameters.AddWithValue("@id", mapId);

      // Delete map entry
      cmd.CommandText = "DELETE FROM maps_v2 WHERE id = @id;";
      cmd.ExecuteNonQuery();

      // Delete all version entries
      cmd.CommandText = "DELETE FROM map_versions_v2 WHERE mapId = @id;";
      cmd.ExecuteNonQuery();

      // Delete all spawn entries
      cmd.CommandText = "DELETE FROM map_spawns_v2 WHERE mapId = @id;";
      cmd.ExecuteNonQuery();
   }

   public static new void deleteMapVersion (MapVersion version) {
      using (MySqlConnection conn = getConnection())
      using (MySqlCommand cmd = conn.CreateCommand()) {
         conn.Open();
         MySqlTransaction transaction = conn.BeginTransaction();
         cmd.Transaction = transaction;
         cmd.Connection = conn;

         try {
            cmd.Parameters.AddWithValue("@mapId", version.mapId);
            cmd.Parameters.AddWithValue("@version", version.version);

            // Unpublish version
            cmd.CommandText = "UPDATE maps_v2 SET publishedVersion = NULL WHERE id = @mapId and publishedVersion = @version";
            cmd.ExecuteNonQuery();

            // Delete version entry
            cmd.CommandText = "DELETE FROM map_versions_v2 WHERE mapId = @mapId AND version = @version;";
            cmd.ExecuteNonQuery();

            // Delete all spawn entries
            cmd.CommandText = "DELETE FROM map_spawns_v2 WHERE mapId = @mapId AND mapVersion = @version;";
            cmd.ExecuteNonQuery();

            transaction.Commit();
         } catch (Exception e) {
            transaction.Rollback();
            throw e;
         }
      }
   }

   public static new void publishLatestVersionForAllGroup (int mapId) {
      using (MySqlConnection conn = getConnection())
      using (MySqlCommand cmd = conn.CreateCommand()) {
         conn.Open();
         MySqlTransaction transaction = conn.BeginTransaction();
         cmd.Transaction = transaction;
         cmd.Connection = conn;

         try {
            List<int> ids = new List<int> { mapId };

            // Find all children of a map
            ids.AddRange(getChildMapIds(cmd, mapId));

            // Delete parent and children
            foreach (int id in ids) {
               cmd.Parameters.Clear();
               cmd.CommandText = "UPDATE maps_v2 SET publishedVersion = (SELECT max(version) FROM map_versions_v2 WHERE mapId = @id) WHERE id = @id";
               cmd.Parameters.AddWithValue("@id", id);
               cmd.ExecuteNonQuery();
            }

            transaction.Commit();
         } catch (Exception e) {
            transaction.Rollback();
            throw e;
         }
      }
   }

   public static new void setLiveMapVersion (MapVersion version) {
      string cmdText = "UPDATE maps_v2 SET publishedVersion = @version WHERE id = @mapId;";
      using (MySqlConnection conn = getConnection())
      using (MySqlCommand cmd = new MySqlCommand(cmdText, conn)) {
         conn.Open();
         cmd.Prepare();

         cmd.Parameters.AddWithValue("@mapId", version.mapId);
         cmd.Parameters.AddWithValue("@version", version.version);

         // Execute the command
         cmd.ExecuteNonQuery();
      }
   }

   #endregion

   #region Shop XML Data

   public static new void updateShopXML (string rawData, string shopName, int xmlId) {
      string xml_id_key = "xml_id, ";
      string xml_id_value = "@xml_id, ";

      // If this is a newly created data, let sql table auto generate id
      if (xmlId < 0) {
         xml_id_key = "";
         xml_id_value = "";
      }

      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand(
            // Declaration of table elements
            "INSERT INTO global.shop_xml_v2 (" + xml_id_key + "xml_name, xmlContent, creator_userID, lastUserUpdate) " +
            "VALUES(" + xml_id_value + "@xml_name, @xmlContent, @creator_userID, NOW()) " +
            "ON DUPLICATE KEY UPDATE xmlContent = @xmlContent, xml_name = @xml_name, lastUserUpdate = NOW()", conn)) {

            conn.Open();
            cmd.Prepare();

            cmd.Parameters.AddWithValue("@xml_id", xmlId);
            cmd.Parameters.AddWithValue("@xml_name", shopName);
            cmd.Parameters.AddWithValue("@xmlContent", rawData);
            cmd.Parameters.AddWithValue("@creator_userID", MasterToolAccountManager.self.currentAccountID);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new List<XMLPair> getShopXML () {
      List<XMLPair> rawDataList = new List<XMLPair>();
      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM global.shop_xml_v2", conn)) {

            conn.Open();
            cmd.Prepare();

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  XMLPair newPair = new XMLPair {
                     isEnabled = true,
                     rawXmlData = dataReader.GetString("xmlContent"),
                     xmlId = dataReader.GetInt32("xml_id"),
                     xmlOwnerId = dataReader.GetInt32("creator_userID")
                  };
                  rawDataList.Add(newPair);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
      return rawDataList;
   }

   public static new void deleteShopXML (int xmlId) {
      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand("DELETE FROM global.shop_xml_v2 WHERE xml_id=@xml_id", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@xml_id", xmlId);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   #endregion

   #region Ship XML Data

   public static new void updateShipXML (string rawData, int typeIndex, Ship.Type shipType, string shipName, bool isActive) {
      string xml_id_key = "xml_id, ";
      string xml_id_value = "@xml_id, ";

      // If this is a newly created data, let sql table auto generate id
      if (typeIndex < 0) {
         xml_id_key = "";
         xml_id_value = "";
      }

      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand(
            // Declaration of table elements
            "INSERT INTO global.ship_xml_v2 (" + xml_id_key + "xmlContent, creator_userID, ship_type, ship_name, isActive, lastUserUpdate) " +
            "VALUES(" + xml_id_value + "@xmlContent, @creator_userID, @ship_type, @ship_name, @isActive, NOW()) " +
            "ON DUPLICATE KEY UPDATE xmlContent = @xmlContent, ship_type = @ship_type, ship_name = @ship_name, isActive = @isActive, lastUserUpdate = NOW()", conn)) {

            conn.Open();
            cmd.Prepare();

            cmd.Parameters.AddWithValue("@xml_id", typeIndex);
            cmd.Parameters.AddWithValue("@xmlContent", rawData);
            cmd.Parameters.AddWithValue("@ship_type", shipType.ToString());
            cmd.Parameters.AddWithValue("@ship_name", shipName);
            cmd.Parameters.AddWithValue("@isActive", isActive);
            cmd.Parameters.AddWithValue("@creator_userID", MasterToolAccountManager.self.currentAccountID);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new List<XMLPair> getShipXML () {
      List<XMLPair> rawDataList = new List<XMLPair>();
      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM global.ship_xml_v2", conn)) {

            conn.Open();
            cmd.Prepare();

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  XMLPair newXMLPair = new XMLPair {
                     xmlId = dataReader.GetInt32("xml_id"),
                     rawXmlData = dataReader.GetString("xmlContent"),
                     isEnabled = dataReader.GetInt32("isActive") == 0 ? false : true
                  };
                  rawDataList.Add(newXMLPair);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
      return new List<XMLPair>(rawDataList);
   }

   public static new void deleteShipXML (int typeID) {
      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand("DELETE FROM global.ship_xml_v2 WHERE xml_id=@xml_id", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@xml_id", typeID);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   #endregion

   #region Achievement XML Data

   public static new void updateAchievementXML (string rawData, string name, int xmlId) {
      string xml_id_key = "xml_id, ";
      string xml_id_value = "@xml_id, ";

      // If this is a newly created data, let sql table auto generate id
      if (xmlId < 0) {
         xml_id_key = "";
         xml_id_value = "";
      }

      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand(
            // Declaration of table elements
            "INSERT INTO global.achievement_xml_v2 (" + xml_id_key + "xml_name, xmlContent, creator_userID, lastUserUpdate) " +
            "VALUES(" + xml_id_value + "@xml_name, @xmlContent, @creator_userID, NOW()) " +
            "ON DUPLICATE KEY UPDATE xmlContent = @xmlContent, lastUserUpdate = NOW()", conn)) {

            conn.Open();
            cmd.Prepare();

            cmd.Parameters.AddWithValue("@xml_id", xmlId);
            cmd.Parameters.AddWithValue("@xml_name", name);
            cmd.Parameters.AddWithValue("@xmlContent", rawData);
            cmd.Parameters.AddWithValue("@creator_userID", MasterToolAccountManager.self.currentAccountID);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new void deleteAchievementXML (string name) {
      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand("DELETE FROM global.achievement_xml_v2 WHERE xml_name=@xml_name", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@xml_name", name);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new List<XMLPair> getAchievementXML () {
      List<XMLPair> rawDataList = new List<XMLPair>();
      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM global.achievement_xml_v2", conn)) {

            conn.Open();
            cmd.Prepare();

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  XMLPair newXML = new XMLPair {
                     isEnabled = true,
                     rawXmlData = dataReader.GetString("xmlContent"),
                     xmlId = dataReader.GetInt32("xml_id"),
                     xmlOwnerId = dataReader.GetInt32("creator_userID"),
                  };
                  rawDataList.Add(newXML);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
      return rawDataList;
   }
   #endregion

   #region Perks

   public static new List<Perk> getPerkPointsForUser (int usrId) {
      List<Perk> points = new List<Perk>();

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM arcane.perks WHERE usrId = @usrId", conn)) {

            conn.Open();

            cmd.Parameters.AddWithValue("@usrId", usrId);
            cmd.Prepare();

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  points.Add(new Perk(dataReader));
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return points;
   }

   public static new void assignPerkPoint (int usrId, int perkId) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "INSERT INTO arcane.perks (usrId, perkId, perkPoints) " +
            "VALUES(@usrId, @perkId, @perkPoints) " +
            "ON DUPLICATE KEY UPDATE perkPoints = perkPoints + @perkPoints;" +
            "UPDATE arcane.perks SET perkPoints = perkPoints - 1 WHERE usrId = @usrId AND perkId = 0;", conn)) {

            conn.Open();

            cmd.Parameters.AddWithValue("@usrId", usrId);
            cmd.Parameters.AddWithValue("@perkId", perkId);
            cmd.Parameters.AddWithValue("@perkPoints", 1);

            cmd.Prepare();

            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new int getUnassignedPerkPoints (int usrId) {
      return getAssignedPointsByPerkId(usrId, Perk.UNASSIGNED_ID);
   }

   public static new int getAssignedPointsByPerkId (int usrId, int perkId) {
      int points = 0;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM arcane.perks WHERE usrId = @usrId AND perkId = @perkId", conn)) {

            conn.Open();
            cmd.Parameters.AddWithValue("@usrId", usrId);
            cmd.Parameters.AddWithValue("@perkId", perkId);
            cmd.Prepare();

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  points = dataReader.GetInt32("perkPoints");
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return points;
   }

   public static new void addPerkPointsForUser (int usrId, int perkId, int perkPoints) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "INSERT INTO arcane.perks (usrId, perkId, perkTypeId, perkPoints) " +
            "VALUES(@usrId, @perkId, @perkTypeId, @perkPoints) " +
            "ON DUPLICATE KEY UPDATE perkPoints = perkPoints + @perkPoints", conn)) {

            conn.Open();

            cmd.Parameters.AddWithValue("@usrId", usrId);
            cmd.Parameters.AddWithValue("@perkId", perkId);
            cmd.Parameters.AddWithValue("@perkPoints", perkPoints);

            cmd.Prepare();

            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new void addPerkPointsForUser (int usrId, List<Perk> perks) {
      StringBuilder cmdText = new StringBuilder("INSERT INTO arcane.perks (usrId, perkId, perkPoints) VALUES ");
      int i = 0;

      using (MySqlConnection conn = getConnection())
      using (MySqlCommand cmd = new MySqlCommand(cmdText.ToString(), conn)) {
         foreach (Perk perk in perks) {
            cmdText.Append($"(@usrId{i}, @perkId{i}, @perkPoints{i})");
            cmd.Parameters.AddWithValue($"@usrId{i}", usrId);
            cmd.Parameters.AddWithValue($"@perkId{i}", perk.perkId);
            cmd.Parameters.AddWithValue($"@perkPoints{i}", perk.points);

            i++;

            if (i != perks.Count()) {
               cmdText.Append(", ");
            }
         }

         cmdText.Append(" ON DUPLICATE KEY UPDATE perkPoints = perkPoints + VALUES(perkPoints); ");

         conn.Open();
         cmd.CommandText = cmdText.ToString();
         cmd.CommandType = System.Data.CommandType.Text;
         cmd.Prepare();
         cmd.ExecuteNonQuery();
      }
   }

   public static new void updatePerksXML (string rawData, int perkId) {
      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand(
            // Declaration of table elements
            "INSERT INTO global.perks_config_xml (xml_id, xmlContent, creator_userID, lastUserUpdate) " +
            "VALUES(@xml_id, @xmlContent, @creator_userID, lastUserUpdate = NOW()) " +
            "ON DUPLICATE KEY UPDATE xmlContent = @xmlContent, lastUserUpdate = NOW()", conn)) {

            conn.Open();
            cmd.Prepare();

            cmd.Parameters.AddWithValue("@xml_id", perkId);
            cmd.Parameters.AddWithValue("@xmlContent", rawData);
            cmd.Parameters.AddWithValue("@creator_userID", MasterToolAccountManager.self.currentAccountID);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new List<PerkData> getPerksXML () {
      List<PerkData> perkDataList = new List<PerkData>();
      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM global.perks_config_xml", conn)) {

            conn.Open();
            cmd.Prepare();

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  perkDataList.Add(new PerkData(dataReader));
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
      return new List<PerkData>(perkDataList);
   }

   public static new void deletePerkXML (int xmlId) {
      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand("DELETE FROM global.perks_config_xml WHERE xml_id=@xml_id", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@xml_id", xmlId);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   #endregion

   #region Books Data

   public static new void upsertBook (string bookContent, string name, int bookId) {
      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand(
            // Declaration of table elements
            "INSERT INTO global.books (bookId, bookTitle, bookContent, creator_userID, lastUserUpdate) " +
            "VALUES(NULLIF(@bookId, 0), @bookTitle, @bookContent, @creator_userID, NOW()) " +
            "ON DUPLICATE KEY UPDATE bookTitle = @bookTitle, bookContent = @bookContent, lastUserUpdate = NOW()", conn)) {

            conn.Open();
            cmd.Prepare();

            cmd.Parameters.AddWithValue("@bookId", bookId);
            cmd.Parameters.AddWithValue("@bookTitle", name);
            cmd.Parameters.AddWithValue("@bookContent", bookContent);
            cmd.Parameters.AddWithValue("@creator_userID", MasterToolAccountManager.self.currentAccountID);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new List<BookData> getBooksList () {
      List<BookData> rawDataList = new List<BookData>();
      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM global.books", conn)) {

            conn.Open();
            cmd.Prepare();

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  rawDataList.Add(new BookData(dataReader));
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
      return new List<BookData>(rawDataList);
   }

   public static new BookData getBookById (int bookId) {
      BookData book = null;
      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM global.books WHERE bookId = @bookId", conn)) {

            conn.Open();
            cmd.Parameters.AddWithValue("@bookId", bookId);
            cmd.Prepare();

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  book = new BookData(dataReader);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return book;
   }

   public static new void deleteBookByID (int bookId) {
      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand("DELETE FROM global.books WHERE bookId=@bookId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@bookId", bookId);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   #endregion

   #region Discoveries Data

   public static new void duplicateDiscovery (DiscoveryData data) {
      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand(
            // Declaration of table elements
            "INSERT INTO global.discoveries_v2 (discoveryName, discoveryDescription, sourceImageUrl, rarity, creator_userID) " +
            "VALUES(@discoveryName, @discoveryDescription, @sourceImageUrl, @rarity, @creator_userID) ", conn)) {

            conn.Open();
            cmd.Prepare();

            cmd.Parameters.AddWithValue("@discoveryName", data.name);
            cmd.Parameters.AddWithValue("@discoveryDescription", data.description);
            cmd.Parameters.AddWithValue("@sourceImageUrl", data.spriteUrl);
            cmd.Parameters.AddWithValue("@rarity", data.rarity);
            cmd.Parameters.AddWithValue("@creator_userID", MasterToolAccountManager.self.currentAccountID);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new void upsertDiscovery (DiscoveryData data) {
      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand(
            // Declaration of table elements
            "INSERT INTO global.discoveries_v2 (discoveryId, discoveryName, discoveryDescription, sourceImageUrl, rarity, creator_userID) " +
            "VALUES(NULLIF(@discoveryId, 0), @discoveryName, @discoveryDescription, @sourceImageUrl, @rarity, @creator_userID) " +
            "ON DUPLICATE KEY UPDATE discoveryName = @discoveryName, discoveryDescription = @discoveryDescription, sourceImageUrl = @sourceImageUrl, rarity = @rarity", conn)) {

            conn.Open();
            cmd.Prepare();

            cmd.Parameters.AddWithValue("@discoveryId", data.discoveryId);
            cmd.Parameters.AddWithValue("@discoveryName", data.name);
            cmd.Parameters.AddWithValue("@discoveryDescription", data.description);
            cmd.Parameters.AddWithValue("@sourceImageUrl", data.spriteUrl);
            cmd.Parameters.AddWithValue("@rarity", data.rarity);
            cmd.Parameters.AddWithValue("@creator_userID", MasterToolAccountManager.self.currentAccountID);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new List<DiscoveryData> getDiscoveriesList () {
      List<DiscoveryData> rawDataList = new List<DiscoveryData>();
      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM global.discoveries_v2", conn)) {

            conn.Open();
            cmd.Prepare();

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  rawDataList.Add(new DiscoveryData(dataReader));
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
      return new List<DiscoveryData>(rawDataList);
   }

   public static new DiscoveryData getDiscoveryById (int discoveryId) {
      DiscoveryData discovery = null;
      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM global.discoveries_v2 WHERE discoveryId = @discoveryId", conn)) {

            conn.Open();
            cmd.Parameters.AddWithValue("@discoveryId", discoveryId);
            cmd.Prepare();

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  discovery = new DiscoveryData(dataReader);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return discovery;
   }

   public static new void deleteDiscoveryById (int discoveryId) {
      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand("DELETE FROM global.discoveries_v2 WHERE discoveryId = @discoveryId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@discoveryId", discoveryId);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   #endregion

   #region Palette XML Data

   public static new void updatePaletteXML (string rawData, string name, int xmlId, int isEnabled, string tag) {
      string xml_id_key = "paletteId, ";
      string xml_id_value = "@paletteId, ";

      // If this is a newly created data, let sql table auto generate id
      if (xmlId < 0) {
         xml_id_key = "";
         xml_id_value = "";
      }

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            // Declaration of table elements
            "INSERT INTO palette (" + xml_id_key + "palette_name, xml_content, creator_userID, lastUserUpdate, isEnabled, tag) " +
            "VALUES(" + xml_id_value + "@palette_name, @xml_content, @creator_userID, NOW(), @isEnabled, @tag) " +
            "ON DUPLICATE KEY UPDATE palette_name = @palette_name, xml_content = @xml_content, lastUserUpdate = NOW(), isEnabled = @isEnabled, tag = @tag", conn)) {

            conn.Open();
            cmd.Prepare();

            cmd.Parameters.AddWithValue("@paletteId", xmlId);
            cmd.Parameters.AddWithValue("@palette_name", name);
            cmd.Parameters.AddWithValue("@xml_content", rawData);
            cmd.Parameters.AddWithValue("@creator_userID", MasterToolAccountManager.self ? MasterToolAccountManager.self.currentAccountID : 0);
            cmd.Parameters.AddWithValue("@isEnabled", isEnabled);
            cmd.Parameters.AddWithValue("@tag", tag);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new void deletePaletteXML (string name) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("DELETE FROM palette WHERE palette_name=@palette_name", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@palette_name", name);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new void deletePaletteXML (int id) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("DELETE FROM palette WHERE paletteId=@paletteId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@paletteId", id);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new List<XMLPair> getPaletteXML (bool onlyEnabledPalettes) {
      List<XMLPair> rawDataList = new List<XMLPair>();
      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM global.palette_recolors" + (onlyEnabledPalettes ? " WHERE isEnabled = 1" : ""), conn)) {

            conn.Open();
            cmd.Prepare();

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  XMLPair newXML = new XMLPair {
                     isEnabled = dataReader.GetInt32("isEnabled") == 1 ? true : false,
                     rawXmlData = dataReader.GetString("xmlContent"),
                     xmlId = dataReader.GetInt32("id"),
                     xmlOwnerId = dataReader.GetInt32("creatorUserId"),

                     // TODO: Confirm if this is no longer needed, it no longer exists in the SQL Table
                     //tag = dataReader.GetString("tag"),
                  };
                  rawDataList.Add(newXML);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
      return rawDataList;
   }

   public static new int getPaletteTagID (string tag) {
      int id = -1;
      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM global.palette_tags WHERE name = @name", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@name", tag);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  id = dataReader.GetInt32("id");
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
      return id;
   }

   public static new List<XMLPair> getPaletteXML (int tagId) {
      List<XMLPair> rawDataList = new List<XMLPair>();
      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM global.palette_recolors WHERE tagId = @tagId", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@tagId", tagId);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  XMLPair newXML = new XMLPair {
                     isEnabled = dataReader.GetInt32("isEnabled") == 1 ? true : false,
                     rawXmlData = dataReader.GetString("xmlContent"),
                     xmlId = dataReader.GetInt32("id"),
                     xmlOwnerId = dataReader.GetInt32("creatorUserID"),
                  };
                  rawDataList.Add(newXML);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
      return rawDataList;
   }

   public static new List<XMLPair> getPaletteXML (int tagId, string subcategory) {
      List<XMLPair> rawDataList = new List<XMLPair>();
      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM global.palette_recolors WHERE tagId = @tagId AND subcategory = @subcategory", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@tagId", tagId);
            cmd.Parameters.AddWithValue("@subcategory", subcategory);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  XMLPair newXML = new XMLPair {
                     isEnabled = dataReader.GetInt32("isEnabled") == 1 ? true : false,
                     rawXmlData = dataReader.GetString("xmlContent"),
                     xmlId = dataReader.GetInt32("id"),
                     xmlOwnerId = dataReader.GetInt32("creatorUserID"),
                  };
                  rawDataList.Add(newXML);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
      return rawDataList;
   }

   #endregion

   #region Crafting XML Data

   public static new void updateCraftingXML (int xmlID, string rawData, string name, int typeId, int category) {
      string xml_id_key = "xml_id, ";
      string xml_id_value = "@xml_id, ";

      // If this is a newly created data, let sql table auto generate id
      if (xmlID < 0) {
         xml_id_key = "";
         xml_id_value = "";
      }

      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand(
            // Declaration of table elements
            "INSERT INTO global.crafting_xml_v2 (" + xml_id_key + "xmlName, xmlContent, creator_userID, lastUserUpdate) " +
            "VALUES(" + xml_id_value + "@xmlName, @xmlContent, @creator_userID, NOW()) " +
            "ON DUPLICATE KEY UPDATE xmlContent = @xmlContent, xmlName = @xmlName, equipmentTypeID = @equipmentTypeID, equipmentCategory = @equipmentCategory, lastUserUpdate = NOW()", conn)) {

            conn.Open();
            cmd.Prepare();

            cmd.Parameters.AddWithValue("@xml_id", xmlID);
            cmd.Parameters.AddWithValue("@xmlName", name);
            cmd.Parameters.AddWithValue("@equipmentTypeID", typeId);
            cmd.Parameters.AddWithValue("@equipmentCategory", category);
            cmd.Parameters.AddWithValue("@xmlContent", rawData);
            cmd.Parameters.AddWithValue("@creator_userID", MasterToolAccountManager.self.currentAccountID);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new List<XMLPair> getCraftingXML () {
      List<XMLPair> rawDataList = new List<XMLPair>();
      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM global.crafting_xml_v2", conn)) {

            conn.Open();
            cmd.Prepare();

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  XMLPair newPair = new XMLPair {
                     isEnabled = true,
                     rawXmlData = dataReader.GetString("xmlContent"),
                     xmlId = dataReader.GetInt32("xml_id"),
                     xmlOwnerId = dataReader.GetInt32("creator_userID"),
                  };
                  rawDataList.Add(newPair);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
      return new List<XMLPair>(rawDataList);
   }

   public static new void deleteCraftingXML (int xmlID) {
      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand("DELETE FROM global.crafting_xml_v2 WHERE xml_id=@xml_id", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@xml_id", xmlID);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   #endregion

   #region Background XML Data

   public static new int updateBackgroundXML (int xmlId, string rawData, string bgName) {
      int latestXMLId = 0;
      try {
         string xml_id_key = "xml_id, ";
         string xml_id_value = "@xml_id, ";

         // If this is a newly created data, let sql table auto generate id
         if (xmlId < 0) {
            xml_id_key = "";
            xml_id_value = "";
         }

         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand(
            // Declaration of table elements
            "INSERT INTO global.background_xml_v2 (" + xml_id_key + "xml_name, xmlContent, creator_userID, lastUserUpdate) " +
            "VALUES(" + xml_id_value + "@xml_name, @xmlContent, @creator_userID, NOW()) " +
            "ON DUPLICATE KEY UPDATE xmlContent = @xmlContent, xml_name = @xml_name, lastUserUpdate = NOW()", conn)) {

            conn.Open();
            cmd.Prepare();

            cmd.Parameters.AddWithValue("@xml_id", xmlId);
            cmd.Parameters.AddWithValue("@xml_name", bgName);
            cmd.Parameters.AddWithValue("@xmlContent", rawData);
            cmd.Parameters.AddWithValue("@creator_userID", MasterToolAccountManager.self == null ? 0 : MasterToolAccountManager.self.currentAccountID);

            // Execute the command
            cmd.ExecuteNonQuery();
            latestXMLId = (int) cmd.LastInsertedId;
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return latestXMLId;
   }

   public static new List<XMLPair> getBackgroundXML () {
      List<XMLPair> rawDataList = new List<XMLPair>();
      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM global.background_xml_v2", conn)) {

            conn.Open();
            cmd.Prepare();

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  XMLPair newXMLPair = new XMLPair {
                     isEnabled = true,
                     xmlId = dataReader.GetInt32("xml_id"),
                     rawXmlData = dataReader.GetString("xmlContent"),
                     xmlOwnerId = dataReader.GetInt32("creator_userID")
                  };
                  rawDataList.Add(newXMLPair);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
      return rawDataList;
   }

   public static new void deleteBackgroundXML (int xmlId) {
      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand("DELETE FROM global.background_xml_v2 WHERE xml_id=@xml_id", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@xml_id", xmlId);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   #endregion

   #region Equipment XML Data

   public static new void updateEquipmentXML (string rawData, int xmlID, EquipmentType equipType, string equipmentName, bool isEnabled) {
      string tableName = "";
      string xmlKey = "xml_id, ";
      string xmlValue = "@xml_id, ";
      if (xmlID <= 0) {
         xmlKey = "";
         xmlValue = "";
      }

      switch (equipType) {
         case EquipmentType.Weapon:
            tableName = "equipment_weapon_xml_v3";
            break;
         case EquipmentType.Armor:
            tableName = "equipment_armor_xml_v3";
            break;
         case EquipmentType.Hat:
            tableName = "equipment_hat_xml_v1";
            break;
      }

      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand(
            // Declaration of table elements
            "INSERT INTO global." + tableName + " (" + xmlKey + "xmlContent, creator_userID, equipment_type, equipment_name, is_enabled, lastUserUpdate) " +
            "VALUES(" + xmlValue + "@xmlContent, @creator_userID, @equipment_type, @equipment_name, @is_enabled, NOW()) " +
            "ON DUPLICATE KEY UPDATE xmlContent = @xmlContent, equipment_type = @equipment_type, equipment_name = @equipment_name, is_enabled = @is_enabled, lastUserUpdate = NOW()", conn)) {

            conn.Open();
            cmd.Prepare();

            cmd.Parameters.AddWithValue("@xml_id", xmlID);
            cmd.Parameters.AddWithValue("@xmlContent", rawData);
            cmd.Parameters.AddWithValue("@equipment_name", equipmentName);
            cmd.Parameters.AddWithValue("@equipment_type", equipType.ToString());
            cmd.Parameters.AddWithValue("@is_enabled", isEnabled ? 1 : 0);
            cmd.Parameters.AddWithValue("@creator_userID", MasterToolAccountManager.self.currentAccountID);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new void deleteEquipmentXML (int xml_id, EquipmentType equipType) {
      string tableName = "";
      switch (equipType) {
         case EquipmentType.Weapon:
            tableName = "equipment_weapon_xml_v3";
            break;
         case EquipmentType.Armor:
            tableName = "equipment_armor_xml_v3";
            break;
         case EquipmentType.Hat:
            tableName = "equipment_hat_xml_v1";
            break;
      }

      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand("DELETE FROM global." + tableName + " WHERE xml_id=@xml_id", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@xml_id", xml_id);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new List<XMLPair> getEquipmentXML (EquipmentType equipType) {
      string tableName = "";
      switch (equipType) {
         case EquipmentType.Weapon:
            tableName = "equipment_weapon_xml_v3";
            break;
         case EquipmentType.Armor:
            tableName = "equipment_armor_xml_v3";
            break;
         case EquipmentType.Hat:
            tableName = "equipment_hat_xml_v1";
            break;
      }

      List<XMLPair> rawDataList = new List<XMLPair>();
      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM global." + tableName, conn)) {

            conn.Open();
            cmd.Prepare();

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  try {
                     XMLPair xmlPair = new XMLPair {
                        isEnabled = dataReader.GetInt32("is_enabled") == 1 ? true : false,
                        rawXmlData = dataReader.GetString("xmlContent"),
                        xmlId = dataReader.GetInt32("xml_id")
                     };
                     rawDataList.Add(xmlPair);
                  } catch {
                     D.debug("Failed to translate: " + dataReader.GetInt32("xml_id"));
                  }
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
      return new List<XMLPair>(rawDataList);
   }

   #endregion

   #region Item Definitions Xml Data

   public static new List<ItemDefinition> getItemDefinitions () {
      List<ItemDefinition> result = new List<ItemDefinition>();

      string cmdText = "SELECT id, category, serializedData FROM global.item_definitions;";

      using (MySqlConnection conn = getConnectionToDevGlobal())
      using (MySqlCommand cmd = new MySqlCommand(cmdText, conn)) {
         conn.Open();
         cmd.Prepare();

         using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
            while (dataReader.Read()) {
               result.Add(ItemDefinition.deserialize(
                  dataReader.GetString("serializedData"),
                  (ItemDefinition.Category) dataReader.GetInt32("category")));
            }
         }
      }

      return result;
   }

   public static new void createNewItemDefinition (ItemDefinition definition) {
      using (MySqlConnection conn = getConnectionToDevGlobal())
      using (MySqlCommand cmd = conn.CreateCommand()) {
         conn.Open();
         MySqlTransaction transaction = conn.BeginTransaction();
         cmd.Transaction = transaction;
         cmd.Connection = conn;

         try {
            // Insert a new empty entry to item definitions
            cmd.CommandText = "INSERT INTO global.item_definitions(category, serializedData) VALUES(@category, @serializedData);";
            cmd.Parameters.AddWithValue("@category", -1);
            cmd.Parameters.AddWithValue("@serializedData", "undefined");
            cmd.ExecuteNonQuery();

            // Get the id of the new inserted entry
            long id = cmd.LastInsertedId;

            // Apply that id to our item definition
            definition.id = (int) id;

            // Populate the database entry with our definition
            cmd.CommandText = "UPDATE global.item_definitions SET category = @category, serializedData = @serializedData, creator_userID = @creatorUserId WHERE id = @id;";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@id", definition.id);
            cmd.Parameters.AddWithValue("@category", (int) definition.category);
            cmd.Parameters.AddWithValue("@serializedData", definition.serialize());
            cmd.Parameters.AddWithValue("@creatorUserId", definition.creatorUserId);
            cmd.ExecuteNonQuery();

            // Commit our transaction
            transaction.Commit();
         } catch (Exception e) {
            // In case there's an error in the middle, revert back, so no empty entry is leftover in the database
            transaction.Rollback();
            throw e;
         }
      }
   }

   public static new void updateItemDefinition (ItemDefinition definition) {
      string cmdText = "UPDATE global.item_definitions SET category = @category, serializedData = @serializedData WHERE id = @id;";
      using (MySqlConnection conn = getConnectionToDevGlobal())
      using (MySqlCommand cmd = new MySqlCommand(cmdText, conn)) {
         conn.Open();
         cmd.Prepare();

         cmd.Parameters.AddWithValue("@id", definition.id);
         cmd.Parameters.AddWithValue("@category", (int) definition.category);
         cmd.Parameters.AddWithValue("@serializedData", definition.serialize());

         cmd.ExecuteNonQuery();
      }
   }

   public static new void deleteItemDefinition (int id) {
      using (MySqlConnection conn = getConnectionToDevGlobal())
      using (MySqlCommand cmd = new MySqlCommand("DELETE FROM global.item_definitions WHERE id = @id;", conn)) {
         conn.Open();
         cmd.Prepare();

         cmd.Parameters.AddWithValue("@id", id);

         cmd.ExecuteNonQuery();
      }
   }

   #endregion

   #region Item Instances

   public static new List<ItemInstance> getItemInstances (object command, int ownerUserId, ItemDefinition.Category category) {
      MySqlCommand cmd = command as MySqlCommand;
      cmd.CommandText = "SELECT * FROM item_instances WHERE item_instances.userId = @ownerUserId AND category = @category;";
      cmd.Parameters.AddWithValue("@ownerUserId", ownerUserId);
      cmd.Parameters.AddWithValue("@category", (int) category);

      List<ItemInstance> result = new List<ItemInstance>();
      using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
         while (dataReader.Read()) {
            result.Add(new ItemInstance(dataReader));
         }
      }

      return result;
   }

   public static new ItemInstance getItemInstance (object command, int userId, int itemDefinitionId) {
      MySqlCommand cmd = command as MySqlCommand;
      cmd.CommandText = "SELECT * FROM item_instances WHERE userId = @userId AND itemDefinitionId = @itemDefinitionId;";
      cmd.Parameters.AddWithValue("@userId", userId);
      cmd.Parameters.AddWithValue("@itemDefinitionId", itemDefinitionId);

      using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
         if (dataReader.Read()) {
            return new ItemInstance(dataReader);
         }
      }
      return null;
   }

   public static new void createOrAppendItemInstance (object command, ItemInstance item) {
      MySqlCommand cmd = command as MySqlCommand;
      if (item.getDefinition().canBeStacked()) {
         // If item can be stacked, we want to check if it already exists
         ItemInstance existingInstance = getItemInstance(cmd, item.ownerUserId, item.itemDefinitionId);
         cmd.Parameters.Clear();

         // If the item exist, update its count
         if (existingInstance != null) {
            increaseItemInstanceCount(cmd, existingInstance.id, item.count);

            // Update item's fields to represent newly updated entry
            item.id = existingInstance.id;
         } else {
            // Otherwise, create a new stack
            createNewItemInstance(cmd, item);
         }
      } else {
         int count = item.count;

         // Since the item cannot be stacked, set its count to 1
         item.count = 1;

         for (int i = 0; i < count; i++) {
            // Create the item
            createNewItemInstance(cmd, item);
         }
      }
   }

   public static new void createNewItemInstance (object command, ItemInstance itemInstance) {
      MySqlCommand cmd = command as MySqlCommand;
      cmd.CommandText = "INSERT INTO item_instances (itemDefinitionId, userId, count, rarity, palettes, category) " +
         "VALUES(@itemDefinitionId, @userId, @count, @rarity, @palettes, @category);";
      cmd.Parameters.AddWithValue("@itemDefinitionId", itemInstance.itemDefinitionId);
      cmd.Parameters.AddWithValue("@userId", (int) itemInstance.ownerUserId);
      cmd.Parameters.AddWithValue("@count", (int) itemInstance.count);
      cmd.Parameters.AddWithValue("@palettes", itemInstance.palettes);
      cmd.Parameters.AddWithValue("@rarity", (int) itemInstance.rarity);
      cmd.Parameters.AddWithValue("@category", (int) itemInstance.getDefinition().category);
      cmd.ExecuteNonQuery();

      // Set the ID that was created for the instance
      itemInstance.id = (int) cmd.LastInsertedId;
   }

   public static new void increaseItemInstanceCount (object command, int id, int increaseBy) {
      MySqlCommand cmd = command as MySqlCommand;
      cmd.CommandText = "UPDATE item_instances SET count = count + @increaseBy WHERE id=@id;";
      cmd.Parameters.AddWithValue("@increaseBy", increaseBy);
      cmd.Parameters.AddWithValue("@id", id);
      cmd.ExecuteNonQuery();
   }

   public static new void decreaseOrDeleteItemInstance (object command, int id, int decreaseBy) {
      MySqlCommand cmd = command as MySqlCommand;
      cmd.Transaction = cmd.Connection.BeginTransaction();
      try {
         // First query deletes the entry which has only 'decreaseBy' of item left
         // Second query decreases the count by 'decreaseBy' if the item wasn't deleted (had more than 'decreaseBy' left)
         cmd.CommandText = "DELETE FROM item_instances WHERE id = @id AND count <= @decreaseBy; " +
            "UPDATE item_instances SET count = count - @decreaseBy WHERE id = @id;";

         cmd.Parameters.AddWithValue("@id", id);
         cmd.Parameters.AddWithValue("@decreaseBy", decreaseBy);
         cmd.ExecuteNonQuery();
         cmd.Transaction.Commit();
      } catch (Exception ex) {
         cmd.Transaction.Rollback();
         throw ex;
      }
   }

   #endregion

   #region Companions Features

   public static new void updateCompanionExp (int xmlId, int userId, int exp) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            // Declaration of table elements
            "UPDATE companions SET companionExp = companionExp + @companionExp WHERE companionId=@companionId and userId=@userId", conn)) {

            conn.Open();
            cmd.Prepare();

            cmd.Parameters.AddWithValue("@companionId", xmlId);
            cmd.Parameters.AddWithValue("@userId", userId);
            cmd.Parameters.AddWithValue("@companionExp", exp);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new void updateCompanionRoster (int xmlId, int userId, int slot) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            // Declaration of table elements
            "INSERT INTO companions (companionId, userId) " +
            "VALUES(@companionId, @userId) " +
            "ON DUPLICATE KEY UPDATE equippedSlot = @equippedSlot", conn)) {

            conn.Open();
            cmd.Prepare();

            cmd.Parameters.AddWithValue("@companionId", xmlId);
            cmd.Parameters.AddWithValue("@userId", userId);
            cmd.Parameters.AddWithValue("@equippedSlot", slot);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new void updateCompanions (int xmlId, int userId, string companionName, int companionLevel, int companionType, int equippedSlot, string iconPath, int companionExp) {
      string xmlKey = "xmlId, ";
      string xmlValue = "@xmlId, ";
      if (xmlId < 0) {
         xmlKey = "";
         xmlValue = "";
      }

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            // Declaration of table elements
            "INSERT INTO companions (" + xmlKey + "userId, companionName, companionLevel, companionType, equippedSlot, iconPath, companionExp) " +
            "VALUES(" + xmlValue + "@userId, @companionName, @companionLevel, @companionType, @equippedSlot, @iconPath, @companionExp) " +
            "ON DUPLICATE KEY UPDATE companionLevel = @companionLevel, equippedSlot = @equippedSlot", conn)) {

            conn.Open();
            cmd.Prepare();

            cmd.Parameters.AddWithValue("@xmlId", xmlId);
            cmd.Parameters.AddWithValue("@userId", userId);
            cmd.Parameters.AddWithValue("@companionName", companionName);
            cmd.Parameters.AddWithValue("@companionLevel", companionLevel);
            cmd.Parameters.AddWithValue("@companionType", companionType);
            cmd.Parameters.AddWithValue("@equippedSlot", equippedSlot);
            cmd.Parameters.AddWithValue("@iconPath", iconPath);
            cmd.Parameters.AddWithValue("@companionExp", companionExp);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new List<CompanionInfo> getCompanions (int userId) {
      List<CompanionInfo> newCompanionInfo = new List<CompanionInfo>();
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM arcane.companions where userId = @userId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@userId", userId);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  CompanionInfo companionInfo = new CompanionInfo {
                     companionId = dataReader.GetInt32("companionId"),
                     companionName = dataReader.GetString("companionName"),
                     companionLevel = dataReader.GetInt32("companionLevel"),
                     companionType = dataReader.GetInt32("companionType"),
                     equippedSlot = dataReader.GetInt32("equippedSlot"),
                     iconPath = dataReader.GetString("iconPath"),
                     companionExp = dataReader.GetInt32("companionExp")
                  };
                  newCompanionInfo.Add(companionInfo);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
      return newCompanionInfo;
   }

   #endregion

   public static new List<Item> getRequiredIngredients (int usrId, List<CraftingIngredients.Type> itemList) {
      int itmCategory = (int) Item.Category.CraftingIngredients;
      List<Item> newItemList = new List<Item>();

      string itemIds = "";
      for (int i = 0; i < itemList.Count; i++) {
         int itmType = (int) itemList[i];
         if (i > 0) {
            itemIds += " or ";
         }
         itemIds += "itmType = " + itmType;
      }

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(string.Format("SELECT * FROM arcane.items where itmCategory = @itmCategory and ({0}) and usrId = @usrId", itemIds), conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@itmCategory", itmCategory);
            cmd.Parameters.AddWithValue("@usrId", usrId);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  int newCategory = DataUtil.getInt(dataReader, "itmCategory");
                  int newType = DataUtil.getInt(dataReader, "itmType");
                  int newitemCount = DataUtil.getInt(dataReader, "itmCount");
                  int newItemID = DataUtil.getInt(dataReader, "itmId");

                  ItemInfo info = new ItemInfo(dataReader);
                  Item newItem = new Item {
                     category = (Item.Category) newCategory,
                     itemTypeId = newType,
                     count = newitemCount,
                     id = newItemID
                  };

                  Item findItem = newItemList.Find(_ => _.itemTypeId == newType && (int) _.category == newCategory);
                  if (newItemList.Contains(findItem)) {
                     int itemIndex = newItemList.IndexOf(findItem);
                     newItemList[itemIndex].count += 1;
                  } else {
                     newItemList.Add(newItem);
                  }
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
      return newItemList;
   }

   #region Treasure Chest Interaction

   public static new TreasureStateData getTreasureStateForChest (int userId, int chestId, string areaId) {

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM treasure_chests WHERE (userId=@userId and chestId=@chestId and areaId=@areaId)", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@userId", userId);
            cmd.Parameters.AddWithValue("@chestId", chestId);
            cmd.Parameters.AddWithValue("@areaId", areaId);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  TreasureStateData info = new TreasureStateData(dataReader);
                  return info;
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return null;
   }

   public static new int updateTreasureStatus (int userId, int treasureId, string areaKey) {
      int lastTreasureId = 0;
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "INSERT INTO treasure_chests (userId, chestId, areaId, status) " +
            "VALUES (@userId, @chestId, @areaId, @status);", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@userId", userId);
            cmd.Parameters.AddWithValue("@chestId", treasureId);
            cmd.Parameters.AddWithValue("@areaId", areaKey);
            cmd.Parameters.AddWithValue("@status", 1);

            // Execute the command
            cmd.ExecuteNonQuery();
            lastTreasureId = (int) cmd.LastInsertedId;
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
      return lastTreasureId;
   }

   #endregion

   #region Crops Xml

   public static new List<CropInfo> getCropInfo (int userId) {
      List<CropInfo> cropList = new List<CropInfo>();

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM crops WHERE usrId=@usrId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrId", userId);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  CropInfo info = new CropInfo(dataReader);
                  cropList.Add(info);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return cropList;
   }

   public static new int insertCrop (CropInfo cropInfo, string areaKey) {
      int cropId = 0;
      string unixString = "CURRENT_TIMESTAMP";

      if (_connectionString.Contains("127.0.0.1")) {
         // Local server fails to process query because it cannot accept null
         unixString = "IFNULL(FROM_UNIXTIME(@creationTime), FROM_UNIXTIME(1))";
      }
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "INSERT INTO crops (usrId, crpType, cropNumber, creationTime, lastWaterTimestamp, waterInterval, areaKey) " +
            "VALUES (@usrId, @crpType, @cropNumber, " + unixString + ", UNIX_TIMESTAMP(), @waterInterval, @areaKey);", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrId", cropInfo.userId);
            cmd.Parameters.AddWithValue("@crpType", cropInfo.cropType);
            cmd.Parameters.AddWithValue("@cropNumber", cropInfo.cropNumber);
            cmd.Parameters.AddWithValue("@creationTime", cropInfo.creationTime);
            cmd.Parameters.AddWithValue("@waterInterval", cropInfo.waterInterval);
            cmd.Parameters.AddWithValue("@areaKey", areaKey);

            // Execute the command
            cmd.ExecuteNonQuery();
            cropId = (int) cmd.LastInsertedId;
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return cropId;
   }

   public static new void waterCrop (CropInfo cropInfo) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "UPDATE crops SET growthLevel = growthLevel + 1, lastWaterTimestamp=UNIX_TIMESTAMP() WHERE usrId=@usrId AND cropNumber=@cropNumber;", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrId", cropInfo.userId);
            cmd.Parameters.AddWithValue("@cropNumber", cropInfo.cropNumber);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new void deleteCrop (int cropNumber, int userId) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("DELETE FROM crops WHERE usrId=@usrId AND cropNumber=@cropNumber", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrId", userId);
            cmd.Parameters.AddWithValue("@cropNumber", cropNumber);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   #endregion

   #region Equipment Features

   public static new List<Armor> getArmorForAccount (int accId, int userId = 0) {
      List<Armor> armorList = new List<Armor>();
      string userClause = (userId == 0) ? " AND users.usrId != @usrId" : " AND users.usrId = @usrId";

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM users LEFT JOIN items ON (users.armId=items.itmId) WHERE accId=@accId " + userClause + " ORDER BY users.usrId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@accId", accId);
            cmd.Parameters.AddWithValue("@usrId", userId);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  Armor armor = new Armor(dataReader);
                  armorList.Add(armor);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return armorList;
   }

   public static new List<Weapon> getWeaponsForAccount (int accId, int userId = 0) {
      List<Weapon> weaponList = new List<Weapon>();
      string userClause = (userId == 0) ? " AND users.usrId != @usrId" : " AND users.usrId = @usrId";

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM users LEFT JOIN items ON (users.wpnId=items.itmId) WHERE accId=@accId " + userClause + " ORDER BY users.usrId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@accId", accId);
            cmd.Parameters.AddWithValue("@usrId", userId);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  Weapon weapon = new Weapon(dataReader);
                  weaponList.Add(weapon);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return weaponList;
   }

   public static new List<Hat> getHatsForAccount (int accId, int userId = 0) {
      List<Hat> hatList = new List<Hat>();
      string userClause = (userId == 0) ? " AND users.usrId != @usrId" : " AND users.usrId = @usrId";

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM users LEFT JOIN items ON (users.hatId=items.itmId) WHERE accId=@accId " + userClause + " ORDER BY users.usrId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@accId", accId);
            cmd.Parameters.AddWithValue("@usrId", userId);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  Hat hat = new Hat(dataReader);
                  hatList.Add(hat);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return hatList;
   }

   public static new List<Weapon> getWeaponsForUser (int userId) {
      List<Weapon> weaponList = new List<Weapon>();

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM items WHERE usrId=@usrId AND items.itmCategory=1 ORDER BY items.itmId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrId", userId);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  Weapon weapon = new Weapon(dataReader);
                  weaponList.Add(weapon);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return weaponList;
   }

   public static new List<Armor> getArmorForUser (int userId) {
      List<Armor> armorList = new List<Armor>();

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM items WHERE usrId=@usrId AND items.itmCategory=2 ORDER BY items.itmId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrId", userId);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  Armor armor = new Armor(dataReader);
                  armorList.Add(armor);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return armorList;
   }

   public static new void setWeaponId (int userId, int newWeaponId) {
      if (newWeaponId != 0 && !hasItem(userId, newWeaponId, (int) Item.Category.Weapon)) {
         D.warning(string.Format("User {0} does not have weapon {1} to equip.", userId, newWeaponId));
         return;
      }

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "UPDATE users SET wpnId=@wpnId WHERE usrId=@usrId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@wpnId", newWeaponId);
            cmd.Parameters.AddWithValue("@usrId", userId);

            // Execute the command
            int rowsAffected = cmd.ExecuteNonQuery();

            if (rowsAffected != 1) {
               D.warning("An UPDATE didn't affect just 1 row, for usrId " + userId);
            }
         }

      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new void setHatId (int userId, int newHatId) {
      if (newHatId != 0 && !hasItem(userId, newHatId, (int) Item.Category.Hats)) {
         D.warning(string.Format("User {0} does not have hat {1} to equip.", userId, newHatId));
         return;
      }

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "UPDATE users SET hatId=@hatId WHERE usrId=@usrId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@hatId", newHatId);
            cmd.Parameters.AddWithValue("@usrId", userId);

            // Execute the command
            int rowsAffected = cmd.ExecuteNonQuery();

            if (rowsAffected != 1) {
               D.warning("An UPDATE didn't affect just 1 row, for usrId " + userId);
            }
         }

      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new void setArmorId (int userId, int newArmorId) {
      if (newArmorId != 0 && !hasItem(userId, newArmorId, (int) Item.Category.Armor)) {
         D.warning(string.Format("User {0} does not have armor {1} to equip.", userId, newArmorId));
         return;
      }

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "UPDATE users SET armId=@armId WHERE usrId=@usrId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@armId", newArmorId);
            cmd.Parameters.AddWithValue("@usrId", userId);

            // Execute the command
            int rowsAffected = cmd.ExecuteNonQuery();

            if (rowsAffected != 1) {
               D.warning("An UPDATE didn't affect just 1 row, for usrId " + userId);
            }
         }

      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new Armor getArmor (int userId) {
      Armor armor = new Armor();

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM users JOIN items ON (users.armId=items.itmId) WHERE users.usrId=@usrId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrId", userId);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  int itemId = dataReader.GetInt32("itmId");
                  int itemTypeId = dataReader.GetInt32("itmType");
                  string palettes = dataReader.GetString("itmPalettes");
                  Item.Category category = (Item.Category) dataReader.GetInt32("itmCategory");

                  if (category == Item.Category.Armor) {
                     armor = new Armor(itemId, itemTypeId, palettes, dataReader.GetString("itmData"));
                  }
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return armor;
   }

   public static new Weapon getWeapon (int userId) {
      Weapon weapon = new Weapon();

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM users JOIN items ON (users.wpnId=items.itmId) WHERE users.usrId=@usrId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrId", userId);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  int itemId = dataReader.GetInt32("itmId");
                  int itemTypeId = dataReader.GetInt32("itmType");
                  string palettes = dataReader.GetString("itmPalettes");
                  Item.Category category = (Item.Category) dataReader.GetInt32("itmCategory");

                  if (category == Item.Category.Weapon) {
                     weapon = new Weapon(itemId, itemTypeId, palettes, dataReader.GetString("itmData"));
                  }
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return weapon;
   }

   #endregion

   #region Crops Features

   public static new List<SiloInfo> getSiloInfo (int userId) {
      List<SiloInfo> siloInfo = new List<SiloInfo>();

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM silo WHERE usrId=@usrId ORDER BY silo.crpType", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrId", userId);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  SiloInfo info = new SiloInfo(dataReader);
                  siloInfo.Add(info);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return siloInfo;
   }

   public static new void addToSilo (int userId, Crop.Type cropType, int amount = 1) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "INSERT INTO silo (usrId, crpType, cropCount) VALUES(@usrId, @crpType, @cropCount) " +
            "ON DUPLICATE KEY UPDATE cropCount = cropCount + " + amount, conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@crpType", (int) cropType);
            cmd.Parameters.AddWithValue("@cropCount", 1);
            cmd.Parameters.AddWithValue("@usrId", userId);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   #endregion

   public static new bool hasItem (int userId, int itemId, int itemCategory) {
      bool found = false;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT itmId FROM items WHERE itmId=@itmId AND usrId=@usrId AND itmCategory=@itemCategory", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@itmId", itemId);
            cmd.Parameters.AddWithValue("@usrId", userId);
            cmd.Parameters.AddWithValue("@itemCategory", itemCategory);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  found = true;
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return found;
   }

   public static new void setNewLocalPosition (int userId, Vector2 localPosition, Direction facingDirection, string areaKey) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "UPDATE users SET localX=@localX, localY=@localY, usrFacing=@usrFacing, areaKey=@areaKey " +
            "WHERE usrId=@usrId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrId", userId);
            cmd.Parameters.AddWithValue("@localX", localPosition.x);
            cmd.Parameters.AddWithValue("@localY", localPosition.y);
            cmd.Parameters.AddWithValue("@usrFacing", (int) facingDirection);
            cmd.Parameters.AddWithValue("@areaKey", areaKey);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new void storeShipHealth (int shipId, int shipHealth) {
      shipHealth = Mathf.Max(shipHealth, 0);

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("UPDATE ships SET ships.health=@shipHealth WHERE ships.shpId = @shipId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@shipId", shipId);
            cmd.Parameters.AddWithValue("@shipHealth", shipHealth);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   #region User Currency Features

   public static new void addGold (int userId, int amount) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("UPDATE users SET usrGold = usrGold + @amount WHERE usrId=@usrId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrId", userId);
            cmd.Parameters.AddWithValue("@amount", amount);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new void addGoldAndXP (int userId, int gold, int XP) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "UPDATE users SET usrGold=usrGold+@gold, usrXP=usrXP+@XP WHERE usrId=@usrId", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrId", userId);
            cmd.Parameters.AddWithValue("@gold", gold);
            cmd.Parameters.AddWithValue("@XP", XP);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new int getGold (int userId) {
      int gold = 0;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT usrGold FROM users WHERE usrId=@usrId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrId", userId);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  gold = dataReader.GetInt32("usrGold");
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return gold;
   }

   public static new int getGems (int accountId) {
      int gems = 0;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT accGems FROM global.accounts WHERE accId=@accId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@accId", accountId);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  gems = dataReader.GetInt32("accGems");
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return gems;
   }

   public static new void addGems (int accountId, int amount) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("UPDATE global.accounts SET accGems = accGems + @amount WHERE accId=@accId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@accId", accountId);
            cmd.Parameters.AddWithValue("@amount", amount);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   #endregion

   #region Chat System Features / Bug Reporting Features

   public static new void saveBugReport (NetEntity player, string subject, string bugReport, int ping, int fps, string playerPosition, byte[] screenshotBytes, string screenResolution, string operatingSystem) {
      try {
         using (MySqlConnection conn = getConnectionToDevGlobal())
         using (MySqlCommand cmd = new MySqlCommand("INSERT INTO global.bug_reports (usrId, usrName, accId, bugSubject, bugLog, ping, fps, playerPosition, screenResolution, operatingSystem, status) VALUES(@usrId, @usrName, @accId, @bugSubject, @bugLog, @ping, @fps, @playerPosition, @screenResolution, @operatingSystem, @status)", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrId", player.userId);
            cmd.Parameters.AddWithValue("@usrName", player.entityName);
            cmd.Parameters.AddWithValue("@accId", player.accountId);
            cmd.Parameters.AddWithValue("@bugSubject", subject);
            cmd.Parameters.AddWithValue("@bugLog", bugReport);
            cmd.Parameters.AddWithValue("@ping", ping);
            cmd.Parameters.AddWithValue("@fps", fps);
            cmd.Parameters.AddWithValue("@playerPosition", playerPosition);
            cmd.Parameters.AddWithValue("@screenResolution", screenResolution);
            cmd.Parameters.AddWithValue("@operatingSystem", operatingSystem);
            cmd.Parameters.AddWithValue("@status", ToolsUtil.UNASSIGNED);

            // Execute the command
            cmd.ExecuteNonQuery();

            // Bug Report's Id
            long bugId = cmd.LastInsertedId;

            // Saving the initial "Create" action for history purposes
            MySqlCommand actionCmd = new MySqlCommand("INSERT INTO global.bug_reports_actions (taskId, actionType, performerAccId) VALUES(@taskId, @actionType, @performerAccId)", conn);
            actionCmd.Prepare();
            actionCmd.Parameters.AddWithValue("@taskId", bugId);
            actionCmd.Parameters.AddWithValue("@actionType", ToolsUtil.CREATE);
            actionCmd.Parameters.AddWithValue("@performerAccId", player.accountId);
            actionCmd.ExecuteNonQuery();

            // Saving screenshot in bug_reports_screenshots
            MySqlCommand screenshotCmd = new MySqlCommand("INSERT INTO global.bug_reports_screenshots (taskId, image) VALUES(@taskId, @image)", conn);
            screenshotCmd.Prepare();
            screenshotCmd.Parameters.AddWithValue("@taskId", bugId);
            screenshotCmd.Parameters.AddWithValue("@image", screenshotBytes);
            screenshotCmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new int storeChatLog (int userId, string userName, string message, DateTime dateTime, ChatInfo.Type chatType, string serverIpAddress) {
      int chatId = 0;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("INSERT INTO chat_log (usrId, userName, message, time, chatType, serverIpAddress) VALUES(@userId, @userName, @message, @time, @chatType, @serverIpAddress) ", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@userId", userId);
            cmd.Parameters.AddWithValue("@message", message);
            cmd.Parameters.AddWithValue("@userName", userName);
            cmd.Parameters.AddWithValue("@serverIpAddress", serverIpAddress);
            cmd.Parameters.AddWithValue("@time", dateTime);
            cmd.Parameters.AddWithValue("@chatType", (int) chatType);

            // Execute the command
            cmd.ExecuteNonQuery();
            chatId = (int) cmd.LastInsertedId;
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return chatId;
   }

   public static new List<ChatInfo> getChat (ChatInfo.Type chatType, int seconds, string serverIpAddress, bool hasInterval = true, int limit = 0) {
      string secondsInterval = "AND time > NOW() - INTERVAL " + seconds + " SECOND";
      if (!hasInterval) {
         secondsInterval = "";
      }

      string joinUserTable = "JOIN users USING (usrId)";
      if (chatType == ChatInfo.Type.Global) {
         joinUserTable = "";
      }

      string limitValue = " limit " + limit;
      if (limit < 1) {
         limitValue = "";
      }

      List<ChatInfo> list = new List<ChatInfo>();

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM chat_log " + joinUserTable + " WHERE (chatType=@chatType and serverIpAddress=@serverIpAddress) " + secondsInterval + " ORDER BY chtId DESC" + limitValue, conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@chatType", chatType);
            cmd.Parameters.AddWithValue("@serverIpAddress", serverIpAddress);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  string message = dataReader.GetString("message");
                  int chatId = dataReader.GetInt32("chtId");
                  DateTime time = dataReader.GetDateTime("time");

                  if (chatType != ChatInfo.Type.Global) {
                     int userId = dataReader.GetInt32("usrId");
                     string senderName = dataReader.GetString("userName");
                     int senderGuild = dataReader.GetInt32("gldId");
                     ChatInfo info = new ChatInfo(chatId, message, time, chatType, senderName, userId);
                     info.guildId = senderGuild;
                     list.Add(info);
                  } else {
                     int userId = dataReader.GetInt32("usrId");
                     string senderName = userId == 0 ? "Server" : "User";
                     if (userId != 0) {
                        try {
                           senderName = dataReader.GetString("userName");
                        } catch {
                           senderName = "Deleted User";
                           //D.editorLog("No data for usrName", Color.red);
                        }
                     }
                     int senderGuild = 0;
                     ChatInfo info = new ChatInfo(chatId, message, time, chatType, senderName, userId);
                     info.guildId = senderGuild;
                     list.Add(info);
                  }
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return list;
   }

   #endregion

   #region Accounts Features / User Info Features

   public static new int getAccountId (string accountName, string accountPassword) {
      int accountId = -1;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT accId FROM global.accounts WHERE accName=@accName AND accPassword=@accPassword", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@accName", accountName);
            cmd.Parameters.AddWithValue("@accPassword", accountPassword);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  accountId = dataReader.GetInt32("accId");
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return accountId;
   }

   public static new List<UserInfo> getUsersForAccount (int accId, int userId = 0) {
      List<UserInfo> userList = new List<UserInfo>();
      string userClause = (userId == 0) ? " AND users.usrId != @usrId" : " AND users.usrId = @usrId";

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM users JOIN global.accounts USING (accId) WHERE accId=@accId " + userClause + " ORDER BY users.usrId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@accId", accId);
            cmd.Parameters.AddWithValue("@usrId", userId);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  UserInfo info = new UserInfo(dataReader);
                  userList.Add(info);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return userList;
   }

   public static new int getAccountStatus (int accountId) {
      int accountStatus = 0;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT accStatus FROM global.accounts WHERE accId=@accId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@accId", accountId);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  accountStatus = dataReader.GetInt32("accStatus");
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return accountStatus;
   }

   public static new int getAccountPermissionLevel (int accountId) {
      int accountStatus = 0;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT usrAdminFlag FROM global.accounts WHERE accId=@accId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@accId", accountId);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  accountStatus = dataReader.GetInt32("usrAdminFlag");
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return accountStatus;
   }

   public static new int getAccountId (int userId) {
      int accountId = -1;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT accId FROM users WHERE usrId=@usrId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrId", userId);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  accountId = dataReader.GetInt32("accId");
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return accountId;
   }

   public static new int getUserId (string username) {
      int userId = -1;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT usrId FROM users WHERE usrName=@usrName", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrName", username);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  userId = dataReader.GetInt32("usrId");
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return userId;
   }

   public static new UserObjects getUserObjects (int userId) {
      UserObjects userObjects = new UserObjects();

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT *, " +
            "armor.itmId AS armorId, armor.itmType AS armorType, armor.itmPalettes AS armorPalettes, armor.itmData AS armorData, " +
            "weapon.itmId AS weaponId, weapon.itmType AS weaponType, weapon.itmPalettes AS weaponPalettes, weapon.itmData AS weaponData, " +
            "hat.itmId AS hatId, hat.itmType AS hatType, hat.itmPalettes AS hatPalettes, hat.itmData AS hatData " +
            "FROM users JOIN global.accounts USING(accId) LEFT JOIN ships USING(shpId) " +
            "LEFT JOIN guilds ON(users.gldId = guilds.gldId)" +
            "LEFT JOIN items AS armor ON(users.armId = armor.itmId) " +
            "LEFT JOIN items AS weapon ON(users.wpnId = weapon.itmId) " +
            "LEFT JOIN items AS hat ON(users.hatId = hat.itmId) " +
            "WHERE users.usrId=@usrId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrId", userId);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  // There might not be any rows returned if an invalid user ID was provided
                  userObjects.accountId = DataUtil.getInt(dataReader, "accId");

                  // If we found a valid account ID, we can go ahead and read in the other various objects
                  if (userObjects.accountId != 0) {
                     userObjects.accountEmail = DataUtil.getString(dataReader, "accEmail");
                     userObjects.isSinglePlayer = DataUtil.getInt(dataReader, "isSinglePlayer") == 1 ? true : false;
                     userObjects.accountCreationTime = dataReader.GetDateTime("accCreationTime").ToBinary();
                     userObjects.userInfo = new UserInfo(dataReader);
                     userObjects.shipInfo = new ShipInfo(dataReader);
                     userObjects.guildInfo = new GuildInfo(dataReader);
                     userObjects.armor = getArmor(dataReader);
                     userObjects.weapon = getWeapon(dataReader);
                     userObjects.hat = getHat(dataReader);
                     userObjects.armorPalettes = userObjects.armor.paletteNames;
                     userObjects.weaponPalettes = userObjects.weapon.paletteNames;

                     // These aren't working as expected, so we just get the info from the objects
                     // userObjects.weaponInfo = new WeaponInfo(dataReader);
                     // userObjects.armorInfo = new ArmorInfo(dataReader);
                  }
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return userObjects;
   }

   public static new string getUserInfoJSON (string userId) {
      UserInfo userInfo = null;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM users JOIN global.accounts USING (accId) WHERE usrId=@usrId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrId", int.Parse(userId));

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  userInfo = new UserInfo(dataReader);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return JsonUtility.ToJson(userInfo);
   }

   public static new UserInfo getUserInfo (string userName) {
      UserInfo userInfo = null;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM users JOIN global.accounts USING (accId) WHERE usrName=@usrName", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrName", userName);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  userInfo = new UserInfo(dataReader);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return userInfo;
   }

   public static new Stats getStats (int userId) {
      Stats stats = new Stats(userId);

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM stats WHERE usrId=@usrId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrId", userId);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  stats.strength = dataReader.GetInt32("strength");
                  stats.precision = dataReader.GetInt32("precision");
                  stats.vitality = dataReader.GetInt32("vitality");
                  stats.intelligence = dataReader.GetInt32("intelligence");
                  stats.spirit = dataReader.GetInt32("spirit");
                  stats.luck = dataReader.GetInt32("luck");
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return stats;
   }

   public static new int createUser (int accountId, int usrAdminFlag, UserInfo userInfo, Area area) {
      int userId = 0;
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "INSERT INTO users (accId, usrName, usrGender, localX, localY, bodyType, usrAdminFlag, usrFacing, hairType, hairPalettes, eyesType, eyesPalettes, armId, areaKey, charSpot) VALUES " +
             "(@accId, @usrName, @usrGender, @localX, @localY, @bodyType, @usrAdminFlag, @usrFacing, @hairType, @hairPalettes, @eyesType, @eyesPalettes, @armId, @areaKey, @charSpot);", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@accId", accountId);
            cmd.Parameters.AddWithValue("@usrName", userInfo.username);
            cmd.Parameters.AddWithValue("@usrGender", (int) userInfo.gender);
            cmd.Parameters.AddWithValue("@localX", userInfo.localPos.x);
            cmd.Parameters.AddWithValue("@localY", userInfo.localPos.y);
            cmd.Parameters.AddWithValue("@bodyType", (int) userInfo.bodyType);
            cmd.Parameters.AddWithValue("@usrAdminFlag", usrAdminFlag);
            cmd.Parameters.AddWithValue("@usrFacing", (int) userInfo.facingDirection);
            cmd.Parameters.AddWithValue("@hairType", (int) userInfo.hairType);
            cmd.Parameters.AddWithValue("@hairPalettes", userInfo.hairPalettes);
            cmd.Parameters.AddWithValue("@eyesType", (int) userInfo.eyesType);
            cmd.Parameters.AddWithValue("@eyesPalettes", userInfo.eyesPalettes);
            cmd.Parameters.AddWithValue("@armId", userInfo.armorId);
            cmd.Parameters.AddWithValue("@areaKey", area.areaKey);
            cmd.Parameters.AddWithValue("@charSpot", userInfo.charSpot);

            // Execute the command
            cmd.ExecuteNonQuery();
            userId = (int) cmd.LastInsertedId;
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return userId;
   }

   public static new void deleteUser (int accountId, int userId) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("DELETE FROM users WHERE accId=@accId AND usrId=@usrId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@accId", accountId);
            cmd.Parameters.AddWithValue("@usrId", userId);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   #endregion

   #region Inventory Features

   public static new Item createNewItem (int userId, Item baseItem) {
      Item newItem = baseItem.Clone();

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "INSERT INTO items (usrId, itmCategory, itmType, itmPalettes, itmData, itmCount) " +
            "VALUES(@usrId, @itmCategory, @itmType, @itmPalettes, @itmData, @itmCount) ", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrId", userId);
            cmd.Parameters.AddWithValue("@itmCategory", (int) baseItem.category);
            cmd.Parameters.AddWithValue("@itmType", (int) baseItem.itemTypeId);
            cmd.Parameters.AddWithValue("@itmPalettes", baseItem.paletteNames);
            cmd.Parameters.AddWithValue("@itmData", baseItem.data);
            cmd.Parameters.AddWithValue("@itmCount", baseItem.count);

            // Execute the command
            cmd.ExecuteNonQuery();
            newItem.id = (int) cmd.LastInsertedId;
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return newItem;
   }

   public static new int insertNewArmor (int userId, int armorType, string palettes) {
      int itemId = 0;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "INSERT INTO items (usrId, itmCategory, itmType, itmPalettes, itmData) " +
                 "VALUES(@usrId, @itmCategory, @itmType, @itmPalettes, @itmData) ", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrId", userId);
            cmd.Parameters.AddWithValue("@itmCategory", (int) Item.Category.Armor);
            cmd.Parameters.AddWithValue("@itmType", armorType);
            cmd.Parameters.AddWithValue("@itmPalettes", palettes);
            cmd.Parameters.AddWithValue("@itmData", "");

            // Execute the command
            cmd.ExecuteNonQuery();
            itemId = (int) cmd.LastInsertedId;
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return itemId;
   }

   public static new int insertNewWeapon (int userId, int weaponType, string palettes) {
      int itemId = 0;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "INSERT INTO items (usrId, itmCategory, itmType, itmPalettes, itmData) " +
                 "VALUES(@usrId, @itmCategory, @itmType, @itmPalettes, @itmData) ", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrId", userId);
            cmd.Parameters.AddWithValue("@itmCategory", (int) Item.Category.Weapon);
            cmd.Parameters.AddWithValue("@itmType", (int) weaponType);
            cmd.Parameters.AddWithValue("@itmPalettes", palettes);
            cmd.Parameters.AddWithValue("@itmData", "");

            // Execute the command
            cmd.ExecuteNonQuery();
            itemId = (int) cmd.LastInsertedId;
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return itemId;
   }

   public static new void setItemOwner (int userId, int itemId) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("UPDATE items SET usrId=@usrId WHERE itmId=@itmId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@itmId", itemId);
            cmd.Parameters.AddWithValue("@usrId", userId);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new int getItemID (int userId, int itmCategory, int itmType) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM items WHERE usrId=@usrId and itmCategory=@itmCategory and itmType=@itmType", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrId", userId);
            cmd.Parameters.AddWithValue("@itmCategory", itmCategory);
            cmd.Parameters.AddWithValue("@itmType", itmType);
            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  return dataReader.GetInt32("itmId");
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
      return 0;
   }

   public static new void updateItemQuantity (int userId, int itemId, int itemCount) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("UPDATE items SET itmCount=@itmCount WHERE usrId=@usrId and itmId=@itmId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrId", userId);
            cmd.Parameters.AddWithValue("@itmId", itemId);
            cmd.Parameters.AddWithValue("@itmCount", itemCount);
            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new void decreaseQuantityOrDeleteItem (int userId, int itemId, int deductedValue) {
      // First query deletes the entry which has only 1 of item left
      // Second query decreases the count by deductedValue if the item wasn't deleted (had more than deductedValue left)
      string cmdText = "DELETE FROM items WHERE usrId=@usrId AND itmId=@itmId AND itmCount<=@deductBy; " +
         "UPDATE items SET itmCount = itmCount - @deductBy WHERE usrId=@usrId AND itmId=@itmId;";
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(cmdText, conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrId", userId);
            cmd.Parameters.AddWithValue("@itmId", itemId);
            cmd.Parameters.AddWithValue("@deductBy", deductedValue);

            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new Item createItemOrUpdateItemCount (int userId, Item baseItem) {
      // Make sure that we have the right class
      Item castedItem = baseItem.getCastItem();

      // Verify if the item can be stacked
      if (castedItem.canBeStacked()) {
         // Retrieve the item from the database, if it exists
         Item databaseItem = getFirstItem(userId, castedItem.category, castedItem.itemTypeId);

         // If the item exist, update its count
         if (databaseItem != null) {
            databaseItem.count += castedItem.count;
            updateItemQuantity(userId, databaseItem.id, databaseItem.count);
            // Return the updated item
            return databaseItem;
         } else {
            // Otherwise, create a new stack
            return createNewItem(userId, castedItem).getCastItem();
         }
      } else {
         // Since the item cannot be stacked, set its count to 1
         castedItem.count = 1;

         // Create the item
         return createNewItem(userId, castedItem).getCastItem();
      }
   }

   public static new void transferItem (Item item, int fromUserId, int toUserId, int amount) {
      // Make sure that we have the right class
      Item fromItem = item.getCastItem();

      // If the item is not stackable, simply update its user id
      if (!fromItem.canBeStacked()) {
         updateItemUserId(item.id, fromUserId, toUserId);
      } else {
         // Determine if the sender has enough items in his stack
         if (fromItem.count < amount) {
            D.error(string.Format("Not enough items in the stack ({0}) to transfer the requested amount ({1})", item.count, amount));
            return;
         }

         // Group the queries in a single atomic transaction    
         StringBuilder query = new StringBuilder();
         query.Append("BEGIN;");

         // Determine how the sender user inventory is updated         
         if (fromItem.count == amount) {
            // If the whole stack must be transferred, the item of fromUser must be deleted
            query.Append("DELETE FROM items WHERE itmId=@fromItmId AND usrId=@fromUsrId;");
         } else {
            // If part of the stack must be transferred, the item count must be updated
            query.Append("UPDATE items SET itmCount=@fromItmCount WHERE itmId=@fromItmId AND usrId=@fromUsrId;");
         }

         // Get the same item type from the recipient inventory, if it exists
         Item toItem = getFirstItem(toUserId, fromItem.category, fromItem.itemTypeId);

         // Determine how the recipient user inventory is updated
         if (toItem != null) {
            // If the recipient has a stack, the item count must be updated
            query.Append("UPDATE items SET itmCount=@toItmCount WHERE usrId=@toUsrId and itmId=@toItmId;");
         } else {
            // If the recipient has no stack, the item must be created
            query.Append("INSERT INTO items (usrId, itmCategory, itmType, itmPalettes, itmData, itmCount) ");
            query.Append("VALUES(@toUsrId, @itmCategory, @itmType, @itmPalettes, @itmData, @toItmCount);");
         }

         // Close the transaction
         query.Append("COMMIT;");

         // Run the query
         try {
            using (MySqlConnection conn = getConnection())
            using (MySqlCommand cmd = new MySqlCommand(query.ToString(), conn)) {

               conn.Open();
               cmd.Prepare();

               // Item parameters
               cmd.Parameters.AddWithValue("@itmCategory", (int) fromItem.category);
               cmd.Parameters.AddWithValue("@itmType", (int) fromItem.itemTypeId);
               cmd.Parameters.AddWithValue("@itmPalettes", fromItem.paletteNames);
               cmd.Parameters.AddWithValue("@itmData", fromItem.data);

               // From
               cmd.Parameters.AddWithValue("@fromItmId", fromItem.id);
               cmd.Parameters.AddWithValue("@fromUsrId", fromUserId);
               cmd.Parameters.AddWithValue("@fromItmCount", fromItem.count - amount);

               // To
               if (toItem != null) {
                  cmd.Parameters.AddWithValue("@toItmId", toItem.id);
                  cmd.Parameters.AddWithValue("@toItmCount", toItem.count + amount);
               } else {
                  cmd.Parameters.AddWithValue("@toItmCount", amount);
               }
               cmd.Parameters.AddWithValue("@toUsrId", toUserId);

               // Execute the command
               cmd.ExecuteNonQuery();
            }
         } catch (Exception e) {
            D.error("MySQL Error: " + e.ToString());
         }
      }
   }

   // Prefer using transferItem() to change an item user id
   private static void updateItemUserId (int itemId, int fromUserId, int toUserId) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("UPDATE items SET usrId=@toUsrId WHERE usrId=@fromUsrId AND itmId=@itmId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@toUsrId", toUserId);
            cmd.Parameters.AddWithValue("@fromUsrId", fromUserId);
            cmd.Parameters.AddWithValue("@itmId", itemId);
            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new int getItemCountByType (int userId, int itemCategory, int itemType) {
      int itemCount = 0;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT IFNULL(SUM(itmCount), 0) AS itemCount FROM items " +
            "WHERE usrId=@usrId AND itmCategory=@itmCategory AND itmType=@itmType", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrId", userId);
            cmd.Parameters.AddWithValue("@itmCategory", itemCategory);
            cmd.Parameters.AddWithValue("@itmType", itemType);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  itemCount = dataReader.GetInt32("itemCount");
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return itemCount;
   }

   public static new int getItemCountByCategory (int userId, Item.Category[] categories) {
      int[] categoryInt = Array.ConvertAll(categories.ToArray(), x => (int) x);
      string categoryJson = JsonConvert.SerializeObject(categoryInt);
      return int.Parse(getItemCount(userId.ToString(), categoryJson, "", ""));
   }

   public static new string getItemCount (string userId, string categoriesJSON, string itemIdsToFilterJSON, string categoriesToFilterJSON) {
      int[] categories = categoriesJSON.Length > 1 ? JsonConvert.DeserializeObject<int[]>(categoriesJSON) : new int[0];
      int[] itemIdsToFilter = itemIdsToFilterJSON.Length > 1 ? JsonConvert.DeserializeObject<int[]>(itemIdsToFilterJSON) : new int[0];
      int[] categoriesToFilter = categoriesToFilterJSON.Length > 1 ? JsonConvert.DeserializeObject<int[]>(categoriesToFilterJSON) : new int[0];

      // Initialize the count
      int itemCount = 0;

      // Build the query
      StringBuilder query = new StringBuilder();
      query.Append("SELECT count(*) AS itemCount FROM items WHERE usrId=@usrId ");

      // Add the category filter only if the first is not 'none' or if there are many
      if ((Item.Category) categories[0] != Item.Category.None || categories.Length > 1) {
         // Setup multiple categories
         query.Append("AND (itmCategory=@itmCategory0");
         for (int i = 1; i < categories.Length; i++) {
            query.Append(" OR itmCategory=@itmCategory" + i);
         }
         query.Append(") ");
      }

      // Filter categories
      if (categoriesToFilter.Length > 0) {
         query.Append("AND itmCategory NOT IN (");
         for (int i = 0; i < categoriesToFilter.Length; i++) {
            query.Append("@filteredCategory" + i + ", ");
         }

         // Delete the last ", "
         query.Length = query.Length - 2;

         query.Append(") ");
      }

      // Filter given item ids
      if (itemIdsToFilter != null && itemIdsToFilter.Length > 0) {
         query.Append("AND itmId NOT IN (");
         for (int i = 0; i < itemIdsToFilter.Length; i++) {
            query.Append("@filteredItemId" + i + ", ");
         }

         // Delete the last ", "
         query.Length = query.Length - 2;

         query.Append(") ");
      }
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(query.ToString(), conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrId", userId);

            for (int i = 0; i < categories.Length; i++) {
               cmd.Parameters.AddWithValue("@itmCategory" + i, (int) categories[i]);
            }

            for (int i = 0; i < itemIdsToFilter.Length; i++) {
               cmd.Parameters.AddWithValue("@filteredItemId" + i, itemIdsToFilter[i]);
            }

            for (int i = 0; i < categoriesToFilter.Length; i++) {
               cmd.Parameters.AddWithValue("@filteredCategory" + i, categoriesToFilter[i]);
            }

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  itemCount = dataReader.GetInt32("itemCount");
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return itemCount.ToString();
   }

   public static new List<Item> getItems (int userId, Item.Category[] categories, int page, int itemsPerPage) {
      return getItems(userId, categories, page, itemsPerPage, new List<int>(), new List<Item.Category>());
   }

   public static new List<Item> getItems (int userId, Item.Category[] categories, int page, int itemsPerPage,
      List<int> itemIdsToFilter, List<Item.Category> categoriesToFilter) {
      // Initialize the list
      List<Item> itemList = new List<Item>();

      // Build the query
      StringBuilder query = new StringBuilder();
      query.Append("SELECT * FROM items WHERE usrId = @usrId ");

      // Add the category filter only if the first is not 'none' or if there are many
      if (categories[0] != Item.Category.None || categories.Length > 1) {
         // Setup multiple categories
         query.Append("AND (itmCategory=@itmCategory0");
         for (int i = 1; i < categories.Length; i++) {
            query.Append(" OR itmCategory=@itmCategory" + i);
         }
         query.Append(") ");
      }

      // Filter categories
      if (categoriesToFilter.Count > 0) {
         query.Append("AND itmCategory NOT IN (");
         for (int i = 0; i < categoriesToFilter.Count; i++) {
            query.Append("@filteredCategory" + i + ", ");
         }

         // Delete the last ", "
         query.Length = query.Length - 2;

         query.Append(") ");
      }

      // Filter given item ids
      if (itemIdsToFilter.Count > 0) {
         query.Append("AND itmId NOT IN (");
         for (int i = 0; i < itemIdsToFilter.Count; i++) {
            query.Append("@filteredItemId" + i + ", ");
         }

         // Delete the last ", "
         query.Length = query.Length - 2;

         query.Append(") ");
      }

      // Sorts the item ID
      query.Append("ORDER BY itmId DESC");

      // Removes the limit if the page is -1
      if (page > 0 && itemsPerPage > 0) {
         query.Append(" LIMIT @start, @perPage");
      }

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(query.ToString(), conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrId", userId);
            cmd.Parameters.AddWithValue("@start", (page - 1) * itemsPerPage);
            cmd.Parameters.AddWithValue("@perPage", itemsPerPage);
            for (int i = 0; i < categories.Length; i++) {
               cmd.Parameters.AddWithValue("@itmCategory" + i, (int) categories[i]);
            }
            for (int i = 0; i < itemIdsToFilter.Count; i++) {
               cmd.Parameters.AddWithValue("@filteredItemId" + i, itemIdsToFilter[i]);
            }
            for (int i = 0; i < categoriesToFilter.Count; i++) {
               cmd.Parameters.AddWithValue("@filteredCategory" + i, categoriesToFilter[i]);
            }

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  int itemId = dataReader.GetInt32("itmId");
                  Item.Category itemCategory = (Item.Category) dataReader.GetInt32("itmCategory");
                  int itemTypeId = dataReader.GetInt32("itmType");
                  string palettes = dataReader.GetString("itmPalettes");
                  string data = dataReader.GetString("itmData");
                  int count = dataReader.GetInt32("itmCount");

                  // Create an Item instance of the proper class, and then add it to the list
                  Item item = new Item(itemId, itemCategory, itemTypeId, count, palettes, data);
                  itemList.Add(item.getCastItem());
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return itemList;
   }

   public static new List<Item> getCraftingIngredients (int usrId, List<CraftingIngredients.Type> ingredientTypes) {
      List<Item> itemList = new List<Item>();

      // If no ingredient is given, return an empty list
      if (ingredientTypes == null || ingredientTypes.Count <= 0) {
         return itemList;
      }

      // Build the item type list condition
      StringBuilder builder = new StringBuilder();
      for (int i = 0; i < ingredientTypes.Count; i++) {
         int itmType = (int) ingredientTypes[i];
         if (i > 0) {
            builder.Append(" or ");
         }
         builder.Append("itmType = " + itmType);
      }

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(string.Format("SELECT * FROM arcane.items WHERE usrId = @usrId AND itmCategory = @itmCategory AND ({0})", builder.ToString()), conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@itmCategory", (int) Item.Category.CraftingIngredients);
            cmd.Parameters.AddWithValue("@usrId", usrId);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  int itemId = DataUtil.getInt(dataReader, "itmId");
                  Item.Category category = (Item.Category) DataUtil.getInt(dataReader, "itmCategory");
                  int itemTypeId = DataUtil.getInt(dataReader, "itmType");
                  string palettes = DataUtil.getString(dataReader, "itmPalettes");
                  string data = DataUtil.getString(dataReader, "itmData");
                  int itemCount = DataUtil.getInt(dataReader, "itmCount");

                  Item newItem = new Item(itemId, category, itemTypeId, itemCount, palettes, data);
                  itemList.Add(newItem);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
      return itemList;
   }

   public static new int insertNewUsableItem (int userId, UsableItem.Type itemType, string palettes) {
      int itemId = 0;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "INSERT INTO items (usrId, itmCategory, itmType, itmPalettes, itmData) " +
                 "VALUES(@usrId, @itmCategory, @itmType, @itmPalettes, @itmData) ", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrId", userId);
            cmd.Parameters.AddWithValue("@itmCategory", (int) Item.Category.Usable);
            cmd.Parameters.AddWithValue("@itmType", (int) itemType);
            cmd.Parameters.AddWithValue("@itmPalettes", palettes);
            cmd.Parameters.AddWithValue("@itmData", "");

            // Execute the command
            cmd.ExecuteNonQuery();
            itemId = (int) cmd.LastInsertedId;
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return itemId;
   }

   public static new int insertNewUsableItem (int userId, UsableItem.Type itemType, Ship.SkinType skinType) {
      int itemId = 0;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "INSERT INTO items (usrId, itmCategory, itmType, itmPalettes, itmData) " +
                 "VALUES(@usrId, @itmCategory, @itmType, @itmPalettes, @itmData) ", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrId", userId);
            cmd.Parameters.AddWithValue("@itmCategory", (int) Item.Category.Usable);
            cmd.Parameters.AddWithValue("@itmType", (int) itemType);
            cmd.Parameters.AddWithValue("@itmPalettes", "");
            cmd.Parameters.AddWithValue("@itmData", "skinType=" + ((int) skinType));

            // Execute the command
            cmd.ExecuteNonQuery();
            itemId = (int) cmd.LastInsertedId;
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return itemId;
   }

   public static new int insertNewUsableItem (int userId, UsableItem.Type itemType, HairLayer.Type hairType) {
      int itemId = 0;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "INSERT INTO items (usrId, itmCategory, itmType, itmPalettes, itmData) " +
                 "VALUES(@usrId, @itmCategory, @itmType, @itmPalettes, @itmData) ", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrId", userId);
            cmd.Parameters.AddWithValue("@itmCategory", (int) Item.Category.Usable);
            cmd.Parameters.AddWithValue("@itmType", (int) itemType);
            cmd.Parameters.AddWithValue("@itmPalettes", "");
            cmd.Parameters.AddWithValue("@itmData", "hairType=" + ((int) hairType));

            // Execute the command
            cmd.ExecuteNonQuery();
            itemId = (int) cmd.LastInsertedId;
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return itemId;
   }

   public static new int deleteItem (int userId, int itemId) {
      int rowsAffected = 0;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("DELETE FROM items WHERE itmId=@itmId AND usrId=@usrId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@itmId", itemId);
            cmd.Parameters.AddWithValue("@usrId", userId);

            // Execute the command
            rowsAffected = cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return rowsAffected;
   }

   public static new Item getItem (int userId, int itemId) {
      Item item = null;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM items WHERE usrId=@usrId AND itmId=@itmId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrId", userId);
            cmd.Parameters.AddWithValue("@itmId", itemId);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  Item.Category category = (Item.Category) dataReader.GetInt32("itmCategory");
                  int itemTypeId = dataReader.GetInt32("itmType");
                  string palettes = dataReader.GetString("itmPalettes");
                  string data = dataReader.GetString("itmData");
                  int count = dataReader.GetInt32("itmCount");

                  // Create an Item instance of the proper class, and then add it to the list
                  item = new Item(itemId, category, itemTypeId, count, palettes, data);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      if (item != null) {
         return item.getCastItem();
      } else {
         return null;
      }
   }

   public static new Item getFirstItem (int userId, Item.Category itemCategory, int itemTypeId) {
      Item item = null;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM items WHERE usrId=@usrId AND itmCategory=@itmCategory AND itmType=@itmType", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrId", userId);
            cmd.Parameters.AddWithValue("@itmCategory", (int) itemCategory);
            cmd.Parameters.AddWithValue("@itmType", itemTypeId);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               if (dataReader.Read()) {
                  int itemId = dataReader.GetInt32("itmId");
                  string palettes = dataReader.GetString("itmPalettes");
                  string data = dataReader.GetString("itmData");
                  int count = dataReader.GetInt32("itmCount");

                  // Create an Item instance of the proper class
                  item = new Item(itemId, itemCategory, itemTypeId, count, palettes, data);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      if (item != null) {
         return item.getCastItem();
      } else {
         return null;
      }
   }

   public static new void updateItemShortcut (int userId, int slotNumber, int itemId) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "INSERT INTO item_shortcuts(userId, slotNumber, itemId) " +
            "VALUES (@userId, @slotNumber, @itemId) " +
            "ON DUPLICATE KEY UPDATE itemId=values(itemId)", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@userId", userId);
            cmd.Parameters.AddWithValue("@slotNumber", slotNumber);
            cmd.Parameters.AddWithValue("@itemId", itemId);

            // Execute the command
            cmd.ExecuteNonQuery();

         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new void deleteItemShortcut (int userId, int slotNumber) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "DELETE FROM item_shortcuts WHERE userId=@userId AND slotNumber=@slotNumber", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@userId", userId);
            cmd.Parameters.AddWithValue("@slotNumber", slotNumber);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new List<ItemShortcutInfo> getItemShortcutList (int userId) {
      List<ItemShortcutInfo> shortcutList = new List<ItemShortcutInfo>();

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM item_shortcuts JOIN items ON item_shortcuts.itemId = items.itmId " +
            "WHERE item_shortcuts.userId=@userId AND items.usrId = @userId " +
            "ORDER BY item_shortcuts.slotNumber", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@userId", userId);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  ItemShortcutInfo shortcut = new ItemShortcutInfo(dataReader);
                  shortcutList.Add(shortcut);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return shortcutList;
   }

   #endregion

   public static new void deleteAllFromTable (int accountId, int userId, string table) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("DELETE " + table + " FROM users JOIN " + table + " USING (usrId) WHERE accId=@accId AND usrId=@usrId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@accId", accountId);
            cmd.Parameters.AddWithValue("@usrId", userId);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new void setHairColor (int userId, string newPalette) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "UPDATE users SET hairPalettes=@hairPalettes WHERE usrId=@usrId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@hairPalettes", newPalette);
            cmd.Parameters.AddWithValue("@usrId", userId);

            // Execute the command
            int rowsAffected = cmd.ExecuteNonQuery();

            if (rowsAffected != 1) {
               D.warning("An UPDATE didn't affect just 1 row, for usrId " + userId);
            }
         }

      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new void setHairType (int userId, HairLayer.Type newType) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "UPDATE users SET hairType=@hairType WHERE usrId=@usrId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@hairType", (int) newType);
            cmd.Parameters.AddWithValue("@usrId", userId);

            // Execute the command
            int rowsAffected = cmd.ExecuteNonQuery();

            if (rowsAffected != 1) {
               D.warning("An UPDATE didn't affect just 1 row, for usrId " + userId);
            }
         }

      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   #region Ship Features

   public static new ShipInfo getShipInfo (int shipId) {
      ShipInfo shipInfo = null;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM ships LEFT JOIN users USING (shpId) WHERE ships.shpId=@shipId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@shipId", shipId);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  shipInfo = new ShipInfo(dataReader);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return shipInfo;
   }

   public static new ShipInfo createStartingShip (int userId) {
      Ship.Type shipType = Ship.Type.Type_1;
      ShipInfo shipInfo = new ShipInfo(0, userId, shipType, Ship.SkinType.None, Ship.MastType.Type_1, Ship.SailType.Type_1, Ship.getDisplayName(shipType),
            PaletteDef.ShipHull.Brown, PaletteDef.ShipHull.Brown, PaletteDef.ShipSail.White, PaletteDef.ShipSail.White, 100, 100, 20,
            80, 80, 15, 100, 90, 10, Rarity.Type.Common, new ShipAbilityInfo(false));
      shipInfo.shipAbilities.ShipAbilities = new int[] { ShipAbilityInfo.DEFAULT_ABILITY };

      System.Xml.Serialization.XmlSerializer ser = new System.Xml.Serialization.XmlSerializer(shipInfo.shipAbilities.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, shipInfo.shipAbilities);
      }

      string serializedShipAbilities = sb.ToString();

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "INSERT INTO ships (usrId, shpType, palette1, palette2, mastType, sailType, shpName, sailPalette1, sailPalette2, supplies, suppliesMax, cargoMax, health, maxHealth, attackRange, speed, sailors, rarity, shipAbilities) " +
            "VALUES(@usrId, @shpType, @palette1, @palette2, @mastType, @sailType, @shipName, @sailPalette1, @sailPalette2, @supplies, @suppliesMax, @cargoMax, @maxHealth, @maxHealth, @attackRange, @speed, @sailors, @rarity, @shipAbilities)", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrId", userId);
            cmd.Parameters.AddWithValue("@shpType", (int) shipInfo.shipType);
            cmd.Parameters.AddWithValue("@skinType", (int) shipInfo.skinType);
            cmd.Parameters.AddWithValue("@palette1", shipInfo.palette1);
            cmd.Parameters.AddWithValue("@palette2", shipInfo.palette2);
            cmd.Parameters.AddWithValue("@mastType", (int) shipInfo.mastType);
            cmd.Parameters.AddWithValue("@sailType", (int) shipInfo.sailType);
            cmd.Parameters.AddWithValue("@shipName", shipInfo.shipName);
            cmd.Parameters.AddWithValue("@sailPalette1", shipInfo.sailPalette1);
            cmd.Parameters.AddWithValue("@sailPalette2", shipInfo.sailPalette2);
            cmd.Parameters.AddWithValue("@supplies", shipInfo.supplies);
            cmd.Parameters.AddWithValue("@suppliesMax", shipInfo.suppliesMax);
            cmd.Parameters.AddWithValue("@cargoMax", shipInfo.cargoMax);
            cmd.Parameters.AddWithValue("@health", shipInfo.maxHealth);
            cmd.Parameters.AddWithValue("@maxHealth", shipInfo.maxHealth);
            cmd.Parameters.AddWithValue("@attackRange", shipInfo.attackRange);
            cmd.Parameters.AddWithValue("@speed", shipInfo.speed);
            cmd.Parameters.AddWithValue("@sailors", shipInfo.sailors);
            cmd.Parameters.AddWithValue("@rarity", (int) shipInfo.rarity);
            cmd.Parameters.AddWithValue("@shipAbilities", serializedShipAbilities);

            // Execute the command
            cmd.ExecuteNonQuery();
            shipInfo.shipId = (int) cmd.LastInsertedId;
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return shipInfo;
   }

   public static new void updateShipAbilities (int shipId, string abilityXML) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("UPDATE ships SET ships.shipAbilities=@shipAbilities WHERE ships.shpId = @shipId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@shipId", shipId);
            cmd.Parameters.AddWithValue("@shipAbilities", abilityXML);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new ShipInfo createShipFromShipyard (int userId, ShipInfo shipyardInfo) {
      ShipInfo shipInfo = new ShipInfo();

      System.Xml.Serialization.XmlSerializer ser = new System.Xml.Serialization.XmlSerializer(shipyardInfo.shipAbilities.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, shipyardInfo.shipAbilities);
      }

      string serializedShipAbilities = sb.ToString();
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "INSERT INTO ships (usrId, shpType, palette1, palette2, mastType, sailType, shpName, sailPalette1, sailPalette2, supplies, suppliesMax, cargoMax, health, maxHealth, damage, sailors, attackRange, speed, rarity, shipAbilities) " +
            "VALUES(@usrId, @shpType, @palette1, @palette2, @mastType, @sailType, @shipName, @sailPalette1, @sailPalette2, @supplies, @suppliesMax, @cargoMax, @health, @maxHealth, @damage, @sailors, @attackRange, @speed, @rarity, @shipAbilities)", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrId", userId);
            cmd.Parameters.AddWithValue("@shpType", (int) shipyardInfo.shipType);
            cmd.Parameters.AddWithValue("@skinType", (int) shipyardInfo.skinType);
            cmd.Parameters.AddWithValue("@palette1", shipyardInfo.palette1);
            cmd.Parameters.AddWithValue("@palette2", shipyardInfo.palette2);
            cmd.Parameters.AddWithValue("@mastType", (int) shipyardInfo.mastType);
            cmd.Parameters.AddWithValue("@sailType", (int) shipyardInfo.sailType);
            cmd.Parameters.AddWithValue("@shipName", shipyardInfo.shipName);
            cmd.Parameters.AddWithValue("@sailPalette1", shipyardInfo.sailPalette1);
            cmd.Parameters.AddWithValue("@sailPalette2", shipyardInfo.sailPalette2);
            cmd.Parameters.AddWithValue("@supplies", shipyardInfo.supplies);
            cmd.Parameters.AddWithValue("@suppliesMax", shipyardInfo.suppliesMax);
            cmd.Parameters.AddWithValue("@cargoMax", shipyardInfo.cargoMax);
            cmd.Parameters.AddWithValue("@health", shipyardInfo.maxHealth);
            cmd.Parameters.AddWithValue("@maxHealth", shipyardInfo.maxHealth);
            cmd.Parameters.AddWithValue("@attackRange", shipyardInfo.attackRange);
            cmd.Parameters.AddWithValue("@damage", shipyardInfo.damage);
            cmd.Parameters.AddWithValue("@sailors", shipyardInfo.sailors);
            cmd.Parameters.AddWithValue("@speed", shipyardInfo.speed);
            cmd.Parameters.AddWithValue("@rarity", (int) shipyardInfo.rarity);
            cmd.Parameters.AddWithValue("@shipAbilities", serializedShipAbilities);

            // Execute the command
            cmd.ExecuteNonQuery();
            shipInfo.shipId = (int) cmd.LastInsertedId;
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return shipInfo;
   }

   public static new List<ShipInfo> getShips (int userId, int page, int shipsPerPage) {
      List<ShipInfo> shipList = new List<ShipInfo>();

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM ships JOIN users USING (usrId) WHERE ships.usrId=@usrId ORDER BY ships.shpType ASC, ships.shpId ASC LIMIT @start, @perPage", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrId", userId);
            cmd.Parameters.AddWithValue("@start", (page - 1) * shipsPerPage);
            cmd.Parameters.AddWithValue("@perPage", shipsPerPage);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  ShipInfo ship = new ShipInfo(dataReader);
                  shipList.Add(ship);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return shipList;
   }

   public static new void setShipSkin (int shipId, Ship.SkinType newSkin) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "UPDATE ships SET skinType=@skinType WHERE shpId=@shipId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@skinType", (int) newSkin);
            cmd.Parameters.AddWithValue("@shipId", shipId);

            // Execute the command
            int rowsAffected = cmd.ExecuteNonQuery();

            if (rowsAffected != 1) {
               D.warning("An UPDATE didn't affect just 1 row, for shipId " + shipId);
            }
         }

      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new void setCurrentShip (int userId, int shipId) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("UPDATE users SET shpId=@shipId WHERE usrId=@userId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@shipId", shipId);
            cmd.Parameters.AddWithValue("@userId", userId);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   #endregion

   #region Jobs Features / Guild Features

   public static new Jobs getJobXP (int userId) {
      Jobs jobs = new Jobs(userId);

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM jobs WHERE usrId=@usrId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrId", userId);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  jobs.farmerXP = dataReader.GetInt32("farming");
                  jobs.explorerXP = dataReader.GetInt32("exploring");
                  jobs.sailorXP = dataReader.GetInt32("sailing");
                  jobs.traderXP = dataReader.GetInt32("trading");
                  jobs.crafterXP = dataReader.GetInt32("crafting");
                  jobs.minerXP = dataReader.GetInt32("mining");
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return jobs;
   }

   public static new GuildInfo getGuildInfo (int guildId) {
      GuildInfo info = null;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM guilds WHERE gldId=@gldId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@gldId", guildId);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  info = new GuildInfo(dataReader);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      // Look up the members only if guild id is valid
      if (guildId > 0) {
         info.guildMembers = DB_Main.getUsersForGuild(guildId).ToArray();
      }

      return info;
   }

   public static new List<UserInfo> getUsersForGuild (int guildId) {
      List<UserInfo> userList = new List<UserInfo>();

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM users JOIN global.accounts USING (accId) WHERE gldId=@gldId ", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@gldId", guildId);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  UserInfo info = new UserInfo(dataReader);
                  userList.Add(info);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return userList;
   }

   public static new int getUserGuildId (int userId) {
      int guildId = -1;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT gldId FROM users WHERE usrId=@usrId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrId", userId);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  guildId = dataReader.GetInt32("gldId");
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return guildId;
   }

   public static new int getMemberCountForGuild (int guildId) {
      int memberCount = 0;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) AS memberCount FROM users WHERE gldId=@gldId ", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@gldId", guildId);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  memberCount = dataReader.GetInt32("memberCount");
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return memberCount;
   }

   public static new int createGuild (GuildInfo guildInfo) {
      int guildId = 0;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "INSERT INTO guilds (gldName, gldIconBorder, gldIconBackground, gldIconSigil, gldIconBackPalettes, gldIconSigilPalettes) " +
            "VALUES(@gldName, @gldIconBorder, @gldIconBackground, @gldIconSigil, @gldIconBackPalettes, @gldIconSigilPalettes) ", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@gldName", guildInfo.guildName);
            cmd.Parameters.AddWithValue("@gldIconBorder", guildInfo.iconBorder);
            cmd.Parameters.AddWithValue("@gldIconBackground", guildInfo.iconBackground);
            cmd.Parameters.AddWithValue("@gldIconSigil", guildInfo.iconSigil);
            cmd.Parameters.AddWithValue("@gldIconBackPalettes", guildInfo.iconBackPalettes);
            cmd.Parameters.AddWithValue("@gldIconSigilPalettes", guildInfo.iconSigilPalettes);

            // Execute the command
            cmd.ExecuteNonQuery();
            guildId = (int) cmd.LastInsertedId;
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return guildId;
   }

   public static new void deleteGuild (int guildId) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("DELETE FROM guilds WHERE gldId=@gldId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@gldId", guildId);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new void assignGuild (int userId, int guildId) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("UPDATE users SET gldId=@gldId WHERE usrId=@userId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@userId", userId);
            cmd.Parameters.AddWithValue("@gldId", guildId);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new void addJobXP (int userId, Jobs.Type jobType, int XP) {
      string columnName = Jobs.getColumnName(jobType);

      // Update the jobs xp for the user
      string query = "UPDATE jobs SET " + columnName + " = " + columnName + " + @XP WHERE usrId=@usrId; ";

      // Log the xp gain in the history table
      query += "INSERT INTO job_history (usrId, jobType, metric, jobTime)" +
            "VALUES (@usrId, @jobType, @XP, @jobTime);";

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(query, conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrId", userId);
            cmd.Parameters.AddWithValue("@XP", XP);

            cmd.Parameters.AddWithValue("@jobType", (int) jobType);
            cmd.Parameters.AddWithValue("@jobTime", DateTime.UtcNow);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new void insertIntoJobs (int userId) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "INSERT INTO jobs (usrId) " +
                 "VALUES(@usrId) ", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrId", userId);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   #endregion

   #region Trade History

   public static new void addToTradeHistory (int userId, TradeHistoryInfo tradeInfo) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "INSERT INTO trade_history (usrId, shpId, areaKey, crgType, amount, unitPrice, totalPrice, unitXP, totalXP, tradeTime) " +
            "VALUES(@usrId, @shpId, @areaKey, @crgType, @amount, @unitPrice, @totalPrice, @unitXP, @totalXP, @tradeTime)", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrId", userId);
            cmd.Parameters.AddWithValue("@shpId", tradeInfo.shipId);
            cmd.Parameters.AddWithValue("@areaKey", tradeInfo.areaKey);
            cmd.Parameters.AddWithValue("@crgType", (int) tradeInfo.cargoType);
            cmd.Parameters.AddWithValue("@amount", tradeInfo.amount);
            cmd.Parameters.AddWithValue("@unitPrice", tradeInfo.pricePerUnit);
            cmd.Parameters.AddWithValue("@totalPrice", tradeInfo.totalPrice);
            cmd.Parameters.AddWithValue("@unitXP", tradeInfo.xpPerUnit);
            cmd.Parameters.AddWithValue("@totalXP", tradeInfo.totalXP);
            cmd.Parameters.AddWithValue("@tradeTime", DateTime.FromBinary(tradeInfo.tradeTime));

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new int getTradeHistoryCount (int userId) {
      int tradeCount = 0;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT count(*) as tradeCount FROM trade_history WHERE usrId=@usrId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrId", userId);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  tradeCount = dataReader.GetInt32("tradeCount");
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return tradeCount;
   }

   public static new List<TradeHistoryInfo> getTradeHistory (int userId, int page, int tradesPerPage) {
      List<TradeHistoryInfo> tradeList = new List<TradeHistoryInfo>();

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM trade_history JOIN users USING (usrId) WHERE trade_history.usrId=@usrId ORDER BY trade_history.tradeTime DESC LIMIT @start, @perPage", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrId", userId);
            cmd.Parameters.AddWithValue("@start", page * tradesPerPage);
            cmd.Parameters.AddWithValue("@perPage", tradesPerPage);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  TradeHistoryInfo trade = new TradeHistoryInfo(dataReader);
                  tradeList.Add(trade);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return tradeList;
   }

   public static new void pruneJobHistory (DateTime untilDate) {

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "DELETE FROM job_history WHERE jobTime<@untilDate", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@untilDate", untilDate);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   #endregion

   #region Leader Boards

   public static new List<LeaderBoardInfo> calculateLeaderBoard (Jobs.Type jobType,
      LeaderBoardsManager.Period period, DateTime startDate, DateTime endDate) {

      List<LeaderBoardInfo> list = new List<LeaderBoardInfo>();

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT usrId, SUM(metric) AS totalMetric FROM job_history " +
            "WHERE jobType = @jobType " +
            "AND jobTime > @startDate AND jobTime <= @endDate " +
            "GROUP BY usrId ORDER BY totalMetric DESC, jobTime DESC LIMIT 10", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@jobType", (int) jobType);
            cmd.Parameters.AddWithValue("@startDate", startDate);
            cmd.Parameters.AddWithValue("@endDate", endDate);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               int userRank = 1;
               while (dataReader.Read()) {
                  int userId = DataUtil.getInt(dataReader, "usrId");
                  int totalMetric = DataUtil.getInt(dataReader, "totalMetric");
                  LeaderBoardInfo entry = new LeaderBoardInfo(userRank, jobType, period, userId, totalMetric);
                  list.Add(entry);
                  userRank++;
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return list;
   }

   public static new void deleteLeaderBoards (LeaderBoardsManager.Period period) {

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "DELETE FROM leader_boards WHERE period=@period", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@period", (int) period);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new void updateLeaderBoards (List<LeaderBoardInfo> entries) {
      // Return if the list is empty
      if (entries.Count <= 0) {
         return;
      }

      // Insert all the rows with the same SQL connection
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "INSERT INTO leader_boards (userRank, jobType, period, usrId, score) " +
            "VALUES (@userRank, @jobType, @period, @usrId, @score)", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.Add(new MySqlParameter("@userRank", MySqlDbType.Int16));
            cmd.Parameters.Add(new MySqlParameter("@jobType", MySqlDbType.Int16));
            cmd.Parameters.Add(new MySqlParameter("@period", MySqlDbType.Int16));
            cmd.Parameters.Add(new MySqlParameter("@usrId", MySqlDbType.Int32));
            cmd.Parameters.Add(new MySqlParameter("@score", MySqlDbType.Int32));

            // Execute the query for each leader board entry
            for (int i = 0; i < entries.Count; i++) {
               cmd.Parameters["@userRank"].Value = entries[i].userRank;
               cmd.Parameters["@jobType"].Value = (int) entries[i].jobType;
               cmd.Parameters["@period"].Value = (int) entries[i].period;
               cmd.Parameters["@usrId"].Value = entries[i].userId;
               cmd.Parameters["@score"].Value = entries[i].score;

               // Execute the command
               cmd.ExecuteNonQuery();
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new void updateLeaderBoardDates (LeaderBoardsManager.Period period,
      DateTime startDate, DateTime endDate) {

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "INSERT INTO leader_board_dates (period, startDate, endDate) VALUES (@period, @startDate, @endDate)" +
            "ON DUPLICATE KEY UPDATE period=values(period), startDate=values(startDate), endDate=values(endDate)", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@period", (int) period);
            cmd.Parameters.AddWithValue("@startDate", startDate);
            cmd.Parameters.AddWithValue("@endDate", endDate);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new DateTime getLeaderBoardEndDate (LeaderBoardsManager.Period period) {

      // If there are no leader boards, sets a long past date to force a recalculation
      DateTime periodEndDate = DateTime.UtcNow.AddYears(-10);

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT endDate FROM leader_board_dates WHERE period=@period", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@period", (int) period);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  periodEndDate = DataUtil.getDateTime(dataReader, "endDate");
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return periodEndDate;
   }

   public static new void getLeaderBoards (LeaderBoardsManager.Period period,
      out List<LeaderBoardInfo> farmingEntries, out List<LeaderBoardInfo> sailingEntries, out List<LeaderBoardInfo> exploringEntries,
      out List<LeaderBoardInfo> tradingEntries, out List<LeaderBoardInfo> craftingEntries, out List<LeaderBoardInfo> miningEntries) {

      farmingEntries = new List<LeaderBoardInfo>();
      sailingEntries = new List<LeaderBoardInfo>();
      exploringEntries = new List<LeaderBoardInfo>();
      tradingEntries = new List<LeaderBoardInfo>();
      craftingEntries = new List<LeaderBoardInfo>();
      miningEntries = new List<LeaderBoardInfo>();

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM leader_boards JOIN users USING (usrID) " +
            "LEFT JOIN guilds ON users.gldId = guilds.gldId " +
            "WHERE leader_boards.period=@period " +
            "ORDER BY leader_boards.jobType, leader_boards.userRank", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@period", (int) period);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  LeaderBoardInfo entry = new LeaderBoardInfo(dataReader);
                  entry.guildInfo = new GuildInfo(dataReader);

                  // Place the entry in its corresponding list
                  switch (entry.jobType) {
                     case Jobs.Type.Farmer:
                        farmingEntries.Add(entry);
                        break;
                     case Jobs.Type.Sailor:
                        sailingEntries.Add(entry);
                        break;
                     case Jobs.Type.Explorer:
                        exploringEntries.Add(entry);
                        break;
                     case Jobs.Type.Trader:
                        tradingEntries.Add(entry);
                        break;
                     case Jobs.Type.Crafter:
                        craftingEntries.Add(entry);
                        break;
                     case Jobs.Type.Miner:
                        miningEntries.Add(entry);
                        break;
                     default:
                        break;
                  }
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   #endregion

   #region Friendship

   public static new void createFriendship (int userId, int friendUserId, Friendship.Status friendshipStatus, DateTime lastContactDate) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "INSERT INTO friendship(usrId, friendUsrId, friendshipStatus, lastContactDate) " +
            "VALUES (@usrId, @friendUsrId, @friendshipStatus, @lastContactDate)", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrId", userId);
            cmd.Parameters.AddWithValue("@friendUsrId", friendUserId);
            cmd.Parameters.AddWithValue("@friendshipStatus", friendshipStatus);
            cmd.Parameters.AddWithValue("@lastContactDate", lastContactDate);

            // Execute the command
            cmd.ExecuteNonQuery();

         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new void updateFriendship (int userId, int friendUserId, Friendship.Status friendshipStatus, DateTime lastContactDate) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "UPDATE friendship SET friendshipStatus=@friendshipStatus, lastContactDate=@lastContactDate " +
            "WHERE usrId=@usrId AND friendUsrId=@friendUsrId", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrId", userId);
            cmd.Parameters.AddWithValue("@friendUsrId", friendUserId);
            cmd.Parameters.AddWithValue("@friendshipStatus", friendshipStatus);
            cmd.Parameters.AddWithValue("@lastContactDate", lastContactDate);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new void deleteFriendship (int userId, int friendUserId) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "DELETE FROM friendship WHERE usrId=@usrId AND friendUsrId=@friendUsrId", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrId", userId);
            cmd.Parameters.AddWithValue("@friendUsrId", friendUserId);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new FriendshipInfo getFriendshipInfo (int userId, int friendUserId) {
      FriendshipInfo friendshipInfo = null;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM friendship JOIN users ON friendship.friendUsrId = users.usrId WHERE friendship.usrId=@usrId AND friendship.friendUsrId=@friendUsrId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrId", userId);
            cmd.Parameters.AddWithValue("@friendUsrId", friendUserId);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  friendshipInfo = new FriendshipInfo(dataReader);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return friendshipInfo;
   }

   public static new List<FriendshipInfo> getFriendshipInfoList (int userId, Friendship.Status friendshipStatus, int page, int friendsPerPage) {
      List<FriendshipInfo> friendList = new List<FriendshipInfo>();

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM friendship JOIN users ON friendship.friendUsrId = users.usrId " +
            "WHERE friendship.usrId=@usrId AND friendship.friendshipStatus=@friendshipStatus " +
            "ORDER BY users.usrName LIMIT @start, @perPage", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrId", userId);
            cmd.Parameters.AddWithValue("@friendshipStatus", friendshipStatus);
            cmd.Parameters.AddWithValue("@start", (page - 1) * friendsPerPage);
            cmd.Parameters.AddWithValue("@perPage", friendsPerPage);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  FriendshipInfo friend = new FriendshipInfo(dataReader);
                  friendList.Add(friend);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return friendList;
   }

   public static new int getFriendshipInfoCount (int userId, Friendship.Status friendshipStatus) {
      int friendCount = 0;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT count(*) as friendCount FROM friendship WHERE usrId=@usrId AND friendshipStatus=@friendshipStatus", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrId", userId);
            cmd.Parameters.AddWithValue("@friendshipStatus", friendshipStatus);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  friendCount = dataReader.GetInt32("friendCount");
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return friendCount;
   }

   #endregion

   public static new bool updateDeploySchedule (long scheduleDateAsTicks, int buildVersion) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("UPDATE deploy_schedule SET schedule_date=@scheduleDate, schedule_version=@scheduleVersion WHERE id=1", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@scheduleVersion", buildVersion.ToString());
            cmd.Parameters.AddWithValue("@scheduleDate", scheduleDateAsTicks.ToString());

            // Execute the command
            cmd.ExecuteNonQuery();
         }
         return true;
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
         return false;
      }
   }

   public static new DeployScheduleInfo getDeploySchedule () {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM deploy_schedule WHERE id=1", conn)) {
            conn.Open();
            cmd.Prepare();

            // Execute the command
            using (var reader = cmd.ExecuteReader()) {
               try {
                  while (reader.Read()) {
                     var info = new DeployScheduleInfo(
                        reader.GetInt32("schedule_date"),
                        reader.GetInt32("schedule_version"));
                     return info;
                  }
               } catch (Exception ex) {
                  return null;
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
         return null;
      }

      return null;
   }

   public static new bool cancelDeploySchedule () {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("UPDATE deploy_schedule SET schedule_date=@scheduleDate, schedule_version=@scheduleVersion WHERE id=1", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@scheduleVersion", string.Empty);
            cmd.Parameters.AddWithValue("@scheduleDate", string.Empty);

            // Execute the command
            cmd.ExecuteNonQuery();
         }

         return true;
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
         return false;
      }
   }

   protected static new bool setMetric (string machineId, string processName, string PID, string keySuffix, string value) {
      try {

         if (string.IsNullOrEmpty(processName)) return false;
         if (string.IsNullOrEmpty(PID)) return false;
         if (string.IsNullOrEmpty(keySuffix)) return false;

         string key = $"{processName}/{PID}/{keySuffix}";
         if (!String.IsNullOrEmpty(machineId)) {
            key = $"{machineId}/{key}";
         }
         key = key.Replace(" ", "_");
         using (MySqlConnection conn = getConnection()) {
            // Open the connection.
            conn.Open();
            // A key is tracked if it is present in the database.

            bool IsKeyAlreadyTracked = false;
            using (MySqlCommand cmd = new MySqlCommand($"SELECT * FROM `metrics` WHERE `key`=@key", conn)) {
               //cmd.Prepare();
               cmd.Parameters.AddWithValue("@key", key);
               using (var reader = cmd.ExecuteReader()) {
                  IsKeyAlreadyTracked = reader.Read();
               }
            }

            // create a new entry for the key if it's not present already,
            // and update the value if already present in the database.
            string query = $"INSERT INTO `metrics` (`key`,`value`) VALUES (@key,@value)";
            if (IsKeyAlreadyTracked) {
               query = $"UPDATE `metrics` SET `value`=@value WHERE `key`=@key";
            }

            using (MySqlCommand cmd = new MySqlCommand(query, conn)) {
               //cmd.Prepare();
               cmd.Parameters.AddWithValue("@key", key);
               cmd.Parameters.AddWithValue("@value", value);
               cmd.ExecuteNonQuery();
            }
         }
         return true;
      } catch (Exception ex) {
         D.error(ex.Message);
         return false;
      }
   }

   public static new bool setMetricPlayersCount (string machineId, string processName, string PID, int playerCount) {
      return setMetric(machineId, processName, PID, "players_count", playerCount.ToString());
   }

   public static new bool setMetricAreaInstancesCount (string machineId, string processName, string PID, int areaInstancesCount) {
      return setMetric(machineId, processName, PID, "area_instances_count", areaInstancesCount.ToString());
   }

   public static new bool setMetricPort (string machineId, string processName, string PID, int port) {
      return setMetric(machineId, processName, PID, "port", port.ToString());
   }

   public static new bool setMetricIP (string machineId, string processName, string PID, string ip) {
      return setMetric(machineId, processName, PID, "ip", ip);
   }

   #region Mail

   public static new int createMail (MailInfo mailInfo) {
      int mailId = -1;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "INSERT INTO mails(recipientUsrId, senderUsrId, receptionDate, isRead, mailSubject, message) " +
            "VALUES (@recipientUsrId, @senderUsrId, @receptionDate, @isRead, @mailSubject, @message)", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@recipientUsrId", mailInfo.recipientUserId);
            cmd.Parameters.AddWithValue("@senderUsrId", mailInfo.senderUserId);
            cmd.Parameters.AddWithValue("@receptionDate", DateTime.FromBinary(mailInfo.receptionDate));
            cmd.Parameters.AddWithValue("@isRead", mailInfo.isRead);
            cmd.Parameters.AddWithValue("@mailSubject", mailInfo.mailSubject);
            cmd.Parameters.AddWithValue("@message", mailInfo.message);

            // Execute the command
            cmd.ExecuteNonQuery();
            mailId = (int) cmd.LastInsertedId;
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return mailId;
   }

   public static new void updateMailReadStatus (int mailId, bool isRead) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "UPDATE mails SET isRead=@isRead WHERE mailId=@mailId", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@mailId", mailId);
            cmd.Parameters.AddWithValue("@isRead", isRead);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new void deleteMail (int mailId) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "DELETE FROM mails WHERE mailId=@mailId", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@mailId", mailId);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new MailInfo getMailInfo (int mailId) {
      MailInfo mailInfo = null;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM mails JOIN users ON mails.senderUsrId = users.usrId WHERE mails.mailId=@mailId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@mailId", mailId);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  mailInfo = new MailInfo(dataReader, false);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return mailInfo;
   }

   public static new List<MailInfo> getMailInfoList (int recipientUserId, int page, int mailsPerPage) {
      List<MailInfo> mailList = new List<MailInfo>();

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT *, (SELECT COUNT(*) FROM items WHERE items.usrId = -mails.mailId) AS attachedItemCount " +
            "FROM mails JOIN users ON mails.senderUsrId = users.usrId " +
            "WHERE mails.recipientUsrId=@recipientUsrId " +
            "ORDER BY mails.receptionDate DESC LIMIT @start, @perPage", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@recipientUsrId", recipientUserId);
            cmd.Parameters.AddWithValue("@start", (page - 1) * mailsPerPage);
            cmd.Parameters.AddWithValue("@perPage", mailsPerPage);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  MailInfo mail = new MailInfo(dataReader, true);
                  mailList.Add(mail);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return mailList;
   }

   public static new int getMailInfoCount (int recipientUserId) {
      int mailCount = 0;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT count(*) AS mailCount FROM mails WHERE mails.recipientUsrId=@recipientUsrId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@recipientUsrId", recipientUserId);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  mailCount = dataReader.GetInt32("mailCount");
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return mailCount;
   }

   #endregion

   #region Minimum Version

   public static new int getMinimumClientGameVersionForWindows () {
      int minVersion = 0;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT minClientVersionWin FROM game_version", conn)) {
            conn.Open();
            cmd.Prepare();

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  minVersion = dataReader.GetInt32("minClientVersionWin");
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return minVersion;
   }

   public static new int getMinimumClientGameVersionForMac () {
      int minVersion = 0;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT minClientVersionMac FROM game_version", conn)) {
            conn.Open();
            cmd.Prepare();

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  minVersion = dataReader.GetInt32("minClientVersionMac");
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return minVersion;
   }

   public static new int getMinimumClientGameVersionForLinux () {
      int minVersion = 0;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT minClientVersionLinux FROM game_version", conn)) {
            conn.Open();
            cmd.Prepare();

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  minVersion = dataReader.GetInt32("minClientVersionLinux");
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return minVersion;
   }

   public static new int getMinimumToolsVersionForWindows () {
      int minVersion = 0;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT minToolsVersionWin FROM game_version", conn)) {
            conn.Open();
            cmd.Prepare();

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  minVersion = dataReader.GetInt32("minToolsVersionWin");
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return minVersion;
   }

   public static new int getMinimumToolsVersionForMac () {
      int minVersion = 0;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT minToolsVersionMac FROM game_version", conn)) {
            conn.Open();
            cmd.Prepare();

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  minVersion = dataReader.GetInt32("minToolsVersionMac");
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return minVersion;
   }

   #endregion

   #region Auction Features

   public static new void deleteAuction (int auctionId) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "DELETE FROM auction_table_v1 WHERE auctionId=@auctionId", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@auctionId", auctionId);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new int createAuction (int sellerUserId, string sellerName, int mailId, DateTime expiryDate,
      int highestBidPrice, int buyoutPrice, Item.Category itemCategory, string itemName, int itemCount) {
      int auctionId = 0;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "INSERT INTO auction_table_v1 (sellerId, sellerName, mailId, expiryDate, buyoutPrice, highestBidPrice, highestBidUser, itemCategory, itemName, itemCount) " +
            "VALUES(@sellerId, @sellerName, @mailId, @expiryDate, @buyoutPrice, @highestBidPrice, @highestBidUser, @itemCategory, @itemName, @itemCount)", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@sellerId", sellerUserId);
            cmd.Parameters.AddWithValue("@sellerName", sellerName);
            cmd.Parameters.AddWithValue("@mailId", mailId);
            cmd.Parameters.AddWithValue("@expiryDate", expiryDate);
            cmd.Parameters.AddWithValue("@buyoutPrice", buyoutPrice);
            cmd.Parameters.AddWithValue("@highestBidPrice", highestBidPrice);
            cmd.Parameters.AddWithValue("@highestBidUser", -1);
            cmd.Parameters.AddWithValue("@itemCategory", itemCategory);
            cmd.Parameters.AddWithValue("@itemName", itemName);
            cmd.Parameters.AddWithValue("@itemCount", itemCount);

            // Execute the command
            cmd.ExecuteNonQuery();
            auctionId = (int) cmd.LastInsertedId;
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return auctionId;
   }

   public static new string getAuctionList (string pageNumberStr, string rowsPerPageStr, string categoryFilter, string userIdStr, string onlyHistory, string onlySelfAuctions) {
      List<AuctionItemData> auctionList = new List<AuctionItemData>();

      // Param translation
      int rowsPerPage = int.Parse(rowsPerPageStr);
      int pageNumber = int.Parse(pageNumberStr);

      string whereClause = getAuctionListWhereClause(userIdStr, categoryFilter, onlyHistory, onlySelfAuctions);

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM auction_table_v1 auctions " +
            "LEFT JOIN items ON(items.usrId = -auctions.mailId AND auctions.mailId > 0) " +
            whereClause +
            " ORDER BY expiryDate LIMIT @start, @perPage"
            , conn)) {
            conn.Open();
            cmd.Prepare();

            UnityEngine.Debug.Log("list query " + cmd.CommandText);
            cmd.Parameters.AddWithValue("@expiryDate", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("@start", (pageNumber - 1) * rowsPerPage);
            cmd.Parameters.AddWithValue("@perPage", rowsPerPage);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  AuctionItemData newAuctionItem = new AuctionItemData(dataReader, true);
                  auctionList.Add(newAuctionItem);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return AuctionItemData.getXmlDataGroup(auctionList);
   }

   public static new string getAuctionListCount (string userIdStr, string filterData, string onlyHistory, string onlyOwnAuctions) {
      int auctionCount = 0;
      string whereClause = getAuctionListWhereClause(userIdStr, filterData, onlyHistory, onlyOwnAuctions);

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT COUNT(*) AS auctionCount FROM arcane.auction_table_v1 " + whereClause
            , conn)) {
            conn.Open();
            cmd.Prepare();

            cmd.Parameters.AddWithValue("@expiryDate", DateTime.UtcNow);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  auctionCount = dataReader.GetInt32("auctionCount");
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return auctionCount.ToString();
   }

   private static string getAuctionListWhereClause (string userIdStr, string categoryFilter, string onlyHistory, string onlyOwnAuctions) {
      StringBuilder clause = new StringBuilder();
      clause.Append(" WHERE ");

      // Set the history and self filters
      if (onlyHistory == "1") {
         clause.Append("expiryDate < @expiryDate AND highestBidUser = ");
         clause.Append(userIdStr);
      } else {
         clause.Append("expiryDate >= @expiryDate ");
         if (onlyOwnAuctions == "1") {
            clause.Append(" AND sellerId = " + userIdStr);
         }
      }

      // Add the category filter
      List<int> categoryInt = JsonConvert.DeserializeObject<List<int>>(categoryFilter);
      if (categoryInt.Count > 0 && categoryInt[0] != 0) {
         clause.Append(" AND (itemCategory = ");
         clause.Append(categoryInt[0]);
         for (int i = 1; i < categoryInt.Count; i++) {
            clause.Append(" OR itemCategory = " + categoryInt[i]);
         }
         clause.Append(") ");
      }

      return clause.ToString();
   }

   public static new List<AuctionItemData> getAuctionsToDeliver () {
      List<AuctionItemData> auctionList = new List<AuctionItemData>();

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM arcane.auction_table_v1 WHERE " +
            "mailId > -1 AND expiryDate<=@expiryDate"
            , conn)) {
            conn.Open();
            cmd.Prepare();

            cmd.Parameters.AddWithValue("@expiryDate", DateTime.UtcNow);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  AuctionItemData newAuctionItem = new AuctionItemData(dataReader, false);
                  auctionList.Add(newAuctionItem);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return auctionList;
   }

   public static new void deliverAuction (int auctionId, int mailId, int recipientUserId) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("", conn)) {
            conn.Open();

            // Set the recipient in the mail linked to the auction, that has the item as attachment
            cmd.CommandText = "UPDATE mails SET recipientUsrId=@recipientUsrId, receptionDate=@receptionDate WHERE mailId=@mailId";
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@mailId", mailId);
            cmd.Parameters.AddWithValue("@recipientUsrId", recipientUserId);
            cmd.Parameters.AddWithValue("@receptionDate", DateTime.UtcNow);
            cmd.ExecuteNonQuery();

            // Set the auction as delivered by clearing the mailId
            cmd.CommandText = "UPDATE auction_table_v1 SET mailId=@mailId WHERE auctionId=@auctionId";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@mailId", -1);
            cmd.Parameters.AddWithValue("@auctionId", auctionId);
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new void updateAuction (int auctionId, int highestBidUser, int highestBidPrice, DateTime expiryDate) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "UPDATE arcane.auction_table_v1 SET highestBidPrice=@highestBidPrice, highestBidUser=@highestBidUser, expiryDate=@expiryDate WHERE auctionId=@auctionId", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@highestBidPrice", highestBidPrice);
            cmd.Parameters.AddWithValue("@highestBidUser", highestBidUser);
            cmd.Parameters.AddWithValue("@auctionId", auctionId);
            cmd.Parameters.AddWithValue("@expiryDate", expiryDate);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new AuctionItemData getAuction (int auctionId, bool readItemData) {
      AuctionItemData auction = null;

      string query = "SELECT * FROM arcane.auction_table_v1 auctions ";
      if (readItemData) {
         query += "LEFT JOIN items ON(items.usrId = -auctions.mailId AND auctions.mailId > 0) ";
      }
      query += "where auctionId=@auctionId";

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(query
            , conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@auctionId", auctionId);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  auction = new AuctionItemData(dataReader, readItemData);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return auction;
   }

   #endregion

   #region Voyages

   public static new int getNewVoyageId () {
      int newVoyageId = 0;

      // Increment the last voyage id and select the new value
      StringBuilder query = new StringBuilder();
      query.Append("BEGIN;");
      query.Append("UPDATE voyages SET lastVoyageId = lastVoyageId + 1;");
      query.Append("SELECT lastVoyageId FROM voyages;");
      query.Append("COMMIT;");

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(query.ToString(), conn)) {
            conn.Open();
            cmd.Prepare();

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  newVoyageId = dataReader.GetInt32("lastVoyageId");
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return newVoyageId;
   }

   public static new int createVoyageGroup (VoyageGroupInfo groupInfo) {
      int groupId = -1;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "INSERT INTO voyage_groups (voyageId, creationDate, deviceName, isQuickmatchEnabled, isPrivate) VALUES " +
            "(@voyageId, @creationDate, @deviceName, @isQuickmatchEnabled, @isPrivate)", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@voyageId", groupInfo.voyageId);
            cmd.Parameters.AddWithValue("@creationDate", DateTime.FromBinary(groupInfo.creationDate));
            cmd.Parameters.AddWithValue("@deviceName", groupInfo.deviceName);
            cmd.Parameters.AddWithValue("@isQuickmatchEnabled", groupInfo.isQuickmatchEnabled);
            cmd.Parameters.AddWithValue("@isPrivate", groupInfo.isPrivate);

            // Execute the command
            cmd.ExecuteNonQuery();
            groupId = (int) cmd.LastInsertedId;
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return groupId;
   }

   public static new VoyageGroupInfo getVoyageGroup (int groupId) {
      VoyageGroupInfo groupInfo = null;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT *, COUNT(*) AS memberCount FROM voyage_groups " +
            "JOIN voyage_group_members ON voyage_groups.groupId = voyage_group_members.groupId " +
            "WHERE voyage_groups.groupId=@groupId GROUP BY voyage_groups.groupId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@groupId", groupId);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  groupInfo = new VoyageGroupInfo(dataReader);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return groupInfo;
   }

   public static new int getGroupCountInVoyage (int voyageId) {
      int groupCount = 0;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT COUNT(*) AS groupCount FROM voyage_groups WHERE voyageId = @voyageId", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@voyageId", voyageId);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  groupCount = dataReader.GetInt32("groupCount");
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return groupCount;
   }

   public static new Dictionary<int, int> getGroupCountInAllVoyages () {
      Dictionary<int, int> voyageToGroupCount = new Dictionary<int, int>();

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT voyageId, COUNT(*) AS groupCount FROM voyage_groups GROUP BY voyageId", conn)) {

            conn.Open();
            cmd.Prepare();

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  int voyageId = dataReader.GetInt32("voyageId");
                  int groupCount = dataReader.GetInt32("groupCount");
                  voyageToGroupCount.Add(voyageId, groupCount);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return voyageToGroupCount;
   }

   public static new void updateVoyageGroupQuickmatchStatus (int groupId, bool isQuickmatchEnabled) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "UPDATE voyage_groups SET isQuickmatchEnabled=@isQuickmatchEnabled WHERE groupId=@groupId", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@groupId", groupId);
            cmd.Parameters.AddWithValue("@isQuickmatchEnabled", isQuickmatchEnabled);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new void deleteVoyageGroup (int groupId) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "DELETE FROM voyage_groups WHERE groupId=@groupId", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@groupId", groupId);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new VoyageGroupInfo getBestVoyageGroupForQuickmatch (int voyageId) {
      VoyageGroupInfo groupInfo = null;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT *, COUNT(*) AS memberCount FROM voyage_groups " +
            "JOIN voyage_group_members ON voyage_groups.groupId = voyage_group_members.groupId " +
            "WHERE voyageId = @voyageId AND isQuickmatchEnabled = 1 " +
            "GROUP BY voyage_groups.groupId ORDER BY creationDate LIMIT 1", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@voyageId", voyageId);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  groupInfo = new VoyageGroupInfo(dataReader);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return groupInfo;
   }

   public static new VoyageGroupInfo getVoyageGroupForMember (int userId) {
      VoyageGroupInfo groupInfo = null;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT *, COUNT(*) AS memberCount FROM voyage_groups " +
            "JOIN voyage_group_members ON voyage_groups.groupId = voyage_group_members.groupId " +
            "WHERE voyage_groups.groupId IN (SELECT groupId FROM voyage_group_members WHERE usrId=@usrId) " +
            "GROUP BY voyage_groups.groupId", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrId", userId);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  groupInfo = new VoyageGroupInfo(dataReader);
               }
            }

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return groupInfo;
   }

   public static new List<int> getVoyageGroupMembers (int groupId) {
      List<int> members = new List<int>();

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM voyage_group_members " +
            "WHERE voyage_group_members.groupId=@groupId", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@groupId", groupId);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  int userId = DataUtil.getInt(dataReader, "usrId");
                  members.Add(userId);
               }
            }

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return members;
   }

   public static new List<int> getAllVoyageGroupMembersForDevice (string deviceName) {
      List<int> members = new List<int>();

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM voyage_group_members " +
            "JOIN voyage_groups ON voyage_groups.groupId = voyage_group_members.groupId " +
            "WHERE voyage_groups.deviceName = @deviceName", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@deviceName", deviceName);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  int userId = DataUtil.getInt(dataReader, "usrId");
                  members.Add(userId);
               }
            }

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return members;
   }

   public static new void addMemberToVoyageGroup (int groupId, int userId) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "INSERT INTO voyage_group_members (groupId, usrId) VALUES " +
            "(@groupId, @usrId)", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@groupId", groupId);
            cmd.Parameters.AddWithValue("@usrId", userId);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new void deleteMemberFromVoyageGroup (int userId) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "DELETE FROM voyage_group_members WHERE usrId=@usrId", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrId", userId);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new void deleteAllVoyageGroups (string deviceName) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "DELETE voyage_groups, voyage_group_members FROM voyage_group_members " +
            "JOIN voyage_groups ON voyage_groups.groupId = voyage_group_members.groupId " +
            "WHERE voyage_groups.deviceName = @deviceName", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@deviceName", deviceName);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   #endregion

   public static new void readTest () {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM server WHERE srvStatus > @srvStatus", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@srvStatus", -1);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  // D.debug(dataReader.GetString("srvMessage"));
               }
            }

            D.debug("Database read test completed.");

            /*UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               D.debug("Success, on main thread: " + UnityThreadHelper.IsMainThread);
            });*/
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static void setServer (string server, string database = "", string uid = "", string password = "") {
      _connectionString = getDefaultConnectionString(server, database, uid, password);
   }

   public static new void setServerFromConfig () {
      string dbServerConfigFile = Path.Combine(Application.dataPath, "dbConfig.json");

      // Check config file
      if (File.Exists(dbServerConfigFile)) {
         string dbServerConfigContent = File.ReadAllText(dbServerConfigFile);
         JSONNode dbServerConfig = JSON.Parse(dbServerConfigContent);

         DB_Main.setServer(
            dbServerConfig["AW_DB_SERVER"].Value,
            dbServerConfig["AW_DB_NAME"].Value,
            dbServerConfig["AW_DB_USER"].Value,
            dbServerConfig["AW_DB_PASS"].Value
         );
      }

      // Use default remote server as fallback
      else {
         D.warning("setServerFromConfig() - no production database config file [" + dbServerConfigFile + "] found. Using default db server [" + DB_Main.RemoteServer + "] as fallback.");
         DB_Main.setServer(DB_Main.RemoteServer);
      }

   }

   #region Equipment Translation

   protected static Armor getArmor (MySqlDataReader dataReader) {
      int itemId = DataUtil.getInt(dataReader, "armorId");
      int itemTypeId = DataUtil.getInt(dataReader, "armorType");
      string palettes = DataUtil.getString(dataReader, "armorPalettes");
      string itemData = DataUtil.getString(dataReader, "armorData");

      return new Armor(itemId, itemTypeId, palettes, itemData);
   }

   protected static Weapon getWeapon (MySqlDataReader dataReader) {
      int itemId = DataUtil.getInt(dataReader, "weaponId");
      int itemTypeId = DataUtil.getInt(dataReader, "weaponType");
      string palettes = DataUtil.getString(dataReader, "weaponPalettes");
      string itemData = DataUtil.getString(dataReader, "weaponData");

      return new Weapon(itemId, itemTypeId, palettes, itemData);
   }

   protected static Hat getHat (MySqlDataReader dataReader) {
      int itemId = 0;
      int itemTypeId = 0;
      string palettes = "";
      string itemData = "";

      // TODO: Make sure the errors in the catch blocks will no longer occur
      try {
         itemId = DataUtil.getInt(dataReader, "hatId");
      } catch {
         D.editorLog("Issue with data: id", Color.red);
      }

      try {
         itemTypeId = DataUtil.getInt(dataReader, "hatType");
      } catch {
         D.editorLog("Issue with data: type", Color.red);
      }

      try {
         palettes = DataUtil.getString(dataReader, "hatPalettes");
      } catch {
         D.editorLog("Issue with data: palettes", Color.red);
      }

      try {
         itemData = DataUtil.getString(dataReader, "hatData");
      } catch {
         D.editorLog("Issue with data: data", Color.red);
      }

      return new Hat(itemId, itemTypeId, palettes, itemData);
   }

   #endregion

   public static new int getUsrAdminFlag (int accountId) {
      int result = -1;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT usrAdminFlag FROM global.accounts WHERE accId = @accountId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@accountId", accountId);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  result = dataReader.GetInt32("usrAdminFlag");
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return result;
   }

   #region Wrapper Call Methods

   public static new T exec<T> (Func<object, T> action) {
      using (MySqlConnection conn = getConnection())
      using (MySqlCommand cmd = conn.CreateCommand()) {
         conn.OpenAsync();
         cmd.Connection = conn;
         T result = action.Invoke(cmd);
         return result;
      }
   }

   public static new void exec (Action<object> action) {
      using (MySqlConnection conn = getConnection())
      using (MySqlCommand cmd = conn.CreateCommand()) {
         conn.OpenAsync();
         cmd.Connection = conn;

         action.Invoke(cmd);
      }
   }

   public static async new Task<T> execAsync<T> (Func<object, T> action) {
      return await Task.Run(async () => {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = conn.CreateCommand()) {
            await conn.OpenAsync();
            cmd.Connection = conn;
            T result = action.Invoke(cmd);
            return result;
         }
      });
   }

   public static async new Task execAsync (Action<object> action) {
      await Task.Run(async () => {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = conn.CreateCommand()) {
            await conn.OpenAsync();
            cmd.Connection = conn;

            action.Invoke(cmd);
         }
      });
   }

   #endregion

   public static MySqlConnection getConnection () {
      return getConnection(_connectionString);
   }

   public static MySqlConnection getConnectionToDevGlobal () {
      return getConnection(_connectionToDevGlobalString);
   }

   public static MySqlConnection getConnection (string connectionString) {
      // Throws a warning if used in the main thread
      if (UnityThreadHelper.IsMainThread && !ClientManager.isApplicationQuitting && MyNetworkManager.wasServerStarted) {
         D.debug("A database query is being run in the main thread - use the background thread instead");
      }

      // In order to support threaded DB calls, each function needs its own Connection
      return new MySqlConnection(connectionString);
   }

   private static DatabaseCredentials loadDatabaseCredentials (string subDir) {

      string dir = "C:/ArcaneWaters/Secure/Databases";
      string file = "dbConfig.json";
      string path = Path.GetFullPath(Path.Combine(dir, subDir, file));
      DatabaseCredentials creds = null;
      bool configExists = File.Exists(path);

      if (!configExists) {
         D.debug($"Couldn't find the database credentials at '{path}'");
         return null;
      }

      try {
         creds = JsonConvert.DeserializeObject<DatabaseCredentials>(File.ReadAllText(path));
      } catch {
      }

      if (creds == null) {
         D.warning($"Couldn't load the database credentials at '{path}'. Invalid format?");
      }

      return creds;
   }

   public static string getDefaultConnectionString (string server = "", string database = "", string uid = "", string password = "") {
      DatabaseCredentials creds = loadDatabaseCredentials("local");
      if (creds != null) {
         server = string.IsNullOrEmpty(creds.server) ? server : creds.server;
         database = string.IsNullOrEmpty(creds.database) ? database : creds.database;
         uid = string.IsNullOrEmpty(creds.user) ? uid : creds.user;
         password = string.IsNullOrEmpty(creds.password) ? password : creds.password;
      }

      return buildConnectionString(
         server == "" ? _remoteServer : server,
         database == "" ? _database : database,
         uid == "" ? _uid : uid,
         password == "" ? _password : password);
   }

   public static string getConnectionToDevGlobalString () {

      DatabaseCredentials creds = loadDatabaseCredentials("Dev");
      if (creds != null) {
         _remoteServerDev = string.IsNullOrEmpty(creds.server) ? _remoteServerDev : creds.server;
         _uidDev = string.IsNullOrEmpty(creds.user) ? _uidDev : creds.user;
         _passwordDev = string.IsNullOrEmpty(creds.password) ? _passwordDev : creds.password;
      }

      return buildConnectionString(_remoteServerDev, _globalDatabase, _uidDev, _passwordDev);
   }

   public static string buildConnectionString (string server, string database, string uid, string password) {
      return "SERVER=" + server + ";" +
          "DATABASE=" + database + ";" +
          "UID=" + uid + ";" +
          "PASSWORD=" + password + ";";
   }

   /*

   public static new void refillSupplies (int userId) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("UPDATE users, ships SET ships.supplies=ships.suppliesMax WHERE users.shpId=ships.shpId AND users.usrId=@userId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@userId", userId);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   */

   #region Account creation and Update
   public static new int createAccount (string accountName, string accountPassword, string accountEmail, int validated) {
      int accountId = 0;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "INSERT INTO global.accounts (accName, accPassword, accEmail, accValidated) VALUES (@accName, @accPassword, @accEmail, @accValidated);", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@accName", accountName);
            cmd.Parameters.AddWithValue("@accPassword", accountPassword);
            cmd.Parameters.AddWithValue("@accEmail", accountEmail);
            cmd.Parameters.AddWithValue("@accValidated", validated);

            // Execute the command
            cmd.ExecuteNonQuery();
            accountId = (int) cmd.LastInsertedId;
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return accountId;
   }

   public static new void updateAccountMode (int accoundId, bool isSinglePlayer) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("UPDATE global.accounts SET isSinglePlayer=@isSinglePlayer WHERE accId=@accId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@accId", accoundId);
            cmd.Parameters.AddWithValue("@isSinglePlayer", isSinglePlayer ? 1 : 0);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new void storeLoginInfo (int usrId, int accId, string ipAddress, string machineIdent) {
      // Storing Login info, excluding both localhost (IPv4) and ::1 (IPv6), and only when usrId > 0
      if (ipAddress != "localhost" && ipAddress != "::1" && ipAddress.Contains("::ffff:") && usrId > 0 && accId > 0) {
         // We need to split the IP Address because its format, ::ffff:0.0.0.0, for example
         string[] finalAddressArray = ipAddress.Split(':');
         string finalAddress = finalAddressArray[finalAddressArray.Length - 1];

         try {
            using (MySqlConnection conn = getConnection())
            using (MySqlCommand cmd = new MySqlCommand("INSERT INTO arcane.logins (usrId, accId, ipAddress, machineIdent, loginSource) VALUES (@usrId, @accId, @ipAddress, @machineIdent, @loginSource);", conn)) {
               conn.Open();
               cmd.Prepare();
               cmd.Parameters.AddWithValue("@usrId", usrId);
               cmd.Parameters.AddWithValue("@accId", accId);
               cmd.Parameters.AddWithValue("@ipAddress", finalAddress);
               cmd.Parameters.AddWithValue("@machineIdent", machineIdent);
               cmd.Parameters.AddWithValue("@loginSource", "game");

               // Execute the command
               cmd.ExecuteNonQuery();
            }
         } catch (Exception e) {
            D.error("MySQL Error: " + e.ToString());
         }
      }
   }

   #endregion

   /*
   public static new void deleteAccount (int accountId) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("DELETE FROM accounts WHERE accId=@accId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@accId", accountId);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new void setSupplies (int shipId, int suppliesAmount) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("UPDATE ships SET supplies=@supplies WHERE shpId=@shpId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@shpId", shipId);
            cmd.Parameters.AddWithValue("@supplies", suppliesAmount);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new List<PortInfo> getPorts () {
      List<PortInfo> portList = new List<PortInfo>();

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM ports", conn)) {
            conn.Open();
            cmd.Prepare();

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  PortInfo info = new PortInfo(dataReader);
                  portList.Add(info);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return portList;
   }

   public static new PortCargoSummary getPortCargoSummary (Barter.Type barterType, int specificPortId = 0) {
      PortCargoSummary cargoSummary = null;
      List<PortCargoInfo> cargoRows = new List<PortCargoInfo>();

      // Check which table we're going to look in
      string table = Barter.getTable(barterType);

      // If a port Id was specified, include that in the query
      string portClause = specificPortId != 0 ? " WHERE prtId = " + specificPortId + " " : "";

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM " + table + portClause + " ORDER BY crgType ASC", conn)) {
            conn.Open();
            cmd.Prepare();

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  int portId = dataReader.GetInt32("prtId");
                  Cargo.Type cargoType = (Cargo.Type) dataReader.GetInt32("crgType");
                  int amount = dataReader.GetInt32("crgCount");

                  cargoRows.Add(new PortCargoInfo(portId, barterType, cargoType, amount));
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      // Now that we've read everything, we can create the cargo summary object
      cargoSummary = new PortCargoSummary(specificPortId, barterType, cargoRows);

      return cargoSummary;
   }

   public static new ShipCargoSummary getShipCargoSummaryForUser (int userId) {
      int shipId = 0;
      int cargoMax = 0;
      int tradePermits = 0;
      ShipCargoSummary cargoSummary = null;
      List<ShipCargoInfo> cargoRows = new List<ShipCargoInfo>();

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT shpId, cargoMax, amount, crgType, usrTradePermits " +
            "FROM users JOIN ships USING (shpId) LEFT JOIN cargo USING (shpId) WHERE users.usrId=@usrId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrId", userId);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  shipId = dataReader.GetInt32("shpId");
                  cargoMax = dataReader.GetInt32("cargoMax");
                  tradePermits = dataReader.GetInt32("usrTradePermits");

                  // The amount and cargo type might be null if the ship doesn't have any cargo on board
                  int amount = DataUtil.getInt(dataReader, "amount");

                  if (amount > 0) {
                     Cargo.Type cargoType = (Cargo.Type) dataReader.GetInt32("crgType");
                     cargoRows.Add(new ShipCargoInfo(cargoType, amount));
                  }
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      // Now that we've read everything, we can create the cargo summary object
      cargoSummary = new ShipCargoSummary(userId, shipId, cargoMax, tradePermits, cargoRows);

      return cargoSummary;
   }

   public static new int getShipCount (int userId) {
      int shipCount = 0;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT count(*) as shipCount FROM ships WHERE usrId=@usrId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrId", userId);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  shipCount = dataReader.GetInt32("shipCount");
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return shipCount;
   }

   public static new int getPortCargoAmount (int portId, Cargo.Type cargoType, Barter.Type barterType) {
      int amount = 0;
      string table = Barter.getTable(barterType);

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT crgCount FROM " + table + " WHERE prtId=@prtId AND crgType=@crgType", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@prtId", portId);
            cmd.Parameters.AddWithValue("@crgType", (int) cargoType);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  amount = dataReader.GetInt32("crgCount");
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return amount;
   }

   public static new void removeCargoFromPort (int portId, Cargo.Type cargoType, Barter.Type barterType, int amount) {
      string table = Barter.getTable(barterType);

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("UPDATE " + table + " SET crgCount = crgCount - @amount WHERE prtId=@prtId AND crgType=@crgType", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@prtId", portId);
            cmd.Parameters.AddWithValue("@crgType", (int) cargoType);
            cmd.Parameters.AddWithValue("@amount", amount);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new void addCargoToShip (int shipId, Barter.Type barterType, Cargo.Type cargoType, int amount) {
      // If we're selling to a port, then the amount is actually negative
      if (barterType == Barter.Type.SellToPort) {
         amount *= -1;
      }

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "INSERT INTO cargo (shpId, crgType, amount) VALUES(@shpId, @crgType, @amount) " +
            "ON DUPLICATE KEY UPDATE amount = amount + @amount", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@shpId", shipId);
            cmd.Parameters.AddWithValue("@crgType", (int) cargoType);
            cmd.Parameters.AddWithValue("@amount", amount);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new void deleteEmptyCargoRow (int shipId, Cargo.Type cargoType) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("DELETE FROM cargo WHERE shpId=@shpId AND crgType=@cargoType AND amount <= 0", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@shpId", shipId);
            cmd.Parameters.AddWithValue("@cargoType", (int) cargoType);

            // Execute the command
            int rowsAffected = cmd.ExecuteNonQuery();

            if (rowsAffected == 0) {
               D.warning(string.Format("Couldn't find cargo row to delete: ship {0}, cargo type {1}", shipId, cargoType));
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new TradeRecord insertTradeRecord (int userId, int shipId, int portId, Barter.Type barterType, Cargo.Type cargoType, int amount, int unitPrice, int unitXP) {
      int tradeRecordId = 0;
      int totalPrice = unitPrice * amount;
      int totalXP = unitXP * amount;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "INSERT INTO trade_history (usrId, shpId, prtId, barterType, crgType, amount, unitPrice, totalPrice, unitXP, totalXP) " +
            "VALUES(@userId, @shipId, @portId, @barterType, @cargoType, @amount, @unitPrice, @totalPrice, @unitXP, @totalXP) ", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@userId", userId);
            cmd.Parameters.AddWithValue("@shipId", shipId);
            cmd.Parameters.AddWithValue("@portId", portId);
            cmd.Parameters.AddWithValue("@barterType", (int) barterType);
            cmd.Parameters.AddWithValue("@cargoType", (int) cargoType);
            cmd.Parameters.AddWithValue("@amount", amount);
            cmd.Parameters.AddWithValue("@unitPrice", unitPrice);
            cmd.Parameters.AddWithValue("@totalPrice", totalPrice);
            cmd.Parameters.AddWithValue("@unitXP", unitXP);
            cmd.Parameters.AddWithValue("@totalXP", totalXP);

            // Execute the command
            cmd.ExecuteNonQuery();
            tradeRecordId = (int) cmd.LastInsertedId;
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      // Now that we know the record ID, we can create the Trade Record object
      DateTime dateTime = DateTime.UtcNow;
      TradeRecord record = new TradeRecord(tradeRecordId, userId, shipId, portId, barterType, cargoType, amount, unitPrice, totalPrice, unitXP, totalXP, dateTime);

      return record;
   }

   public static new TradeHistory getTradeHistory (int userId) {
      TradeHistory tradeHistory = new TradeHistory(userId);

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM trade_history WHERE usrId=@usrId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrId", userId);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  TradeRecord record = new TradeRecord(dataReader);

                  // Add the record to the History object
                  tradeHistory.addRecord(record);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return tradeHistory;
   }

   public static new void incrementTradePermits () {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("UPDATE users SET usrTradePermits = LEAST(usrTradePermits + 1, @maxTradePermits)", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@maxTradePermits", TradePermitManager.MAX_TRADE_PERMITS);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new List<int> getTestUserIds () {
      List<int> userList = new List<int>();

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM users JOIN accounts USING (accID) WHERE usrName LIKE 'Test%' AND accEmail LIKE '%" + AdminManager.TEST_EMAIL_DOMAIN + "'", conn)) {
            conn.Open();
            cmd.Prepare();

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  userList.Add(dataReader.GetInt32("usrId"));
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return userList;
   }

   public static new bool hasItem (int userId, int itemId, int itemCategory) {
      bool found = false;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT itmId FROM items WHERE itmId=@itmId AND usrId=@usrId AND itmCategory=@itemCategory", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@itmId", itemId);
            cmd.Parameters.AddWithValue("@usrId", userId);
            cmd.Parameters.AddWithValue("@itemCategory", itemCategory);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  found = true;
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return found;
   }

   public static new List<BuildingLoc> getBuildingLocs () {
      List<BuildingLoc> buildingLocList = new List<BuildingLoc>();

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM buildings", conn)) {
            conn.Open();
            cmd.Prepare();

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  BuildingLoc buildingLoc = new BuildingLoc();
                  buildingLoc.buildingId = dataReader.GetInt32("bldId");
                  buildingLoc.areaId = dataReader.GetInt32("areaId");
                  buildingLoc.portId = dataReader.GetInt32("prtId");
                  buildingLoc.buildingType = (Building.Type) dataReader.GetInt32("bldType");
                  buildingLoc.chimneyLocation = (Chimney.Location) dataReader.GetInt32("chimneyLocation");
                  buildingLoc.localX = dataReader.GetFloat("localX");
                  buildingLoc.localY = dataReader.GetFloat("localY");
                  buildingLocList.Add(buildingLoc);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return buildingLocList;
   }

   public static new List<Area> getAreas () {
      List<Area> areas = new List<Area>();

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM areas", conn)) {
            conn.Open();
            cmd.Prepare();

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  int areaId = dataReader.GetInt32("areaId");
                  string areaName = dataReader.GetString("areaName");
                  Area.Type areaType = (Area.Type) dataReader.GetInt32("areaType");
                  TileType tileType = (TileType) dataReader.GetInt32("tileType");
                  int worldX = dataReader.GetInt32("worldX");
                  int worldY = dataReader.GetInt32("worldY");
                  int versionNumber = dataReader.GetInt32("versionNumber");
                  int creatorServerId = dataReader.GetInt32("creatorServerId");

                  Area area = new Area(areaId, areaName, areaType, tileType, worldX, worldY, versionNumber, creatorServerId);
                  areas.Add(area);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return areas;
   }

   public static new DateTime getLastUnlock (int accId, int areaId, float localX, float localY) {
      DateTime unlockTime = DateTime.MinValue;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT MAX(unlockTime) as lastUnlock FROM unlocks " +
            "WHERE accId=@accId AND areaId=@areaId AND localX=@localX AND localY=@localY", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@accId", accId);
            cmd.Parameters.AddWithValue("@areaId", areaId);
            cmd.Parameters.AddWithValue("@localX", localX);
            cmd.Parameters.AddWithValue("@localY", localY);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  string key = "lastUnlock";
                  var ordinal = dataReader.GetOrdinal(key);

                  // Make sure it's not null
                  if (dataReader.IsDBNull(ordinal)) {
                     continue;
                  }

                  unlockTime = dataReader.GetDateTime(key);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return unlockTime;
   }

   public static new void storeUnlock (int accountId, int userId, int areaId, float localX, float localY, int gold, int itemId) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("INSERT INTO unlocks (accId, usrId, areaId, localX, localY, gold, itmId) " +
            "VALUES(@accountId, @userId, @areaId, @localX, @localY, @gold, @itemId) ", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@accountId", accountId);
            cmd.Parameters.AddWithValue("@userId", userId);
            cmd.Parameters.AddWithValue("@areaId", areaId);
            cmd.Parameters.AddWithValue("@localX", localX);
            cmd.Parameters.AddWithValue("@localY", localY);
            cmd.Parameters.AddWithValue("@gold", gold);
            cmd.Parameters.AddWithValue("@itemId", itemId);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new void setFlagship (int userId, int shipId) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("UPDATE users SET shpId=@shipId WHERE usrId=@userId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@shipId", shipId);
            cmd.Parameters.AddWithValue("@userId", userId);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new List<NPC_Info> getNPCs (int areaId) {
      List<NPC_Info> npcs = new List<NPC_Info>();

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM npcs WHERE areaId=@areaId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@areaId", areaId);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  NPC_Info info = new NPC_Info(dataReader);
                  npcs.Add(info);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return npcs;
   }

   public static new void insertNPC (NPC_Info info) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("INSERT INTO npcs (npcType, npcName, areaId, localX, localY, hairType, bodyType, eyesType, " +
               "armorType, armorExtraType, hairExtraType, hairColor1, hairColor2, eyesColor1, armorColor1, armorColor2, armorExtraColor1, armorExtraColor2, hairExtraColor1, hairExtraColor2)" +
               "VALUES(@npcType, @npcName, @areaId, @localX, @localY, @hairType, " +
               "@bodyType, @eyesType, @armorType, @armorExtraType, @hairExtraType, @hairColor1, @hairColor2, @eyesColor1, @armorColor1, @armorColor2, " +
               "@armorExtraColor1, @armorExtraColor2, @hairExtraColor1, @hairExtraColor2) ", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@npcType", info.npcType);
            cmd.Parameters.AddWithValue("@npcName", info.npcName);
            cmd.Parameters.AddWithValue("@areaId", info.areaId);
            cmd.Parameters.AddWithValue("@localX", info.localX);
            cmd.Parameters.AddWithValue("@localY", info.localY);
            cmd.Parameters.AddWithValue("@hairType", info.hairType);
            cmd.Parameters.AddWithValue("@bodyType", info.bodyType);
            cmd.Parameters.AddWithValue("@eyesType", info.eyesType);
            cmd.Parameters.AddWithValue("@armorType", info.armorType);
            cmd.Parameters.AddWithValue("@armorExtraType", info.armorExtraType);
            cmd.Parameters.AddWithValue("@hairExtraType", info.hairExtraType);
            cmd.Parameters.AddWithValue("@hairColor1", info.hairColor1);
            cmd.Parameters.AddWithValue("@hairColor2", info.hairColor2);
            cmd.Parameters.AddWithValue("@eyesColor1", info.eyesColor1);
            cmd.Parameters.AddWithValue("@armorColor1", info.armorColor1);
            cmd.Parameters.AddWithValue("@armorColor2", info.armorColor2);
            cmd.Parameters.AddWithValue("@armorExtraColor1", info.armorExtraColor1);
            cmd.Parameters.AddWithValue("@armorExtraColor2", info.armorExtraColor2);
            cmd.Parameters.AddWithValue("@hairExtraColor1", info.hairExtraColor1);
            cmd.Parameters.AddWithValue("@hairExtraColor2", info.hairExtraColor2);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new void deleteNPCs () {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("DELETE FROM npcs", conn)) {
            conn.Open();
            cmd.Prepare();

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new List<Shop_ShipInfo> getShopShips () {
      List<Shop_ShipInfo> ships = new List<Shop_ShipInfo>();

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM shops_shipyard ORDER BY shpType ASC, stockId DESC", conn)) {
            conn.Open();
            cmd.Prepare();

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  Shop_ShipInfo info = new Shop_ShipInfo(dataReader);
                  ships.Add(info);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return ships;
   }

   public static new int insertShip (PortInfo port, Shop_ShipInfo shipInfo) {
      int stockId = 0;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "INSERT INTO shops_shipyard (portId, shpType, color1, color2, mastType, sailType, sailColor1, sailColor2, supplies, suppliesMax, cargoMax, maxHealth, damage, cost) " +
            "VALUES(@portId, @shpType, @color1, @color2, @mastType, @sailType, @sailColor1, @sailColor2, @supplies, @suppliesMax, @cargoMax, @maxHealth, @damage, @cost)", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@portId", port.portId);
            cmd.Parameters.AddWithValue("@shpType", (int) shipInfo.shipType);
            cmd.Parameters.AddWithValue("@color1", (int) shipInfo.color1);
            cmd.Parameters.AddWithValue("@color2", (int) shipInfo.color2);
            cmd.Parameters.AddWithValue("@mastType", (int) shipInfo.mastType);
            cmd.Parameters.AddWithValue("@sailType", (int) shipInfo.sailType);
            cmd.Parameters.AddWithValue("@sailColor1", shipInfo.sailColor1);
            cmd.Parameters.AddWithValue("@sailColor2", shipInfo.sailColor2);
            cmd.Parameters.AddWithValue("@supplies", shipInfo.supplies);
            cmd.Parameters.AddWithValue("@suppliesMax", shipInfo.suppliesMax);
            cmd.Parameters.AddWithValue("@cargoMax", shipInfo.cargoMax);
            cmd.Parameters.AddWithValue("@maxHealth", shipInfo.maxHealth);
            cmd.Parameters.AddWithValue("@damage", shipInfo.damage);
            cmd.Parameters.AddWithValue("@cost", shipInfo.cost);

            // Execute the command
            cmd.ExecuteNonQuery();
            stockId = (int) cmd.LastInsertedId;
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return stockId;
   }

   public static new void deleteShopShips (PortInfo port) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("DELETE FROM shops_shipyard WHERE portId=@portId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@portId", port.portId);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new List<Shop_ItemInfo> getShopItems () {
      List<Shop_ItemInfo> items = new List<Shop_ItemInfo>();

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM shops_items ORDER BY itmType ASC, stockId DESC", conn)) {
            conn.Open();
            cmd.Prepare();

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  Shop_ItemInfo info = new Shop_ItemInfo(dataReader);
                  items.Add(info);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return items;
   }

   public static new void deleteShopItems (PortInfo port) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("DELETE FROM shops_items WHERE portId=@portId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@portId", port.portId);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new int insertItem (PortInfo port, Shop_ItemInfo itemInfo) {
      int stockId = 0;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "INSERT INTO shops_items (portId, itmCategory, itmType, itmColor1, itmColor2, itmData, cost) " +
            "VALUES(@portId, @itmCategory, @itmType, @color1, @color2, @itmData, @cost)", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@portId", port.portId);
            cmd.Parameters.AddWithValue("@itmCategory", (int) itemInfo.itemCategory);
            cmd.Parameters.AddWithValue("@itmType", itemInfo.itemType);
            cmd.Parameters.AddWithValue("@color1", (int) itemInfo.color1);
            cmd.Parameters.AddWithValue("@color2", (int) itemInfo.color2);
            cmd.Parameters.AddWithValue("@itmData", itemInfo.itemData);
            cmd.Parameters.AddWithValue("@cost", itemInfo.cost);

            // Execute the command
            cmd.ExecuteNonQuery();
            stockId = (int) cmd.LastInsertedId;
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return stockId;
   }

   public static new ItemInfo createItemFromStock (int userId, Shop_ItemInfo itemStock) {
      ItemInfo itemInfo = new ItemInfo();
      itemInfo.itemCategory = itemStock.itemCategory;
      itemInfo.itemType = itemStock.itemType;
      itemInfo.color1 = itemStock.color1;
      itemInfo.color2 = itemStock.color2;
      itemInfo.itemData = itemStock.itemData;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "INSERT INTO items (usrId, itmCategory, itmType, itmColor1, itmColor2, itmData) " +
            "VALUES(@usrId, @itmCategory, @itmType, @itmColor1, @itmColor2, @itmData) ", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@usrId", userId);
            cmd.Parameters.AddWithValue("@itmCategory", (int) itemStock.itemCategory);
            cmd.Parameters.AddWithValue("@itmType", (int) itemStock.itemType);
            cmd.Parameters.AddWithValue("@itmColor1", (int) itemStock.color1);
            cmd.Parameters.AddWithValue("@itmColor2", (int) itemStock.color2);
            cmd.Parameters.AddWithValue("@itmData", itemStock.itemData);

            // Execute the command
            cmd.ExecuteNonQuery();
            itemInfo.itemId = (int) cmd.LastInsertedId;
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return itemInfo;
   }

   public static new int unlockDiscovery (int userId, Discovery.Type discoveryType, int primaryKeyID, int areaId) {
      int discoveryId = 0;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "INSERT IGNORE INTO discoveries (usrId, discoveryType, primaryKeyId, areaId) " +
            "VALUES(@userId, @discoveryType, @primaryKeyID, @areaId)", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@userId", userId);
            cmd.Parameters.AddWithValue("@discoveryType", discoveryType);
            cmd.Parameters.AddWithValue("@primaryKeyID", primaryKeyID);
            cmd.Parameters.AddWithValue("@areaId", areaId);

            // Execute the command
            cmd.ExecuteNonQuery();
            discoveryId = (int) cmd.LastInsertedId;
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return discoveryId;
   }

   public static new List<Discovery> getDiscoveries (int userId, int areaId) {
      List<Discovery> list = new List<Discovery>();
      string areaClause = (areaId > 0) ? " AND areaId=@areaId" : "";

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM discoveries WHERE usrId=@userId" + areaClause, conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@userId", userId);

            // Only insert the areaId if a valid one was specified
            if (areaId > 0) {
               cmd.Parameters.AddWithValue("@areaId", areaId);
            }

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  Discovery discovery = new Discovery(dataReader);
                  list.Add(discovery);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return list;
   }

   public static new List<SeaMonsterSpawnInfo> getSeaMonsterSpawns () {
      List<SeaMonsterSpawnInfo> list = new List<SeaMonsterSpawnInfo>();

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM seamonster_spawns", conn)) {
            conn.Open();
            cmd.Prepare();

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  SeaMonsterSpawnInfo info = new SeaMonsterSpawnInfo(dataReader);
                  list.Add(info);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return list;
   }

   public static new void deleteAllSeaMonsterSpawns () {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("DELETE FROM seamonster_spawns", conn)) {
            conn.Open();
            cmd.Prepare();

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new int insertSeaMonsterSpawn (int areaId, SeaMonster.Type seaMonsterType, float localX, float localY) {
      int smsId = 0;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "INSERT INTO seamonster_spawns (areaId, seaMonsterType, localX, localY) " +
            "VALUES(@areaId, @seaMonsterType, @localX, @localY)", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@areaId", areaId);
            cmd.Parameters.AddWithValue("@seaMonsterType", (int) seaMonsterType);
            cmd.Parameters.AddWithValue("@localX", localX);
            cmd.Parameters.AddWithValue("@localY", localY);

            // Execute the command
            cmd.ExecuteNonQuery();
            smsId = (int) cmd.LastInsertedId;
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return smsId;
   }

   public static new int insertArea (string areaName, Area.Type areaType, TileType tileType, int worldX, int worldY, int versionNumber, int serverId) {
      int areaId = 0;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "INSERT INTO areas (areaName, areaType, tileType, worldX, worldY, versionNumber, creatorServerId) " +
            "VALUES(@areaName, @areaType, @tileType, @worldX, @worldY, @versionNumber, @serverId)", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@areaName", areaName);
            cmd.Parameters.AddWithValue("@areaType", (int) areaType);
            cmd.Parameters.AddWithValue("@tileType", (int) tileType);
            cmd.Parameters.AddWithValue("@worldX", worldX);
            cmd.Parameters.AddWithValue("@worldY", worldY);
            cmd.Parameters.AddWithValue("@versionNumber", versionNumber);
            cmd.Parameters.AddWithValue("@serverId", serverId);

            // Execute the command
            cmd.ExecuteNonQuery();
            areaId = (int) cmd.LastInsertedId;
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return areaId;
   }

   public static new void deleteOldTreasureAreas () {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("DELETE FROM areas WHERE areaType=4 AND NOW() - INTERVAL 7 DAY > creationTime", conn)) {
            conn.Open();
            cmd.Prepare();

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new void insertServer (string address, int port) {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "INSERT IGNORE INTO servers (srvAddress, srvPort) " +
            "VALUES(@address, @port)", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@address", address);
            cmd.Parameters.AddWithValue("@port", port);

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   public static new int getServerId (string address, int port) {
      int serverId = 0;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT srvId FROM servers WHERE srvAddress=@address AND srvPort=@port", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@address", address);
            cmd.Parameters.AddWithValue("@port", port);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  serverId = dataReader.GetInt32("srvId");
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      if (serverId <= 0) {
         D.warning(string.Format("Invalid server ID {0} for address {1} and port {2}", serverId, address, port));
      }

      return serverId;
   }

   public static new int insertSite (int innerAreaId, int outerAreaId, string siteName, int siteLevel, Site.Type siteType, float localX, float localY) {
      int siteId = 0;

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand(
            "INSERT INTO sites (innerAreaId, outerAreaId, siteName, siteLevel, siteType, localX, localY) " +
            "VALUES(@innerAreaId, @outerAreaId, @siteName, @siteLevel, @siteType, @localX, @localY)", conn)) {

            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@innerAreaId", innerAreaId);
            cmd.Parameters.AddWithValue("@outerAreaId", outerAreaId);
            cmd.Parameters.AddWithValue("@siteName", siteName);
            cmd.Parameters.AddWithValue("@siteLevel", siteLevel);
            cmd.Parameters.AddWithValue("@siteType", (int) siteType);
            cmd.Parameters.AddWithValue("@localX", localX);
            cmd.Parameters.AddWithValue("@localY", localY);

            // Execute the command
            cmd.ExecuteNonQuery();
            siteId = (int) cmd.LastInsertedId;
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return siteId;
   }

   public static new SiteLoc getSiteLoc (int siteId) {
      SiteLoc siteLoc = new SiteLoc();

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM sites WHERE siteId=@siteId", conn)) {
            conn.Open();
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@siteId", siteId);

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  siteLoc = new SiteLoc(dataReader);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return siteLoc;
   }

   public static new List<SiteLoc> getSiteLocs () {
      List<SiteLoc> list = new List<SiteLoc>();

      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM sites", conn)) {
            conn.Open();
            cmd.Prepare();

            // Create a data reader and Execute the command
            using (MySqlDataReader dataReader = cmd.ExecuteReader()) {
               while (dataReader.Read()) {
                  SiteLoc siteLoc = new SiteLoc(dataReader);
                  list.Add(siteLoc);
               }
            }
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }

      return list;
   }

   public static new void deleteAllAreas () {
      try {
         using (MySqlConnection conn = getConnection())
         using (MySqlCommand cmd = new MySqlCommand("DELETE FROM areas", conn)) {
            conn.Open();
            cmd.Prepare();

            // Execute the command
            cmd.ExecuteNonQuery();
         }
      } catch (Exception e) {
         D.error("MySQL Error: " + e.ToString());
      }
   }

   */

   #region Private Variables

   // Some development tables must be accessible from any build
   private static string _globalDatabase = "global";
   private static string _remoteServerDev = "devdb.c1whxibm6zeb.us-east-2.rds.amazonaws.com";
   private static string _uidDev = "userAdKmE";
   private static string _passwordDev = "HEqbVDsvvCza5n4N";

   // Default credentials
   private static string _database = "arcane";
   private static string _remoteServer = _remoteServerDev;
   private static string _uid = _uidDev;
   private static string _password = _passwordDev;

   private static string _connectionString = getDefaultConnectionString(_remoteServer);
   private static string _connectionToDevGlobalString = getConnectionToDevGlobalString();

   #endregion
}

#endif
