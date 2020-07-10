//#define NUBIS
#if NUBIS
using System;
using System.Text;
using MySql.Data.MySqlClient;
#endif

namespace NubisTranslator
{
   public class Fetch_Craftable_Hats_v1Controller
   {
      public static string fetchCraftableHats (int usrId) {
#if NUBIS
         try {
            // Connect to the server.
            using (MySqlConnection connection = DB_Main.getConnection()) {
               connection.Open();
               using (MySqlCommand command = new MySqlCommand(
                  "SELECT itmId, itmCategory, itmType, crafting_xml_v2.xmlContent AS craftingXML, equipment_hat_xml_v1.xmlContent AS equipmentXML " +
                  "FROM arcane.items " +
                  "RIGHT JOIN arcane.crafting_xml_v2 ON(itmType = crafting_xml_v2.xml_id AND itmData LIKE '%blueprintType=hat%' AND crafting_xml_v2.equipmentCategory = 3) " +
                  "RIGHT JOIN arcane.equipment_hat_xml_v1 ON(itmType = equipment_hat_xml_v1.xml_id AND itmData LIKE '%blueprintType=hat%') " +
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