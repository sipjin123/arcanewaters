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
   public Item[] armorArray;
   public Item[] weaponArray;
   public Item[] hatArray;

   // We have to deal with these separately because of a Unity bug
   public string[] armorPalettes1;
   public string[] armorPalettes2;

   // The equipment xml Id's of the starting armor
   public int[] equipmentIds;

   // The sprite Id's of the starting armor
   public int[] spriteIds;

   #endregion

   public CharacterListMessage () { }

   public CharacterListMessage (uint netId) {
      this.netId = netId;
   }

   public CharacterListMessage (uint netId, UserInfo[] userArray, Item[] armorArray, Item[] weaponArray, Item[] hatArray, string[] armorPalettes1, string[] armorPalettes2, int[] equipmentIds, int[] spriteIds) {
      this.netId = netId;
      this.userArray = userArray;
      this.armorArray = armorArray;
      this.weaponArray = weaponArray;
      this.armorPalettes1 = armorPalettes1;
      this.armorPalettes2 = armorPalettes2;
      this.equipmentIds = equipmentIds;
      this.spriteIds = spriteIds;
      this.hatArray = hatArray;
   }
}