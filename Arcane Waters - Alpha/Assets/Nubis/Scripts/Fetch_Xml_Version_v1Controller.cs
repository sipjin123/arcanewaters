//#define NUBIS
#if NUBIS
using System;
using MySql.Data.MySqlClient;
#endif

namespace NubisTranslator {
   public class Fetch_Xml_Version_v1Controller {
      public static string fetchXmlVersion () {
         #if NUBIS
         try {
            using (MySqlConnection connection = DB_Main.getConnection()) {
               connection.Open();
               using (MySqlCommand command = new MySqlCommand(
                  "SELECT version FROM arcane.xml_status where id = 1",
                  connection)) {

                  using (MySqlDataReader reader = command.ExecuteReader()) {
                     while (reader.Read()) {
                        string version = reader.GetString("version");
                        return version;
                     }
                  }
               }
            }
         } catch {
            return string.Empty;
         }
         #endif
         return "0";
      }
   }
}