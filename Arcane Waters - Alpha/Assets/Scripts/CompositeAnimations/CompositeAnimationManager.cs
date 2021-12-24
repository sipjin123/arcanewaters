public class CompositeAnimationManager : GenericGameManager
{
   #region Public Variables

   // The waving animation (West and East)
   public CompositeAnimation WavingWE;

   // The waving animation (North)
   public CompositeAnimation WavingN;

   // The waving animation (South)
   public CompositeAnimation WavingS;

   // The dancing animation
   public CompositeAnimation Dancing;

   // The kneeling animation (West and East)
   public CompositeAnimation KneelingWE;

   // The kneeling animation (North)
   public CompositeAnimation KneelingN;

   // The kneeling animation (South)
   public CompositeAnimation KneelingS;

   // Reference to self
   public static CompositeAnimationManager self;

   #endregion

   protected override void Awake () {
      base.Awake();
      self = this;
   }
}
