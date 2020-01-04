using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ShipAbilityManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static ShipAbilityManager self;

   // Determines if the list is generated already
   public bool hasInitialized;

   // Holds the list of the xml translated data
   public List<ShipAbilityData> shipAbilityDataList;

   #endregion

   private void Awake () {
      self = this; 
   }

   public void initializDataCache () {
      if (!hasInitialized) {
         shipAbilityDataList = new List<ShipAbilityData>();
         hasInitialized = true;

         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            List<string> rawXMLData = DB_Main.getShipAbilityXML();

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               foreach (string rawText in rawXMLData) {
                  TextAsset newTextAsset = new TextAsset(rawText);
                  ShipAbilityData shipAbilityData = Util.xmlLoad<ShipAbilityData>(newTextAsset);

                  if (!_shipAbilityData.ContainsKey(shipAbilityData.abilityName)) {
                     _shipAbilityData.Add(shipAbilityData.abilityName, shipAbilityData);
                     shipAbilityDataList.Add(shipAbilityData);
                  }
               }
            });
         });
      }
   }

   public ShipAbilityData getAbility (string name) {
      return _shipAbilityData[name];
   }

   public ShipAbilityData getAbility (Attack.Type attackType) {
      return shipAbilityDataList.Find(_ => _.selectedAttackType == attackType);
   }

   #region Private Variables

   // The cached data 
   private Dictionary<string, ShipAbilityData> _shipAbilityData = new Dictionary<string, ShipAbilityData>();

   #endregion
}
