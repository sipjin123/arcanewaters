﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using System.Text;
using UnityEngine.Networking;

namespace SteamLoginSystem
{
   public class SteamLoginManagerServer : MonoBehaviour {
      #region Public Variables
      
      #if IS_SERVER_BUILD

      // Self
      public static SteamLoginManagerServer self;

      // Steam key parameters for web api request
      public static string STEAM_WEB_USER_API_KEY = "1EB0664926636257A9861504BE93721B";
      public static string STEAM_WEB_PUBLISHER_API_KEY = "16FBA4602CFF4C139DC40E01D58F8869";

      // The WEB API request for ticket authentication
      public const string STEAM_AUTHENTICATE_TICKET = "https://api.steampowered.com/ISteamUserAuth/AuthenticateUserTicket/v1/?";
      public const string OWNERSHIP_WEB_REQUEST = "https://partner.steam-api.com/ISteamUser/CheckAppOwnership/v2/?";

      // Unity events containing response data
      public AuthenticateTicketEvent authenticateTicketEvent;
      public AppOwnershipEvent appOwnershipEvent;

      // The various parameters used for the web api requests
      public const string PARAM_STEAM_ID = "steamid=";
      public const string PARAM_KEY = "key=";
      public const string PARAM_APPID = "appid=";
      public const string PARAM_TICKET = "ticket=";

      // The arcane waters steam app id
      public const string GAME_APPID = "1266340";

      // Shows the fetched data logs
      public bool isLogActive;

      #endif

      #endregion

      private void Awake () {
         self = this;
      }

      [ServerOnly]
      public void authenticateTicket (byte[] authTicketData, uint m_pcbTicket) {
         StartCoroutine(CO_ProcessAuthentication(authTicketData, m_pcbTicket));
      }

      private IEnumerator CO_ProcessAuthentication (byte[] authTicketData, uint m_pcbTicket) {
         Array.Resize(ref authTicketData, (int) m_pcbTicket);

         //format as Hex 
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
            D.warning(www.error);
         } else {
            string rawData = www.downloadHandler.text;
            rawData = rawData.Replace("params", "newParams");
            AuthenticateTicketResponse authTicketResponse = JsonUtility.FromJson<AuthenticateTicketResponse>(rawData);

            if (isLogActive) {
               D.editorLog("The raw data is: " + rawData, Color.magenta);
               D.editorLog(authTicketResponse.response.newParams.ownersteamid, Color.cyan);
               D.editorLog(authTicketResponse.response.newParams.steamid, Color.cyan);
               D.editorLog(authTicketResponse.response.newParams.vacbanned.ToString(), Color.cyan);
               D.editorLog(authTicketResponse.response.newParams.publisherbanned.ToString(), Color.cyan);
            }
            authenticateTicketEvent.Invoke(authTicketResponse);
         }
      }

      [ServerOnly]
      public void getOwnershipInfo (string steamId) {
         StartCoroutine(CO_ProcessAppOwnership(steamId));
      }

      private IEnumerator CO_ProcessAppOwnership (string steamId) {
         // A php web request that will fetch the ownership info using the { Steam Publisher Web API Key }
         string webRequest = OWNERSHIP_WEB_REQUEST + PARAM_KEY + STEAM_WEB_PUBLISHER_API_KEY + "&" + PARAM_STEAM_ID + steamId + "&" + PARAM_APPID + GAME_APPID;
         if (isLogActive) {
            D.editorLog("Requesting: " + webRequest, Color.red);
         }

         UnityWebRequest www = UnityWebRequest.Get(webRequest);
         yield return www.SendWebRequest();
         if (www.isNetworkError || www.isHttpError) {
            D.warning(www.error);
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

            appOwnershipEvent.Invoke(ownershipResponse);
         }
      }

      #region Private Variables

      #endregion
   }
}