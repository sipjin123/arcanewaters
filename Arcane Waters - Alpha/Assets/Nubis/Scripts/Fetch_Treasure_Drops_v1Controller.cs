//#define NUBIS
#if NUBIS
using System;
using System.Text;
using MySql.Data.MySqlClient;
#endif

namespace NubisTranslator
{
   public class Fetch_Treasure_Drops_v1Controller
   {
      public static string fetchTreasureDrops () {
#if NUBIS
         try {
            // Connect to the server.
            using (MySqlConnection connection = DB_Main.getConnection()) {
               connection.Open();
               using (MySqlCommand command = new MySqlCommand("SELECT * from arcane.treasure_drops_xml_v1", connection)) {
                  StringBuilder stringBuilder = new StringBuilder();
                  using (MySqlDataReader reader = command.ExecuteReader()) {
                     while (reader.Read()) {
                        int xmlId = reader.GetInt32("biomeType");
                        string xmlContent = reader.GetString("xmlContent");
                        string result = $"[next]{xmlId}[space]{xmlContent}";
                        stringBuilder.AppendLine(result);
                     }
                  }
                  return stringBuilder.ToString();
               }
            }
         } catch {
            return "Failed to Query";
         }
#endif
         return "";
      }
   }
}