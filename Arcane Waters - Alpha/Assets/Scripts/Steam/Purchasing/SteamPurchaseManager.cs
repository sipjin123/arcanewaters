using System;
using Newtonsoft.Json;
using Steamworks;
using UnityEngine;

namespace Steam.Purchasing
{
   public class SteamPurchaseManager : MonoBehaviour
   {
      #region Public Variables

      // Self
      public static SteamPurchaseManager self;

      #endregion

      public void Awake () {
         self = this;
      }

      public void OnEnable () {
         if (SteamManager.Initialized) {
            // Initialize callbacks
            _microTxnAuthorizationResponseCallback = Callback<MicroTxnAuthorizationResponse_t>.Create(onMicroTxnAuthorizationResponse);
         }
      }

      public void onMicroTxnAuthorizationResponse (MicroTxnAuthorizationResponse_t param) {        
         // This callback is called when the user completes the purchase process, by either confirming or canceling the purchase
         Global.player.rpc.Cmd_CompleteSteamPurchase(param.m_ulOrderID, param.m_unAppID, param.m_bAuthorized > 0);
      }

      #region Private Variables

      // Initialize callbacks
      private Callback<MicroTxnAuthorizationResponse_t> _microTxnAuthorizationResponseCallback;

      #endregion
   }
}
