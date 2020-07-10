//#define NUBIS
#if NUBIS
using System;
using System.Text;
using MySql.Data.MySqlClient;
using UnityEngine;
#endif

namespace NubisTranslator {
   public class Fetch_Single_Blueprint_v4Controller {
      public static string fetchSingleBlueprint (int bpId, int usrId) {
#if NUBIS
         try {
            using (MySqlConnection connection = DB_Main.getConnection()) {
               connection.Open();
               string query = "SELECT itmId, itmCategory, itmType, arcane.crafting_xml_v2.xmlContent as craftingXML, " +
                              "CASE " +
                              "WHEN      itmCategory = 7 and itmData like '%blueprintType=weapon%' THEN arcane.equipment_weapon_xml_v3.xmlContent " +
                              "WHEN      itmCategory = 7 and itmData like '%blueprintType=armor%' THEN arcane.equipment_armor_xml_v3.xmlContent " +
                              "WHEN      itmCategory = 7 and itmData like '%blueprintType=hat%' THEN arcane.equipment_hat_xml_v1.xmlContent " +
                              "END AS equipmentXML " +
                              "FROM arcane.items " +
                              "left join arcane.crafting_xml_v2 " +
                              "on (itmData like '%blueprintType=weapon%' and itmType = equipmentTypeID and arcane.crafting_xml_v2.equipmentCategory = 1) " +
                              "or (itmData like '%blueprintType=armor%' and itmType = equipmentTypeID and arcane.crafting_xml_v2.equipmentCategory = 2) " +
                              "or (itmData like '%blueprintType=hat%' and itmType = equipmentTypeID and arcane.crafting_xml_v2.equipmentCategory = 3) " +
                              "left join arcane.equipment_weapon_xml_v3 on (itmData like '%blueprintType=weapon%' and itmType = arcane.equipment_weapon_xml_v3.xml_id) " +
                              "left join arcane.equipment_armor_xml_v3  on (itmData like '%blueprintType=armor%' and  itmType = arcane.equipment_armor_xml_v3.xml_id) " +
                              "left join arcane.equipment_hat_xml_v1  on (itmData like '%blueprintType=hat%' and  itmType = arcane.equipment_hat_xml_v1.xml_id) " +
                              "where (itmCategory = 7 and itmId = @itmId) and items.usrId = @usrId";
               using (MySqlCommand command = new MySqlCommand(query,connection)) {
                  command.Parameters.AddWithValue("@itmId", bpId);
                  command.Parameters.AddWithValue("@usrId", usrId);

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
#endif
         return "";
      }
   }
} 