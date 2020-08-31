//#define NUBIS
#if NUBIS
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Net;
using System.Threading.Tasks;
using System.IO;
using System;
using static NubisLogger;

public class NubisManager : MonoBehaviour
{
   #region Public Variables

   #endregion

   /// <summary>
   /// Unity Start Message.
   /// </summary>
   public void Start () {
      Application.targetFrameRate = 1;
      i("NubisManager starting");
      startNubis();
   }

   public void OnDestroy () {
      stopNubis();
   }

   internal async Task ProcessRequestAsync (HttpListenerContext context) {
      await Task.Run(() => {
         try {
            if (context.Request.Url.Segments == null || context.Request.Url.Segments.Length < 1) {
               NubisResponse.OK(context, "");
               return;
            }
            string endpoint = context.Request.Url.Segments[1].Replace("/", "");
            switch (endpoint) {
               case NubisEndpoints.RPC:
                  var result = NubisRelay.call(context.Request.Url.AbsoluteUri);
                  NubisResponse.Content(context, result);
                  break;
               case NubisEndpoints.TERMINATE:
                  NubisResponse.OK(context, "Stop request received.");
                  stopNubis();
                  break;
               case NubisEndpoints.LOG:
                  NubisResponse.LOG(context);
                  break;
               case NubisEndpoints.STATUS:
                  NubisResponse.OK(context);
                  break;
               case NubisEndpoints.VERSION:
                  NubisResponse.Content(context,NubisStatics.VERSION);
                  break;
               default:
                  i($"New Request | Sender: {context.Request.RemoteEndPoint.ToString()} | URL: {context.Request.Url}");
                  NubisResponse.NotFound(context);
                  break;
            }
         } catch (Exception ex) {
            NubisResponse.InternalServerError(context);
            e(ex);
         }

      });
   }

   private bool isNubisSupported () {
      i($"is {NubisStatics.APP_NAME} Supported?...");
      bool httpListenerSupported = HttpListener.IsSupported;
      if (httpListenerSupported) {
         i($"{NubisStatics.APP_NAME} can run on this platform!");
      } else {
         i($"{NubisStatics.APP_NAME} is not supported on this platform...");
      }
      return httpListenerSupported;
   }
   
   private void startNubis () {
      if (!isNubisSupported()) return;
      if (Status == NubisStatus.Starting || Status == NubisStatus.Running) return;
      Status = NubisStatus.Starting;
      DefaultFolders.Initialize();
      i($"{NubisStatics.APP_NAME} starting...");
      configuration = NubisConfiguration.LoadSafe();
      webServer = new NubisWebServer(this, configuration.WebServerPort);
      webServer.init();
   }

   public void stopNubis () {
      if (Status == NubisStatus.Stopping || Status == NubisStatus.Idle) return;

      Status = NubisStatus.Stopping;
      i($"{NubisStatics.APP_NAME} stopping...");
      webServer?.stop();
   }


   #region Private Variables

   // Reference to the current instance of the configuration.
   private NubisConfiguration configuration;

   // Reference to the current status of Nubis.
   public NubisStatus Status { get; private set; } = NubisStatus.Idle;

   // Reference to the WebServer.
   private NubisWebServer webServer = null;

   #endregion

}
#else
using UnityEngine;
public class NubisManager : MonoBehaviour
{

}

#endif