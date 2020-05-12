//#define NUBIS
#if NUBIS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace Nubis.Controllers {
   public class Fetch_Map_Data_v1Controller {
      public static string fetchMapData (string mapName) {

         try {
            // Connect to the server.
            string connString = DB_Main.buildConnectionString(DB_Main.RemoteServer);

            if (String.IsNullOrEmpty(connString)) return string.Empty;

            using (MySqlConnection connection = new MySqlConnection(connString)) {

               connection.Open();

               using (MySqlCommand command = new MySqlCommand(
                  "SELECT gameData FROM map_versions_v2 left join maps_v2 on mapid = id WHERE name = @mapName ORDER BY version DESC LIMIT 1",
                  connection)) {

                  command.Parameters.AddWithValue("@mapName", mapName);

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

         return string.Empty;
      }

   }
}
#endif