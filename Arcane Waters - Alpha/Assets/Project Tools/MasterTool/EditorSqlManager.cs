﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using static EquipmentToolManager;
using System.Xml.Serialization;

#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

public class EditorSQLManager {
   public enum EditorToolType {
      None = 0,
      BattlerAbility = 1,
      Achievement = 2,
      Crafting = 3,
      Equipment = 4,
      LandMonster = 5,
      NPC = 6,
      SeaMonster = 11,
      Ship = 12,
      ShipAbility = 13,
      Shop = 14,
      SoundEffects = 16,
      Crops = 17,
      Books = 18,
      Discoveries = 19,
      Background = 20,
      Equipment_Weapon = 21,
      Equipment_Armor = 22,
      Equipment_Hat = 23,
      Perks = 25,
      Palette = 26,
      Treasure_Drops = 27,
      Quest = 28,
      ItemDefinitions = 29,
      Tool_Tip = 30,
      Projectiles = 31,
      Tutorial = 32,
      Map_Keys = 33,
      SFX = 34,
      Haircuts = 35,
      Gems = 36,
      ShipSkins = 37,
      Consumables = 38,
      Dyes = 39,
      LandPowerups = 40,
      QuestItems = 41,
      Equipment_Ring = 42,
      Equipment_Necklace = 43,
      Equipment_Trinket = 44
   }

   public static string getSqlTable (EditorToolType editorType) {
      string tableName = getSQLTableByName(editorType);
      if (tableName == "") {
         int subType = 0;
         switch (editorType) {
            case EditorToolType.Equipment_Weapon:
               editorType = EditorToolType.Equipment;
               subType = 1;
               break;
            case EditorToolType.Equipment_Armor:
               editorType = EditorToolType.Equipment;
               subType = 2;
               break;
            case EditorToolType.Equipment_Hat:
               editorType = EditorToolType.Equipment;
               subType = 3;
               break;
            case EditorToolType.Equipment_Ring:
               editorType = EditorToolType.Equipment;
               subType = 4;
               break;
            case EditorToolType.Equipment_Necklace:
               editorType = EditorToolType.Equipment;
               subType = 5;
               break;
            case EditorToolType.Equipment_Trinket:
               editorType = EditorToolType.Equipment;
               subType = 6;
               break;
         }
         tableName = getSQLTableByID(editorType, (EquipmentType) subType);
      }

      return tableName;
   }

   public static string getSQLTableByName (EditorToolType editorType, int subType = 0) {
      switch (editorType) {
         case EditorToolType.BattlerAbility:
            return "ability_xml_v2";
         case EditorToolType.ShipAbility:
            return "ship_ability_xml_v2";
         case EditorToolType.Shop:
            return "shop_xml_v2";
         case EditorToolType.Books:
            return "books_xml";
         case EditorToolType.Crops:
            return "crops_xml_v1";
         case EditorToolType.Background:
            return "background_xml_v2";
         case EditorToolType.Perks:
            return "perks_config_xml";
         case EditorToolType.ItemDefinitions:
            return XmlVersionManagerServer.ITEM_DEFINITIONS_TABLE;
      }
      return "";
   }

   public static string getSQLTableByID (EditorToolType editorType, EquipmentType subType = 0) {
      switch (editorType) {
         case EditorToolType.Equipment:
            switch (subType) {
               case EquipmentType.Weapon:
                  return XmlVersionManagerServer.WEAPON_TABLE;
               case EquipmentType.Armor:
                  return XmlVersionManagerServer.ARMOR_TABLE;
               case EquipmentType.Hat:
                  return XmlVersionManagerServer.HAT_TABLE;
               case EquipmentType.Ring:
                  return XmlVersionManagerServer.RING_TABLE;
               case EquipmentType.Necklace:
                  return XmlVersionManagerServer.NECKLACE_TABLE;
               case EquipmentType.Trinket:
                  return XmlVersionManagerServer.TRINKET_TABLE;
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
         case EditorToolType.LandPowerups:
            return "land_powerup_xml_v1";
         case EditorToolType.NPC:
            return "npc_xml_v2";
         case EditorToolType.SeaMonster:
            return "sea_monster_xml_v2";
         case EditorToolType.Ship:
            return "ship_xml_v2";
         case EditorToolType.Perks:
            return "perks_config_xml";
         case EditorToolType.Treasure_Drops:
            return "treasure_drops_xml_v2";
         case EditorToolType.Quest:
            return "quest_data_xml_v1";
         case EditorToolType.Palette:
            return "palette_recolors";
         case EditorToolType.Projectiles:
            return "projectiles_xml_v3";
         case EditorToolType.Tutorial:
            return "tutorial_xml_v1";
         case EditorToolType.ItemDefinitions:
            return XmlVersionManagerServer.ITEM_DEFINITIONS_TABLE;
         case EditorToolType.Haircuts:
            return XmlVersionManagerServer.HAIRCUTS_TABLE;
         case EditorToolType.Gems:
            return XmlVersionManagerServer.GEMS_TABLE;
         case EditorToolType.ShipSkins:
            return XmlVersionManagerServer.SHIP_SKINS_TABLE;
         case EditorToolType.Consumables:
            return XmlVersionManagerServer.CONSUMABLES_TABLE;
         case EditorToolType.Dyes:
            return XmlVersionManagerServer.DYES_TABLE;
      }
      return "";
   }
}

[Serializable]
public class SQLEntryNameClass {
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
public class SQLEntryIDClass {
   // Unique iterative id autogenerated in the sql table
   public int xmlID;

   // The owner of the entry
   public int ownerID;

   public SQLEntryIDClass () { }

#if IS_SERVER_BUILD

   public SQLEntryIDClass (MySqlDataReader dataReader, bool isUpdated) {
      if (isUpdated) {
         this.xmlID = DataUtil.getInt(dataReader, "xmlId");
         this.ownerID = DataUtil.getInt(dataReader, "creatorUserID");
      } else {
         this.xmlID = DataUtil.getInt(dataReader, "xml_id");
         this.ownerID = DataUtil.getInt(dataReader, "creator_userID");
      }
   }

#endif
}

[Serializable]
public class XMLPair {
   // Unique iterative id autogenerated in the sql table
   public int xmlId;

   // The id of the data creator
   public int xmlOwnerId;

   // The xml content 
   public string rawXmlData;

   // The xml name in the database
   [XmlIgnore]
   public string xmlName;

   // Tag classifying palette
   public string tag;

   // Determines if this data is enabled
   public bool isEnabled;
}