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

      // Auth Ticket Data
      public byte[] m_Ticket;

      // Auth Ticket Size
      public uint m_pcbTicket;

      // The test encryption data cache
      public string encryptedData = "";

      // Unity event containing response data
      public GetAuthTicketEvent getAuthTicketEvent;

      // Shows the fetched data logs
      public bool isLogActive;

      // The minimum length of the steam id
      public const int MIN_STEAM_ID_LENGTH = 5;

      // The default steam password to be encrypted
      public const string STEAM_PASSWORD = "arcane_steam!pw";

      #endregion

      private void Awake () {
         self = this;
         _getAuthSessionTicketResponse = Callback<GetAuthSessionTicketResponse_t>.Create(onGetAuthSessionTicketResponse);
      }

      public void getAuthenticationTicket () {
         m_Ticket = new byte[1024];
         _hAuthTicket = SteamUser.GetAuthSessionTicket(m_Ticket, 1024, out m_pcbTicket);
      }

      private void onGetAuthSessionTicketResponse (GetAuthSessionTicketResponse_t pCallback) {
         Array.Resize(ref m_Ticket, (int) m_pcbTicket);

         //format as Hex 
         StringBuilder ticketHexCode = new StringBuilder();
         foreach (byte b in m_Ticket) {
            ticketHexCode.AppendFormat("{0:x2}", b);
         }

         if (isLogActive) {
            D.editorLog("Hex encoded ticket is: " + ticketHexCode.ToString(), Color.blue);
            Debug.Log("[" + GetAuthSessionTicketResponse_t.k_iCallback + " - GetAuthSessionTicketResponse] - " + pCallback.m_hAuthTicket + " -- " + pCallback.m_eResult);
         }

         getAuthTicketEvent.Invoke(new GetAuthTicketResponse {
            m_Ticket = this.m_Ticket,
            m_pcbTicket = this.m_pcbTicket
         });
      }

      public static string getSteamState () {
         string steamState = "";
         string clientBuildState = "";

         // Check build state
         if (Util.getJenkinsBuildId().StartsWith(Util.DEVELOPMENT_BUILD)) {
            clientBuildState = "Development";
         } else if (Util.getJenkinsBuildId().StartsWith(Util.PRODUCTION_BUILD)) {
            clientBuildState = "Production";
         } else if (Util.getJenkinsBuildId().StartsWith(Util.STANDALONE_BUILD)) {
            clientBuildState = "Standalone";
         } else {
            clientBuildState = "Undefined {" + Util.getJenkinsBuildId() + "}";
         }

         // Check steam state
         if (SteamAPI.IsSteamRunning() && SteamManager.Initialized) {
            steamState += "Steam : ";
            if (SteamUtils.GetAppID().ToString() == SteamLoginManagerServer.GAMEPLAYTEST_APPID) {
               steamState += "Playtest : Production";
            } else {
               steamState += "Main : " + clientBuildState;
            }
         }

         return (String.IsNullOrEmpty(steamState) ? "Non-Steam : " + clientBuildState: steamState);
      }

      #region Private Variables

      // The authentication data
      private HAuthTicket _hAuthTicket;

      // The callback for the ticket response
      protected Callback<GetAuthSessionTicketResponse_t> _getAuthSessionTicketResponse;

      #endregion
   }
}