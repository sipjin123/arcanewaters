using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using Mirror;
#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif
using System;

public class RemoteSetting
{
   #region Public Variables

   // The id of the setting
   public int id;

   // The name of the setting
   public string name;

   // The value of the setting
   public string value;

   // The description of the setting
   public string description;

   // The default value of the setting
   public string defaultValue;

   // The type of the value
   public RemoteSettingValueType valueType;

   // The time the setting was updated last
   public DateTime timestamp;

   // Represents the type of a RemoteSetting
   public enum RemoteSettingValueType
   {
      // Used to represent any custom data type
      OTHER = 0,

      // Integer type
      INT = 1,

      // Boolean type
      BOOL = 2,

      // Float/Double type
      FLOAT = 3,

      // String type
      STRING = 4,

      // Unsigned integer type
      UINT = 5
   }

   #endregion

   #if IS_SERVER_BUILD

   public static RemoteSetting create (MySqlDataReader dataReader) {
      RemoteSetting setting = new RemoteSetting();
      try {
         setting.id = DataUtil.getInt(dataReader, "id");
         setting.name = DataUtil.getString(dataReader, "rsName");
         setting.value = DataUtil.getString(dataReader, "rsValue");
         setting.description = DataUtil.getString(dataReader, "rsDescription");
         setting.defaultValue = DataUtil.getString(dataReader, "rsDefaultValue");
         setting.valueType = (RemoteSettingValueType) DataUtil.getInt(dataReader, "rsValueType");
         setting.timestamp = DataUtil.getDateTime(dataReader, "rsTimestamp");
      } catch (Exception ex) {
         D.debug("Error in parsing MySqlData for RemoteSetting " + ex.ToString());
         return null;
      }
      return setting;
   }

   #endif

   public static RemoteSetting create (string settingName, string settingValue, int id = 0, string settingDescription = "", string settingDefaultValue = "", RemoteSettingValueType settingValueType = RemoteSettingValueType.OTHER) {
      RemoteSetting setting = new RemoteSetting();
      setting.id = id;
      setting.name = settingName;
      setting.value = settingValue;
      setting.description = settingDescription;
      setting.defaultValue = settingDefaultValue;
      setting.valueType = settingValueType;
      setting.timestamp = DateTime.UtcNow;
      return setting;
   }

   public int toInt () {
      bool castSucceeded = int.TryParse(value, out int result);

      if (castSucceeded) {
         return result;
      }

      return 0;
   }

   public float toFloat () {
      bool castSucceeded = float.TryParse(value, out float result);

      if (castSucceeded) {
         return result;
      }

      return 0;
   }

   public uint toUint () {
      bool castSucceeded = uint.TryParse(value, out uint result);

      if (castSucceeded) {
         return result;
      }

      return 0;
   }

   public bool toBool () {
      bool castSucceeded = bool.TryParse(value, out bool result);

      if (castSucceeded) {
         return result;
      }

      return false;
   }

   #region Private Variables

   #endregion
}
