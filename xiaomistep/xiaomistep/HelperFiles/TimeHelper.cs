using System;

namespace xiaomistep.HelperFiles
{
    public static class TimeHelper
    {
     
        public static void Init()
        {
            Task.Run(async () =>
            {
                await Task.Delay(new TimeSpan(1, 0, 0));
                PlayNugetPackage.TimeHelper.SyncTime();
            });
        }
        private static async Task< DateTime> GetNTP()
        {
            var temp= await PlayNugetPackage.TimeHelper.GetNTPTime();
            if (temp != null)
                return temp.Value;
            return DateTime.UtcNow;
        }
        /// <summary>
        /// 返回8时区的时间
        /// </summary>

        //public static async Task<DateTime> GetNTPPDateTimeNow()
        //{
        //    return (await GetNTP()).AddHours(8);
        //}
        //public static async Task<DateTime> GetNTPDateNow()
        //{
        //    var temp = await GetNTPPDateTimeNow();
        //    return DateTime.Parse(temp.ToString("D"));
        //}

        public static async Task<long> Get1970ToNowSecondsAsync()
        {
            var time =await GetNTP();
            return (time.ToUniversalTime().Ticks - 621355968000000000) / 10000000;
        }
        public static async Task<long> Get1970ToNowMillisecondsAsync()
        {
            var time = await GetNTP();
            return (time.ToUniversalTime().Ticks - 621355968000000000) / 10000;
        }
        
    }
}
