using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Mirror;

public class GearManager : EquipmentManager
{
   #region Public Variables

   // The unique database inventory id of the gear
   [SyncVar]
   public int equippedNecklaceDbId, equippedRingDbId, equippedTrinketDbId;

   // The Sprite Id
   [SyncVar]
   public int necklaceSpriteId, ringSpriteId, trinketSpriteId;

   // Equipment xml Id
   [SyncVar]
   public int equippedNecklaceXmlId, equippedRingXmlId, equippedTrinkedXmlId;

   // Gear colors
   [SyncVar]
   public string necklacePalettes, ringPalettes, trinkedPalettes;

   // The current gear data
   public RingStatData cachedRingData;
   public TrinketStatData cachedTrinketData;
   public NecklaceStatData cachedNecklaceData;

   #endregion

   #region Booleans and getters

   public bool hasRing () {
      return (ringSpriteId != 0);
   }

   public bool hasNecklace () {
      return (necklaceSpriteId != 0);
   }
   
   public bool hasTrinket () {
      return (trinketSpriteId != 0);
   }

   public Ring getRing () {
      if (cachedRingData != null) {
         return RingStatData.translateDataToRing(cachedRingData);
      }

      return new Ring();
   }

   public Necklace getNecklace () {
      if (cachedNecklaceData != null) {
         return NecklaceStatData.translateDataToNecklace(cachedNecklaceData);
      }

      return new Necklace();
   }

   public Trinket getTrinket () {
      if (cachedTrinketData != null) {
         return TrinketStatData.translateDataToTrinket(cachedTrinketData);
      }

      return new Trinket();
   }

   #endregion

   [TargetRpc]
   public void Target_ReceiveEquipRing (int newRingId, int newRingSqlId, string rawData, bool equipOnStart) {
      RingStatData ringData = Util.xmlLoad<RingStatData>(rawData);
      cachedRingData = ringData;

      // Play a sound
      if (!equipOnStart) {
         SoundEffectManager.self.playFmodSfx(SoundEffectManager.EQUIP);
      }

      D.adminLog("Equipped Ring SQL: {" + ringData.sqlId +
         "} Name: {" + ringData.equipmentName +
         "} Class: {" + ringData.ringType + "}", D.ADMIN_LOG_TYPE.Equipment);

      Global.getUserObjects().ring = new Ring {
         id = newRingId,
         category = Item.Category.Ring,
         itemTypeId = newRingSqlId
      };
   }

   [TargetRpc]
   public void Target_ReceiveEquipNecklace (int newNecklaceId, int newNecklaceSqlId, string rawData, bool equipOnStart) {
      NecklaceStatData necklaceData = Util.xmlLoad<NecklaceStatData>(rawData);
      cachedNecklaceData = necklaceData;

      // Play a sound
      if (!equipOnStart) {
         SoundEffectManager.self.playFmodSfx(SoundEffectManager.EQUIP);
      }

      D.adminLog("Equipped Necklace SQL: {" + necklaceData.sqlId +
         "} Name: {" + necklaceData.equipmentName +
         "} Class: {" + necklaceData.necklaceType + "}", D.ADMIN_LOG_TYPE.Equipment);

      Global.getUserObjects().necklace = new Necklace {
         id = newNecklaceId,
         category = Item.Category.Necklace,
         itemTypeId = newNecklaceSqlId
      };
   }

   [TargetRpc]
   public void Target_ReceiveEquipTrinket (int newTrinketId, int newTrinketSqlId, string rawData, bool equipOnStart) {
      TrinketStatData trinketData = Util.xmlLoad<TrinketStatData>(rawData);
      cachedTrinketData = trinketData;

      // Play a sound
      if (!equipOnStart) {
         SoundEffectManager.self.playFmodSfx(SoundEffectManager.EQUIP);
      }

      D.adminLog("Equipped Trinket SQL: {" + trinketData.sqlId +
         "} Name: {" + trinketData.equipmentName +
         "} Class: {" + trinketData.trinketType + "}", D.ADMIN_LOG_TYPE.Equipment);

      Global.getUserObjects().trinket = new Trinket {
         id = newTrinketId,
         category = Item.Category.Trinket,
         itemTypeId = newTrinketSqlId
      };
   }

   [ClientRpc]
   public void Rpc_EquipRing (string rawEquipData) {
      RingStatData newData = Util.xmlLoad<RingStatData>(rawEquipData);
      cachedRingData = newData;
   }

   [ClientRpc]
   public void Rpc_EquipNecklace (string rawEquipData) {
      NecklaceStatData newData = Util.xmlLoad<NecklaceStatData>(rawEquipData);
      cachedNecklaceData = newData;
   }

   [ClientRpc]
   public void Rpc_EquipTrinket (string rawEquipData) {
      TrinketStatData newData = Util.xmlLoad<TrinketStatData>(rawEquipData);
      cachedTrinketData = newData;
   }

   [Server]
   public void updateRingSyncVars (int ringXmlId, int newDbId, bool equipOnStart = false) {
      RingStatData ringData = EquipmentXMLManager.self.getRingData(ringXmlId);
      if (ringData == null) {
         if (ringXmlId != 0) {
            D.debug("Ring data is null for {" + ringXmlId + "}");
         }
         ringData = RingStatData.getDefaultData();
      }
      cachedRingData = ringData;

      // Assign the gear ID
      this.equippedRingXmlId = ringXmlId;
      this.equippedRingDbId = newDbId;
      this.ringSpriteId = ringData.ringType;

      Target_ReceiveEquipRing(newDbId, ringXmlId, RingStatData.serializeRingStatData(ringData), equipOnStart);
      Rpc_EquipRing(RingStatData.serializeRingStatData(ringData));
   }

   [Server]
   public void updateNecklaceSyncVars (int necklaceXmlId, int newDbId, bool equipOnStart = false) {
      NecklaceStatData necklaceData = EquipmentXMLManager.self.getNecklaceData(necklaceXmlId);
      if (necklaceData == null) {
         if (necklaceXmlId != 0) {
            D.debug("Necklace data is null for {" + necklaceXmlId + "}");
         }
         necklaceData = NecklaceStatData.getDefaultData();
      }
      cachedNecklaceData = necklaceData;

      // Assign the gear ID
      this.equippedNecklaceXmlId = necklaceXmlId;
      this.equippedNecklaceDbId = newDbId;
      this.necklaceSpriteId = necklaceData.necklaceType;

      Target_ReceiveEquipNecklace(newDbId, necklaceXmlId, NecklaceStatData.serializeNecklaceStatData(necklaceData), equipOnStart);
      Rpc_EquipNecklace(NecklaceStatData.serializeNecklaceStatData(necklaceData));
   }

   [Server]
   public void updateTrinketSyncVars (int trinketXmlId, int newDbId, bool equipOnStart = false) {
      TrinketStatData trinketData = EquipmentXMLManager.self.getTrinketData(trinketXmlId);
      if (trinketData == null) {
         if (trinketXmlId != 0) {
            D.debug("Trinket data is null for {" + trinketXmlId + "}");
         }
         trinketData = TrinketStatData.getDefaultData();
      }
      cachedTrinketData = trinketData;

      // Assign the gear ID
      this.equippedTrinkedXmlId = trinketXmlId;
      this.equippedTrinketDbId = newDbId;
      this.trinketSpriteId = trinketData.trinketType;

      Target_ReceiveEquipTrinket(newDbId, trinketXmlId, TrinketStatData.serializeTrinketStatData(trinketData), equipOnStart);
      Rpc_EquipTrinket(TrinketStatData.serializeTrinketStatData(trinketData));
   }

   #region Private Variables

   #endregion
}
