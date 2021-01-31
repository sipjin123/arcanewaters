using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.SceneManagement;
using System;
using System.Linq;

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
   public const string shipAbilityScene = "ShipAbility Tool";
   public const string shopScene = "ShopTool";
   public const string backgroundScene = "BackgroundTool";
   public const string soundEffectScene = "Sound Effects Tool";
   public const string booksToolScene = "Books Tool";
   public const string cropsDataScene = "CropsDataTool";
   public const string discoveriesToolScene = "Discoveries Tool";
   public const string paletteToolScene = "Palette Tool";
   public const string perksToolScene = "Perk Tool";
   public const string treasureDropToolScene = "Treasure Drops Tool";
   public const string questDataToolScene = "QuestData Tool";
   public const string itemDefinitionToolScene = "ItemDefinition Tool";

   // Loading delay before fetching XML Data
   public static float loadDelay = 2;

   // Login instance of the master tool
   public GameObject loginInstance;

   // Initial reference for image manager before initialization
   public ImageManager imageManagerReference;

   // Undefined template name
   public static string UNDEFINED = "Undefined";

   // The parent of the tool selections
   public Transform toolTemplateHolder, toolTemplateFinalHolder;

   // List of children in the content holder
   public List<Transform> childList = new List<Transform>();

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
      clickShipAbilityScene,
      clickShopScene,
      clickBackgroundScene,
      clickSoundEffectScene,
      clickBooksToolScene,
      clickCropsDataScene,
      clickDiscoveriesToolScene,
      clickPaletteToolScene,
      clickPerkToolScene,
      clickTreasureDropToolScene,
      clickQuestDataToolScene,
      clickItemDefinitionToolScene,
      exitButton;

   #endregion

   private void Awake () {
      // Limit CPU usage in tools build
      QualitySettings.vSyncCount = 0;
      Application.targetFrameRate = 60;

      childList = new List<Transform>();
      foreach (Transform child in toolTemplateHolder) {
         childList.Add(child);
      }

      foreach (Transform child in childList.OrderBy(_=>_.name)) {
         child.SetParent(toolTemplateFinalHolder);
      }

      exitButton.onClick.AddListener(() => {
         Application.Quit();
      });

      clickTreasureDropToolScene.onClick.AddListener(() => {
         SceneManager.LoadScene(treasureDropToolScene);
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
      clickShipAbilityScene.onClick.AddListener(() => {
         SceneManager.LoadScene(shipAbilityScene);
      });
      clickShopScene.onClick.AddListener(() => {
         SceneManager.LoadScene(shopScene);
      });
      clickBackgroundScene.onClick.AddListener(() => {
         SceneManager.LoadScene(backgroundScene);
      });
      clickSoundEffectScene.onClick.AddListener(() => {
         SceneManager.LoadScene(soundEffectScene);
      });
      clickBooksToolScene.onClick.AddListener(() => {
         SceneManager.LoadScene(booksToolScene);
      });
      clickCropsDataScene.onClick.AddListener(() => {
         SceneManager.LoadScene(cropsDataScene);
      });
      clickDiscoveriesToolScene.onClick.AddListener(() => {
         SceneManager.LoadScene(discoveriesToolScene);
      });
      clickPaletteToolScene.onClick.AddListener(() => {
         SceneManager.LoadScene(paletteToolScene);
      });
      clickPerkToolScene.onClick.AddListener(() => {
         SceneManager.LoadScene(perksToolScene);
      });
      clickQuestDataToolScene.onClick.AddListener(() => {
         SceneManager.LoadScene(questDataToolScene);
      });
      clickItemDefinitionToolScene.onClick.AddListener(() => {
         SceneManager.LoadScene(itemDefinitionToolScene);
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