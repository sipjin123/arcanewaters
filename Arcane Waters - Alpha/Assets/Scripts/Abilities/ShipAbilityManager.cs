﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Events;
using System.Linq;

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

   // The default ship ability id
   public static int[] SHIP_ABILITY_DEFAULT = { 1, 33, 34, 35, 36 };

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

                  if (!shipAbilityDataList.Exists(_ => _.abilityId == rawData.xmlId) && rawData.isEnabled) {
                     shipAbilityData.abilityId = rawData.xmlId;
                     ShipAbilityPair newAbilityDataPair = new ShipAbilityPair {
                        abilityId = rawData.xmlId,
                        abilityName = shipAbilityData.abilityName,
                        shipAbilityData = shipAbilityData
                     };

                     // TODO: Remove when web tool is updated
                     if (newAbilityDataPair.abilityId == 50) {
                        newAbilityDataPair.shipAbilityData.summonCount = 5;
                        newAbilityDataPair.shipAbilityData.summonSeamonsterId = 41;
                        newAbilityDataPair.shipAbilityData.attackBufferCountMin = 10;
                        newAbilityDataPair.shipAbilityData.attackBufferCountMax = 15;
                        newAbilityDataPair.shipAbilityData.isMelee = true;
                     }
                     if (newAbilityDataPair.abilityId == 52) {
                        newAbilityDataPair.shipAbilityData.isMelee = true;
                     }
                     if (newAbilityDataPair.abilityId == 51) {
                        newAbilityDataPair.shipAbilityData.isMelee = true;
                        newAbilityDataPair.shipAbilityData.attackBufferCountMin = 10;
                        newAbilityDataPair.shipAbilityData.attackBufferCountMax = 15;
                     }
                     shipAbilityDataList.Add(newAbilityDataPair);
                  }
               }
               hasInitialized = true;
               finishedDataSetup.Invoke();
            });
         });
      }
   }

   public void receiveDataFromZipData (ShipAbilityPair[] dataCollection) {
      if (!hasInitialized) {
         shipAbilityDataList = new List<ShipAbilityPair>();
         foreach (ShipAbilityPair data in dataCollection) {
            if (!shipAbilityDataList.Exists(_=>_.abilityId == data.abilityId)) {
               data.shipAbilityData.abilityId = data.abilityId;
               ShipAbilityPair newAbilityDataPair = new ShipAbilityPair {
                  abilityName = data.abilityName,
                  abilityId = data.abilityId,
                  shipAbilityData = data.shipAbilityData
               };

               // TODO: Remove when web tool is updated
               if (newAbilityDataPair.abilityId == 50) {
                  newAbilityDataPair.shipAbilityData.summonCount = 5;
                  newAbilityDataPair.shipAbilityData.summonSeamonsterId = 41;
                  newAbilityDataPair.shipAbilityData.attackBufferCountMin = 10;
                  newAbilityDataPair.shipAbilityData.attackBufferCountMax = 15;
                  newAbilityDataPair.shipAbilityData.isMelee = true;
               }
               if (newAbilityDataPair.abilityId == 52) {
                  newAbilityDataPair.shipAbilityData.isMelee = true;
               }
               if (newAbilityDataPair.abilityId == 51) {
                  newAbilityDataPair.shipAbilityData.isMelee = true;
                  newAbilityDataPair.shipAbilityData.attackBufferCountMin = 10;
                  newAbilityDataPair.shipAbilityData.attackBufferCountMax = 15;
               }
               shipAbilityDataList.Add(newAbilityDataPair);
            }
         }
         hasInitialized = true;
         finishedDataSetup.Invoke();
      }
   }

   public ShipAbilityData getAbility (int id) {
      ShipAbilityPair shipAbility = shipAbilityDataList.Find(_ => _.shipAbilityData.abilityId == id);
      if (shipAbility == null) {
         D.editorLog("ERROR: Ship Ability Manager Missing ability: {" + id + "}", Color.red);
         return shipAbilityDataList[0].shipAbilityData;
      }
      return shipAbility.shipAbilityData;
   }

   public ShipAbilityData getAbility (Attack.Type attackType) {
      ShipAbilityPair shipAbility = shipAbilityDataList.Find(_ => _.shipAbilityData.selectedAttackType == attackType);
      if (shipAbility == null) {
         D.editorLog("Missing ability: " + attackType, Color.red);
         return shipAbilityDataList[0].shipAbilityData;
      }
      return shipAbility.shipAbilityData;
   }

   public static List<int> getRandomAbilities (int abilityCount) {
      List<ShipAbilityPair> totalAbilityList = new List<ShipAbilityPair>();

      // Select only the ship type abilities to be randomized by the ship entries in the shop
      foreach (ShipAbilityPair ability in self.shipAbilityDataList.FindAll(_ => _.shipAbilityData.seaEntityAbilityType != SeaEntityAbilityType.SeaMonsters).Where(_=>_.abilityId != ShipAbilityInfo.DEFAULT_ABILITY)) {
         totalAbilityList.Add(ability);
      }

      int offensiveAbilityCount = 0;
      List<int> randomAbilityList = new List<int>();
      if (totalAbilityList.Count > 0) {
         while (randomAbilityList.Count < abilityCount) {
            // Picks a random ability from the list 
            ShipAbilityPair newAbility = totalAbilityList.ChooseRandom();

            // Keeps track of the offensive ability count, each ship should have atleast one attack ability
            if (newAbility.shipAbilityData.shipCastType == ShipAbilityData.ShipCastType.Target) {
               offensiveAbilityCount++;
            }
            randomAbilityList.Add(newAbility.abilityId);

            // Removes the selected ability from the list to prevent duplication
            totalAbilityList.Remove(totalAbilityList.Find(_=>_ == newAbility));
         }
      } else {
         D.debug("No abilities available");
      }

      // Check if the offensive abilities are set to 0
      if (offensiveAbilityCount == 0) {
         // Enforce the first ability to be an offensive skill to prevent all abilities from randomizing into all buffs
         randomAbilityList[0] = totalAbilityList.FindAll(_ => _.shipAbilityData.shipCastType == ShipAbilityData.ShipCastType.Target).ChooseRandom().abilityId;
      }

      return randomAbilityList;
   }

   #region Private Variables

   #endregion
}
