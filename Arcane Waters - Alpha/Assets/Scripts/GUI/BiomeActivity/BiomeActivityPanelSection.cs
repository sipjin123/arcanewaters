using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class BiomeActivityPanelSection : MonoBehaviour
{
   #region Public Variables

   // Load Blocker when data is fetching
   public GameObject loadBlocker;

   // The rows for all the biomes
   public BiomeActivityRow forestBiomeRow;
   public BiomeActivityRow desertBiomeRow;

   #endregion

   public void show () {
      gameObject.SetActive(true);
      setLoadBlocker(true);
      Global.player.rpc.Cmd_GetUserCountHavingVisitedBiomes();
   }

   public void receiveUserCountHavingVisitedBiomes (int forestUserCount) {
      forestBiomeRow.initializeRow(forestUserCount);
      desertBiomeRow.initializeRow(0);

      setLoadBlocker(false);
   }

   public void setLoadBlocker (bool isOn) {
      loadBlocker.SetActive(isOn);
   }

   public void hide () {
      gameObject.SetActive(false);
   }

   #region Private Variables

   #endregion
}
