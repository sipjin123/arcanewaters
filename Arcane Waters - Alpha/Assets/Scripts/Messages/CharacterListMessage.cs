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

   public CharacterListMessage (UserInfo[] userArray, Item[] armorArray, Item[] weaponArray, Item[] hatArray, string[] armorPalettes, int[] equipmentIds, int[] spriteIds) {
      this.userArray = userArray;
      this.armorArray = armorArray;
      this.weaponArray = weaponArray;
      this.armorPalettes = armorPalettes;
      this.equipmentIds = equipmentIds;
      this.spriteIds = spriteIds;
      this.hatArray = hatArray;
   }
}