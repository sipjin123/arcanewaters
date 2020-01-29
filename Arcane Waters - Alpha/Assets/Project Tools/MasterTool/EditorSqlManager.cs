﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

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
      Tutorial = 15
   }

   public static string getSQLTableByName (EditorToolType editorType, int subType = 0) {
      switch (editorType) {
         case EditorToolType.BattlerAbility:
            return "ability_xml";
         case EditorToolType.Achievement:
            return "achievement_xml";
         case EditorToolType.Crafting:
            return "crafting_xml";
         case EditorToolType.ShipAbility:
            return "ship_ability_xml";
         case EditorToolType.Shop:
            return "shop_xml";
         case EditorToolType.Tutorial:
            return "tutorial_xml";
      }
      return "";
   }

   public static string getSQLTableByID (EditorToolType editorType, int subType = 0) {
      switch (editorType) {
         case EditorToolType.Equipment:
            switch (subType) {
               case 1:
                  return "equipment_weapon_xml";
               case 2:
                  return "equipment_armor_xml";
               case 3:
                  return "equipment_helm_xml";
               default:
                  return "";
            }
         case EditorToolType.LandMonster:
            return "land_monster_xml";
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
            return "sea_monster_xml";
         case EditorToolType.Ship:
            return "ship_xml";
      }
      return "";
   }
}

[Serializable]
public class SQLEntryNameClass
{
   public string dataName;

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
   public int dataID;

   public int ownerID;

   public SQLEntryIDClass () { }

#if IS_SERVER_BUILD

   public SQLEntryIDClass (MySqlDataReader dataReader) {
      this.dataID = DataUtil.getInt(dataReader, "xml_id");
      this.ownerID = DataUtil.getInt(dataReader, "creator_userID");
   }

#endif
}

[Serializable]
public class XMLPair
{
   // Unique iterative id autogenerated in the sql table
   public int xml_id;

   // The xml content 
   public string raw_xml_data;

   // Determines if this data is enabled
   public bool is_enabled;
}