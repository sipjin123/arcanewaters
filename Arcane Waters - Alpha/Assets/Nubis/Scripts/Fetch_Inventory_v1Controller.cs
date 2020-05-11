#define NUBIS
#if NUBIS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace Nubis.Controllers {
   public class Fetch_Inventory_v1Controller {
      public static string userInventory (int usrId, EquipmentType equipType) {
         try {
            // Connect to the server.
            string connString = DB_Main.buildConnectionString(DB_Main.RemoteServer);
            string equipmentTable = "";
            int equipmentCategory = 1;
            switch (equipType) {
               case EquipmentType.Weapon:
                  equipmentTable = "equipment_weapon_xml_v3";
                  equipmentCategory = 1;
                  break;
               case EquipmentType.Armor:
                  equipmentTable = "equipment_armor_xml_v3";
                  equipmentCategory = 2;
                  break;
            }

            if (String.IsNullOrEmpty(connString)) return string.Empty;

            using (MySqlConnection connection = new MySqlConnection(connString)) {

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
      }
   }
}
#endif