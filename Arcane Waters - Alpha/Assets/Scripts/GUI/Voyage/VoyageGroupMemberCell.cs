using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using ProceduralMap;
using Mirror;
using UnityEngine.EventSystems;

public class VoyageGroupMemberCell : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
   #region Public Variables

   // The character portrait
   public CharacterPortrait characterPortrait;

   // The frame image
   public Image frameImage;

   // The hp bar
   public Image hpBar;

   // The colors of the hp bar
   public Gradient hpBarGradient;

   // The tooltip container
   public GameObject tooltipBox;

   // The name of the player
   public Text playerNameText;

   // The level of the player
   public Text playerLevelText;

   #endregion

   public void Awake () {
      // Disable the hp bar
      hpBar.enabled = false;

      // Hide the tooltip
      tooltipBox.SetActive(false);
   }

   public void Start () {
      // Regularly update the portrait if the user is locally visible
      InvokeRepeating(nameof(updatePortrait), Random.Range(0f, 2f), 3f);
   }

   public void setCellForGroupMember (VoyageGroupMemberCellInfo cellInfo) {
      _userId = cellInfo.userId;

      Weapon weapon = WeaponStatData.translateDataToWeapon(WeaponStatData.getDefaultData());
      WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(cellInfo.weapon.itemTypeId);
      if (weaponData != null) {
         weapon = WeaponStatData.translateDataToWeapon(weaponData);
         weapon.id = cellInfo.weapon.id;
         weapon.paletteNames = cellInfo.weapon.paletteNames;
      }

      Armor armor = ArmorStatData.translateDataToArmor(ArmorStatData.getDefaultData());
      ArmorStatData armorData = EquipmentXMLManager.self.getArmorDataBySqlId(cellInfo.armor.itemTypeId);
      if (armorData != null) {
         armor = ArmorStatData.translateDataToArmor(armorData);
         armor.id = cellInfo.armor.id;
         armor.paletteNames = cellInfo.armor.paletteNames;
      }

      Hat hat = HatStatData.translateDataToHat(HatStatData.getDefaultData());
      HatStatData hatData = EquipmentXMLManager.self.getHatData(cellInfo.hat.itemTypeId);
      if (hatData != null) {
         hat = HatStatData.translateDataToHat(hatData);
         hat.id = cellInfo.hat.id;
         hat.paletteNames = cellInfo.hat.paletteNames;
      }

      characterPortrait.updateLayers(cellInfo.gender, cellInfo.bodyType, cellInfo.eyesType, cellInfo.hairType, cellInfo.eyesPalettes, cellInfo.hairPalettes, armor, weapon, hat);
      playerNameText.text = cellInfo.userName;
      playerLevelText.text = "LvL " + LevelUtil.levelForXp(cellInfo.userXP).ToString();
   }

   public void Update () {
      if (Global.player == null || !Global.player.isLocalPlayer || !VoyageGroupManager.isInGroup(Global.player)) {
         return;
      }

      // Allow right clicking to bring up the context menu, only if no panel is opened
      if (InputManager.isLeftClickKeyPressed() && _mouseOver && !PanelManager.self.hasPanelInLinkedList()) {
         PanelManager.self.contextMenuPanel.showDefaultMenuForUser(_userId, playerNameText.text, true);
      }

      // Try to find the entity of the displayed user
      NetEntity entity = EntityManager.self.getEntity(_userId);
      if (entity == null) {
         hpBar.enabled = false;
         return;
      }

      // Update the portrait background
      characterPortrait.updateBackground(entity);

      int currentHP = entity.currentHealth;
      int maxHP = entity.maxHealth;

      // If the user is in battle, get the battler hp values
      if (entity.isInBattle()) {
         Battler battler = BattleManager.self.getBattler(_userId);
         if (battler != null) {
            currentHP = battler.displayedHealth;
            maxHP = battler.getStartingHealth();
         }
      }

      // Update the hp bar
      hpBar.enabled = true;
      hpBar.fillAmount = (float) currentHP / maxHP;
      hpBar.color = hpBarGradient.Evaluate(hpBar.fillAmount);
   }

   private void updatePortrait () {
      // Try to find the entity of the displayed user
      NetEntity entity = EntityManager.self.getEntity(_userId);
      if (entity == null) {
         characterPortrait.updateBackground(null);
         return;
      }

      // Update the whole portrait
      if (entity is PlayerBodyEntity) {
         characterPortrait.updateLayers(entity);
      } else {
         characterPortrait.updateBackground(entity);
      }

      playerNameText.text = entity.entityName;
      playerLevelText.text = "LvL " + LevelUtil.levelForXp(entity.XP).ToString();
   }

   public void OnPointerEnter (PointerEventData eventData) {
      tooltipBox.SetActive(true);
      _mouseOver = true;
   }

   public void OnPointerExit (PointerEventData eventData) {
      tooltipBox.SetActive(false);
      _mouseOver = false;
   }

   public int getUserId () {
      return _userId;
   }

   public bool isMouseOver () {
      return _mouseOver;
   }

   #region Private Variables

   // The id of the displayed user
   private int _userId = -1;

   // Gets set to true when the mouse is hovering the cell
   private bool _mouseOver = false;

   #endregion
}