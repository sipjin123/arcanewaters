using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using UnityEngine.Networking;
using Newtonsoft.Json;

public class WebToolsManager : GenericGameManager {
   #region Public Variables

   // Self
   public static WebToolsManager self;

   #endregion

   protected override void Awake () {
      base.Awake();
      self = this;
   }

   #region Server History

   public IEnumerator CO_GetServerHistory (int maxRows, DateTime startDate) {
      string branch = Util.getBranchType();
      string db;

      bool isOnline = false;

      if (branch == "Development") {
         db = "dev";
      } else {
         db = "prod";
      }

      // First, we check if the server is online
      UnityWebRequest wwwIsOnline = UnityWebRequest.Get(WebToolsUtil.IS_SERVER_ONLINE.Replace("{db}", db));
      wwwIsOnline.SetRequestHeader(WebToolsUtil.GAME_TOKEN_HEADER, WebToolsUtil.GAME_TOKEN);

      yield return wwwIsOnline.SendWebRequest();

      if (wwwIsOnline.responseCode == 200) {
         isOnline = wwwIsOnline.downloadHandler.text == "true";
      } else {
         D.error("Could not get server online status");
      }

      // Then we fetch the server history
      List<ServerHistoryInfo> serverHistoryList = new List<ServerHistoryInfo>();

      UnityWebRequest wwwServerHistory = UnityWebRequest.Get(WebToolsUtil.GET_SERVER_HISTORY.Replace("{db}", db) + string.Format("?maxRows={0}&startDate={1}", maxRows, startDate.ToString("o")));
      wwwServerHistory.SetRequestHeader(WebToolsUtil.GAME_TOKEN_HEADER, WebToolsUtil.GAME_TOKEN);

      yield return wwwServerHistory.SendWebRequest();

      if (wwwServerHistory.responseCode == 200) {
         serverHistoryList = JsonConvert.DeserializeObject<List<ServerHistoryInfo>>(wwwServerHistory.downloadHandler.text);
      } else {
         D.error("Could not get server history list");
      }

      // After getting the data, we update the panel!
      ServerStatusPanel.self.updatePanelWithServerHistory(isOnline, serverHistoryList);
   }

   #endregion

   #region Private Variables

   #endregion
}
