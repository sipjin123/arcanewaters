using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;

public class SpecialtyManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static SpecialtyManager self;

   // For editor preview of data
   public List<PlayerSpecialtyData> specialtyDataList = new List<PlayerSpecialtyData>();

   #endregion

   public void Awake () {
      self = this;
   }

   public PlayerSpecialtyData getSpecialtyData (Specialty.Type specialtytype) {
      PlayerSpecialtyData returnData = _specialtyData[specialtytype];
      if (returnData == null) {
         Debug.LogWarning("The Specialty Does not Exist yet!: " + specialtytype);
      }
      return returnData;
   }

   public void initializeDataCache () {
      _specialtyData = new Dictionary<Specialty.Type, PlayerSpecialtyData>();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<string> rawXMLData = DB_Main.getPlayerClassXML(ClassManager.PlayerStatType.Specialty);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (string rawText in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(rawText);
               PlayerSpecialtyData specialtyData = Util.xmlLoad<PlayerSpecialtyData>(newTextAsset);
               Specialty.Type uniqueID = specialtyData.type;

               // Save the data in the memory cache
               if (!_specialtyData.ContainsKey(uniqueID)) {
                  _specialtyData.Add(uniqueID, specialtyData);
                  specialtyDataList.Add(specialtyData);
               }
            }
         });
      });
   }

   public void addSpecialtyInfo (PlayerSpecialtyData specialtyData) {
      Specialty.Type uniqueID = specialtyData.type;
      // Save the data in the memory cache
      if (!_specialtyData.ContainsKey(uniqueID)) {
         _specialtyData.Add(uniqueID, specialtyData);
         specialtyDataList.Add(specialtyData);
      }
   }

   #region Private Variables

   // The cached data collection
   private Dictionary<Specialty.Type, PlayerSpecialtyData> _specialtyData = new Dictionary<Specialty.Type, PlayerSpecialtyData>();

   #endregion
}