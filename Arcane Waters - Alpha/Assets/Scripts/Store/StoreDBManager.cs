using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using static PaletteToolManager;
using System.Linq;
using Store;

public class StoreDBManager : GenericGameManager
{
   #region Public Variables

   // The id of the GemStore Tag
   public int gemStoreTag = 6;

   // Did this manager execute?
   public bool hasExecuted;

   // Is Executing
   public bool isExecuting;

   // Initialization flag
   public bool isInitialized;

   // Force the disabled state?
   public bool isDisabled;

   // Self
   public static StoreDBManager self;

   #endregion

   protected override void Awake () {
      base.Awake();

      self = this;
   }

   public void Start () {
      isInitialized = true;
   }

   public void initialize () {
      if (isDisabled) {
         return;
      }

      D.debug("Updating Store Items...");
      isExecuting = true;

      linkPalettes();
      linkDyes();
      toggleDyes();

      hasExecuted = true;
      isExecuting = false;
      D.debug("Updating Store Items: OK");
   }

   private Dictionary<int, PaletteToolData> fetchAllPalettes () {
      Dictionary<int, PaletteToolData> fetchedPalettes = new Dictionary<int, PaletteToolData>();
      List<RawPaletteToolData> paletteDatabaseContent = DB_Main.getPaletteXmlContent(XmlVersionManagerServer.PALETTE_DATA_TABLE);

      foreach (RawPaletteToolData rawPaletteData in paletteDatabaseContent) {
         PaletteToolData paletteData = Util.xmlLoad<PaletteToolData>(rawPaletteData.xmlData);

         // Manually inject these values to the newly fetched xml translated data
         paletteData.subcategory = rawPaletteData.subcategory;
         paletteData.tagId = rawPaletteData.tagId;

         fetchedPalettes.Add(rawPaletteData.xmlId, paletteData);
      }

      return fetchedPalettes;
   }

   private Dictionary<int, DyeData> fetchAllDyes () {
      Dictionary<int, DyeData> fetchedDyes = new Dictionary<int, DyeData>();
      List<XMLPair> dyesXML = DB_Main.getDyesXML();

      foreach (XMLPair xmlPair in dyesXML) {
         try {
            TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
            DyeData dyeData = Util.xmlLoad<DyeData>(newTextAsset);
            dyeData.itemID = xmlPair.xmlId;

            // Save the data in the memory cache
            if (!fetchedDyes.ContainsKey(xmlPair.xmlId)){
               fetchedDyes.Add(xmlPair.xmlId, dyeData);
            }
         } catch {
         }
      }

      return fetchedDyes;
   }

   private Dictionary<ulong, StoreItem> fetchAllStoreItems () {
      Dictionary<ulong, StoreItem> fetchedStoreItems = new Dictionary<ulong, StoreItem>();
      List<StoreItem> storeItems = DB_Main.getAllStoreItems();

      foreach (StoreItem storeItem in storeItems) {
         fetchedStoreItems.Add(storeItem.id, storeItem);
      }

      return fetchedStoreItems;
   }

   /// <summary>
   /// Creates a Dye record in the database for each valid palette
   /// </summary>
   /// <returns></returns>
   private bool linkPalettes () {
      try {
         Dictionary<int, PaletteToolData> palettes = fetchAllPalettes();
         Dictionary<int, DyeData> dyes = fetchAllDyes();

         foreach (KeyValuePair<int, PaletteToolData> pair in palettes) {
            PaletteToolData palette = pair.Value;
            int paletteId = pair.Key;

            if (palette.paletteType == (int) PaletteImageType.Armor || palette.paletteType == (int) PaletteImageType.Hair || palette.paletteType == (int) PaletteImageType.Weapon) {
               if (palette.tagId == gemStoreTag) {
                  // Check if the palette has a matching dye
                  bool hasMatchingDye = dyes.Any(_ => _.Value.paletteId == paletteId);

                  if (!hasMatchingDye) {
                     // Create Dye for the palette
                     DyeData newDyeData = new DyeData { paletteId = paletteId, itemName = computeDyeName(palette), itemDescription = computeDyeDescription(palette) };
                     DB_Main.updateDyeXML(-1, newDyeData.serializeXML(), 157658);
                  }
               }
            }
         }

         return true;
      } catch (System.Exception ex) {
         D.error(ex.Message);
      }

      return false;
   }

   /// <summary>
   /// Creates a store item record in the database for each valid dye
   /// </summary>
   /// <returns></returns>
   private bool linkDyes () {
      try {
         Dictionary<int, DyeData> dyes = fetchAllDyes();
         Dictionary<ulong, StoreItem> storeItems = fetchAllStoreItems();

         foreach (KeyValuePair<int, DyeData> pair in dyes) {
            DyeData dye = pair.Value;
            int dyeId = pair.Key;
            bool hasMatchingStoreItem = storeItems.Any(_ => _.Value.itemId == dyeId);

            if (!hasMatchingStoreItem) {
               // Create Store item for the Dye
               ulong newStoreItemId = DB_Main.createStoreItem();
               DB_Main.updateStoreItem(newStoreItemId, Item.Category.Dye, dyeId, true, 50, dye.itemName, dye.itemDescription);
            }
         }

         return true;
      } catch (System.Exception ex) {
         D.error(ex.Message);
      }

      return false;
   }

   /// <summary>
   /// Toggles the dyes. Dyes, whose palettes are not found or invalid, are disabled
   /// </summary>
   /// <returns></returns>
   private bool toggleDyes () {
      try {
         Dictionary<int, PaletteToolData> palettes = fetchAllPalettes();
         Dictionary<int, DyeData> dyes = fetchAllDyes();

         foreach (KeyValuePair<int, DyeData> pair in dyes) {
            DyeData dye = pair.Value;
            int dyeId = pair.Key;
            PaletteToolData palette = palettes.ContainsKey(dye.paletteId) ? palettes[dye.paletteId] : null;
            bool shouldEnable = (palette != null && palette.tagId == gemStoreTag);
            bool updated = DB_Main.toggleDyeXML(dyeId, shouldEnable, 157658);

            if (!updated) {
               D.warning($"StoreDBManager: Couldn't update dye {dyeId}");
            }
         }

         return true;
      } catch (System.Exception ex) {
         D.error(ex.Message);
      }

      return false;
   }

   private string computeDyeName (PaletteToolData palette) {
      if (palette == null) {
         return "Color Dye.";
      }

      string name = palette.paletteName.ToLower();

      if (palette.paletteType == (int) PaletteImageType.Armor) {
         name = name.Replace("armor", "");

         if (palette.paletteName.ToLower().StartsWith("hat")) {
            name = name.Replace("hat", "");
         }
      }

      if (palette.paletteType == (int) PaletteImageType.Hair) {
         name = name.Replace("hair", "");
      }

      if (palette.paletteType == (int) PaletteImageType.Weapon) {
         name = name.Replace("weapon", "");
      }

      if (palette.isPrimary()) {
         name = name.Replace("primary", "");
      }

      if (palette.isSecondary()) {
         name = name.Replace("secondary", "");
      }

      if (palette.isAccent()) {
         name = name.Replace("accent", "");
      }

      name = name.Replace("_", " ").Trim();
      name = trimInside(name);
      return Util.UppercaseFirst(name);
   }

   private string computeDyeDescription (PaletteToolData palette) {
      if (palette == null) {
         return "Common Dye.";
      }

      string desc = "";

      if (palette.paletteType == (int) PaletteImageType.Armor) {
         desc = "Armor Dye.";

         if (palette.paletteName.ToLower().StartsWith("hat")) {
            desc = "Hat Dye.";
         }
      }

      if (palette.paletteType == (int) PaletteImageType.Hair) {
         desc = "Hair Dye.";
      }

      if (palette.paletteType == (int) PaletteImageType.Weapon) {
         desc = "Weapon Dye.";
      }

      if (palette.isPrimary()) {
      }

      if (palette.isSecondary()) {
         desc += " (secondary)";
      }

      if (palette.isAccent()) {
         desc += " (accent)";
      }

      desc = desc.Replace("_", " ").Trim();
      desc = trimInside(desc);
      return Util.UppercaseFirst(desc);
   }

   private string trimInside(string source) {
      string[] parts = source.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
      string result = "";

      foreach (string part in parts) {
         result += part.Trim() + " ";
      }

      return result.Trim();
   }

   #region Private Variables

   #endregion
}
