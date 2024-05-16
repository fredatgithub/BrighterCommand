namespace Paramore.Brighter.Extensions.Hosting
{
    public class TimedOutboxArchiverOptions
    {
        /// <summary>
        /// The timer interval in seconds. 
        /// </summary>
        public int TimerInterval { get; set; } = 15;

        /// <summary>
        /// The minimum age in hours to Archive
        /// </summary>
        public int MinimumAge { get; set; } = 24;
    }
}
