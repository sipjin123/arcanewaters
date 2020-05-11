#define NUBIS
#if NUBIS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace Nubis.Controllers
{
   public class Fetch_Craftable_Weapons_v3Controller
   {
      public static string fetchCraftableWeapons (int usrId) {

         try {
            // Connect to the server.
            string connString = DB_Main.buildConnectionString(DB_Main.RemoteServer);

            if (String.IsNullOrEmpty(connString)) return string.Empty;

            using (MySqlConnection connection = new MySqlConnection(connString)) {

               connection.Open();

               using (MySqlCommand command = new MySqlCommand(
                  "SELECT itmId, itmCategory, itmType, crafting_xml_v2.xmlContent as craftingXML, equipment_weapon_xml_v3.xmlContent as equipmentXML FROM items " +
                  "LEFT JOIN crafting_xml_v2 on(itmType like '100%' and REPLACE(itmType, '100', '') = equipmentTypeID and crafting_xml_v2.equipmentCategory = 1) " +
                  "LEFT JOIN equipment_weapon_xml_v3 on(itmCategory = 7 and itmType like '100%' and REPLACE(itmType, '100', '') = equipment_weapon_xml_v3.equipmentTypeID) " +
                  "WHERE(itmCategory = 7 and itmType like '100%') and items.usrId = @usrId",
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