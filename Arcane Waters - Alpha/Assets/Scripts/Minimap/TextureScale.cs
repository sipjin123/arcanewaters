// Only works on ARGB32, RGB24 and Alpha8 textures that are marked readable
using System.Threading;

using UnityEngine;

public class TextureScale
{
   public class ThreadData
   {
      public int start;
      public int end;
      public ThreadData (int s, int e) {
         start = s;
         end = e;
      }
   }

   private static Color[] texColors;
   private static Color[] newColors;
   private static int w;
   private static float ratioX;
   private static float ratioY;
   private static int w2;
   private static int finishCount;
   private static Mutex mutex;

   public static void Point (Texture2D tex, int newWidth, int newHeight) {
      ThreadedScale(tex, newWidth, newHeight);
   }

   private static void ThreadedScale (Texture2D tex, int newWidth, int newHeight) {
      texColors = tex.GetPixels();
      newColors = new Color[newWidth * newHeight];

      ratioX = ((float) tex.width) / newWidth;
      ratioY = ((float) tex.height) / newHeight;

      w = tex.width;
      w2 = newWidth;
      var cores = Mathf.Min(SystemInfo.processorCount, newHeight);
      var slice = newHeight / cores;

      finishCount = 0;
      if (mutex == null) {
         mutex = new Mutex(false);
      }
      if (cores > 1) {
         int i = 0;
         ThreadData threadData;
         for (i = 0; i < cores - 1; i++) {
            threadData = new ThreadData(slice * i, slice * (i + 1));
            ParameterizedThreadStart ts = new ParameterizedThreadStart(PointScale);
            Thread thread = new Thread(ts);
            thread.Start(threadData);
         }
         threadData = new ThreadData(slice * i, newHeight);

         PointScale(threadData);

         while (finishCount < cores) {
            Thread.Sleep(1);
         }
      } else {
         ThreadData threadData = new ThreadData(0, newHeight);

         PointScale(threadData);

      }

      tex.Resize(newWidth, newHeight);
      tex.SetPixels(newColors);
      tex.Apply();

      texColors = null;
      newColors = null;
   }

   public static void PointScale (System.Object obj) {
      ThreadData threadData = (ThreadData) obj;
      for (var y = threadData.start; y < threadData.end; y++) {
         var thisY = (int) (ratioY * y) * w;
         var yw = y * w2;
         for (var x = 0; x < w2; x++) {
            newColors[yw + x] = texColors[(int) (thisY + ratioX * x)];
         }
      }

      mutex.WaitOne();
      finishCount++;
      mutex.ReleaseMutex();
   }

   private static Color ColorLerpUnclamped (Color c1, Color c2, float value) {
      return new Color(c1.r + (c2.r - c1.r) * value,
          c1.g + (c2.g - c1.g) * value,
          c1.b + (c2.b - c1.b) * value,
          c1.a + (c2.a - c1.a) * value);
   }
}