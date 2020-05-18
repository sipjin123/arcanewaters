// TODO: This class shouldn't be guarded with #if NUBIS
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;

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
      return funcName;
   }

   private static string getQueryStringFromArguments (params string[] args) {
      if (args == null || args.Length == 0) return string.Empty;
      int argIdx = 0;
      StringBuilder sbuilder = new StringBuilder();
      foreach (string arg in args) {
         sbuilder.Append((argIdx == 0) ? "?" : "&");
         string name = arg.GetType().Name + argIdx.ToString();
         string value = arg;
         sbuilder.Append($"{name}={value}");
      }
      return sbuilder.ToString();
   }

   public static async Task<string> call (string function, params string[] args) {
      try {
         string endpoint = getEndpointFromFunctionName(function);
         string queryString = getQueryStringFromArguments(args);
         string remoteServer = Global.getAddress(MyNetworkManager.ServerType.AmazonVPC);
         int remotePort = REMOTE_PORT;
         string url = $"http://{remoteServer}:{remotePort}/{endpoint}{queryString}";
         HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, url);
         HttpClient client = new HttpClient();
         HttpResponseMessage response = await client.SendAsync(message);
         return await response.Content.ReadAsStringAsync();
      } catch {
         return string.Empty;
      }
   }
}

