using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;

public class SpecialtyManager : XmlManager {
   #region Public Variables

   // Self
   public static SpecialtyManager self;

   #endregion

   public void Awake () {
      self = this;
   }

   private void Start () {
      initializeDataCache();
   }

   public PlayerSpecialtyData getSpecialtyData (Specialty.Type specialtytype) {
      PlayerSpecialtyData returnData = _specialtyData[specialtytype];
      if (returnData == null) {
         Debug.LogWarning("The Specialty Does not Exist yet!: " + specialtytype);
      }
      return returnData;
   }

   private void initializeDataCache () {
      _specialtyData = new Dictionary<Specialty.Type, PlayerSpecialtyData>();

      // Iterate over the files
      foreach (TextAsset textAsset in textAssets) {
         // Read and deserialize the file
         PlayerSpecialtyData specialtyData = Util.xmlLoad<PlayerSpecialtyData>(textAsset);
         Specialty.Type uniqueID = specialtyData.type;

         // Save the data in the memory cache
         if (!_specialtyData.ContainsKey(uniqueID)) {
            _specialtyData.Add(uniqueID, specialtyData);
         }
      }
   }

   public override void loadAllXMLData () {
      base.loadAllXMLData();
      loadXMLData(PlayerSpecialtyToolManager.FOLDER_PATH);
   }

   public override void clearAllXMLData () {
      base.clearAllXMLData();
      textAssets = new List<TextAsset>();
   }

   #region Private Variables

   // The cached data collection
   private Dictionary<Specialty.Type, PlayerSpecialtyData> _specialtyData = new Dictionary<Specialty.Type, PlayerSpecialtyData>();

   #endregion
}