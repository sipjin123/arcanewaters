using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using System.Linq;
using static PaletteToolManager;

public class DyeXMLManager : MonoBehaviour
{
   #region Public Variables

   // A convenient self reference
   public static DyeXMLManager self;

   // References to all the dye data
   public List<DyeData> dyeList { get { return _dyeStatList.ToList(); } }

   // Is loaded?
   public bool isLoaded;

   // Determines if data setup is done
   public UnityEvent finishedDataSetup = new UnityEvent();

   #endregion

   private void Awake () {
      self = this;
   }

   public DyeData getDyeData (int dyeId) {
      if (_dyeDataRegistry.ContainsKey(dyeId)) {
         return _dyeDataRegistry[dyeId];
      }

      return null;
   }

   private void finishedLoading () {
      isLoaded = true;
      finishedDataSetup.Invoke();
   }

   public void initializeDataCache () {
      _dyeDataRegistry = new Dictionary<int, DyeData>();
      _dyeStatList = new List<DyeData>();

      List<XMLPair> dyesXML = DB_Main.getDyesXML();

      foreach (XMLPair xmlPair in dyesXML) {
         if (xmlPair.isEnabled) {
            try {
               TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
               DyeData rawData = Util.xmlLoad<DyeData>(newTextAsset);
               rawData.itemID = xmlPair.xmlId;

               // Save the data in the memory cache
               if (!_dyeDataRegistry.ContainsKey(xmlPair.xmlId) && xmlPair.isEnabled) {
                  _dyeDataRegistry.Add(xmlPair.xmlId, rawData);
                  _dyeStatList.Add(rawData);
               }
            } catch {
               D.editorLog("Failed to translate data: " + xmlPair.rawXmlData, Color.red);
            }
         }
      }

      finishedLoading();
   }

   public void receiveDataFromZipData (List<DyeData> data) {
      foreach (DyeData rawData in data) {
         int uniqueID = rawData.itemID;

         // Save the data in the memory cache
         if (!_dyeDataRegistry.ContainsKey(uniqueID)) {
            _dyeDataRegistry.Add(uniqueID, rawData);
            _dyeStatList.Add(rawData);
         }
      }

      finishedLoading();
   }

   public void resetAllData () {
      if (_dyeDataRegistry == null) {
         _dyeDataRegistry = new Dictionary<int, DyeData>();
      }

      _dyeDataRegistry.Clear();
      _dyeStatList.Clear();
   }

   public PaletteImageType getDyeType (int dyeId) {
      DyeData dyeData = getDyeData(dyeId);

      if (dyeData != null) {
         PaletteToolData palette = PaletteSwapManager.self.getPalette(dyeData.paletteId);

         if (palette != null) {
            if (palette.paletteType == (int) PaletteImageType.Hair && palette.isPrimary()) {
               return PaletteImageType.Hair;
            }

            if (palette.paletteType == (int) PaletteImageType.Armor) {
               bool isActuallyHat = palette.paletteName.ToLower().StartsWith("hat");
               return isActuallyHat ? PaletteImageType.Hat : PaletteImageType.Armor;
            }

            if (palette.paletteType == (int) PaletteImageType.Weapon) {
               return PaletteImageType.Weapon;
            }
         }
      }

      return PaletteImageType.None;
   }

   #region Private Variables

   // Display list for editor reviewing
   [SerializeField]
   private List<DyeData> _dyeStatList = new List<DyeData>();

   // Data registry
   private Dictionary<int, DyeData> _dyeDataRegistry = new Dictionary<int, DyeData>();

   #endregion
}