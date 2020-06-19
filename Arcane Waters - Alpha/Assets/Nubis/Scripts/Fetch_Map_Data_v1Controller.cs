﻿//#define NUBIS
#if NUBIS
using System;
using MySql.Data.MySqlClient;
#endif

namespace NubisTranslator {
   public class Fetch_Map_Data_v1Controller {
      public static string fetchMapData (string mapName, int version) {
#if NUBIS
         try {
            using (MySqlConnection connection = DB_Main.getConnection()) {
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
               }
            }
         } catch {
            return string.Empty;
         }
#endif
         return string.Empty;
      }
   }
}