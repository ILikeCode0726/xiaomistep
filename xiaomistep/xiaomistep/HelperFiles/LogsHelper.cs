using Newtonsoft.Json;
using xiaomistep.Models;

namespace xiaomistep.HelperFiles
{
    /// <summary>
    /// 日志
    /// </summary>
    public static class LogsHelper
    {
        private static string logsPath = AppDomain.CurrentDomain.BaseDirectory + "logs.txt";
        private static IList<LogModel> logs = new List<LogModel>();

        private static void PrintLog(string msg,Level level)
        {
            DateTime time = DateTime.UtcNow.AddHours(8);
            AddLog(new LogModel() { Level = level, Message = msg, Time = time });
        }
        private static void AddLog(LogModel log)
        {
            logs.Add(log);
            if (!System.IO.File.Exists(logsPath))
            {
                System.IO.File.Create(logsPath).Close();
            }
            System.IO.File.WriteAllText(logsPath, JsonConvert.SerializeObject(logs));
        }

        public static void Init()
        {
            if (!System.IO.File.Exists(logsPath))
            {
                System.IO.File.Create(logsPath).Close();
            }
            logs = JsonConvert.DeserializeObject<IList<LogModel>>(System.IO.File.ReadAllText(logsPath)) ?? new List<LogModel>();
        }

        /// <summary>
        /// 错误日志
        /// </summary>
        /// <param name="msg"></param>
        public static void Error(string msg)
        {
            PrintLog(msg,Level.Error);
        }
        /// <summary>
        /// 正常日志
        /// </summary>
        /// <param name="msg"></param>
        public static void Info(string msg)
        {
            PrintLog(msg, Level.Info);
        }
        /// <summary>
        /// 查询所有日志
        /// </summary>
        /// <returns></returns>
        public static IList<LogModel> GetAllLog()
        {
            return logs.ToList();
        }
        /// <summary>
        /// 查询错误日志
        /// </summary>
        /// <returns></returns>
        public static IList<LogModel> GetAllErrorLog()
        {
            return logs.Where(m=>m.Level== Level.Error).ToList();
        }
        /// <summary>
        /// 查询今天的日志
        /// </summary>
        /// <returns></returns>
        public static IList<LogModel> GetTodayLog()
        {
            DateTime time = PlayNugetPackage.TimeHelper.Now;
            return logs.Where(m=>m.Time!=null&& DateTime.Equals(m.Time.Value.Date, time.Date)).ToList();
        }
        /// <summary>
        /// 查询今天的错误日志
        /// </summary>
        /// <returns></returns>
        public static IList<LogModel> GetTodayErrorLog()
        {
            DateTime time = PlayNugetPackage.TimeHelper.Now;
            return logs.Where(m => m.Time != null && DateTime.Equals(m.Time.Value.Date, time.Date) && m.Level==Level.Error).ToList();
        }
    }
}
