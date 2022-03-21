using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;

internal class CustomQuant : ProGifNeuQuant
{
   private List<CustomFrame> frames;
   private int bytesPerFrame;
   private GIFReplayManager.EncodeProgress progress;

   public CustomQuant (List<CustomFrame> frames, int sample, GIFReplayManager.EncodeProgress progress, bool reserveSizeAutoTrans = false)
      : base(null, getDataLength(frames), sample, reserveSizeAutoTrans) {

      this.frames = frames;
      bytesPerFrame = frames[0].Width * frames[0].Height * 3;
      this.progress = progress;
   }

   static int getDataLength (List<CustomFrame> frames) {
      int l = frames[0].Width * frames[0].Height * 3;
      int maxCount = int.MaxValue / l;
      return Mathf.Min(maxCount, frames.Count) * l;
   }

   public override void Learn () {
      int i, j, b, g, r;
      int radius, rad, rad2, alpha, step, delta, samplepixels;
      int pix, lim;

      if (lengthcount < minpicturebytes)
         samplefac = 1;

      alphadec = 30 + ((samplefac - 1) / 3);
      pix = 0;
      lim = lengthcount;
      samplepixels = lengthcount / (3 * samplefac);
      delta = samplepixels / ncycles;
      alpha = initalpha;
      radius = initradius;

      rad = radius >> radiusbiasshift;

      if (rad <= 1)
         rad = 0;

      rad2 = rad * rad;
      for (i = 0; i < rad; i++)
         radpower[i] = alpha * ((rad2 - i * i) * radbias / rad2);

      if (lengthcount < minpicturebytes) {
         step = 3;
      } else if ((lengthcount % prime1) != 0) {
         step = 3 * prime1;
      } else {
         if ((lengthcount % prime2) != 0) {
            step = 3 * prime2;
         } else {
            if ((lengthcount % prime3) != 0)
               step = 3 * prime3;
            else
               step = 3 * prime4;
         }
      }

      i = 0;
      while (i < samplepixels) {
         progress.progress = (((float) i) / samplepixels) * 0.6f;
         // ONLY CHANGES WE MADE FROM THE LIBRARY IS IN THE FOLLOWING 5 LINES OF CODE BELOW
         int f = pix / bytesPerFrame;
         int fi = pix % bytesPerFrame;
         b = (frames[f].Data[fi + 0] & 0xff) << netbiasshift;
         g = (frames[f].Data[fi + 1] & 0xff) << netbiasshift;
         r = (frames[f].Data[fi + 2] & 0xff) << netbiasshift;
         j = Contest(b, g, r);

         if (!greyscale) {
            Altersingle(alpha, j, b, g, r);

            if (rad != 0) Alterneigh(rad, j, b, g, r); // Alter neighbours
         }

         pix += step;

         if (pix >= lim)
            pix -= lengthcount;

         i++;

         if (delta == 0)
            delta = 1;

         if (i % delta == 0) {
            alpha -= alpha / alphadec;
            radius -= radius / radiusdec;
            rad = radius >> radiusbiasshift;

            if (rad <= 1)
               rad = 0;

            rad2 = rad * rad;
            for (j = 0; j < rad; j++)
               radpower[j] = alpha * ((rad2 - j * j) * radbias / rad2);
         }
      }
   }

}
