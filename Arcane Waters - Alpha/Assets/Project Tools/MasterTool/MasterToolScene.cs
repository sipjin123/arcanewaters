using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.SceneManagement;
using System;

public class MasterToolScene : MonoBehaviour {
   #region Public Variables

   // Scene Names
   public static string masterScene = "MasterTool";
   public const string abilityScene = "Ability Data Tool";
   public const string monsterScene = "Monster Data Tool";
   public const string seaMonsterScene = "Sea Monster Data Tool";
   public const string npcScene = "NPC Tool";
   public const string craftingScene = "CraftingScene";
   public const string mapScene = "MapCreationTool";
   public const string shipScene = "Ship Data Tool";
   public const string equipmentScene = "Equipment Data Tool";
   public const string usableItemScene = "UsableItemData Tool";
   public const string achievementScene = "Achievement Tool";
   public const string playerClassScene = "PlayerClass Tool";
   public const string playerFactionScene = "PlayerFaction Tool";
   public const string playerSpecialtyScene = "PlayerSpecialty Tool";
   public const string playerJobScene = "PlayerJob Tool";
   public const string shipAbilityScene = "ShipAbility Tool";
   public const string shopScene = "ShopTool";
   public const string tutorialScene = "Tutorial Tool";
   public const string backgroundScene = "BackgroundTool";
   public const string soundEffectScene = "Sound Effects Tool";
   public const string cropsDataScene = "CropsDataTool";

   // Loading delay before fetching XML Data
   public static float loadDelay = 2;

   // Login instance of the master tool
   public GameObject loginInstance;

   // Initial reference for image manager before initialization
   public ImageManager imageManagerReference;

   // Undefined template name
   public static string UNDEFINED = "Undefined";

   // Button triggers to open scene
   public Button clickAbilityScene, 
      clickMonsterScene, 
      clickSeaMonsterScene, 
      clickNPCScene, 
      clickCraftingScene, 
      clickMapScene, 
      clickShipScene,
      clickEquipmentScene,
      clickUsableItemScene,
      clickAchievementScene,
      clickPlayerClassScene,
      clickFactionScene,
      clickSpecialtyScene,
      clickJobScene,
      clickShipAbilityScene,
      clickShopScene,
      clickTutorialScene,
      clickBackgroundScene,
      clickSoundEffectScene,
      clickCropsDataScene,
      exitButton;

   #endregion

   private void Awake () {
      exitButton.onClick.AddListener(() => {
         Application.Quit();
      });

      clickAbilityScene.onClick.AddListener(() => {
         SceneManager.LoadScene(abilityScene);
      });
      clickMonsterScene.onClick.AddListener(() => {
         SceneManager.LoadScene(monsterScene);
      });
      clickSeaMonsterScene.onClick.AddListener(() => {
         SceneManager.LoadScene(seaMonsterScene);
      });
      clickNPCScene.onClick.AddListener(() => {
         SceneManager.LoadScene(npcScene);
      });
      clickCraftingScene.onClick.AddListener(() => {
         SceneManager.LoadScene(craftingScene);
      });
      clickMapScene.onClick.AddListener(() => {
         SceneManager.LoadScene(mapScene);
      });
      clickShipScene.onClick.AddListener(() => {
         SceneManager.LoadScene(shipScene);
      });
      clickEquipmentScene.onClick.AddListener(() => {
         SceneManager.LoadScene(equipmentScene);
      });
      clickUsableItemScene.onClick.AddListener(() => {
         SceneManager.LoadScene(usableItemScene);
      });
      clickAchievementScene.onClick.AddListener(() => {
         SceneManager.LoadScene(achievementScene);
      });
      clickPlayerClassScene.onClick.AddListener(() => {
         SceneManager.LoadScene(playerClassScene);
      });
      clickFactionScene.onClick.AddListener(() => {
         SceneManager.LoadScene(playerFactionScene);
      });
      clickSpecialtyScene.onClick.AddListener(() => {
         SceneManager.LoadScene(playerSpecialtyScene);
      });
      clickJobScene.onClick.AddListener(() => {
         SceneManager.LoadScene(playerJobScene);
      });
      clickShipAbilityScene.onClick.AddListener(() => {
         SceneManager.LoadScene(shipAbilityScene);
      });
      clickShopScene.onClick.AddListener(() => {
         SceneManager.LoadScene(shopScene);
      });
      clickTutorialScene.onClick.AddListener(() => {
         SceneManager.LoadScene(tutorialScene);
      });
      clickBackgroundScene.onClick.AddListener(() => {
         SceneManager.LoadScene(backgroundScene);
      });
      clickSoundEffectScene.onClick.AddListener(() => {
         SceneManager.LoadScene(soundEffectScene);
      });
      clickCropsDataScene.onClick.AddListener(() => {
         SceneManager.LoadScene(cropsDataScene);
      });

      if (MasterToolAccountManager.self == null) {
         // Login panel will be created only once, if the scene resets and the login panel is already active it will no longer instantiate
         GameObject loginPanel = Instantiate(loginInstance);

         // Image manager will be injected to the login panel
         imageManagerReference.transform.SetParent(loginPanel.transform);
         imageManagerReference.gameObject.SetActive(true);
      } 
   }

   #region Private Variables

   #endregion
}