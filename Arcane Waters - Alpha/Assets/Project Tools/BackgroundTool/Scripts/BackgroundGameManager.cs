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

   // Determines if data is initialized
   public bool isInitialized;

   #endregion

   private void Awake () {
      self = this;

      // Fetch the default background data for clients
      if (!NetworkServer.active) {
         initializeDataCache(true);
      }
   }

   public void initializeDataCache (bool ifDefault) {
      backgroundContentList = new List<BackgroundContentData>();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<XMLPair> backgroundData = DB_Main.getBackgroundXML(ifDefault);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (XMLPair xmlPair in backgroundData) {
               TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
               BackgroundContentData bgContentData = Util.xmlLoad<BackgroundContentData>(newTextAsset);
               bgContentData.xmlId = xmlPair.xmlId;
               backgroundContentList.Add(bgContentData);
            }

            if (!ifDefault) {
               isInitialized = true;
            }
            BattleManager.self.initializeBattleBoards();
         });
      });
   }

   public void receiveNewContent (BackgroundContentData[] bgContentData) {
      if (!isInitialized) {
         backgroundContentList = new List<BackgroundContentData>();
         foreach (BackgroundContentData bgContent in bgContentData) {
            backgroundContentList.Add(bgContent);
         }

         BattleManager.self.initializeBattleBoards();
         StartCoroutine(CO_DelayProcessData());
      }
   }

   private IEnumerator CO_DelayProcessData () {
      yield return new WaitForSeconds(1);

      BackgroundContentData primaryBGData = backgroundContentList[0];
      BattleBoard board = BattleManager.self.getBattleBoard(primaryBGData.biomeType);
      setSpritesToClientBoard(primaryBGData.xmlId, board);
      board.gameObject.SetActive(true);
   }

   public void setSpritesToClientBoard (int boardXmlID, BattleBoard board) {
      BackgroundContentData bgContent = backgroundContentList.Find(_ => _.xmlId == boardXmlID);
      processBoardContent(bgContent, board);
   }

   public void setSpritesToRandomBoard (BattleBoard board) {
      BackgroundContentData bgContentData = new BackgroundContentData();
      List<BackgroundContentData> contentDataList = backgroundContentList.FindAll(_ => _.biomeType == board.biomeType);
      if (contentDataList.Count > 0) {
         if (contentDataList.Count < 2) {
            bgContentData = contentDataList[0];
         } else {
            int contentLength = contentDataList.Count;
            int randomIndex = Random.Range(0, 2);
            bgContentData = contentDataList[randomIndex];
         }
      } else {
         bgContentData = backgroundContentList[0];
      }

      processBoardContent(bgContentData, board);
   }

   public void setSpritesToBattleBoard (BattleBoard board) {
      BackgroundContentData bgContentData = backgroundContentList.Find(_ => _.biomeType == board.biomeType);

      // Fetch the default background data if biome does not exist
      if (bgContentData == null) {
         bgContentData = backgroundContentList[0];
      }

      processBoardContent(bgContentData, board);
   }

   private void processBoardContent (BackgroundContentData bgContentData, BattleBoard board) {
      List<GameObject> leftBattleSpots = new List<GameObject>();
      List<GameObject> rightBattleSpots = new List<GameObject>();
      List<SimpleAnimation> simpleAnimList = new List<SimpleAnimation>();
      board.centerPoint.gameObject.DestroyChildren();

      foreach (SpriteTemplateData spriteTempData in bgContentData.spriteTemplateList) {
         bool isAnimatedSprite = spriteTempData.contentCategory == ImageLoader.BGContentCategory.Interactive || spriteTempData.contentCategory == ImageLoader.BGContentCategory.Animating;
         SpriteTemplate spriteTempObj = Instantiate(isAnimatedSprite ? spriteTemplateAnimatedPrefab : spriteTemplatePrefab, board.centerPoint).GetComponent<SpriteTemplate>();

         float zOffset = -spriteTempData.layerIndex - spriteTempData.zAxisOffset;
         spriteTempObj.transform.localPosition = new Vector3(spriteTempData.localPositionData.x, spriteTempData.localPositionData.y, zOffset);
         spriteTempObj.spriteRender.sprite = ImageManager.getSprite(spriteTempData.spritePath);
         spriteTempObj.hasActiveClicker = false;

         if (!spriteTempObj.spriteRender.sprite.name.Contains(PLACEHOLDER)) {
            // Process placeholders
            if (spriteTempObj.spriteRender.sprite.name.Contains(BATTLE_POS_KEY_LEFT)) {
               leftBattleSpots.Add(spriteTempObj.gameObject);
            }
            if (spriteTempObj.spriteRender.sprite.name.Contains(BATTLE_POS_KEY_RIGHT)) {
               rightBattleSpots.Add(spriteTempObj.gameObject);
            }

            // Process animated sprites
            if (spriteTempData.contentCategory == ImageLoader.BGContentCategory.Interactive) {
               int spriteCount = ImageManager.getSprites(spriteTempData.spritePath).Length;
               AnimatedSprite animatedSprite = spriteTempObj.GetComponent<AnimatedSprite>();
               animatedSprite.setToLoop();
               animatedSprite.setToInteractable();
               animatedSprite.setSpriteCount(spriteCount);

               BoxCollider2D collider2d = spriteTempObj.gameObject.AddComponent<BoxCollider2D>();
               collider2d.isTrigger = true;
               simpleAnimList.Add(spriteTempObj.GetComponent<SimpleAnimation>());
            } else if (spriteTempData.contentCategory == ImageLoader.BGContentCategory.Animating) {
               AnimatedSprite animatedSprite = spriteTempObj.GetComponent<AnimatedSprite>();
               animatedSprite.setToLoop();
            }

            spriteTempObj.spriteData.contentCategory = spriteTempData.contentCategory;
            spriteTempObj.gameObject.name = spriteTempObj.spriteRender.sprite.name;

            if (spriteTempData.contentCategory == ImageLoader.BGContentCategory.SpawnPoints_Attackers || spriteTempData.contentCategory == ImageLoader.BGContentCategory.SpawnPoints_Defenders) {
               spriteTempObj.spriteRender.enabled = false;
               spriteTempObj.gameObject.name = ImageLoader.BGContentCategory.PlaceHolders.ToString();
            }
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
         board.recalibrateBattleSpots(leftBattleSpots, rightBattleSpots, bgContentData.xmlId);
      }
   }

   #region Private Variables

   #endregion
}
