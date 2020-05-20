//#define NUBIS
#if NUBIS
using System;
using System.Text;
using MySql.Data.MySqlClient;
#endif

namespace NubisTranslator {
   public class Fetch_Inventory_v1Controller {
      public static string userInventory (int usrId, int equipType) {
#if NUBIS
         try {
            string equipmentTable = "";
            int equipmentCategory = equipType;
            switch (equipType) {
               case 1:
                  equipmentTable = "equipment_weapon_xml_v3";
                  break;
               case 2:
                  equipmentTable = "equipment_armor_xml_v3";
                  break;
            }
            using (MySqlConnection connection = DB_Main.getConnection()) {
               connection.Open();
               using (MySqlCommand command = new MySqlCommand(
               "SELECT xmlContent, itmId FROM items left join " + equipmentTable + " on itmType = xml_id where itmCategory = " + equipmentCategory + " and usrId = @usrId", connection)) {
                  command.Parameters.AddWithValue("@usrId", usrId);

                  StringBuilder stringBuilder = new StringBuilder();
                  using (MySqlDataReader reader = command.ExecuteReader()) {
                     while (reader.Read()) {
                        string xmlData = reader.GetString("xmlContent");
                        int itmId = reader.GetInt32("itmId");
                        string result = $"[next]{xmlData}[space]{itmId}";
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