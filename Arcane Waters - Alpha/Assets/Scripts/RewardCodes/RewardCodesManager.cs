using System;
using System.Net.Http;
using Newtonsoft.Json;
using UnityEngine;

namespace Rewards
{
   public class RewardCodesManager : MonoBehaviour
   {
      #region Public Variables

      // Self
      public static RewardCodesManager self;

      #endregion

      private void Awake () {
         self = this;
      }

      public void createRewardCodeFor (string steamId, Action<string> callback) {
         if (Util.isEmpty(_urlSendRewardCode)) {
            return;
         }

         // Create a reward code
         string code = Util.getRandomString(length: 8);

         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            try {
               // send the code in the api
               _httpClient = _httpClient ?? new HttpClient();
               HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, _urlSendRewardCode);
               request.Headers.Add("AccessToken", _accessToken);
               request.Content = new StringContent(JsonConvert.SerializeObject(new { steamId, code }), System.Text.Encoding.UTF8, "application/json");

               // Send request
               _httpClient.SendAsync(request).ContinueWith(t => {
                  if (t.Result.StatusCode != System.Net.HttpStatusCode.OK) {
                     D.warning($"RewardsCodesManager: Tried to send reward code but received an unexpected status code '{t.Result.StatusCode}'");
                     return;
                  }

                  // Send the code back to the main thread
                  UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                     callback?.Invoke(code);
                  });
               });
            } catch (System.Exception ex) {
               D.error(ex.Message);
            }
         });
      }

      public void verifyRewardCodeFor (string steamId, string code, Action<bool> callback) {
         if (Util.isEmpty(_urlVerifyRewardCode)) {
            return;
         }

         // Prepare request
         _httpClient = _httpClient ?? new HttpClient();
         HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, _urlVerifyRewardCode);
         request.Headers.Add("AccessToken", _accessToken);
         request.Content = new StringContent(JsonConvert.SerializeObject(new { steamId, code }), System.Text.Encoding.UTF8, "application/json");

         // Send request
         _httpClient.SendAsync(request).ContinueWith(t => {
            callback?.Invoke(t.Result.StatusCode == System.Net.HttpStatusCode.OK);
         });
      }

      #region Private Variables

      // Http client used for web requests
      private HttpClient _httpClient;

      #if IS_SERVER_BUILD

      // The api url to the reward code verification endpoint
      private string _urlVerifyRewardCode = "https://arcanewaters.com/api/v1/verifyRewardCode";

      // The api url to the reward code verification endpoint
      private string _urlSendRewardCode = "https://arcanewaters.com/api/v1/sendRewardCode";

      // The access token used to validate api calls
      private string _accessToken = "MUJFMThDMEQ0MDU0QTJCREYwNDc1QkFCNjlBMzU5Q0Q2MTY5MDdGOTQxODcyODE5RDkyQUY5QTM4RkRERjIxMjJDMzMzRjQ1RTlDRjVDNUMzMzJCRDMwMTcwRjc0RDE4MkUxQThFQjYxMkU1MjMwN" +
         "ENBNjU3NzZDQjcwM0NFQzJFMERENTdFNjU4RDRGMEI4QjI2OTcxODNCREUwQkVBOEEzRkE2RjBDQTBDN0NENDQ5NEFGNkRCQTg2NEUyODJERDhERjk5N0FDRDQzNjI3NkI2M0QzMTg2OTQ1MzQ4NTZCNDMwOTkyMzA2NDYyNzFDNkQwRDl" +
         "BNTBERkMzQjRCRTlFRTBGN0NCNDVGQjJBQjBCQjBDODM2ODVEQTdDMDIwRTRGODA2NTkxRDg5MUNFRTFCRERGNzIxRTVBQzlDMENBQ0Y0NTZCRUZBMDBFM0EwQkI0NUJGNkNBRTFBRDQ1Njg1NjBCQjYyMDFBNEM2MENCODVFQTg4" +
         "NDc5NkFGODUwNUJCQ0U5QTcwM0JFMDNFODkyOEQ4QjE2RDdEMUJENjI0NjZFNzk1OEQ3MEQ4QjUzRDRGRDEwNTY4ODdBODVCQUJEOUY2NUEwNTU2MzgzNDcxRjkwNkQ0Nzk2NTE3NjEwNkExRERFM0FGRjhDOUJDN0UxN0NCMTFE" +
         "Njg4Njg3MjBDRThERTFEMzhFODI5RUIxQjI1RDdEM0YyNEYxODZCNjQwQjVFRUUwNEREQkVGRDA1Qzk1MzFGQThCRTUzNUQ4QTIyQ0VERUZCREE0N0EzNDJBNzcwRkYyRkEyODE4NTA=";

      #else
   
      // The api url to the reward code verification endpoint
      private string _urlVerifyRewardCode = "";

      // The api url to the reward code verification endpoint
      private string _urlSendRewardCode = "";

      // The access token used to validate api calls
      private string _accessToken = "";

      #endif
      
      #endregion
   }
}