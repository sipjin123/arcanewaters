﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using static EquipmentToolManager;

#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

public class EditorSQLManager
{
   public enum EditorToolType
   {
      None = 0,
      BattlerAbility = 1,
      Achievement = 2,
      Crafting = 3,
      Equipment = 4,
      LandMonster = 5,
      NPC = 6,
      PlayerClass = 7,
      PlayerFaction = 8,
      PlayerJob = 9,
      PlayerSpecialty = 10,
      SeaMonster = 11,
      Ship = 12,
      ShipAbility = 13,
      Shop = 14,
      Tutorial = 15,
      SoundEffects = 16,
      Crops = 17,
      Books = 18,
      Discoveries = 19,
      Background = 20,
      Equipment_Weapon = 21,
      Equipment_Armor = 22,
      Equipment_Helm = 23,
   }

   public static string getSQLTableByName (EditorToolType editorType, int subType = 0) {
      switch (editorType) {
         case EditorToolType.BattlerAbility:
            return "ability_xml_v2";
         case EditorToolType.ShipAbility:
            return "ship_ability_xml_v2";
         case EditorToolType.Shop:
            return "shop_xml_v2";
         case EditorToolType.Tutorial:
            return "tutorial_xml";
         case EditorToolType.Books:
            return "books_xml";
      }
      return "";
   }

   public static string getSQLTableByID (EditorToolType editorType, EquipmentType subType = 0) {
      switch (editorType) {
         case EditorToolType.Equipment:
            switch (subType) {
               case EquipmentType.Weapon:
                  return "equipment_weapon_xml_v3";
               case EquipmentType.Armor:
                  return "equipment_armor_xml_v3";
               case EquipmentType.Helm:
                  return "equipment_helm_xml_v2";
               default:
                  return "";
            }
         case EditorToolType.Achievement:
            return "achievement_xml_v2";
         case EditorToolType.Crops:
            return "crops_xml_v1";
         case EditorToolType.Crafting:
            return "crafting_xml_v2";
         case EditorToolType.LandMonster:
            return "land_monster_xml_v3";
         case EditorToolType.NPC:
            return "npc_xml";
         case EditorToolType.PlayerClass:
            return "player_class_xml";
         case EditorToolType.PlayerFaction:
            return "player_faction_xml";
         case EditorToolType.PlayerJob:
            return "player_job_xml";
         case EditorToolType.PlayerSpecialty:
            return "player_specialty_xml";
         case EditorToolType.SeaMonster:
            return "sea_monster_xml_v2";
         case EditorToolType.Ship:
            return "ship_xml_v2";
      }
      return "";
   }
}

[Serializable]
public class SQLEntryNameClass
{
   // Unique name of the entry that is declared in the sql database
   public string dataName;

   // The owner of the entry
   public int ownerID;

   public SQLEntryNameClass () { }

#if IS_SERVER_BUILD

   public SQLEntryNameClass (MySqlDataReader dataReader) {
      this.dataName = DataUtil.getString(dataReader, "xml_name");
      this.ownerID = DataUtil.getInt(dataReader, "creator_userID");
   }

#endif
}

[Serializable]
public class SQLEntryIDClass
{
   // Unique iterative id autogenerated in the sql table
   public int xmlID;

   // The owner of the entry
   public int ownerID;

   public SQLEntryIDClass () { }

#if IS_SERVER_BUILD

   public SQLEntryIDClass (MySqlDataReader dataReader) {
      this.xmlID = DataUtil.getInt(dataReader, "xml_id");
      this.ownerID = DataUtil.getInt(dataReader, "creator_userID");
   }

#endif
}

[Serializable]
public class XMLPair
{
   // Unique iterative id autogenerated in the sql table
   public int xmlId;

   // The id of the data creator
   public int xmlOwnerId;

   // The xml content 
   public string rawXmlData;

   // Determines if this data is enabled
   public bool isEnabled;
}