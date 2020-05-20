//#define NUBIS
#if NUBIS
using System;
using System.Text;
using MySql.Data.MySqlClient;
#endif

namespace NubisTranslator {
   public class Fetch_Craftable_Weapons_v4Controller {
      public static string fetchCraftableWeapons (int usrId) {
#if NUBIS
         try {
            // Connect to the server.
            using (MySqlConnection connection = DB_Main.getConnection()) {
               connection.Open();
               using (MySqlCommand command = new MySqlCommand(
                  "SELECT itmId, itmCategory, itmType, crafting_xml_v2.xmlContent AS craftingXML, equipment_weapon_xml_v3.xmlContent AS equipmentXML " +
                  "FROM items " +
                  "RIGHT JOIN crafting_xml_v2 ON(itmType = crafting_xml_v2.equipmentTypeID AND itmData LIKE '%blueprintType=weapon%' AND crafting_xml_v2.equipmentCategory = 1) " +
                  "RIGHT JOIN equipment_weapon_xml_v3 ON(itmType = equipment_weapon_xml_v3.equipmentTypeID AND itmData LIKE '%blueprintType=weapon%') " +
                  "WHERE(itmCategory = 7) AND items.usrId = @usrId",
                  connection)) {
                  command.Parameters.AddWithValue("@usrId", usrId);

                  StringBuilder stringBuilder = new StringBuilder();
                  using (MySqlDataReader reader = command.ExecuteReader()) {
                     while (reader.Read()) {
                        int itmId = reader.GetInt32("itmId");
                        int itmCategory = reader.GetInt32("itmCategory");
                        int itmType = reader.GetInt32("itmType");
                        string craftingXML = reader.GetString("craftingXML");
                        string equipmentXML = reader.GetString("equipmentXML");
                        string result = $"[next]{itmId}[space]{itmCategory}[space]{itmType}[space]{craftingXML}[space]{equipmentXML}[space]";
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