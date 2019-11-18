using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class SpecialyManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static SpecialyManager self;

   // The files containing the specialty data
   public TextAsset[] specialtyDataAssets;

   // Determines if the list is generated already
   public bool hasInitialized;

   // Holds the list of the xml translated data [FOR EDITOR DISPLAY DATA REVIEW]
   public List<PlayerSpecialtyData> specialtyDataList;

   #endregion

   public void Awake () {
      self = this;
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
      if (!hasInitialized) {
         specialtyDataList = new List<PlayerSpecialtyData>();
         hasInitialized = true;
         // Iterate over the files
         foreach (TextAsset textAsset in specialtyDataAssets) {
            // Read and deserialize the file
            PlayerSpecialtyData specialtyData = Util.xmlLoad<PlayerSpecialtyData>(textAsset);
            Specialty.Type uniqueID = specialtyData.type;

            // Save the data in the memory cache
            if (!_specialtyData.ContainsKey(uniqueID)) {
               _specialtyData.Add(uniqueID, specialtyData);
               specialtyDataList.Add(specialtyData);
            }
         }
      }
   }

   #region Private Variables

   // The cached data collection
   private Dictionary<Specialty.Type, PlayerSpecialtyData> _specialtyData = new Dictionary<Specialty.Type, PlayerSpecialtyData>();

   #endregion
}