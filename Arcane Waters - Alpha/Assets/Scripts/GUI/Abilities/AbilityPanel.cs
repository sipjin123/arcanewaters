using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;
using System.Linq;

public class AbilityPanel : Panel, IPointerClickHandler {
   #region Public Variables

   // Row initialization for all abilities
   public AbilitySelectionTemplate abilityTemplate;
   public GameObject abilityParent;
   public List<AbilitySelectionTemplate> abilityTemplateList;

   // Row initialization for equipped abilities
   public AbilitySelectionTemplate equippedAbilityTemplate;
   public GameObject equippedAbilityParent;
   public List<AbilitySelectionTemplate> equippedAbilityTemplateList;

   // Skill equip and unequip
   public Button equipButton, unequipButton, unequipAllButton;

   // Cached abilities
   public AbilitySQLData currentInventoryAbility, currentEquippedInventoryAbility;

   // Determines how many abilities have already been equipped 
   int equippedSkillCount = 0;

   // The list of ability slots available
   public List<AbilitySlot> abilityslotList;

   // The maximum skills that can be equipped in combat
   public static int MAX_EQUIP_SKILL = 5;

   #endregion

   public void receiveDataFromServer (AbilitySQLData[] abilityLibrary, List<AbilitySQLData> equippedAbilitList) {
      abilityslotList = new List<AbilitySlot>();
      abilityslotList.Add(new AbilitySlot { abilitySlotID = 0, abilityID = 0 });
      abilityslotList.Add(new AbilitySlot { abilitySlotID = 1, abilityID = 0 });
      abilityslotList.Add(new AbilitySlot { abilitySlotID = 2, abilityID = 0 });
      abilityslotList.Add(new AbilitySlot { abilitySlotID = 3, abilityID = 0 });
      abilityslotList.Add(new AbilitySlot { abilitySlotID = 4, abilityID = 0 });

      equipButton.onClick.RemoveAllListeners();
      unequipButton.onClick.RemoveAllListeners();
      unequipAllButton.onClick.RemoveAllListeners();

      equipButton.onClick.AddListener(() => {
         equipAbility(currentInventoryAbility);
      });

      unequipButton.onClick.AddListener(() => {
         unequipAbility(currentEquippedInventoryAbility);
      });

      unequipAllButton.onClick.AddListener(() => {
         foreach (AbilitySQLData ability in equippedAbilitList) {
            unequipAbility(ability);
         }
      });

      equippedSkillCount = 0;
      abilityParent.DestroyChildren();
      abilityTemplateList = new List<AbilitySelectionTemplate>();
      foreach (AbilitySQLData ability in abilityLibrary) {
         AbilitySelectionTemplate template = Instantiate(abilityTemplate.gameObject, abilityParent.transform).GetComponent<AbilitySelectionTemplate>();
         BasicAbilityData abilityData = AbilityManager.getAbility(ability.abilityID, AbilityType.Standard);
         bool isEquipped = equippedAbilitList.Exists(_ => _.abilityID == ability.abilityID);

         template.abilityData = abilityData;
         template.abilitySQLData = ability;
         template.skillName.text = abilityData.itemName;
         template.skillIcon.sprite = ImageManager.getSprite(abilityData.itemIconPath);
         template.highlightObj.SetActive(false);

         if (isEquipped) {
            template.gameObject.SetActive(false);
         }

         template.selectButton.onClick.AddListener(() => {
            currentInventoryAbility = ability;
            currentEquippedInventoryAbility = null;
            highlightAbility(template);
         });
         abilityTemplateList.Add(template);
      }
      setupEquippedAbilities(equippedAbilitList);
   }

   private void setupEquippedAbilities (List<AbilitySQLData> equippedAbilitList) {
      equippedAbilitList = equippedAbilitList.OrderBy(_ => _.equipSlotIndex).ToList();
      equippedAbilityParent.DestroyChildren();
      equippedAbilityTemplateList = new List<AbilitySelectionTemplate>();

      int skillCounter = 0;
      foreach (AbilitySQLData ability in equippedAbilitList) {
         AbilitySelectionTemplate template = Instantiate(equippedAbilityTemplate.gameObject, equippedAbilityParent.transform).GetComponent<AbilitySelectionTemplate>();
         BasicAbilityData abilityData = AbilityManager.getAbility(ability.abilityID, AbilityType.Standard);
         bool isEquipped = equippedAbilitList.Exists(_ => _.abilityID == ability.abilityID);

         template.abilityData = abilityData;
         template.abilitySQLData = ability;
         template.skillName.text = abilityData.itemName;
         template.skillIcon.sprite = ImageManager.getSprite(abilityData.itemIconPath);
         template.skillSlot.text = isEquipped ? equippedAbilitList.Find(_ => _.abilityID == ability.abilityID).equipSlotIndex.ToString() : "";

         if (template.skillSlot.text != "") {
            abilityslotList.Find(_ => _.abilitySlotID == int.Parse(template.skillSlot.text)).abilityID = ability.abilityID;
         }

         template.selectButton.onClick.AddListener(() => {
            highlightAbility(template);
            currentInventoryAbility = null;
            currentEquippedInventoryAbility = ability;
         });
         template.highlightObj.SetActive(false);
         equippedAbilityTemplateList.Add(template);
         skillCounter++;
      }

      // Fill out the blank skills
      if (skillCounter < MAX_EQUIP_SKILL) {
         for (int i = 0; i < MAX_EQUIP_SKILL - skillCounter; i++) {
            AbilitySelectionTemplate template = Instantiate(equippedAbilityTemplate.gameObject, equippedAbilityParent.transform).GetComponent<AbilitySelectionTemplate>();
            template.skillName.text = "Unassigned";
         }
      }
   }

   private void equipAbility (AbilitySQLData ability) {
      if (ability == null) {
         return;
      }

      if (equippedSkillCount < MAX_EQUIP_SKILL) {
         foreach (var temp in abilityslotList) {
            if (temp.abilityID == 0) {
               ability.equipSlotIndex = temp.abilitySlotID;
               Global.player.rpc.Cmd_UpdateAbility(ability.abilityID, ability.equipSlotIndex);
               break;
            }
         }
      }
   }

   private void unequipAbility (AbilitySQLData ability) {
      if (ability == null) {
         return;
      }
      ability.equipSlotIndex = -1;

      Global.player.rpc.Cmd_UpdateAbility(ability.abilityID, ability.equipSlotIndex);
   }

   private void unequipAbility (List<AbilitySQLData> abilities) {
      if (abilities == null) {
         return;
      }
      foreach (AbilitySQLData ability in abilities) {
         ability.equipSlotIndex = -1;
      }
      Global.player.rpc.Cmd_UpdateAbilities(abilities.ToArray());
   }

   private void highlightAbility (AbilitySelectionTemplate selectedTemplate) {
      foreach (AbilitySelectionTemplate template in abilityTemplateList) {
         template.highlightObj.SetActive(false);
      }
      foreach (AbilitySelectionTemplate template in equippedAbilityTemplateList) {
         template.highlightObj.SetActive(false);
      }
      selectedTemplate.highlightObj.SetActive(true);
   }

   public void OnPointerClick (PointerEventData eventData) {
   }

   #region Private Variables

   #endregion
}
