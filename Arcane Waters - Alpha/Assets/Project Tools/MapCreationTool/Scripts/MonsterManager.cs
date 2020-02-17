﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MapCreationTool
{
   public class MonsterManager : MonoBehaviour
   {
      public static event System.Action OnLoaded;
      public static MonsterManager instance { get; private set; }

      private BattlerXMLContent[] landMonsters = new BattlerXMLContent[0];
      private SeaMonsterXMLContent[] seaMonsters = new SeaMonsterXMLContent[0];

      public Dictionary<int, BattlerXMLContent> idToLandMonster { get; private set; }
      public Dictionary<int, SeaMonsterXMLContent> idToSeaMonster { get; private set; }

      public bool loaded { get; private set; }

      private void Awake () {
         instance = this;
      }

      private IEnumerator Start () {
         yield return new WaitUntil(() => ImageManager.self != null);

         loadAllMonsters();
      }

      public Texture2D getLandMonsterTexture (int id) {
         if (!idToLandMonster.ContainsKey(id)) {
            Debug.LogError("Unrecognized monster ID.");
            return null;
         }

         return ImageManager.getTexture(idToLandMonster[id].battler.imagePath);
      }

      public Texture2D getSeaMonsterTexture (int id) {
         if (!idToSeaMonster.ContainsKey(id)) {
            Debug.LogError("Unrecognized monster ID.");
            return null;
         }

         return ImageManager.getTexture(idToSeaMonster[id].seaMonsterData.defaultSpritePath);
      }

      public Texture2D getFirstLandMonsterTexture () {
         return landMonsterCount == 0 ? null : ImageManager.getTexture(landMonsters[0].battler.imagePath);
      }

      public Texture2D getFirstSeaMonsterTexture () {
         return seaMonsterCount == 0 ? null : ImageManager.getTexture(seaMonsters[0].seaMonsterData.defaultSpritePath);
      }

      public string[] formLandMonsterSelectionOptions () {
         return landMonsters.Select(n => n.xmlId + ": " + n.battler.enemyName).ToArray();
      }

      public string[] formSeaMonsterSelectionOptions () {
         return seaMonsters.Select(n => n.xmlId + ": " + n.seaMonsterData.monsterName).ToArray();
      }

      public int seaMonsterCount
      {
         get { return seaMonsters.Length; }
      }

      public int landMonsterCount
      {
         get { return landMonsters.Length; }
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
         idToLandMonster = new Dictionary<int, BattlerXMLContent>();
         idToSeaMonster = new Dictionary<int, SeaMonsterXMLContent>();

         foreach (XMLPair data in landMonsterData) {
            BattlerData battler = Util.xmlLoad<BattlerData>(new TextAsset(data.rawXmlData));

            if (!idToLandMonster.ContainsKey(data.xmlId)) {
               idToLandMonster.Add(data.xmlId, new BattlerXMLContent {
                  xmlId = data.xmlId,
                  battler = battler,
                  isEnabled = data.isEnabled
               });
            }
         }

         foreach (XMLPair data in seaMonsterData) {
            SeaMonsterEntityData monster = Util.xmlLoad<SeaMonsterEntityData>(new TextAsset(data.rawXmlData));

            if (!idToSeaMonster.ContainsKey(data.xmlId)) {
               idToSeaMonster.Add(data.xmlId, new SeaMonsterXMLContent {
                  xmlId = data.xmlId,
                  seaMonsterData = monster,
                  isEnabled = data.isEnabled
               });
            }
         }

         landMonsters = idToLandMonster.OrderBy(n => n.Key).Select(n => n.Value).ToArray();
         seaMonsters = idToSeaMonster.OrderBy(n => n.Key).Select(n => n.Value).ToArray();

         loaded = true;
         OnLoaded?.Invoke();
      }
   }
}