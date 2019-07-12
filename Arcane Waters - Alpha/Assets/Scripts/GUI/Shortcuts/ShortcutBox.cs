using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ShortcutBox : MonoBehaviour {
   #region Public Variables

   // The required tutorial step before this box becomes available (if any)
   public Step requiredTutorialStep = Step.None;

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
      Weapon.Type weaponType = Weapon.Type.None;
      if (Global.player is PlayerBodyEntity) {
         PlayerBodyEntity body = (PlayerBodyEntity) Global.player;
         weaponType = body.weaponManager.weaponType;
      }

      // Make the box highlighted if we've equipped the associated weapon
      _containerImage.color = Color.white;
      if ((itemNumber == 1 && weaponType == Weapon.Type.Seeds) ||
         (itemNumber == 2 && weaponType == Weapon.Type.WateringPot) ||
         (itemNumber == 3 && weaponType == Weapon.Type.Pitchfork)) {
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
