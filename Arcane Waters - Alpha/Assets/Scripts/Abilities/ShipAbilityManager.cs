using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Events;

public class ShipAbilityManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static ShipAbilityManager self;

   // Determines if the list is generated already
   public bool hasInitialized;

   // Holds the list of the xml translated data
   public List<ShipAbilityData> shipAbilityDataList;

   // Determines if data setup is done
   public UnityEvent finishedDataSetup = new UnityEvent();

   #endregion

   private void Awake () {
      self = this; 
   }

   public void initializDataCache () {
      if (!hasInitialized) {
         shipAbilityDataList = new List<ShipAbilityData>();
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
               finishedDataSetup.Invoke();
               hasInitialized = true;
            });
         });
      }
   }

   public void receiveDataFromServer (ShipAbilityData[] dataCollection) {
      if (!hasInitialized) {
         shipAbilityDataList = new List<ShipAbilityData>();
         foreach (ShipAbilityData data in dataCollection) {
            if (!_shipAbilityData.ContainsKey(data.abilityName)) {
               _shipAbilityData.Add(data.abilityName, data);
               this.shipAbilityDataList.Add(data);
            }
         }
         finishedDataSetup.Invoke();
         hasInitialized = true;
      }
   }

   public ShipAbilityData getAbility (string name) {
      return _shipAbilityData[name];
   }

   public ShipAbilityData getAbility (Attack.Type attackType) {
      return shipAbilityDataList.Find(_ => _.selectedAttackType == attackType);
   }

   public static List<string> getRandomAbilities (int abilityCount) {
      List<string> totalAbilityList = new List<string>();
      foreach (ShipAbilityData ability in self.shipAbilityDataList) {
         totalAbilityList.Add(ability.abilityName);
      }

      List<string> randomAbilityList = new List<string>();
      if (totalAbilityList.Count > 0) {
         while (randomAbilityList.Count < abilityCount) {
            string newAbility = totalAbilityList.ChooseRandom();
            randomAbilityList.Add(newAbility);
            totalAbilityList.Remove(totalAbilityList.Find(_=>_ == newAbility));
         }
      } else {
         D.debug("No abilities available");
      }

      return randomAbilityList;
   }

   #region Private Variables

   // The cached data 
   private Dictionary<string, ShipAbilityData> _shipAbilityData = new Dictionary<string, ShipAbilityData>();

   #endregion
}
