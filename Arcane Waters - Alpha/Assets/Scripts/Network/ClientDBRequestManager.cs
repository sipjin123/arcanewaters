using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using Steamworks;
using SteamLoginSystem;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Threading;
using Newtonsoft.Json;

public class ClientDBRequestManager
{
   #region Public Variables

   // The number of milliseconds after which DB requests are cancelled due to timeout
   public static int TIMEOUT = 10000;

   #endregion

   public static async Task<T> exec<T> (string functionName, params object[] parameters) {
      if (Global.player == null) {
         return default;
      }

      // Generate a new id for the request
      int requestId = ++_lastRequestId;

      NubisCallInfo callInfo = NubisCallInfoCreator.create(functionName, parameters);
      string callInfoSerialized = callInfo.serialize();

      // Request the data from the server
      Global.player.rpc.Cmd_RequestDBData(requestId, callInfoSerialized);

      // Add the task completion source to the dictionary so that it can be found when receiving the data
      TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
      _requests.Add(requestId, tcs);

      object result = "";
      try {
         // Wait until the server answers or the timeout is reached
         await Task.WhenAny(tcs.Task, Task.Delay(TIMEOUT));

         if (!tcs.Task.IsCompleted) {
            tcs.TrySetCanceled();
            D.error("Timeout when requesting the execution of DB function " + functionName);
         } else {
            result = tcs.Task.Result;
         }
      } catch (Exception e) {
         D.error("Error when requesting the execution of DB function " + functionName + ": " + e.ToString());
      }

      // Remove the request from the dictionary
      _requests.Remove(requestId);

      return (T)result;
   }

   public static void receiveRequestData (int requestId, byte[] data) {
      if (_requests.TryGetValue(requestId, out TaskCompletionSource<object> tcs)) {
         try {
            string serializedData = Encoding.ASCII.GetString(data);
            NubisCallResult ncr = NubisCallResult.deserialize(serializedData);
            tcs.TrySetResult(ncr.getTypedValue());
         } catch (Exception e) {
            D.error("Error when receiving the DB request result: " + e.ToString());
         }
      }
   }

   #region Private Variables

   // Contains the DB requests that are waiting for an answer from the server
   private static Dictionary<int, TaskCompletionSource<object>> _requests = new Dictionary<int, TaskCompletionSource<object>>();

   // The last used request id
   private static int _lastRequestId = 0;

   #endregion
}