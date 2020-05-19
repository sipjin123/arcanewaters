#if NUBIS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace Nubis.Controllers
{
   public class Fetch_Crafting_Ingredients_v3Controller
   {
      public static string fetchCraftingIngredients (int usrId) {
         try {
            // Connect to the server.
            string connString = DB_Main.buildConnectionString(DB_Main.RemoteServer);

            if (String.IsNullOrEmpty(connString)) return string.Empty;

            using (MySqlConnection connection = new MySqlConnection(connString)) {

               connection.Open();
               string result = "";
               using (MySqlCommand command = new MySqlCommand(
                  "SELECT itmId, itmCategory, itmType " +
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
      }

   }
} 
#endif