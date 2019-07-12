using System.Runtime.InteropServices;
using UnityEngine;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class EquipMessage : MessageBase {
   #region Public Variables

   // The network instance id associated with this message
   public uint netId;

   // The new armor id
   public int newArmorId;

   // The new weapon id
   public int newWeaponId;

   #endregion

   public EquipMessage () { }

   public EquipMessage (uint netId) {
      this.netId = netId;
   }

   public EquipMessage (uint netId, int newArmorId, int newWeaponId) {
      this.netId = netId;
      this.newArmorId = newArmorId;
      this.newWeaponId = newWeaponId;
   }
}