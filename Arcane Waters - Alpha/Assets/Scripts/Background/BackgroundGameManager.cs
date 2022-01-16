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
   public List<BackgroundContentData> backgroundContentList = new List<BackgroundContentData>();

   // Prefab for sprite templates
   public GameObject spriteTemplatePrefab, spriteTemplateAnimatedPrefab;

   // Key sprite names to determine battle positon status
   public static string BATTLE_POS_KEY_LEFT = "Battle_Position_Left";
   public static string BATTLE_POS_KEY_RIGHT = "Battle_Position_Right";
   public static string PLACEHOLDER = "Placeholder";

   // The address of animated mid ground elements
   public static string MID_GROUND_ANIMATED = "MidgroundAnimatedElements";

   // Determines if data is initialized
   public bool isInitialized;

   #endregion

   private void Awake () {
      self = this;
   }

   public void initializeDataCache () {
      resetBackgroundList();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<XMLPair> backgroundData = DB_Main.getBackgroundXML();

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (XMLPair xmlPair in backgroundData) {
               TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
               BackgroundContentData bgContentData = Util.xmlLoad<BackgroundContentData>(newTextAsset);
               bgContentData.xmlId = xmlPair.xmlId;
               backgroundContentList.Add(bgContentData);
            }

            isInitialized = true;
            BattleManager.self.initializeBattleBoards();
         });
      });
   }

   public void receiveNewContent (BackgroundContentData[] bgContentData) {
      if (!isInitialized) {
         resetBackgroundList();

         foreach (BackgroundContentData bgContent in bgContentData) {
            backgroundContentList.Add(bgContent);
         }

         BattleManager.self.initializeBattleBoards();
         StartCoroutine(CO_DelayProcessData());
      }
   }

   public void activateBgContent (int bgXmlId, bool isShipBattle) {
      BattleManager.self.initializeBattleBoards();
      BattleBoard board = BattleManager.self.battleBoard;
      setSpritesToClientBoard(bgXmlId, board);
      board.gameObject.SetActive(true);
      board.toggleShipBattleBackgroundElements(show: isShipBattle);
   }

   private void resetBackgroundList () {
      backgroundContentList = new List<BackgroundContentData>();
   }

   private IEnumerator CO_DelayProcessData () {
      yield return new WaitForSeconds(1);

      BackgroundContentData primaryBGData = backgroundContentList[0];
      BattleBoard board = BattleManager.self.battleBoard;
      setSpritesToClientBoard(primaryBGData.xmlId, board);
      board.gameObject.SetActive(true);
   }

   public void setSpritesToClientBoard (int boardXmlID, BattleBoard board) {
      BackgroundContentData bgContent = backgroundContentList.Find(_ => _.xmlId == boardXmlID);
      if (bgContent == null) {
         bgContent = backgroundContentList[0];
      }
      processBoardContent(bgContent, board);
   }

   public void setSpritesToRandomBoard (BattleBoard board) {
      BackgroundContentData bgContentData = new BackgroundContentData();

      // Biome should never be none, if for some reason it becomes none, log and use default biome
      if (board.biomeType == Biome.Type.None) {
         bgContentData = backgroundContentList[0];
      } else {
         List<BackgroundContentData> contentDataList = backgroundContentList.FindAll(_ => _.biomeType == board.biomeType);
         if (contentDataList.Count > 0) {
            if (contentDataList.Count < 2) {
               bgContentData = contentDataList[0];
            } else {
               int contentLength = contentDataList.Count;
               int randomIndex = Random.Range(0, contentLength);
               bgContentData = contentDataList[randomIndex];
            }
         } else {
            bgContentData = backgroundContentList[0];
         }
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
      try {
         board.centerPoint.gameObject.DestroyChildren();
      } catch {
         D.debug("Failed to fetch board centerpoint");
      }

      // Set up weather type
      board.setWeather(bgContentData.weatherType, bgContentData.biomeType);

      int leftSpotIndex = 1;
      int rightSpotIndex = 1;
      foreach (SpriteTemplateData spriteTempData in bgContentData.spriteTemplateList) {
         bool isAnimatedSprite = spriteTempData.contentCategory == ImageLoader.BGContentCategory.Interactive || spriteTempData.contentCategory == ImageLoader.BGContentCategory.Animating;
         GameObject spriteTempObj = Instantiate(isAnimatedSprite ? spriteTemplateAnimatedPrefab : spriteTemplatePrefab);
         spriteTempObj.transform.SetParent(board.centerPoint);

         SpriteTemplate spriteTemp = spriteTempObj.GetComponent<SpriteTemplate>();
         if (spriteTemp.guiClicker) { 
            spriteTemp.guiClicker.SetActive(false);
         }
         if (!spriteTempData.spritePath.Contains(PLACEHOLDER)) {
            float zOffset = -(spriteTempData.layerIndex - 1) - spriteTempData.zAxisOffset;
            spriteTemp.transform.localPosition = new Vector3(spriteTempData.localPositionData.x, spriteTempData.localPositionData.y, zOffset);
            if (spriteTempData.spritePath.Contains(BATTLE_POS_KEY_LEFT)) {
               leftBattleSpots.Add(spriteTemp.gameObject);
            }
            if (spriteTempData.spritePath.Contains(BATTLE_POS_KEY_RIGHT)) {
               rightBattleSpots.Add(spriteTemp.gameObject);
            }
         }

         if (spriteTemp != null && !Util.isBatch()) {
            float zOffset = -(spriteTempData.layerIndex - 1) - spriteTempData.zAxisOffset;
            spriteTemp.transform.localPosition = new Vector3(spriteTempData.localPositionData.x, spriteTempData.localPositionData.y, zOffset);
            spriteTemp.hasActiveClicker = false;
            spriteTemp.spriteRender.sprite = ImageManager.getSprite(spriteTempData.spritePath);

            if (!spriteTemp.spriteRender.sprite.name.Contains(PLACEHOLDER)) {
               // Process animated sprites
               if (spriteTempData.contentCategory == ImageLoader.BGContentCategory.Interactive) {
                  int spriteCount = ImageManager.getSprites(spriteTempData.spritePath).Length;
                  AnimatedSprite animatedSprite = spriteTemp.GetComponent<AnimatedSprite>();
                  animatedSprite.setToLoop();
                  animatedSprite.setToInteractable();
                  animatedSprite.setSpriteCount(spriteCount);

                  BoxCollider2D collider2d = spriteTemp.gameObject.AddComponent<BoxCollider2D>();
                  collider2d.size = new Vector2(collider2d.size.x / 2, collider2d.size.y / 2);
                  collider2d.isTrigger = true;
                  simpleAnimList.Add(spriteTemp.GetComponent<SimpleAnimation>());
               } else if (spriteTempData.contentCategory == ImageLoader.BGContentCategory.Animating) {
                  AnimatedSprite animatedSprite = spriteTemp.GetComponent<AnimatedSprite>();
                  animatedSprite.setToLoop();
               }

               // Add z snap component to interactives to have z axis adjustments with battlers
               bool isAnimatedMidGround = spriteTempData.spritePath.ToLower().Contains(MID_GROUND_ANIMATED.ToLower()) && spriteTempData.contentCategory == ImageLoader.BGContentCategory.Animating;
               if (spriteTempData.contentCategory == ImageLoader.BGContentCategory.Interactive || isAnimatedMidGround) {
                  ZSnap zSnap = spriteTemp.gameObject.AddComponent<ZSnap>();
                  zSnap.isActive = true;
                  if (isAnimatedMidGround) {
                     zSnap.offsetZ = -0.04f;
                  }
               }

               spriteTemp.spriteData.contentCategory = spriteTempData.contentCategory;
               spriteTemp.gameObject.name = spriteTemp.spriteRender.sprite.name;

               if (spriteTempData.contentCategory == ImageLoader.BGContentCategory.SpawnPoints_Attackers || spriteTempData.contentCategory == ImageLoader.BGContentCategory.SpawnPoints_Defenders) {
                  if (spriteTempData.contentCategory == ImageLoader.BGContentCategory.SpawnPoints_Attackers) {
                     spriteTemp.gameObject.name = "RightBattleSpot_" + rightSpotIndex;
                     rightSpotIndex++;
                  }
                  if (spriteTempData.contentCategory == ImageLoader.BGContentCategory.SpawnPoints_Defenders) {
                     spriteTemp.gameObject.name = "LeftBattleSpot_" + leftSpotIndex;
                     leftSpotIndex++;
                  }
                  spriteTemp.spriteRender.enabled = false;
               }
            } else {
               spriteTemp.spriteRender.enabled = false;
               spriteTemp.gameObject.name = ImageLoader.BGContentCategory.PlaceHolders.ToString();
            }
         }
      }

      // Initialize animated sprites after all sprites have been placed
      foreach (SimpleAnimation simpleAnim in simpleAnimList) {
         simpleAnim.enabled = true;
      }

      // Finalize battle setup
      if (leftBattleSpots.Count > 0 && rightBattleSpots.Count > 0) {
         board.recalibrateBattleSpots(leftBattleSpots, rightBattleSpots, bgContentData.xmlId);
      } else {
         D.debug("Not enough battle spots to initialize battle!");
      }
   }

   #region Private Variables

   #endregion
}
