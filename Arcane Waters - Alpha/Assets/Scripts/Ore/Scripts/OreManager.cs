using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class OreManager : NetworkBehaviour
{
   void Awake () {
      _player = GetComponent<NetEntity>();
   }

   private void Start () {
      // Fetches ore info from the server
      if (_player.isServer == false && _player.isLocalPlayer) {
         if (AreaManager.self.getArea(_player.areaType).GetComponent<OreArea>() != null) {
            this.Cmd_GetOreArea((int) _player.areaType);
         }
      }
   }

   public void initializeOreData() {
      // Ore setup
      if (_player.isLocalPlayer && _player.isServer) {
         List<Area> areaList = AreaManager.self.getAreas();
         for (int i = 0; i < areaList.Count; i++) {
            if (areaList[i].GetComponent<OreArea>() != null) {
               areaList[i].GetComponent<OreArea>().initOreArea();
               this.initializeIndividualOre((int) areaList[i].areaType);
            }
         }
      }
   }

   [Command]
   public void Cmd_GetOreArea (int areaID) {
      getOreForArea(areaID);
   }

   [Server]
   private void initializeIndividualOre (int areaID) {
      Area areas = AreaManager.self.getArea((Area.Type) areaID);
      OreArea oreArea = areas.GetComponent<OreArea>();
      int oreSpawnCount = oreArea.oreList.Count;

      List<Transform> spawnPoints = oreArea.spawnPointList;
      List<OreArea.SpawnPointIndex> positionList = oreArea.getPotentialSpawnPoints(oreSpawnCount);
      List<OreInfo> newOreList = new List<OreInfo>();
      List<int> spawnindex = new List<int>();
      Area.Type areaType = (Area.Type) areaID;

      // Create new data for each ore
      for (int i = 0; i < oreSpawnCount; i++) {
         // Data setup for new Ore Info
         string oreID = ((int) areaType) + "" + i;
         int oreIndex = i;
         int oreIntID = int.Parse(oreID);
         string oreTypeString = (oreArea.oreList[i].oreData.oreType).ToString();
         OreInfo createdInfo = new OreInfo(oreIntID, oreTypeString, oreTypeString, areaType.ToString(), positionList[i].coordinates.x, positionList[i].coordinates.y, true, oreIndex);
         newOreList.Add(createdInfo);
         spawnindex.Add(positionList[i].index);
      }

      int newOreListCount = newOreList.Count;
      for (int i = 0; i < newOreListCount; i++) {
         _player.rpc.Target_ReceiveOreInfo(_player.connectionToClient, newOreList[i], spawnindex[i]);
      }
   }

   [Server]
   protected void getOreForArea (int areaID) {
      // Data fetch for the ore in a specific area
      Area areas = AreaManager.self.getArea((Area.Type) areaID);
      OreArea oreArea = areas.GetComponent<OreArea>();

      // Ore info extraction
      List<OreObj> oreObjectList = oreArea.oreList;
      List<OreInfo> newOreInfo = new List<OreInfo>();
      List<int> spawnindex = new List<int>();

      // Register the current ore info of the server into a list
      for (int i = 0; i < oreObjectList.Count; i++) {
         OreInfo createdInfo = new OreInfo();
         createdInfo.position = oreObjectList[i].transform.localPosition;
         createdInfo.oreIndex = i;
         createdInfo.isEnabled = true;
         createdInfo.oreType = oreObjectList[i].oreData.oreType;
         createdInfo.oreName = oreObjectList[i].oreData.oreType + "_" + i;
         createdInfo.areaType = (Area.Type) areaID;

         newOreInfo.Add(createdInfo);
         spawnindex.Add(oreObjectList[i].oreSpawnID);
      }

      int newOreListCount = newOreInfo.Count;
      for (int i = 0; i < newOreListCount; i++) {
         _player.rpc.Target_ReceiveOreInfo(_player.connectionToClient, newOreInfo[i], spawnindex[i]);
      }
   }

   #region Private Variables

   // Our associated Player object
   protected NetEntity _player;

   #endregion
}