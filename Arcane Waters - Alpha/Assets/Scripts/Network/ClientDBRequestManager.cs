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

public class ClientDBRequestManager
{
   #region Public Variables

   // The number of milliseconds after which DB requests are cancelled due to timeout
   public static int TIMEOUT = 10000;

   #endregion

   public static async Task<string> exec (string functionName, params string[] parameters) {
      if (Global.player == null) {
         return "";
      }

      // Generate a new id for the request
      int requestId = ++_lastRequestId;

      // Request the data from the server
      Global.player.rpc.Cmd_RequestDBData(requestId, functionName, parameters);

      // Add the task completion source to the dictionary so that it can be found when receiving the data
      TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();
      _requests.Add(requestId, tcs);

      string result = "";
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

      return result;
   }

   public static void receiveRequestData (int requestId, byte[] data) {
      if (_requests.TryGetValue(requestId, out TaskCompletionSource<string> tcs)) {
         try {
            tcs.TrySetResult(Encoding.ASCII.GetString(data));
         } catch (Exception e) {
            D.error("Error when receiving the DB request result: " + e.ToString());
         }
      }
   }

   #region Private Variables

   // Contains the DB requests that are waiting for an answer from the server
   private static Dictionary<int, TaskCompletionSource<string>> _requests = new Dictionary<int, TaskCompletionSource<string>>();

   // The last used request id
   private static int _lastRequestId = 0;

   #endregion
}