using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Networking;
using ICSharpCode.SharpZipLib.Zip;
using System.IO;
using System;
using Steamworks;
using System.Text;

public class SteamTester : MonoBehaviour {
   #region Public Variables

   #pragma warning disable 0649

   Int32 k_unSecretData = 0x5444;
   private byte[] m_Ticket;
   private uint m_pcbTicket;
   private HAuthTicket m_HAuthTicket;
   private Callback<GetAuthSessionTicketResponse_t> m_GetAuthSessionTicketResponse;
   //private Callback<SteamNetAuthenticationStatus_t> m_GetAuthSessionstatus;
   private CallResult<EncryptedAppTicketResponse_t> OnEncryptedAppTicketResponseCallResult;
   public Text testLong;
   public List<string> wordList = new List<string>();

   #pragma warning restore 0649

   #endregion

   private void Awake () {
      m_GetAuthSessionTicketResponse = Callback<GetAuthSessionTicketResponse_t>.Create(OnAuthSessionTickerResponse);
      //m_GetAuthSessionstatus = Callback<SteamNetAuthenticationStatus_t>.Create(OnSteamNetAuthenticationStatus); 
      OnEncryptedAppTicketResponseCallResult = CallResult<EncryptedAppTicketResponse_t>.Create(OnEncryptedAppTicketResponse);
   }
   
   private void OnGUI () {
      if (GUILayout.Button("Check my Friends")) {
         int friendCount = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
         Debug.LogError("[STEAM-FRIENDS] Listing " + friendCount + " Friends.");
         for (int i = 0; i < friendCount; ++i) {
            CSteamID friendSteamId = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);
            string friendName = SteamFriends.GetFriendPersonaName(friendSteamId);
            EPersonaState friendState = SteamFriends.GetFriendPersonaState(friendSteamId);

            Debug.LogError(friendName + " is " + friendState);
         }
      }

      if (GUILayout.Button("Check my Steam ID")) {
         CSteamID steamId = SteamUser.GetSteamID();
         string steamName = SteamFriends.GetFriendPersonaName(steamId);
         Debug.LogError(steamId + " : "+ steamName);
      }

      GUILayout.Space(20);


      if (GUILayout.Button("ForceAuth Auth")) {
         byte[] k_unSecretData = System.BitConverter.GetBytes(0x5444);
         SteamAPICall_t handle = SteamUser.RequestEncryptedAppTicket(k_unSecretData, sizeof(uint));
         OnEncryptedAppTicketResponseCallResult.Set(handle);
      }

      if (GUILayout.Button("Get Auth")) {

         m_Ticket = new byte[1024];
         m_HAuthTicket = SteamUser.GetAuthSessionTicket(m_Ticket, 1024, out m_pcbTicket);
         //StartCoroutine(CO_ProcessClientData());
      }

      if (GUILayout.Button("Begin Auth")) {
         D.editorLog(m_Ticket.Length.ToString(), Color.red);
         foreach (var temp in m_Ticket) {
           // D.editorLog(temp.ToString(), Color.cyan);
         }

         char[] cArray = Encoding.ASCII.GetString(m_Ticket).ToCharArray();

         int coutner = 0;
         string stringTally = "";
         foreach (var temp in cArray) {
            if (coutner > 20) {
               coutner = 0;
               wordList.Add(stringTally);
               D.editorLog("Added new word: "+stringTally, Color.yellow);
               stringTally = "";
            }
            stringTally += temp.ToString();
            coutner++;
         }


         D.editorLog(m_HAuthTicket.ToString(), Color.red);
         EBeginAuthSessionResult ret = SteamUser.BeginAuthSession(m_Ticket, (int) m_pcbTicket, SteamUser.GetSteamID());
         
      }

      if (GUILayout.Button("Corout")) {
         StartCoroutine(CO_ProcessClientData());
      }
   }

   private IEnumerator CO_ProcessClientData () {
      yield return new WaitForSeconds(1);
      string site = "https://api.steampowered.com/ISteamUserAuth/AuthenticateUserTicket/v1";

      WWWForm form = new WWWForm();
      form.AddField("steamid", "76561198005628784");
      form.AddField("sessionkey", "test");
      form.AddField("encrypted_loginkey", "teste");

      using (UnityWebRequest www = UnityWebRequest.Post(site, form)) {
         yield return www.SendWebRequest();

         if (www.isNetworkError || www.isHttpError) {
            Debug.Log(www.error);
         } else {
            Debug.Log("Form upload complete!");
         }
      }

   }

   private void OnAuthSessionTickerResponse (GetAuthSessionTicketResponse_t pCallback) {
      D.editorLog(pCallback.m_eResult.ToString());
      D.editorLog(pCallback.m_hAuthTicket.ToString());
      D.editorLog(m_pcbTicket.ToString()); 
   }
   /*
   private void OnSteamNetAuthenticationStatus (SteamNetAuthenticationStatus_t pCallback) {
      D.editorLog(pCallback.m_debugMsg.ToString());
      D.editorLog(pCallback.m_eAvail.ToString());
   }*/

   void OnEncryptedAppTicketResponse (EncryptedAppTicketResponse_t pCallback, bool bIOFailure) {
      D.editorLog("[" + EncryptedAppTicketResponse_t.k_iCallback + " - EncryptedAppTicketResponse] - " + pCallback.m_eResult, Color.red);
      D.editorLog("Encrypting app ticket response: " + pCallback.m_eResult, Color.red);
   }

   #region Private Variables

   #endregion
}