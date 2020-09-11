using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System;

namespace ItemDefinitionTool
{
   public class ItemDefinitionToolManager : MonoBehaviour
   {
      #region Public Variables

      // Singleton instance
      public static ItemDefinitionToolManager self;

      // Item definition that is currently being edited
      public static ItemDefinition selectedItemDefinition;

      // Prefab of master tool account manager for when we are testing this scene individually
      public MasterToolAccountManager accountManagerPref = null;

      // Button for creating new item definitions
      public Button createNewButton;

      #endregion

      private void Awake () {
         self = this;

         if (MasterToolAccountManager.self != null && !MasterToolAccountManager.canAlterData()) {
            createNewButton.gameObject.SetActive(false);
         }

#if UNITY_EDITOR
         if (MasterToolAccountManager.self == null) {
            MasterToolAccountManager loginPanel = Instantiate(accountManagerPref);

            loginPanel.passwordField.text = "test";
            loginPanel.loginButton.onClick.Invoke();
         }
#endif
      }

      private void Start () {
         load();
      }

      private void load () {
         XmlLoadingPanel.self.startLoading();
         ItemDefinitionList.self.set(new List<ItemDefinition>());

         ItemDefinitionManager.self.loadFromDatabase((list) => {
            ItemDefinitionList.self.set(list);
            XmlLoadingPanel.self.finishLoading();
         });
      }

      public void duplicateDefinition (int id) {
         XmlLoadingPanel.self.startLoading();
         ItemDefinition definition = ItemDefinitionManager.self.getDefinition(id);
         definition.creatorUserId = MasterToolAccountManager.self.currentAccountID;

         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            DB_Main.createNewItemDefinition(definition);

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               load();
            });
         });
      }

      public void editDefinition (int id) {
         selectedItemDefinition = ItemDefinitionManager.self.getDefinition(id);
         ItemDefinitionEditPanel.self.show();
      }

      public void newDefinition () {
         selectedItemDefinition = new ItemDefinition { id = -1, creatorUserId = MasterToolAccountManager.self.currentAccountID };
         ItemDefinitionEditPanel.self.show();
      }

      public void deleteDefinition (int id) {
         XmlLoadingPanel.self.startLoading();
         ItemDefinition definition = ItemDefinitionManager.self.getDefinition(id);

         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            DB_Main.deleteItemDefinition(definition.id);

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               load();
            });
         });
      }

      public void saveSelectedDefinition () {
         if (selectedItemDefinition == null) {
            D.error("Selected item definition should not be null");
            return;
         }

         XmlLoadingPanel.self.startLoading();
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            if (selectedItemDefinition.id == -1) {
               DB_Main.createNewItemDefinition(selectedItemDefinition);
            } else {
               DB_Main.updateItemDefinition(selectedItemDefinition);
            }

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               load();
            });
         });
      }

      public void mainMenu () {
         UnityEngine.SceneManagement.SceneManager.LoadScene(MasterToolScene.masterScene);
      }

      #region Private Variables

      #endregion
   }
}