
    public class DeployScheduleInfo
    {
        /// <summary>
        /// The scheduled date represented as ticks.
        /// </summary>
        public long DateAsTicks;
        /// <summary>
        /// The Build's Version Number.
        /// </summary>
        public int Build;
        public DeployScheduleInfo(long dateAsTicks, int build)
        {
            DateAsTicks = dateAsTicks;
            Build = build;
        }
    }
