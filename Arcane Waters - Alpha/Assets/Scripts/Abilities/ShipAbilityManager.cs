﻿using UnityEngine;
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
   public List<ShipAbilityPair> shipAbilityDataList;

   // Determines if data setup is done
   public UnityEvent finishedDataSetup = new UnityEvent();

   #endregion

   private void Awake () {
      self = this; 
   }

   public void initializDataCache () {
      if (!hasInitialized) {
         shipAbilityDataList = new List<ShipAbilityPair>();
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            List<XMLPair> rawXMLData = DB_Main.getShipAbilityXML();

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               foreach (XMLPair rawData in rawXMLData) {
                  TextAsset newTextAsset = new TextAsset(rawData.rawXmlData);
                  ShipAbilityData shipAbilityData = Util.xmlLoad<ShipAbilityData>(newTextAsset);

                  if (!_shipAbilityData.ContainsKey(rawData.xmlId)) {
                     _shipAbilityData.Add(rawData.xmlId, shipAbilityData);
                     shipAbilityDataList.Add(new ShipAbilityPair { 
                        abilityId = rawData.xmlId,
                        abilityName = shipAbilityData.abilityName,
                        shipAbilityData = shipAbilityData
                     });
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

   public ShipAbilityData getAbility (int id) {
      return _shipAbilityData[id];
   }

   public ShipAbilityData getAbility (Attack.Type attackType) {
      return shipAbilityDataList.Find(_ => _.shipAbilityData.selectedAttackType == attackType).shipAbilityData;
   }

   public static List<int> getRandomAbilities (int abilityCount) {
      List<int> totalAbilityList = new List<int>();
      foreach (ShipAbilityPair ability in self.shipAbilityDataList) {
         totalAbilityList.Add(ability.abilityId);
      }

      List<int> randomAbilityList = new List<int>();
      if (totalAbilityList.Count > 0) {
         while (randomAbilityList.Count < abilityCount) {
            int newAbility = totalAbilityList.ChooseRandom();
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
   private Dictionary<int, ShipAbilityData> _shipAbilityData = new Dictionary<int, ShipAbilityData>();

   #endregion
}
