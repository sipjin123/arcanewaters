using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using BackgroundTool;

public class BackgroundGameManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static BackgroundGameManager self;

   // List of background data loaded from the server
   public List<BackgroundContentData> backgroundContentList;

   // Prefab for sprite templates
   public GameObject spriteTemplatePrefab;

   // Key sprite names to determine battle positon status
   public static string BATTLE_POS_KEY_LEFT = "Battle_Position_Left";
   public static string BATTLE_POS_KEY_RIGHT = "Battle_Position_Right";
   public static string PLACEHOLDER = "Placeholder";
   
   #endregion

   private void Awake () {
      self = this;
   }

   public void initializeDataCache () {
      backgroundContentList = new List<BackgroundContentData>();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<XMLPair> backgroundData = DB_Main.getBackgroundXML();
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (XMLPair xmlPair in backgroundData) {
               TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
               BackgroundContentData bgContentData = Util.xmlLoad<BackgroundContentData>(newTextAsset);
               backgroundContentList.Add(bgContentData);
            }

            BattleManager.self.initializeBattleBoards();
         });
      });
   }

   public void setSpritesToBattleBoard (BattleBoard board) {
      // TODO: Temporary set all battle boards to have the same custom sprites

      List<GameObject> leftBattleSpots = new List<GameObject>();
      List<GameObject> rightBattleSpots = new List<GameObject>();
      foreach (SpriteTemplateData spriteTempData in backgroundContentList[0].spriteTemplateList) {
         SpriteTemplate spriteTempObj = Instantiate(spriteTemplatePrefab, board.centerPoint).GetComponent<SpriteTemplate>();

         float zOffset = -spriteTempData.layerIndex - spriteTempData.zAxisOffset;
         spriteTempObj.transform.localPosition = new Vector3(spriteTempData.localPositionData.x, spriteTempData.localPositionData.y, zOffset);
         spriteTempObj.spriteRender.sprite = ImageManager.getSprite(spriteTempData.spritePath);

         if (!spriteTempObj.spriteRender.sprite.name.Contains(PLACEHOLDER)) {
            if (spriteTempObj.spriteRender.sprite.name.Contains(BATTLE_POS_KEY_LEFT)) {
               leftBattleSpots.Add(spriteTempObj.gameObject);
            }
            if (spriteTempObj.spriteRender.sprite.name.Contains(BATTLE_POS_KEY_RIGHT)) {
               rightBattleSpots.Add(spriteTempObj.gameObject);
            }
         }
      }

      if (leftBattleSpots.Count > 0 && rightBattleSpots.Count > 0) {
         board.recalibrateBattleSpots(leftBattleSpots, rightBattleSpots);
      }
   }

   #region Private Variables

   #endregion
}
