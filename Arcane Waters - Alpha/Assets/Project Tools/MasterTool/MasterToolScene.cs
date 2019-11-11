using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.SceneManagement;

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

   // Button triggers to open scene
   public Button clickAbilityScene, 
      clickMonsterScene, 
      clickSeaMonsterScene, 
      clickNPCScene, 
      clickCraftingScene, 
      clickMapScene, 
      clickShipScene,
      clickEquipmentScene,
      clickUsableItemScene;

   #endregion

   private void Awake () {
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
   }

   #region Private Variables

   #endregion
}
