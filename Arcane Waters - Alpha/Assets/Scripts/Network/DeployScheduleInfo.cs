
    public class DeployScheduleInfo
    {
        /// <summary>
        /// The scheduled date represented as ticks.
        /// </summary>
        public int DateAsTicks;
        /// <summary>
        /// The Build's Version Number.
        /// </summary>
        public int Build;
        public DeployScheduleInfo(int dateAsTicks, int build)
        {
            DateAsTicks = dateAsTicks;
            Build = build;
        }
    }
