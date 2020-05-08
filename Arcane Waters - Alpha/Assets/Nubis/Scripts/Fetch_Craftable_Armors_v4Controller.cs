#if NUBIS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace Nubis.Controllers
{
   public class Fetch_Craftable_Armors_v4Controller
    {
      public static string fetchCraftableArmors (int usrId) {

         try {
            // Connect to the server.
            string connString = DB_Main.buildConnectionString(DB_Main.RemoteServer);

            if (String.IsNullOrEmpty(connString)) return string.Empty;

            using (MySqlConnection connection = new MySqlConnection(connString)) {

               connection.Open();

               using (MySqlCommand command = new MySqlCommand(
                  "SELECT itmId, itmCategory, itmType, crafting_xml_v2.xmlContent AS craftingXML, equipment_armor_xml_v3.xmlContent AS equipmentXML " +
                  "FROM items "+
                  "RIGHT JOIN crafting_xml_v2 " +
                  "ON(itmType = crafting_xml_v2.equipmentTypeID AND itmData LIKE '%blueprintType=armor%' AND crafting_xml_v2.equipmentCategory = 2) "+
                  "RIGHT JOIN equipment_armor_xml_v3 " +
                  "ON(itmType = equipment_armor_xml_v3.equipmentTypeID AND itmData LIKE '%blueprintType=armor%') " +
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

      }

   }

}
#endif