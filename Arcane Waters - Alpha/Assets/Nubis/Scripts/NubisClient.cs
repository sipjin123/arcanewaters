// TODO: This class shouldn't be guarded with #if NUBIS
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using System;
using UnityEngine;

/// <summary>
/// Allows to communicate with Nubis through reflection calls.
/// </summary>
internal class NubisClient
{
   #region Public Variables

   // Nubis Remote Port
   public const int REMOTE_PORT = 7900;

   #endregion

   private static string getEndpointFromFunctionName (string funcName) {
      if (string.IsNullOrEmpty(funcName)) return string.Empty;
      funcName = System.Net.WebUtility.UrlEncode(funcName);
      return funcName;
   }

   private static string getQueryStringFromArguments (params string[] args) {
      if (args == null || args.Length == 0) return string.Empty;
      int argIdx = 0;
      StringBuilder sbuilder = new StringBuilder();
      foreach (string arg in args) {
         if (string.IsNullOrEmpty(arg)) continue;
         sbuilder.Append((argIdx == 0) ? "?" : "&");
         string name = arg.GetType().Name + argIdx.ToString();
         string value = arg;
         name = System.Net.WebUtility.UrlEncode(name);
         value = System.Net.WebUtility.UrlEncode(value);
         sbuilder.Append($"{name}={value}");
         argIdx++;
      }
      return sbuilder.ToString();
   }
   private static string buildUrl (string endpoint, string queryString, string remoteServer, int remotePort) {
      return $"http://{remoteServer}:{remotePort}/{NubisEndpoints.RPC}/{endpoint}{queryString}";
   }
   private static async Task<string> requestImpl (string function, params string[] args) {

#if IS_SERVER_BUILD && CLOUD_BUILD
      string server = Global.getAddress(MyNetworkManager.ServerType.Localhost);
#else
      string server = Global.getAddress(MyNetworkManager.ServerType.AmazonVPC);
      
#endif

#if UNITY_EDITOR
      server = Global.getAddress(MyNetworkManager.ServerType.AmazonVPC);
#endif

      string endpoint = getEndpointFromFunctionName(function);
      string query = getQueryStringFromArguments(args);
      string url = buildUrl(endpoint, query, server, REMOTE_PORT);
      HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, url);
      HttpClient client = new HttpClient();
      HttpResponseMessage response = await client.SendAsync(message);
      return await response.Content.ReadAsStringAsync();
   }
   public static async Task<T> callXml<T> (string function, params object[] rawArgs) {
      string[] args = Array.ConvertAll(rawArgs, x => x.ToString());
      try {
         string result = await requestImpl(function, args);
         return Util.xmlLoad<T>(result);
      } catch {
         return default(T);
      }
   }
   public static async Task<T> callJSONList<T> (string function, params object[] rawArgs) {
      string[] args = Array.ConvertAll(rawArgs, x => x.ToString());
      try {
         string result = await requestImpl(function, args);
         return JsonConvert.DeserializeObject<T>(result);
      } catch {
         return default(T);
      }
   }
   public static async Task<T> callJSONClass<T> (string function, params object[] rawArgs) {
      string[] args = Array.ConvertAll(rawArgs, x => x.ToString());
      try {
         string result = await requestImpl(function, args);
         return JsonUtility.FromJson<T>(result);
      } catch {
         return default(T);
      }
   }
   public static async Task<string> call (string function, params object[] rawArgs) {
      string[] args = Array.ConvertAll(rawArgs, x => x.ToString());
      try {
         return await requestImpl(function, args);
      } catch {
         return string.Empty;
      }
   }
}

