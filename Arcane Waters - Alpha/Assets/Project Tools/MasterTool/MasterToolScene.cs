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

   // Button triggers to open scene
   public Button clickAbilityScene, clickMonsterScene, clickSeaMonsterScene, clickNPCScene, clickCraftingScene;

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
   }

   #region Private Variables

   #endregion
}
