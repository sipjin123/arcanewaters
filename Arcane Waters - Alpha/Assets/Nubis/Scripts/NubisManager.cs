#if NUBIS
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Net;
using System.Threading.Tasks;
using System.IO;
using System;
using Nubis.Controllers;
using static NubisLogger;

public class NubisManager : MonoBehaviour
{
   #region Public Variables

   #endregion

   /// <summary>
   /// Unity Start Message.
   /// </summary>
   public void Start () {
      i("NubisManager starting");
      _ = StartNubis();
   }
   
   public void OnDestroy () {
      Stop();
   }

   public bool Initialize () {
      bool IsSupported = CheckSystemRequirements();
      if (!IsSupported) return false;
      return true;
   }
   private static void Content (HttpListenerContext context, string message = "") {
      try {
         i("Replying to client...");
         using (StreamWriter writer = new StreamWriter(context.Response.OutputStream)) {
            context.Response.StatusCode = 200;
            writer.WriteLine(message);
            writer.Flush();
         }
         context.Response.Close();
         i("Replying to client: DONE");
      } catch {
         i("Replying to client: FAILED");
      }
   }
   private static void OK (HttpListenerContext context, string message = "OK") {
      try {
         i("Replying to client...");
         using (StreamWriter writer = new StreamWriter(context.Response.OutputStream)) {
            context.Response.StatusCode = 200;
            writer.WriteLine($"<html><p style=\"color:green;\">{message}</p></html>");
            writer.Flush();
         }
         context.Response.Close();
         i("Replying to client: OK");
      } catch {
         i("Replying to client: FAILED");
      }
   }
   private async Task ProcessRequestAsync (HttpListenerContext context) {
      await Task.Run(() => {
         try {
            //string Fetch_Craftable_Armors_v3_Endpoint = "Fetch_Craftable_Armors_v3";
            //string action = context.Request.QueryString.Get("action");
            if (context.Request.Url.Segments == null || context.Request.Url.Segments.Length < 1) {
            OK(context,"");
            return;
            }
            string endpoint = context.Request.Url.Segments[1].Replace("/", "");
            switch (endpoint) {
               case "fetch_craftable_armors_v3": // params: INT usrId
                  string str_armor_usrid_v3 = context.Request.QueryString.Get("usrId");
                  int armor_usrid_v3 = int.Parse(str_armor_usrid_v3);
                  string craftableArmors_v3 = Fetch_Craftable_Armors_v3Controller.fetchCraftableArmors(armor_usrid_v3);
                  Content(context, craftableArmors_v3);
                  break;
               case "fetch_craftable_armors_v4": // params: INT usrId
                  string str_armor_v4_usrid = context.Request.QueryString.Get("usrId");
                  int armor_usrid_v4 = int.Parse(str_armor_v4_usrid);
                  string craftableArmors_v4 = Fetch_Craftable_Armors_v4Controller.fetchCraftableArmors(armor_usrid_v4);
                  Content(context, craftableArmors_v4);
                  break;
               case "fetch_craftable_weapons_v3": // params: INT usrId
                  string str_weapon_v3_usrid = context.Request.QueryString.Get("usrId");
                  int weapons_v3_usrid = int.Parse(str_weapon_v3_usrid);
                  string craftableWeapons_v3 = Fetch_Craftable_Weapons_v3Controller.fetchCraftableWeapons(weapons_v3_usrid);
                  Content(context, craftableWeapons_v3);
                  break;
               case "fetch_craftable_weapons_v4": // params: INT usrId
                  string str_weapons_v4_usrid = context.Request.QueryString.Get("usrId");
                  int weapons_v4_usrid = int.Parse(str_weapons_v4_usrid);
                  string craftableWeapons_v4 = Fetch_Craftable_Weapons_v4Controller.fetchCraftableWeapons(weapons_v4_usrid);
                  Content(context, craftableWeapons_v4);
                  break;
               case "fetch_crafting_ingredients_v3": // params: INT usrId
                  string str_ingredient_v3_usrid = context.Request.QueryString.Get("usrId");
                  int ingredient_v3_usrid = int.Parse(str_ingredient_v3_usrid);
                  string craftingIngredients_v3 = Fetch_Crafting_Ingredients_v3Controller.fetchCraftingIngredients(ingredient_v3_usrid);
                  Content(context, craftingIngredients_v3);
                  break;
               case "fetch_equipped_items_v3": // params: INT usrId
                  string str_equipped_item_v3_usrid = context.Request.QueryString.Get("usrId");
                  int equipped_item_v3_usrid = int.Parse(str_equipped_item_v3_usrid);
                  string equippedItems_v3 = Fetch_Equipped_Items_v3Controller.fetchEquippedItems(equipped_item_v3_usrid);
                  Content(context, equippedItems_v3);
                  break;
               case "fetch_single_blueprint_v4": // params: INT bpId, INT usrId 
                  string str_single_blueprint_v4_bpid = context.Request.QueryString.Get("bpId");
                  string str_single_blueprint_v4_usrid = context.Request.QueryString.Get("usrId");
                  int single_blueprint_v4_bpid = int.Parse(str_single_blueprint_v4_bpid);
                  int single_blueprint_v4_usrid = int.Parse(str_single_blueprint_v4_usrid);
                  string singleBlueprint_v4 = Fetch_Single_Blueprint_v4Controller.fetchSingleBlueprint(single_blueprint_v4_bpid, single_blueprint_v4_usrid);
                  Content(context, singleBlueprint_v4);
                  break;
               case "user_data_v1": // params: INT usrId
                  string str_user_data_v1_usrid = context.Request.QueryString.Get("usrId");
                  int user_data_usrid = int.Parse(str_user_data_v1_usrid);
                  string userData = User_Data_v1Controller.userData(user_data_usrid);
                  Content(context, userData);
                  break;
               case NubisEndpoints.stop:
                  string msg = "Stop request received.";
                  i(msg);
                  OK(context, msg);
                  Stop();
                  break;
               case NubisEndpoints.log:
                  i("Log requsted.");
                  using (StreamWriter writer = new StreamWriter(context.Response.OutputStream)) {
                     foreach (string line in File.ReadAllLines(NubisConfiguration.ConfigFilePath()))
                        writer.WriteLine(line);
                  }
                  context.Response.StatusCode = 200;
                  context.Response.Close();
                  break;
               default:
                  OK(context);
                  break;
            }
         } catch (Exception ex) {
            e(ex);
            OK(context);
         }
         
      });
   }
   public async Task StartNubis () {
      if (Status == NubisStatus.Starting || Status == NubisStatus.Running) return;
      Status = NubisStatus.Starting;
      DefaultFolders.Initialize();
      i($"{NubisStatics.AppName} starting...");
      configuration = NubisConfiguration.LoadSafe();
      await CreateWebServerAsync(configuration.WebServerPort);
   }
   private bool CheckSystemRequirements () {
      i("Checking System Requirements...");
      bool httpListenerSupported = HttpListener.IsSupported;
      if (!httpListenerSupported) {
         i($"{NubisStatics.AppName} is not supported on this platform...");
         return false;
      }
      i($"{NubisStatics.AppName} can run on this platform!");
      i("Checking System Requirements: DONE.");
      return true;
   }
   private void WebServerLoop () {
      try {
         i($"{NubisStatics.AppName} waiting for requests.");
         do {
            HttpListenerContext context = httpServer.GetContext(); // blocks until a request is received.
            i($"request received! -from: {context.Request.RemoteEndPoint.ToString()} -url: {context.Request.Url}");
            _ = ProcessRequestAsync(context);
         }
         while (httpServer.IsListening);
      } catch (InvalidOperationException ex) {
         // httpServer not started or Stopped or closed. or no URI to respond to. thus remember to add prefixes.
         e(ex);
      } catch (HttpListenerException ex) {
         // native call failed.
         i(ex.Message + " | errorCode: " + ex.ErrorCode + " | nativeErrorCode: " + ex.NativeErrorCode);
         if (ex.NativeErrorCode == 995) {
            i($"The error {ex.NativeErrorCode} can be safely ignored in most cases.");
         };
      }
      i($"{NubisStatics.AppName} web server stopped.");
   }
   private bool StartWebServer (HttpListener httpServer, int port) {
      // try to start web server.
      try {
         httpServer.Prefixes.Add($"http://*:{port.ToString()}/");
         i($"Trying to start {NubisStatics.AppName}...");
         httpServer.Start();
      } catch (InvalidOperationException ex) {
         // httpServer not started or Stopped or closed. or no URI to respond to. so remember to add prefixes.
         e(ex);
         i($"{NubisStatics.AppName} failed to start.");
         return false;
      } catch (HttpListenerException ex) {
         // native call failed.
         i(ex.Message + " | errorCode: " + ex.ErrorCode + " | nativeErrorCode: " + ex.NativeErrorCode);
         if (ex.ErrorCode == 5) {
            i($"Please, Run {NubisStatics.AppName} as an Administrator.");
         }
         i($"{NubisStatics.AppName} failed to start.");
         return false;
      }
      return true;
   }
   private async Task CreateWebServerAsync (int port) {
      try {
         // create web server.
         httpServer = new HttpListener();
         bool success = StartWebServer(httpServer, port);
         if (!success) return;
         await Task.Run(WebServerLoop);
      } catch (Exception ex) {
         e(ex);
      }
   }
   public void Stop () {
      if (Status == NubisStatus.Stopping || Status == NubisStatus.Idle) return;
      Status = NubisStatus.Stopping;
      i($"{NubisStatics.AppName} stopping...");
      httpServer?.Abort();
   }

   #region Private Variables

   // Reference to the current instance of the configuration.
   private NubisConfiguration configuration;
   // Reference to the webServer the butler is listening with.
   private HttpListener httpServer;
   //// List of the processes that have been run.
   //private List<ProcessInfo> spawnedProcesses = new List<ProcessInfo>();
   //// Reference to the current status of the Butler.
   public NubisStatus Status { get; private set; } = NubisStatus.Idle;

   #endregion

}
#endif