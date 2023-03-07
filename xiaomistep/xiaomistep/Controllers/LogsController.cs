using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using xiaomistep.HelperFiles;

namespace xiaomistep.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LogsController : ControllerBase
    {
        [HttpGet]
        public string GetTodayLogs()
        {
            string str = string.Empty;
            var logs= LogsHelper.GetTodayLog();
            if (logs != null)
            {
                foreach (var item in logs)
                {
                    if(item != null && item.Time != null)
                    {
                        str += "时间" + item.Time.Value.ToString("G") + "---内容:" + item.Message + "\n";
                    }
                    
                }
            }
            if(string.IsNullOrEmpty(str))
            {
                return "今日暂无日志";
            }
            return str;
        }

        [HttpGet("GetAllLogs")]
        public string GetAllLogs()
        {
            string str = string.Empty;
            var logs = LogsHelper.GetAllLog();
            if (logs != null)
            {
                foreach (var item in logs)
                {
                    if (item != null && item.Time != null)
                        str += "时间" + item.Time.Value.ToString("G") + "---内容:" + item.Message + "\n";
                }
            }
            if (string.IsNullOrEmpty(str))
            {
                return "暂无日志";
            }
            return str;
        }
        [HttpGet("GetAllErrorLogs")]
        public string GetAllErrorLogs()
        {
            string str = string.Empty;
            var logs = LogsHelper.GetAllErrorLog();
            if (logs != null)
            {
                foreach (var item in logs)
                {
                    if (item != null && item.Time != null)
                        str += "时间" + item.Time.Value.ToString("G") + "---内容:" + item.Message + "\n";
                }
            }
            if (string.IsNullOrEmpty(str))
            {
                return "暂无错误日志";
            }
            return str;
        }

        [HttpGet("GetTodayErrorLogs")]
        public string GetTodayErrorLogs()
        {
            string str = string.Empty;
            var logs = LogsHelper.GetTodayErrorLog();
            if (logs != null)
            {
                foreach (var item in logs)
                {
                    if (item != null && item.Time != null)
                        str += "时间" + item.Time.Value.ToString("G") + "---内容:" + item.Message + "\n";
                }
            }
            if (string.IsNullOrEmpty(str))
            {
                return "今日暂无错误日志";
            }
            return str;
        }
    }
}
