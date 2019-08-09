﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

#if IS_SERVER_BUILD

using MySql.Data.MySqlClient;

public class DataUtil {
   #region Public Variables

   #endregion

   public static int getInt (MySqlDataReader dataReader, string key) {
      var ordinal = dataReader.GetOrdinal(key);

      // Default to 0
      if (dataReader.IsDBNull(ordinal)) {
         return 0;
      }

      return dataReader.GetInt32(key);
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
