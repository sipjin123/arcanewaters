using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Networking;
using Steamworks;
using System;
using System.Security.Cryptography;
using System.Text;

namespace SteamLoginSystem
{
   public class SteamLoginManager : MonoBehaviour {
      #region Public Variables

      // Self
      public static SteamLoginManager self;
      
      // The WEB API request for ticket authentication
      public const string STEAM_AUTHENTICATE_TICKET = "https://api.steampowered.com/ISteamUserAuth/AuthenticateUserTicket/v1/?";
      public const string OWNERSHIP_WEB_REQUEST = "http://localhost/getSteamAppOwnership_v1.php?usrId="; // "http://arcanewaters.com/getSteamAppOwnership_v1.php?usrId=";

      // The various parameters used for the web api requests
      public const string PARAM_STEAM_ID = "steamid=";
      public const string PARAM_KEY = "key=";
      public const string PARAM_APPID = "appid=";
      public const string PARAM_TICKET = "ticket=";

      // TODO: Might want to put this info in the server database for security purposes
      // Steam key parameters for web api request
      public static string STEAM_WEB_USER_API_KEY = "1EB0664926636257A9861504BE93721B";

      // The arcane waters steam app id
      public const string GAME_APPID = "1266340";

      // Auth Ticket Data
      public byte[] m_Ticket;

      // Auth Ticket Size
      public uint m_pcbTicket;

      // The test encryption data cache
      public string encryptedData = "";

      // The event that will contain the response of the ownership query
      public AppOwnershipEvent appOwnershipEvent;

      #endregion

      private void Awake () {
         self = this;
         _getAuthSessionTicketResponse = Callback<GetAuthSessionTicketResponse_t>.Create(onGetAuthSessionTicketResponse);
      }

      private void OnGUI () {
         if (GUILayout.Button("Get Auth")) {
            getAuthentication();
         }

         if (GUILayout.Button("Authenticate Ticket")) {
            StartCoroutine(CO_ProcessAuthentication());
         }

         if (GUILayout.Button("Get App Ownership")) {
            getOwnershipInfo();
         }

         if (GUILayout.Button("Encrypt")) {
            encryptedData = SteamLoginEncryption.Encrypt("burlCodetesT1152");
            D.editorLog(encryptedData, Color.green);
         }

         if (GUILayout.Button("Decrypt")) {
            string decryptedData = SteamLoginEncryption.Decrypt(encryptedData);
            D.editorLog(decryptedData, Color.red);
         }
      }

      private void getAuthentication () {
         m_Ticket = new byte[1024];
         _hAuthTicket = SteamUser.GetAuthSessionTicket(m_Ticket, 1024, out m_pcbTicket);
      }

      private void onGetAuthSessionTicketResponse (GetAuthSessionTicketResponse_t pCallback) {
         Debug.Log("[" + GetAuthSessionTicketResponse_t.k_iCallback + " - GetAuthSessionTicketResponse] - " + pCallback.m_hAuthTicket + " -- " + pCallback.m_eResult);
      }

      public void getOwnershipInfo () {
         StartCoroutine(CO_ProcessAppOwnership());
      }

      private IEnumerator CO_ProcessAuthentication () {
         yield return new WaitForSeconds(.1f);
         Array.Resize(ref m_Ticket, (int) m_pcbTicket);

         //format as Hex 
         StringBuilder ticketHexCode = new StringBuilder();

         foreach (byte b in m_Ticket) {
            ticketHexCode.AppendFormat("{0:x2}", b);
         }

         D.editorLog("Hex encoded ticket is: " + ticketHexCode.ToString(), Color.blue);

         string webRequest = STEAM_AUTHENTICATE_TICKET +
            PARAM_KEY + STEAM_WEB_USER_API_KEY + "&" +
            PARAM_APPID + GAME_APPID + "&" +
            PARAM_TICKET + ticketHexCode.ToString();

         UnityWebRequest www = UnityWebRequest.Get(webRequest);
         D.editorLog("Requesting: " + webRequest, Color.red);

         yield return www.SendWebRequest();
         if (www.isNetworkError || www.isHttpError) {
            D.warning(www.error);
         } else {
            string rawData = www.downloadHandler.text;
            rawData = rawData.Replace("params", "newParams");
            AuthenticateTicketResponse authCall = JsonUtility.FromJson<AuthenticateTicketResponse>(rawData);

            D.editorLog("The raw data is: " + rawData, Color.magenta);
            D.editorLog(authCall.response.newParams.ownersteamid, Color.cyan);
            D.editorLog(authCall.response.newParams.steamid, Color.cyan);
            D.editorLog(authCall.response.newParams.vacbanned.ToString(), Color.cyan);
            D.editorLog(authCall.response.newParams.publisherbanned.ToString(), Color.cyan);
         }
      }

      private IEnumerator CO_ProcessAppOwnership () {
         yield return new WaitForSeconds(.1f);

         // A php web request that will fetch the ownership info using the { Steam Publisher Web API Key }
         string webRequest = OWNERSHIP_WEB_REQUEST + SteamUser.GetSteamID().ToString();

         D.editorLog("Requesting: " + webRequest, Color.red);
         UnityWebRequest www = UnityWebRequest.Get(webRequest);

         yield return www.SendWebRequest();
         if (www.isNetworkError || www.isHttpError) {
            D.warning(www.error);
         } else {
            string rawData = www.downloadHandler.text;
            AppOwnerShipResponse ownershipResponse = JsonUtility.FromJson<AppOwnerShipResponse>(rawData);

            D.editorLog("The raw data is: " + rawData, Color.magenta);
            D.editorLog(ownershipResponse.appownership.ownersteamid, Color.cyan);
            D.editorLog(ownershipResponse.appownership.ownsapp.ToString(), Color.cyan);
            D.editorLog(ownershipResponse.appownership.permanent.ToString(), Color.cyan);
            D.editorLog(ownershipResponse.appownership.result, Color.cyan);
            D.editorLog(ownershipResponse.appownership.sitelicense.ToString(), Color.cyan);
            D.editorLog(ownershipResponse.appownership.timestamp.ToString(), Color.cyan);
            D.editorLog(DateTime.Parse(ownershipResponse.appownership.timestamp).ToString(), Color.cyan);

            appOwnershipEvent.Invoke(ownershipResponse);
         }
      }

      #region Private Variables

      // The authentication data
      private HAuthTicket _hAuthTicket;

      // The callback for the ticket response
      protected Callback<GetAuthSessionTicketResponse_t> _getAuthSessionTicketResponse;

      #endregion
   }
}