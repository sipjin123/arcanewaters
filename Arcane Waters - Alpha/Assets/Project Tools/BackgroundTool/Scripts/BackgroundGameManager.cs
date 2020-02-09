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
   public GameObject spriteTemplatePrefab, spriteTemplateAnimatedPrefab;

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
      BackgroundContentData bgContentData = backgroundContentList.Find(_ => _.biomeType == board.biomeType);

      // Fetch the default background data if biome does not exist
      if (bgContentData == null) {
         bgContentData = backgroundContentList[0];
      }

      List<GameObject> leftBattleSpots = new List<GameObject>();
      List<GameObject> rightBattleSpots = new List<GameObject>();
      List<SimpleAnimation> simpleAnimList = new List<SimpleAnimation>();
      foreach (SpriteTemplateData spriteTempData in bgContentData.spriteTemplateList) {
         bool isAnimatedSprite = spriteTempData.contentCategory == ImageLoader.BGContentCategory.Animated;
         SpriteTemplate spriteTempObj = Instantiate(isAnimatedSprite ? spriteTemplateAnimatedPrefab : spriteTemplatePrefab, board.centerPoint).GetComponent<SpriteTemplate>();

         float zOffset = -spriteTempData.layerIndex - spriteTempData.zAxisOffset;
         spriteTempObj.transform.localPosition = new Vector3(spriteTempData.localPositionData.x, spriteTempData.localPositionData.y, zOffset);
         spriteTempObj.spriteRender.sprite = ImageManager.getSprite(spriteTempData.spritePath);

         if (!spriteTempObj.spriteRender.sprite.name.Contains(PLACEHOLDER)) {
            // Process placeholders
            if (spriteTempObj.spriteRender.sprite.name.Contains(BATTLE_POS_KEY_LEFT)) {
               leftBattleSpots.Add(spriteTempObj.gameObject);
            }
            if (spriteTempObj.spriteRender.sprite.name.Contains(BATTLE_POS_KEY_RIGHT)) {
               rightBattleSpots.Add(spriteTempObj.gameObject);
            }

            // Process animated sprites
            if (spriteTempData.contentCategory == ImageLoader.BGContentCategory.Animated) {
               BoxCollider2D collider2d = spriteTempObj.gameObject.AddComponent<BoxCollider2D>();
               collider2d.isTrigger = true;
               simpleAnimList.Add(spriteTempObj.GetComponent<SimpleAnimation>());
            }

            spriteTempObj.spriteData.contentCategory = spriteTempData.contentCategory;
            spriteTempObj.gameObject.name = spriteTempObj.spriteRender.sprite.name;
         } else {
            spriteTempObj.spriteRender.enabled = false;
            spriteTempObj.gameObject.name = ImageLoader.BGContentCategory.PlaceHolders.ToString();
         }
      }

      // Initialize animated sprites after all sprites have been placed
      foreach (SimpleAnimation simpleAnim in simpleAnimList) {
         simpleAnim.enabled = true;
      }

      if (leftBattleSpots.Count > 0 && rightBattleSpots.Count > 0) {
         board.recalibrateBattleSpots(leftBattleSpots, rightBattleSpots);
      }
   }

   #region Private Variables

   #endregion
}
