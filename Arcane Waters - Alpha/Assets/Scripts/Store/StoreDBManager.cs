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
         //paletteData.subcategory = rawPaletteData.subcategory;
         //paletteData.tagId = rawPaletteData.tagId;

         fetchedPalettes.Add(rawPaletteData.xmlId, paletteData);
      }

      return fetchedPalettes;
   }

   private Dictionary<XMLPair, DyeData> fetchAllDyes () {
      Dictionary<XMLPair, DyeData> fetchedDyes = new Dictionary<XMLPair, DyeData>();
      List<XMLPair> dyesXML = DB_Main.getDyesXML();

      foreach (XMLPair xmlPair in dyesXML) {
         try {
            DyeData dyeData = Util.xmlLoad<DyeData>(xmlPair.rawXmlData);
            dyeData.itemID = xmlPair.xmlId;

            // Save the data in the memory cache
            if (!fetchedDyes.ContainsKey(xmlPair)) {
               fetchedDyes.Add(xmlPair, dyeData);
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
   /// Creates a Store entry in the database for each valid palette
   /// </summary>
   /// <returns></returns>
   private bool linkPalettes () {
      try {
         Dictionary<int, PaletteToolData> palettes = fetchAllPalettes();
         Dictionary<ulong, StoreItem> storeItems = fetchAllStoreItems();
         PaletteImageType[] supportedDyeTypes = new PaletteImageType[] { PaletteImageType.Armor, PaletteImageType.Hair, PaletteImageType.Hat, PaletteImageType.Weapon };

         foreach (KeyValuePair<int, PaletteToolData> pair in palettes) {
            PaletteToolData palette = pair.Value;
            int paletteId = pair.Key;
            bool hasMatchingStoreItem = storeItems.Any(_ => _.Value.itemId == paletteId);

            if (!hasMatchingStoreItem) {
               if (supportedDyeTypes.Contains((PaletteImageType) palette.paletteType) && palette.hasTag(StoreScreen.GEM_STORE_TAG)) {
                  // Create Store item for the Dye
                  ulong newStoreItemId = DB_Main.createStoreItem();
                  DB_Main.updateStoreItem(newStoreItemId, Item.Category.Dye, paletteId, true, 50, palette.paletteDisplayName, palette.paletteDescription);
               }
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

   private string trimInside (string source) {
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
