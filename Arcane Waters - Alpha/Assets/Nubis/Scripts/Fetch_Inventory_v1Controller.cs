//#define NUBIS
#if NUBIS
using System;
using System.Text;
using MySql.Data.MySqlClient;
#endif

namespace NubisTranslator {
   public class Fetch_Inventory_v1Controller {
      public static string userInventory (int usrId, int currentPage, int category, int weaponId, int armorId) {
         int offset = currentPage * InventoryPanel.ITEMS_PER_PAGE;
         bool hasItemFilter = category != 0;
         string itemFilterContent = "and (itmCategory = " + category+")";
         if (!hasItemFilter) {
            itemFilterContent = "and (itmCategory = " + (int) Item.Category.Weapon + " or itmCategory = " + (int) Item.Category.Armor + " or itmCategory = " + (int) Item.Category.CraftingIngredients + ")";
         }

         string weaponFilter = "";
         string armorFilter = "";
         if (weaponId > 0) {
            weaponFilter = " and itmId != " + weaponId;
         }
         if (armorId > 0) {
            armorFilter = " and itmId != " + armorId;
         }

         #if NUBIS
         try {
            using (MySqlConnection connection = DB_Main.getConnection()) {
               connection.Open();
               using (MySqlCommand command = new MySqlCommand(
               "SELECT itmId, itmCategory, itmType, itmCount, itmData, itmPalette1, itmPalette2 FROM arcane.items where (usrId = @usrId "+ itemFilterContent + weaponFilter + armorFilter + ") order by itmCategory limit " + InventoryPanel.ITEMS_PER_PAGE + " offset " + offset, connection)) {
                  command.Parameters.AddWithValue("@usrId", usrId);

                  StringBuilder stringBuilder = new StringBuilder();
                  using (MySqlDataReader reader = command.ExecuteReader()) {
                     while (reader.Read()) {
                        int itmId = reader.GetInt32("itmId");
                        int itmCategory = reader.GetInt32("itmCategory");
                        int itmType = reader.GetInt32("itmType");
                        int itmCount = reader.GetInt32("itmCount");
                        string itmData = ""; 
                        string itmPalette1 = ""; 
                        string itmPalette2 = "";

                        try {
                           itmData = reader.GetString("itmData");
                        } catch {
                           D.editorLog("Blank item data");
                        }
                        try {
                           itmPalette1 = reader.GetString("itmPalette1");
                        } catch {
                           D.editorLog("Blank Palette 1");
                        }
                        try {
                           itmPalette2 = reader.GetString("itmPalette2");
                        } catch {
                           D.editorLog("Blank Palette 2");
                        }

                        string result = $"[next]{itmId}[space]{itmCategory}[space]{itmType}[space]{itmCount}[space]{itmData}[space]{itmPalette1}[space]{itmPalette2}";
                        stringBuilder.AppendLine(result);
                     }
                  }
                  return stringBuilder.ToString();
               }
            }
         } catch (Exception e) {
            D.error("MySQL Error: " + e.ToString());
         }
         #endif
         return "Failed to Query";
      }

      public static string userInventoryCount (int usrId, int categoryFilter) {
         bool hasItemFilter = categoryFilter != 0;
         string itemFilterContent = "and (itmCategory = " + categoryFilter + ")";
         if (!hasItemFilter) {
            itemFilterContent = "and (itmCategory = " + (int) Item.Category.Weapon + " or itmCategory = " + (int) Item.Category.Armor + " or itmCategory = " + (int) Item.Category.CraftingIngredients + ")";
         }

         #if NUBIS
         try {
            using (MySqlConnection connection = DB_Main.getConnection()) {
               connection.Open();

               using (MySqlCommand command = new MySqlCommand("SELECT COUNT(*) as itemCount FROM arcane.items where (usrId = @usrId " + itemFilterContent + ")", connection)) {
                  command.Parameters.AddWithValue("@usrId", usrId);

                  StringBuilder stringBuilder = new StringBuilder();
                  using (MySqlDataReader reader = command.ExecuteReader()) {
                     while (reader.Read()) {
                        int itmCount = reader.GetInt32("itemCount");

                        return itmCount.ToString();
                     }
                  }
                  return stringBuilder.ToString();
               }
            }
         } catch (Exception e) {
            D.error("MySQL Error: " + e.ToString());
            return "0";
         }
         #endif
         return "0";
      }
   }
}