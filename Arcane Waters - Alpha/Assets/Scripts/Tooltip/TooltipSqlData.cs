using System;
#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

[Serializable]
public class TooltipSqlData {

   #region Public Variables

   // Tooltip id in the database
   public int id = 0;

   // The keys to be referenced for in game usage
   public string key1 = "";
   public string key2 = "";
   
   // The message of the tool tip
   public string value = "";

   // Location area of the tooltip
   public int displayLocation = 0;

   #endregion

   public TooltipSqlData () {
      
   }

#if IS_SERVER_BUILD

   public TooltipSqlData (MySqlDataReader dataReader) {
      this.id = DataUtil.getInt(dataReader, "id");
      this.key1 = DataUtil.getString(dataReader, "key1");
      this.key2 = DataUtil.getString(dataReader, "key2");
      this.value = DataUtil.getString(dataReader, "value");
      this.displayLocation = DataUtil.getInt(dataReader, "displayLocation");
   }

#endif
}
