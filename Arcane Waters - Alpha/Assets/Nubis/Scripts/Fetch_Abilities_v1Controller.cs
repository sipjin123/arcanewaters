//#define NUBIS
#if NUBIS
using System;
using System.Collections.Generic;
using System.Text;
using MySql.Data.MySqlClient;
#endif

namespace NubisTranslator
{
   public class Fetch_Abilities_v1Controller {
      public static string userAbilities (int usrId) {
#if NUBIS
         try {
            List<AbilitySQLData> abilityList = new List<AbilitySQLData>();
            StringBuilder stringBuilder = new StringBuilder();
            try {
               using (MySqlConnection conn = DB_Main.getConnection())
               using (MySqlCommand cmd = new MySqlCommand(
                  "SELECT * FROM ability_table WHERE (userID=@userID)", conn)) {

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
                  D.editorLog("REsult of fetch: " + Util.serialize<AbilitySQLData>(abilityList).Length);
                  foreach (string splitString in Util.serialize<AbilitySQLData>(abilityList)) {
                     stringBuilder.AppendLine(splitString + "_space_");
                  }
                  return stringBuilder.ToString();
               }
            } catch (Exception e) {
               D.error("MySQL Error: " + e.ToString());
            }
         } catch (Exception e) {
            D.error("MySQL Error: " + e.ToString());
         }
#endif
         return "Failed to Query";
      }
   }
}