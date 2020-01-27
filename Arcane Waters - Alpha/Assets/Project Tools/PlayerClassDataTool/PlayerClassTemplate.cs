using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using static EditorSQLManager;
using System;

public class PlayerClassTemplate : MonoBehaviour {
   #region Public Variables

   // Name of the class
   public Text nameText;

   // Index of the class 
   public Text indexText;

   // Button for showing the panel in charge of editing the class
   public Button editButton;

   // Button for deleting a class
   public Button deleteButton;

   // Icon of the class
   public Image itemIcon;

   // Button for duplicating this template
   public Button duplicateButton;

   // Set up what type of editor tool template this script is: (Class, Faction, Specialty, Job)
   public EditorToolType editortoolType;

   #endregion

   private void OnEnable () {
      if (!MasterToolAccountManager.canAlterData()) {
         deleteButton.gameObject.SetActive(false);
         duplicateButton.gameObject.SetActive(false);
      }

      switch (editortoolType) {
         case EditorToolType.PlayerClass:
            int typeIndex = (int) Enum.Parse(typeof(Class.Type), indexText.text);
            if (typeIndex != 0 && !PlayerClassTool.self.didUserCreateData((int) typeIndex)) {
               deleteButton.gameObject.SetActive(false);
               editButton.gameObject.SetActive(false);
            }
            break;
         case EditorToolType.PlayerFaction:
            typeIndex = (int) Enum.Parse(typeof(Faction.Type), indexText.text);
            if (typeIndex != 0 && !PlayerFactionToolManager.self.didUserCreateData((int) typeIndex)) {
               deleteButton.gameObject.SetActive(false);
               editButton.gameObject.SetActive(false);
            }
            break;
         case EditorToolType.PlayerSpecialty:
            typeIndex = (int) Enum.Parse(typeof(Specialty.Type), indexText.text);
            if (typeIndex != 0 && !PlayerSpecialtyToolManager.self.didUserCreateData((int) typeIndex)) {
               deleteButton.gameObject.SetActive(false);
               editButton.gameObject.SetActive(false);
            }
            break;
         case EditorToolType.PlayerJob:
            typeIndex = (int) Enum.Parse(typeof(Jobs.Type), indexText.text);
            if (typeIndex != 0 && !PlayerJobToolManager.self.didUserCreateData((int) typeIndex)) {
               deleteButton.gameObject.SetActive(false);
               editButton.gameObject.SetActive(false);
            }
            break;
      }
   }

   #region Private Variables

   #endregion
}
