using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ShortcutBox : MonoBehaviour {
   #region Public Variables

   // The required tutorial step before this box becomes available (if any)
   public int requiredTutorialStep = 0;

   // The item number associated with this shortcut
   public int itemNumber = 0;

   #endregion

   private void Start () {
      // Look up components
      _button = GetComponent<Button>();
      _containerImage = GetComponent<Image>();
   }

   private void Update () {
      // Check what type of weapon the player has equipped
      int weaponType = 0;
      Weapon.ActionType actionType = Weapon.ActionType.None;
      if (Global.player is PlayerBodyEntity) {
         PlayerBodyEntity body = (PlayerBodyEntity) Global.player;
         weaponType = body.weaponManager.weaponType;
         actionType = body.weaponManager.actionType;
      }

      // Make the box highlighted if we've equipped the associated weapon
      _containerImage.color = Color.white;
      if ((itemNumber == 1 && actionType == Weapon.ActionType.PlantCrop) ||
         (itemNumber == 2 && actionType == Weapon.ActionType.WaterCrop) ||
         (itemNumber == 3 && actionType == Weapon.ActionType.HarvestCrop)) {
         _containerImage.color = Util.getColor(255, 160, 160);
      }
   }

   public void equipWeapon () {
      PlayerBodyEntity body = (PlayerBodyEntity) Global.player;
      body.Cmd_EquipWeapon(itemNumber);
   }

   #region Private Variables

   // Our associated Button
   protected Button _button;

   // Our container image
   protected Image _containerImage;

   #endregion
}
