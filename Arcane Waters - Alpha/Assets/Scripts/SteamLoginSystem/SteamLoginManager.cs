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

      #endregion

      private void Awake () {
         self = this;
         _getAuthSessionTicketResponse = Callback<GetAuthSessionTicketResponse_t>.Create(onGetAuthSessionTicketResponse);
      }

      private void OnGUI () {
         if (GUILayout.Button("Get Auth")) {
            getAuthenticationTicket();
         }

         if (GUILayout.Button("Authenticate Ticket")) {
            //authenticateTicket();
         }

         if (GUILayout.Button("Get App Ownership")) {
            //getOwnershipInfo();
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

      #region Private Variables

      // The authentication data
      private HAuthTicket _hAuthTicket;

      // The callback for the ticket response
      protected Callback<GetAuthSessionTicketResponse_t> _getAuthSessionTicketResponse;

      #endregion
   }
}