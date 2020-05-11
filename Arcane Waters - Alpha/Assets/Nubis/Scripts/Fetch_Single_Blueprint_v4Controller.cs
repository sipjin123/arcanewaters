#define NUBIS
#if NUBIS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using UnityEngine;

namespace Nubis.Controllers
{
   public class Fetch_Single_Blueprint_v4Controller
   {
      public static string fetchSingleBlueprint (int usrId, int bpId) {

         try {
            // Connect to the server.
            string connString = DB_Main.buildConnectionString(DB_Main.RemoteServer);

            if (String.IsNullOrEmpty(connString)) return string.Empty;

            using (MySqlConnection connection = new MySqlConnection(connString)) {

               connection.Open();

               string query = "SELECT itmId, itmCategory, itmType, arcane.crafting_xml_v2.xmlContent as craftingXML, " +
                              "CASE " +
                              "WHEN      itmCategory = 7 and itmData like '%blueprintType=weapon%' THEN arcane.equipment_weapon_xml_v3.xmlContent " +
                              "WHEN      itmCategory = 7 and itmData like '%blueprintType=armor%' THEN arcane.equipment_armor_xml_v3.xmlContent " +
                              "END AS equipmentXML " +
                              "FROM arcane.items " +
                              "left join arcane.crafting_xml_v2 " +
                              "on (itmData like '%blueprintType=weapon%' and itmType = equipmentTypeID and arcane.crafting_xml_v2.equipmentCategory = 1) " +
                              "or (itmData like '%blueprintType=armor%' and itmType = equipmentTypeID and arcane.crafting_xml_v2.equipmentCategory = 2) " +
                              "left join arcane.equipment_weapon_xml_v3 on (itmData like '%blueprintType=weapon%' and itmType = arcane.equipment_weapon_xml_v3.equipmentTypeID) " +
                              "left join arcane.equipment_armor_xml_v3  on (itmData like '%blueprintType=armor%' and  itmType = arcane.equipment_armor_xml_v3.equipmentTypeID) " +
                              "where (itmCategory = 7 and itmId = @itmId) and items.usrId = @usrId";

               using (MySqlCommand command = new MySqlCommand(query,connection)) {

                  command.Parameters.AddWithValue("@usrId", usrId);
                  command.Parameters.AddWithValue("@itmId", bpId);

                  StringBuilder builder = new StringBuilder();

                  using (MySqlDataReader reader = command.ExecuteReader()) {

                     while (reader.Read()) {
                        int itmId = reader.GetInt32("itmId");
                        int itmCategory = reader.GetInt32("itmCategory");
                        int itmType = reader.GetInt32("itmType");
                        string craftingXML = reader.GetString("craftingXML");
                        string equipmentXML = reader.GetString("equipmentXML");

                        string result = $"[next]{itmId}[space]{itmCategory}[space]{itmType}[space]{craftingXML}[space]{equipmentXML}[space]";

                        builder.AppendLine(result);
                     }
                  }

                  return builder.ToString();
               }
            }

         } catch (Exception ex) {
            Debug.LogError(ex.Message);
            return string.Empty;

         }

      }

   }
} 
#endif