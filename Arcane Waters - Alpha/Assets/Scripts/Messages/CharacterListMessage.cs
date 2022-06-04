using System.Runtime.InteropServices;
using UnityEngine;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class CharacterListMessage : NetworkMessage
{
   #region Public Variables

   // The character info
   public UserInfo[] userArray;
   public bool[] deletionStatusArray;
   public bool[] nameAvailabilityStatusArray;
   public Item[] armorArray;
   public Item[] weaponArray;
   public Item[] hatArray;

   // We have to deal with these separately because of a Unity bug
   public string[] armorPalettes;

   // The equipment xml Id's of the starting armor
   public int[] equipmentIds;

   // The sprite Id's of the starting armor
   public int[] spriteIds;

   #endregion

   public CharacterListMessage () { }

   public CharacterListMessage (UserInfo[] userArray, bool[] deletionStatusArray, bool[] nameAvailabilityStatusArray, Item[] armorArray, Item[] weaponArray, Item[] hatArray, string[] armorPalettes, int[] equipmentIds, int[] spriteIds) {
      this.userArray = userArray;
      this.deletionStatusArray = deletionStatusArray;
      this.nameAvailabilityStatusArray = nameAvailabilityStatusArray;
      this.armorArray = armorArray;
      this.weaponArray = weaponArray;
      this.armorPalettes = armorPalettes;
      this.equipmentIds = equipmentIds;
      this.spriteIds = spriteIds;
      this.hatArray = hatArray;
   }
}