using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

#if IS_SERVER_BUILD

using MySql.Data.MySqlClient;

public class DataUtil {
   #region Public Variables

   #endregion

   public static ulong getUInt64 (MySqlDataReader dataReader, string key) {
      var ordinal = dataReader.GetOrdinal(key);

      // Default to 0
      if (dataReader.IsDBNull(ordinal)) {
         return 0;
      }

      return dataReader.GetUInt64(key);
   }

   public static ushort getUshort (MySqlDataReader dataReader, string key) {
      var ordinal = dataReader.GetOrdinal(key);

      // Default to 0
      if (dataReader.IsDBNull(ordinal)) {
         return 0;
      }

      return dataReader.GetUInt16(key);
   }

   public static int getInt (MySqlDataReader dataReader, string key) {
      var ordinal = dataReader.GetOrdinal(key);

      // Default to 0
      if (dataReader.IsDBNull(ordinal)) {
         return 0;
      }

      return dataReader.GetInt32(key);
   }

   public static bool getBoolean (MySqlDataReader dataReader, string key) {
      var ordinal = dataReader.GetOrdinal(key);

      // Default to false
      if (dataReader.IsDBNull(ordinal)) {
         return false;
      }

      return dataReader.GetInt32(key) > 0 ? true : false;
   }

   public static float getFloat (MySqlDataReader dataReader, string key) {
      var ordinal = dataReader.GetOrdinal(key);

      // Default to 0
      if (dataReader.IsDBNull(ordinal)) {
         return 0;
      }

      return dataReader.GetFloat(key);
   }

   public static string getString (MySqlDataReader dataReader, string key) {
      var ordinal = dataReader.GetOrdinal(key);

      // Default to empty string
      if (dataReader.IsDBNull(ordinal)) {
         return "";
      }

      return dataReader.GetString(key);
   }

   public static long getLong (MySqlDataReader dataReader, string key) {
      var ordinal = dataReader.GetOrdinal(key);

      // Default to 0
      if (dataReader.IsDBNull(ordinal)) {
         return 0;
      }

      return dataReader.GetInt64(key);
   }

   public static System.DateTime getDateTime (MySqlDataReader dataReader, string key) {
      var ordinal = dataReader.GetOrdinal(key);

      // Default to min
      if (dataReader.IsDBNull(ordinal)) {
         return System.DateTime.MinValue;
      }

      return dataReader.GetDateTime(key);
   }

   #region Private Variables

   #endregion
}

#endif
