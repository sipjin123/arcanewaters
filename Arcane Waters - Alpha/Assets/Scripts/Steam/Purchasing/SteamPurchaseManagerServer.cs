using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Steamworks;
using Store;
using UnityEngine;

namespace Steam.Purchasing
{
   public class SteamPurchaseManagerServer : MonoBehaviour
   {
      #region Public Variables

      // Debug SteamID
      public ulong debugSteamId = 0;

      // Self
      public static SteamPurchaseManagerServer self;

      #endregion

      public void Awake () {
         self = this;
      }

      private bool isSandboxModeEnabled () {
         RemoteSetting rsetting = DB_Main.getRemoteSetting(RemoteSettingsManager.SettingNames.STEAM_PURCHASES_SANDBOX_MODE_ACTIVE);
         
         if (rsetting == null) {
            return true;
         }

         return rsetting.toBool();
      }

      public string getMicroTxnBaseUrl () {
         if (isSandboxModeEnabled()) {
            return STEAMWEBAPI_MICROTXN_SANDBOX;
         }

         return STEAMWEBAPI_MICROTXN;
      }

      public async Task<GetUserInfoResult> getUserInfo (ulong steamId) {
         if (string.IsNullOrEmpty(steamId.ToString())) {
            D.debug("Steam Purchasing Error! Invalid Steam Id: " + steamId.ToString());
            return null;
         }

         string query = "?";
         query += $"key={SteamStatics.STEAM_WEB_PUBLISHER_API_KEY}&";
         query += $"steamid={steamId}";

         HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Get, getMicroTxnBaseUrl() + STEAMWEBAPI_GET_USER_INFO + query);
         return await fetch<GetUserInfoResult>(httpRequest);
      }

      public async Task<InitTxnResult> initTxn (SteamPurchaseInfo purchase) {
         if (purchase == null) {
            D.debug("Steam Purchase Error! Purchase object is null");
            return null;
         }

         string steamUserId = purchase.steamId.ToString();

         if (string.IsNullOrEmpty(steamUserId)) {
            D.debug("Steam Purchasing Error! Invalid Steam Id: " + steamUserId);
            return null;
         }

         HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, getMicroTxnBaseUrl() + STEAMWEBAPI_INIT_TXN);

         List<KeyValuePair<string, string>> parameters = new List<KeyValuePair<string, string>>();
         parameters.Add(new KeyValuePair<string, string>("key", SteamStatics.STEAM_WEB_PUBLISHER_API_KEY));
         parameters.Add(new KeyValuePair<string, string>("orderid", purchase.orderId.ToString()));
         parameters.Add(new KeyValuePair<string, string>("steamid", steamUserId));
         parameters.Add(new KeyValuePair<string, string>("appid", purchase.appId.ToString()));
         parameters.Add(new KeyValuePair<string, string>("itemcount", purchase.items.Count.ToString()));
         parameters.Add(new KeyValuePair<string, string>("language", purchase.language));
         parameters.Add(new KeyValuePair<string, string>("currency", purchase.currency));

         int i = 0;

         foreach (SteamPurchaseItem item in purchase.items) {
            parameters.Add(new KeyValuePair<string, string>($"itemid[{i}]", item.itemId));
            parameters.Add(new KeyValuePair<string, string>($"qty[{i}]", item.quantity.ToString()));
            parameters.Add(new KeyValuePair<string, string>($"amount[{i}]", item.totalCost.ToString()));
            parameters.Add(new KeyValuePair<string, string>($"description[{i}]", item.description));
            i++;
         }

         httpRequest.Content = new FormUrlEncodedContent(parameters);
         return await fetch<InitTxnResult>(httpRequest);
      }

      public async Task<FinalizeTxnResult> finalizeTxn (ulong orderId, uint appId) {
         HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, getMicroTxnBaseUrl() + STEAMWEBAPI_FINALIZE_TXN);

         List<KeyValuePair<string, string>> parameters = new List<KeyValuePair<string, string>>();
         parameters.Add(new KeyValuePair<string, string>("key", SteamStatics.STEAM_WEB_PUBLISHER_API_KEY));
         parameters.Add(new KeyValuePair<string, string>("orderid", orderId.ToString()));
         parameters.Add(new KeyValuePair<string, string>("appid", appId.ToString()));

         httpRequest.Content = new FormUrlEncodedContent(parameters);
         return await fetch<FinalizeTxnResult>(httpRequest);
      }

      private async Task<T> fetch<T> (HttpRequestMessage request) {
         using (HttpClient httpClient = new HttpClient()) {
            HttpResponseMessage response = await httpClient.SendAsync(request);

            if (response != null) {
               string contentStr = await response.Content.ReadAsStringAsync();

               if (response.StatusCode != System.Net.HttpStatusCode.OK) {
                  D.error(contentStr);
               }

               JObject obj = JObject.Parse(contentStr);
               JToken r = obj["response"];
               T result = JsonConvert.DeserializeObject<T>(r.ToString());
               return result;
            }
         }
         return default;
      }

      #region Private Variables

      // The web API post command for altering achievements
      private const string STEAMWEBAPI_MICROTXN = "https://partner.steam-api.com/ISteamMicroTxn/";

      // The web API post command for making microtransactions sandbox
      private const string STEAMWEBAPI_MICROTXN_SANDBOX = "https://partner.steam-api.com/ISteamMicroTxnSandbox/";

      // Get User Info Endpoint
      private const string STEAMWEBAPI_GET_USER_INFO = "GetUserInfo/v2/";

      // Init Txn Endpoint
      private const string STEAMWEBAPI_INIT_TXN = "InitTxn/v3/";

      // Finalize Txn Endpoint
      private const string STEAMWEBAPI_FINALIZE_TXN = "FinalizeTxn/v2/";

      #endregion
   }
}
