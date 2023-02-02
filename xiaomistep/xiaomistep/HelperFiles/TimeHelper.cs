namespace xiaomistep.HelperFiles
{
    public static class TimeHelper
    {
        /// <summary>
        /// 返回8时区的时间
        /// </summary>
        public static DateTime DateTimeNow
        {
            get
            {
                return DateTime.UtcNow.AddHours(8);
            }
        }

        public static DateTime DateNow
        {
            get
            {
                return DateTime.Parse(DateTimeNow.ToString("D"));
            }
        }

        public static long Get1970ToNowSeconds
        {
            get
            {
                return (System.DateTime.UtcNow.ToUniversalTime().Ticks - 621355968000000000) / 10000000;
            }
        }

        public static long Get1970ToNowMilliseconds
        {
            get
            {
                return (System.DateTime.UtcNow.ToUniversalTime().Ticks - 621355968000000000) / 10000;
            }
        }
    }
}
