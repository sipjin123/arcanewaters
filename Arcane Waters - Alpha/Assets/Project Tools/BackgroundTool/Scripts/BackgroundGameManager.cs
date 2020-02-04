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
      foreach (SpriteTemplateData spriteTempData in backgroundContentList[0].spriteTemplateList) {
         SpriteTemplate spriteTempObj = Instantiate(spriteTemplatePrefab, board.centerPoint).GetComponent<SpriteTemplate>();
         
         spriteTempObj.transform.localPosition = new Vector3(spriteTempData.localPosition.x, spriteTempData.localPosition.y, -spriteTempData.layerIndex);
         spriteTempObj.transform.localEulerAngles = new Vector3(0, 0, spriteTempData.rotationAlteration);
         spriteTempObj.spriteRender.sprite = ImageManager.getSprite(spriteTempData.spritePath);
         spriteTempObj.transform.localScale = new Vector3(spriteTempData.scaleAlteration, spriteTempData.scaleAlteration, spriteTempData.scaleAlteration);
      }
   }

   #region Private Variables

   #endregion
}
