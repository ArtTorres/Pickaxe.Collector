using System;

namespace Pickaxe.Collector.Tools
{
    class Timer
    {
        public DateTime StartTime { get; set; }
        public DateTime? StopTime { get; set; }
        public bool IsActive { get; private set; }

        public int LeftTime
        {
            get
            {
                int totalSeconds = (!this.StopTime.HasValue) ?
                    (int)(this.TimeWindow - DateTime.Now.Subtract(this.StartTime).TotalSeconds) :
                    (int)(this.TimeWindow - this.StopTime.Value.Subtract(this.StartTime).TotalSeconds);

                return totalSeconds <= 0 ? 0 : totalSeconds;
            }
        }

        public int UsedTime
        {
            get
            {
                int totalSeconds = (int)(this.TimeWindow - this.LeftTime);

                return totalSeconds <= 0 ? 0 : totalSeconds;
            }
        }

        public double TimeWindow { get; private set; }

        public Timer(int timeWindow)
        {
            this.TimeWindow = timeWindow * 60.0d;
        }

        public void Start()
        {
            this.StartTime = DateTime.Now;
            this.StopTime = null;
            this.IsActive = true;
        }

        public void Stop()
        {
            this.StopTime = DateTime.Now;
            this.IsActive = false;
        }
    }
}
