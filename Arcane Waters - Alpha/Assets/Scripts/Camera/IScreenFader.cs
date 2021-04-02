public interface IScreenFader
{
   /// <summary>
   /// 
   /// </summary>
   /// <returns>Duration of the effect</returns>
   float fadeIn ();

   /// <summary>
   /// 
   /// </summary>
   /// <returns>Duration of the effect</returns>
   float fadeOut ();

   /// <summary>
   /// 
   /// </summary>
   /// <returns>Duration of the fade in effect</returns>
   float getFadeInDuration ();

   /// <summary>
   /// 
   /// </summary>
   /// <returns>Duration of the fade out effect</returns>
   float getFadeOutDuration ();
}
