using UnityEngine;
using System.Collections.Generic;
using Store;
using UnityEditor;
using static PaletteToolManager;
using System.Linq;

public class StoreItemsGenerationTool : MonoBehaviour
{
   #region Public Variables

   #endregion

   #region Public Methods

   [MenuItem("Util/Generate Items For Gem Store")]
   public static void generateStoreItems () {
      // Prompt the user
      if (!EditorUtility.DisplayDialog("Store Items Generator", "Do you want to generate the items for the gem store?", "Yes", "No")) {
         Debug.Log("StoreItemsGenerationTool: Operation cancelled by the user.");
         return;
      }

      // Generate Dyes
      Dictionary<int, PaletteToolData> palettes = fetchAllPalettes();
      Dictionary<ulong, StoreItem> storeItems = fetchAllStoreItems();
      generateOrUpdateStoreItemsForPalettes(palettes, storeItems);

      Debug.Log("StoreItemsGenerationTool: Store Items Generation Completed");
      EditorUtility.DisplayProgressBar("StoreItemsGenerationTool", "Generation Completed", 1.0f);
      EditorUtility.ClearProgressBar();
   }

   #endregion

   #region Private Methods

   private static Dictionary<int, PaletteToolData> fetchAllPalettes () {
      Dictionary<int, PaletteToolData> fetchedPalettes = new Dictionary<int, PaletteToolData>();
      List<RawPaletteToolData> paletteDatabaseContent = DB_Main.getPaletteXmlContent(XmlVersionManagerServer.PALETTE_DATA_TABLE);

      int counter = 0;
      foreach (RawPaletteToolData rawPaletteData in paletteDatabaseContent) {
         PaletteToolData paletteData = Util.xmlLoad<PaletteToolData>(rawPaletteData.xmlData);
         EditorUtility.DisplayProgressBar("StoreItemsGenerationTool [1/3]", $"Downloading And Parsing Palette {paletteData.paletteDisplayName} ({counter}/{paletteDatabaseContent.Count})", (float)counter / paletteDatabaseContent.Count);
         counter++;

         // Manually inject these values to the newly fetched xml translated data
         paletteData.subcategory = rawPaletteData.subcategory;
         paletteData.tagId = rawPaletteData.tagId;

         fetchedPalettes.Add(rawPaletteData.xmlId, paletteData);
      }

      EditorUtility.DisplayProgressBar("StoreItemsGenerationTool [1/3]", $"Downloading Palettes Completed", 1.0f);
      return fetchedPalettes;
   }

   private static Dictionary<ulong, StoreItem> fetchAllStoreItems () {
      Dictionary<ulong, StoreItem> fetchedStoreItems = new Dictionary<ulong, StoreItem>();
      List<StoreItem> storeItems = DB_Main.getAllStoreItems();

      int counter = 0;
      foreach (StoreItem storeItem in storeItems) {
         EditorUtility.DisplayProgressBar("StoreItemsGenerationTool [2/3]", $"Downloading And Parsing StoreItem {storeItem.id} ({counter}/{storeItems.Count})", (float) counter / storeItems.Count);
         counter++;

         fetchedStoreItems.Add(storeItem.id, storeItem);
      }

      EditorUtility.DisplayProgressBar("StoreItemsGenerationTool [2/3]", $"Downloading StoreItems Completed", 1.0f);
      return fetchedStoreItems;
   }

   /// <summary>
   /// Creates a Store entry in the database for each valid palette
   /// </summary>
   /// <returns></returns>
   private static bool generateOrUpdateStoreItemsForPalettes (Dictionary<int, PaletteToolData> palettes, Dictionary<ulong, StoreItem> storeItems) {
      try {
         PaletteImageType[] supportedDyeTypes = new PaletteImageType[] { PaletteImageType.Armor, PaletteImageType.Hair, PaletteImageType.Hat, PaletteImageType.Weapon };

         int counter = 0;
         int skippedPalettes = 0;
         foreach (KeyValuePair<int, PaletteToolData> pair in palettes) {
            EditorUtility.DisplayProgressBar("StoreItemsGenerationTool [3/3]", $"Processing Palette {pair.Value.paletteName} ({counter}/{palettes.Count})", (float) counter / palettes.Count);
            counter++;

            int paletteId = pair.Key;
            PaletteToolData palette = pair.Value;

            if (storeItems.Any(_ => _.Value.itemId == paletteId)) {
               Debug.Log($"StoreItemsGenerationTool: palette {paletteId} was skipped");
               skippedPalettes++;
               continue;
            }

            // Link only dye palettes that should be displayed in the store
            if (supportedDyeTypes.Contains((PaletteImageType) palette.paletteType) && palette.hasTag(StoreScreen.GEM_STORE_TAG)) {
               ulong newStoreItemId = DB_Main.createStoreItem();
               DB_Main.updateStoreItem(newStoreItemId, Item.Category.Dye, paletteId, true, 50, palette.paletteDisplayName, palette.paletteDescription);
            }
         }

         Debug.Log($"StoreItemsGenerationTool: Skipped palettes: {skippedPalettes}");
         EditorUtility.DisplayProgressBar("StoreItemsGenerationTool [3/3]", $"Processing Palettes Completed", 1.0f);
         return true;
      } catch (System.Exception ex) {
         D.error($"StoreItemsGenerationTool: {ex.Message}");
      }

      return false;
   }

   #endregion

   #region Private Variables

   #endregion
}
