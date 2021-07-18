﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MapCreationTool
{
   public class MonsterManager : MonoBehaviour {
      public static event System.Action OnLoaded;
      public static MonsterManager instance { get; private set; }

      private BattlerXMLContent[] landMonsters = new BattlerXMLContent[0];
      private SeaMonsterXMLContent[] seaMonsters = new SeaMonsterXMLContent[0];

      public Dictionary<int, BattlerXMLContent> idToLandMonster { get; private set; }
      public Dictionary<int, SeaMonsterXMLContent> idToSeaMonster { get; private set; }

      public List<LootGroupData> lootGroups = new List<LootGroupData>();

      public bool loaded { get; private set; }

      private void Awake () {
         instance = this;
      }

      private IEnumerator Start () {
         yield return new WaitUntil(() => ImageManager.self != null);

         loadAllLootGroupData();
         loadAllMonsters();
      }

      public Texture2D getLandMonsterTexture (int id) {
         if (!idToLandMonster.ContainsKey(id)) {
            Debug.LogWarning($"Unrecognized monster ID {id}.");
            return null;
         }

         return ImageManager.getSprite(idToLandMonster[id].battler.imagePath).texture;
      }

      public Texture2D getSeaMonsterTexture (int seamonsterType) {
         if (!idToSeaMonster.Values.ToList().Exists(_=> _.seaMonsterData.seaMonsterType == (SeaMonsterEntity.Type) seamonsterType)) {
            Debug.LogWarning($"Unrecognized monster ID {seamonsterType}.");
            return null;
         }

         return ImageManager.getSprite(idToSeaMonster.Values.ToList().Find(_=>_.seaMonsterData.seaMonsterType == (SeaMonsterEntity.Type) seamonsterType).seaMonsterData.defaultSpritePath).texture;
      }

      public Texture2D getFirstLandMonsterTexture () {
         return landMonsterCount == 0 ? null : ImageManager.getSprite(landMonsters[0].battler.imagePath).texture;
      }

      public Texture2D getFirstSeaMonsterTexture () {
         return seaMonsterCount == 0 ? null : ImageManager.getSprite(seaMonsters[0].seaMonsterData.defaultSpritePath).texture;
      }

      public SelectOption[] formLandMonsterSelectionOptions () {
         return landMonsters.Select(n => new SelectOption(
            ((int) n.battler.enemyType).ToString(),
            n.battler.enemyType + ": " + n.battler.enemyName)
         ).ToArray();
      }

      public SelectOption[] formLootGroupSelectionOptions () {
         return lootGroups.Select(n => new SelectOption(
            (n.xmlId).ToString(),
            n.lootGroupName)
         ).ToArray();
      }

      public SelectOption[] formSeaMonsterSelectionOptions () {
         return seaMonsters.Where(sm => sm.seaMonsterData.roleType != RoleType.Minion && sm.seaMonsterData.seaMonsterType != SeaMonsterEntity.Type.PirateShip).Select(n => new SelectOption(
            ((int) n.seaMonsterData.seaMonsterType).ToString(),
            (int) n.seaMonsterData.seaMonsterType + ": " + n.seaMonsterData.monsterName)
         ).ToArray();
      }

      public SelectOption[] formPirateShipOptions () {
         List<SelectOption> returnOptions = new List<SelectOption>();
         List<SelectOption> extractedOptions = seaMonsters.Where(sm => sm.seaMonsterData.seaMonsterType == SeaMonsterEntity.Type.PirateShip).Select(n => new SelectOption(
            ((int) n.xmlId).ToString(),
            (int) n.seaMonsterData.seaMonsterType + ": " + n.seaMonsterData.monsterName)
         ).ToList();

         returnOptions.Add(new SelectOption("None", "-1"));
         foreach (SelectOption option in extractedOptions) {
            returnOptions.Add(option);
         }

         return returnOptions.ToArray();
      }

      public List<SeaMonsterXMLContent> getSeaMonsters () {
         return seaMonsters.ToList();
      }

      public int seaMonsterCount
      {
         get { return seaMonsters.Length; }
      }

      public int landMonsterCount
      {
         get { return landMonsters.Length; }
      }

      private void loadAllLootGroupData () {
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            List<XMLPair> lootDropXmlData = DB_Main.getBiomeTreasureDrops();

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               // Loads all the loot drops xml
               foreach (XMLPair xmlPair in lootDropXmlData) {
                  TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
                  LootGroupData lootDropData = Util.xmlLoad<LootGroupData>(newTextAsset);
                  int uniqueId = xmlPair.xmlId;

                  if (!lootGroups.Exists(_=>_.xmlId == uniqueId)) {
                     lootGroups.Add(lootDropData);
                  }
               }
            });
         });
      }

      private void loadAllMonsters () {
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            List<XMLPair> landMonsterData = DB_Main.getLandMonsterXML();
            List<XMLPair> seaMonsterData = DB_Main.getSeaMonsterXML();

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               setData(landMonsterData, seaMonsterData);
            });
         });
      }

      private void setData (List<XMLPair> landMonsterData, List<XMLPair> seaMonsterData) {
         try {
            idToLandMonster = new Dictionary<int, BattlerXMLContent>();
            idToSeaMonster = new Dictionary<int, SeaMonsterXMLContent>();

            foreach (XMLPair data in landMonsterData) {
               BattlerData battler = Util.xmlLoad<BattlerData>(new TextAsset(data.rawXmlData));
               if (battler.enemyType != Enemy.Type.PlayerBattler) {
                  int enemyTypeID = (int) battler.enemyType;

                  if (!idToLandMonster.ContainsKey(enemyTypeID) && data.isEnabled) {
                     idToLandMonster.Add(enemyTypeID, new BattlerXMLContent {
                        xmlId = data.xmlId,
                        battler = battler,
                        isEnabled = data.isEnabled
                     });
                  }
               }
            }

            foreach (XMLPair data in seaMonsterData) {
               SeaMonsterEntityData monster = Util.xmlLoad<SeaMonsterEntityData>(new TextAsset(data.rawXmlData));
               int monsterID = (int) monster.seaMonsterType;

               if (!idToSeaMonster.ContainsKey(data.xmlId) && data.isEnabled) {
                  idToSeaMonster.Add(data.xmlId, new SeaMonsterXMLContent {
                     xmlId = data.xmlId,
                     seaMonsterData = monster,
                     isEnabled = data.isEnabled
                  });
               }
            }

            landMonsters = idToLandMonster.OrderBy(n => n.Key).Select(n => n.Value).ToArray();
            seaMonsters = idToSeaMonster.OrderBy(n => n.Key).Select(n => n.Value).ToArray();
         } catch (Exception ex) {
            Utilities.warning("Failed to load monster manager. Exception:\n" + ex);
            UI.messagePanel.displayError("Failed to load monster manager. Exception:\n" + ex);
         }

         loaded = true;
         OnLoaded?.Invoke();
      }
   }
}