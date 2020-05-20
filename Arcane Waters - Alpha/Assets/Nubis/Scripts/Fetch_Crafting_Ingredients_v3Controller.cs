//#define NUBIS
#if NUBIS
using System;
using MySql.Data.MySqlClient;
#endif

namespace NubisTranslator {
   public class Fetch_Crafting_Ingredients_v3Controller {
      public static string fetchCraftingIngredients (int usrId) {
#if NUBIS
         try {
            using (MySqlConnection connection = DB_Main.getConnection()) {
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
         } catch {
            return "Failed to Query";
         }
#endif
         return "";
      }
   }
} 