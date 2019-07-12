using System.Runtime.InteropServices;
using UnityEngine;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class CharacterListMessage : MessageBase {
   #region Public Variables

   // The network instance id associated with this message
   public uint netId;

   // The character info
   public UserInfo[] userArray;
   public Armor[] armorArray;
   public Weapon[] weaponArray;

   // We have to deal with these separately because of a Unity bug
   public int[] armorColors1;
   public int[] armorColors2;

   #endregion

   public CharacterListMessage () { }

   public CharacterListMessage (uint netId) {
      this.netId = netId;
   }

   public CharacterListMessage (uint netId, UserInfo[] userArray, Armor[] armorArray, Weapon[] weaponArray, int[] armorColors1, int[] armorColors2) {
      this.netId = netId;
      this.userArray = userArray;
      this.armorArray = armorArray;
      this.weaponArray = weaponArray;
      this.armorColors1 = armorColors1;
      this.armorColors2 = armorColors2;
   }
}