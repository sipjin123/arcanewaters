using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using System.Text;
using UnityEngine.Networking;
using SteamLoginSystem;

namespace SteamLoginSystem
{
   public class SteamLoginManagerServer : MonoBehaviour {
      #region Public Variables

      // Self
      public static SteamLoginManagerServer self;

      // Steam key parameters for web api request
      public static string STEAM_WEB_USER_API_KEY = "1EB0664926636257A9861504BE93721B";
      public static string STEAM_WEB_PUBLISHER_API_KEY = "16FBA4602CFF4C139DC40E01D58F8869";

      // The WEB API request for ticket authentication
      public const string STEAM_AUTHENTICATE_TICKET = "https://api.steampowered.com/ISteamUserAuth/AuthenticateUserTicket/v1/?";
      public const string OWNERSHIP_WEB_REQUEST = "https://partner.steam-api.com/ISteamUser/CheckAppOwnership/v2/?";

      // Unity events lists containing response data
      public List<AuthenticateTicketEvent> authenticateTicketEventActiveList, authenticateTicketEventDisposedList;
      public List<AppOwnershipEvent> appOwnershipEventActiveList, appOwnershipEventDisposeList;

      // The various parameters used for the web api requests
      public const string PARAM_STEAM_ID = "steamid=";
      public const string PARAM_KEY = "key=";
      public const string PARAM_APPID = "appid=";
      public const string PARAM_TICKET = "ticket=";

      // The arcane waters steam app id
#if PLAYTEST
      public const string GAME_APPID = "1266370";
#else
      public const string GAME_APPID = "1266340";
#endif

      // Shows the fetched data logs
      public bool isLogActive;

      // The limit of tracked disposed events
      public static int DISPOSE_CAP = 10;

      #endregion

      private void Awake () {
         self = this;
      }

      public void authenticateTicket (byte[] authTicketData, uint m_pcbTicket, AuthenticateTicketEvent newTicketEvent) {
         StartCoroutine(CO_ProcessAuthentication(authTicketData, m_pcbTicket, newTicketEvent));
      }

      private IEnumerator CO_ProcessAuthentication (byte[] authTicketData, uint m_pcbTicket, AuthenticateTicketEvent newTicketEvent) {
         authenticateTicketEventActiveList.Add(newTicketEvent);
         Array.Resize(ref authTicketData, (int) m_pcbTicket);

         // Format bytes into hex code
         StringBuilder ticketHexCode = new StringBuilder();
         foreach (byte b in authTicketData) {
            ticketHexCode.AppendFormat("{0:x2}", b);
         }

         string webRequest = STEAM_AUTHENTICATE_TICKET +
            PARAM_KEY + STEAM_WEB_USER_API_KEY + "&" +
            PARAM_APPID + GAME_APPID + "&" +
            PARAM_TICKET + ticketHexCode.ToString();
         if (isLogActive) {
            D.editorLog("Hex encoded ticket is: " + ticketHexCode.ToString(), Color.blue);
            D.editorLog("Requesting: " + webRequest, Color.red);
         }

         UnityWebRequest www = UnityWebRequest.Get(webRequest);
         yield return www.SendWebRequest();
         if (www.isNetworkError || www.isHttpError) {
            D.debug("Network Error Has been Triggered! " + www.error);
         } else {
            string rawData = www.downloadHandler.text;
            rawData = rawData.Replace("params", "newParams");
            AuthenticateTicketResponse authTicketResponse = JsonUtility.FromJson<AuthenticateTicketResponse>(rawData);

            if (string.IsNullOrEmpty(authTicketResponse.response.newParams.ownersteamid)) {
               D.debug("Error! Steam Ticket Authentication Failed! Response:{" + rawData + "}");
            }

            if (isLogActive) {
               D.editorLog("The raw data is: " + rawData, Color.magenta);
               D.editorLog(authTicketResponse.response.newParams.ownersteamid, Color.cyan);
               D.editorLog(authTicketResponse.response.newParams.steamid, Color.cyan);
               D.editorLog(authTicketResponse.response.newParams.vacbanned.ToString(), Color.cyan);
               D.editorLog(authTicketResponse.response.newParams.publisherbanned.ToString(), Color.cyan);
            }

            newTicketEvent.Invoke(authTicketResponse);
         }
      }

      public void getOwnershipInfo (string steamId, AppOwnershipEvent newOwnershipEvent) {
         StartCoroutine(CO_ProcessAppOwnership(steamId, newOwnershipEvent));
      }

      private IEnumerator CO_ProcessAppOwnership (string steamId, AppOwnershipEvent newOwnershipEvent) {
         appOwnershipEventActiveList.Add(newOwnershipEvent);
         if (string.IsNullOrEmpty(steamId)) {
            D.debug("Error! Steam ID is Invalid! ID:{" + steamId + "}");
         }

         // A php web request that will fetch the ownership info using the { Steam Publisher Web API Key }
         string webRequest = OWNERSHIP_WEB_REQUEST + PARAM_KEY + STEAM_WEB_PUBLISHER_API_KEY + "&" + PARAM_STEAM_ID + steamId + "&" + PARAM_APPID + GAME_APPID;
         if (isLogActive) {
            D.editorLog("Requesting: " + webRequest, Color.red);
         }

         UnityWebRequest www = UnityWebRequest.Get(webRequest);
         yield return www.SendWebRequest();
         if (www.isNetworkError || www.isHttpError) {
            D.debug(www.error);
         } else {
            string rawData = www.downloadHandler.text;
            AppOwnerShipResponse ownershipResponse = JsonUtility.FromJson<AppOwnerShipResponse>(rawData);

            if (isLogActive) {
               D.editorLog("The raw data is: " + rawData, Color.magenta);
               D.editorLog(ownershipResponse.appownership.ownersteamid, Color.cyan);
               D.editorLog(ownershipResponse.appownership.ownsapp.ToString(), Color.cyan);
               D.editorLog(ownershipResponse.appownership.permanent.ToString(), Color.cyan);
               D.editorLog(ownershipResponse.appownership.result, Color.cyan);
               D.editorLog(ownershipResponse.appownership.sitelicense.ToString(), Color.cyan);
               D.editorLog(ownershipResponse.appownership.timestamp.ToString(), Color.cyan);
               D.editorLog(DateTime.Parse(ownershipResponse.appownership.timestamp).ToString(), Color.cyan);
            }

            newOwnershipEvent.Invoke(ownershipResponse);
         }
      }

      public void disposeAuthenticationEvent (AuthenticateTicketEvent authTicketEvent) {
         authTicketEvent.RemoveAllListeners();
         authenticateTicketEventActiveList.Remove(authTicketEvent);
         authenticateTicketEventDisposedList.Add(authTicketEvent);
      }

      public void disposeOwnershipEvent (AppOwnershipEvent ownershipEvent) {
         ownershipEvent.RemoveAllListeners();
         appOwnershipEventActiveList.Remove(ownershipEvent);
         appOwnershipEventDisposeList.Add(ownershipEvent);

         // Try to keep track of dispose list for analysis, when reaches cap clear the list
         if (appOwnershipEventDisposeList.Count > DISPOSE_CAP) {
            appOwnershipEventDisposeList.Clear();
         }
      }
   }
}