//#define NUBIS
#if NUBIS
using System;
using MySql.Data.MySqlClient;
using System.Text;
#endif

namespace NubisTranslator {
   public class Fetch_Equipped_Items_v3Controller {
      public static string fetchEquippedItems (int usrId) {
#if NUBIS
         try {
            using (MySqlConnection connection = DB_Main.getConnection()) {
               connection.Open();
               using (MySqlCommand command = new MySqlCommand(
                  "SELECT itmId, itmCategory, itmType, " +
                  "CASE " +
                  "WHEN itmCategory = 1 THEN arcane.equipment_weapon_xml_v3.xmlContent " +
                  "WHEN itmCategory = 2 THEN arcane.equipment_armor_xml_v3.xmlContent " +
                  "END AS equipmentXML " +
                  "FROM arcane.items " +
                  "left join arcane.equipment_weapon_xml_v3 on(itmCategory = 1 and itmType = arcane.equipment_weapon_xml_v3.equipmentTypeID) " +
                  "left join arcane.equipment_armor_xml_v3 on(itmCategory = 2 and itmType = arcane.equipment_armor_xml_v3.equipmentTypeID) " +
                  "left join arcane.users on armId = itmId or wpnId = itmId " +
                  "where(armId = itmId or wpnId = itmId) and items.usrId = @usrId",
                  connection)) {
                  command.Parameters.AddWithValue("@usrId", usrId);

                  StringBuilder stringBuilder = new StringBuilder();
                  using (MySqlDataReader reader = command.ExecuteReader()) {
                     while (reader.Read()) {
                        int itmId = reader.GetInt32("itmId");
                        int itmCategory = reader.GetInt32("itmCategory");
                        int itmType = reader.GetInt32("itmType");
                        string equipmentXML = reader.GetString("equipmentXML");
                        string result = $"[next]{itmId}[space]{itmCategory}[space]{itmType}[space]{equipmentXML}[space]";
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