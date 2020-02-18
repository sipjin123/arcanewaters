using System.Runtime.InteropServices;
using UnityEngine;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class CharacterEquipmentMessage : MessageBase
{
   #region Public Variables

   // The network instance id associated with this message
   public uint netId;

   // Gender type of the armor
   public Gender.Type gender;

   // The armor ID 
   public int armorID;

   // The material to use for the armor
   public MaterialType materialType;

   #endregion

   public CharacterEquipmentMessage () { }

   public CharacterEquipmentMessage (uint netId) {
      this.netId = netId;
   }

   public CharacterEquipmentMessage (uint netId, int materialtype, int gender, int armorID) {
      this.netId = netId;
      this.materialType = (MaterialType) materialtype;
      this.gender = (Gender.Type) gender;
      this.armorID = armorID;
   }
}