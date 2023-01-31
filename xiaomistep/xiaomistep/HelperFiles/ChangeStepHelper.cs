using System.Net.Http;
using System;
using System.Text.RegularExpressions;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;

namespace xiaomistep.HelperFiles
{
    /// <summary>
    /// 
    /// </summary>
    public class ChangeStepHelper
    {
        /// <summary>
        /// 开始执行刷步数
        /// </summary>
        /// <param name="account">账号</param>
        /// <param name="pwd">密码</param>
        /// <param name="step">步数</param>
        /// <returns>是否成功</returns>
        public async Task<bool> Start(string account,string pwd,int step)
        {

            string code = await LoginAndGetCode(account, pwd);
            if (string.IsNullOrEmpty(code))
            {
                LogsHelper.Error("账号:" + account + "执行失败1");
                return false;
            }
            (string, string) temp = await GetToken(code);
            if (string.IsNullOrEmpty(temp.Item1)|| string.IsNullOrEmpty(temp.Item2))
            {
                LogsHelper.Error("账号:" + account + "执行失败2");
                return false;
            }
            string apptoken = await GetAppToken(temp.Item1);
            if (string.IsNullOrEmpty(apptoken))
            {
                LogsHelper.Error("账号:" + account + "执行失败3");
                return false;
            }
            return await ChangeStep(apptoken, temp.Item2, step);
        }

        /// <summary>
        /// 创建HttpClient
        /// </summary>
        /// <param name="headers">Headers</param>
        /// <returns>忽略了https证书并且设置好了Headers的HttpClient</returns>
        HttpClient CreatHttpClient(List<(string, string)> headers)
        {
            HttpClientHandler handler = new HttpClientHandler
            {
                //https证书忽略
                ServerCertificateCustomValidationCallback = (message, certificate2, arg3, arg4) => true,
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ClientCertificates = { }
            };
            HttpClient httpClient = new HttpClient(handler);
            //增加Header
            if (headers != null && headers.Count > 0)
            {
                foreach (var item in headers)
                {
                    httpClient.DefaultRequestHeaders.Add(item.Item1, item.Item2);
                }
            }
            return httpClient;
        }
        /// <summary>
        /// 账号密码登录
        /// </summary>
        /// <param name="account">账号</param>
        /// <param name="pwd">密码</param>
        /// <returns>登录成功返回code，失败返回空字符串</returns>
        async Task<string> LoginAndGetCode(string account, string pwd)
        {
            HttpClient httpClient = CreatHttpClient(new List<(string, string)> { ("UserAgent", "MiFit/4.6.0 (iPhone; iOS 14.0.1; Scale/2.00)") });
            // 构造POST参数
            HttpContent postContent = new FormUrlEncodedContent(new Dictionary<string, string>()
           {
              {"client_id", "HuaMi"},
              {"password", pwd},
              {"redirect_uri", "https://s3-us-west-2.amazonaws.com/hm-registration/successsignin.html"},
              {"token", "access"}
           });
            var temp = await httpClient.PostAsync($"https://api-user.huami.com/registrations/+86{account}/tokens", postContent);
            //登录成功
            if (temp.IsSuccessStatusCode == true)
            {
                if (temp.RequestMessage != null && temp.RequestMessage.RequestUri != null)
                {
                    //code代码在重定向的地址中，可以复制temp.RequestMessage.RequestUri地址直接在浏览器查看
                    if (temp.RequestMessage.RequestUri.Query.Contains("access=") && temp.RequestMessage.RequestUri.Query.Split("access=")[1].Contains("&"))
                    {
                        try
                        {
                            return temp.RequestMessage.RequestUri.Query.Split("access=")[1].Split('&')[0];
                        }
                        catch (Exception ex)
                        {
                            LogsHelper.Error(ex.ToString());
                            Console.WriteLine(ex.ToString());
                            return string.Empty;
                        }
                    }
                    else
                    {
                        LogsHelper.Error("获取code失败,不包含access");
                        return string.Empty;
                    }
                }
            }
            LogsHelper.Error("获取code失败,错误码"+ (int)temp.StatusCode);
            return string.Empty;
        }
        /// <summary>
        /// 得到token以及userid
        /// </summary>
        /// <param name="code">登陆成功得到的code</param>
        /// <returns>返回token, userid，都为空字符串表示失败</returns>
        async Task<(string, string)> GetToken(string code)
        {
            HttpClient httpClient = CreatHttpClient(new List<(string, string)>() { ("UserAgent", "MiFit/4.6.0 (iPhone; iOS 14.0.1; Scale/2.00)") });
            HttpContent postContent = new FormUrlEncodedContent(new Dictionary<string, string>()
            {
              {"app_name", "com.xiaomi.hm.health"},
              {"app_version", "4.6.0"},
              {"code", code},
              {"country_code", "CN"},
              {"device_id", "2C8B4939-0CCD-4E94-8CBA-CB8EA6E613A1"},
              {"device_model", "phone"},
              {"grant_type", "access_token"},
              {"third_name", "huami_phone"},
            });
            var temp = await httpClient.PostAsync("https://account.huami.com/v2/client/login", postContent);
            //请求成功
            if (temp.IsSuccessStatusCode == true)
            {
                JObject result = JObject.Parse(await temp.Content.ReadAsStringAsync());
                if (result.GetValue("token_info") != null)
                {
                    try
                    {
                        string token = (result.GetValue("token_info") as dynamic)?.login_token ?? string.Empty;
                        string userid = (result.GetValue("token_info") as dynamic)?.user_id ?? string.Empty;
                        return (token, userid);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                        LogsHelper.Error(ex.ToString());
                        return (string.Empty, string.Empty);
                    }
                }
            }
            LogsHelper.Error("获取token失败,错误码" + (int)temp.StatusCode);
            return (string.Empty, string.Empty);
        }
        /// <summary>
        /// 根据登录token得到APPtoken
        /// </summary>
        /// <param name="login_token">登录token</param>
        /// <returns>APPtoken，失败返回空字符串</returns>
        async Task<string> GetAppToken(string login_token)
        {
            HttpClient httpClient = CreatHttpClient(new List<(string, string)>() { ("UserAgent", "MiFit/4.6.0 (iPhone; iOS 14.0.1; Scale/2.00)") });
            var temp = await httpClient.GetAsync($"https://account-cn.huami.com/v1/client/app_tokens?app_name=com.xiaomi.hm.health&dn=api-user.huami.com%2Capi-mifit.huami.com%2Capp-analytics.huami.com&login_token={login_token}&os_version=4.1.0");
            if (temp.IsSuccessStatusCode == true)
            {
                JObject result = JObject.Parse(await temp.Content.ReadAsStringAsync());
                if (result.GetValue("token_info") != null)
                {
                    try
                    {
                        return (result.GetValue("token_info") as dynamic)?.app_token ?? string.Empty;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                        LogsHelper.Error(ex.ToString());
                        return string.Empty;
                    }
                }
            }
            LogsHelper.Error("获取apptoken失败,错误码" + (int)temp.StatusCode);
            return string.Empty;
        }

        /// <summary>
        /// 获取时间戳
        /// </summary>
        /// <returns></returns>
        async Task<string> GetTime()
        {
            HttpClient httpClient = CreatHttpClient(new List<(string, string)>());
            var temp = await httpClient.GetAsync("http://api.m.taobao.com/rest/api3.do?api=mtop.common.getTimestamp");

            if (temp.IsSuccessStatusCode == true)
            {
                JObject result = JObject.Parse(await temp.Content.ReadAsStringAsync());
                if (result.GetValue("data") != null)
                {
                    try
                    {
                        return (result.GetValue("data") as dynamic)?.t ?? string.Empty;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                        LogsHelper.Error(ex.ToString());
                        return string.Empty;
                    }

                }
            }
            LogsHelper.Error("获取时间戳失败,错误码" + (int)temp.StatusCode);
            return string.Empty;
        }
        /// <summary>
        /// 修改步数
        /// </summary>
        /// <param name="app_token">app_token</param>
        /// <param name="userid">userid</param>
        /// <param name="step">步数</param>
        /// <returns>是否成功</returns>
        async Task<bool> ChangeStep(string app_token, string userid, int step)
        {
            HttpClient httpClient = CreatHttpClient(new List<(string, string)> { ("UserAgent", "Mozilla/5.0 (iPhone; CPU iPhone OS 13_4_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148 MicroMessenger/7.0.12(0x17000c2d) NetType/WIFI Language/zh_CN"), ("apptoken", app_token) });
            string dateTimeNow = DateTime.UtcNow.ToString("yyyy-M-d");
            string data = "[{\"summary\":\"{\\\"slp\\\":{\\\"ss\\\":73,\\\"lt\\\":304,\\\"dt\\\":0," +
                "\\\"st\\\":1589920140,\\\"lb\\\":36,\\\"dp\\\":92,\\\"is\\\":208,\\\"rhr\\\":0," +
                "\\\"stage\\\":[{\\\"start\\\":269,\\\"stop\\\":357,\\\"mode\\\":2},{\\\"start\\\":358," +
                "\\\"stop\\\":380,\\\"mode\\\":3},{\\\"start\\\":381,\\\"stop\\\":407,\\\"mode\\\":2}," +
                "{\\\"start\\\":408,\\\"stop\\\":423,\\\"mode\\\":3},{\\\"start\\\":424,\\\"stop\\\":488," +
                "\\\"mode\\\":2},{\\\"start\\\":489,\\\"stop\\\":502,\\\"mode\\\":3},{\\\"start\\\":503," +
                "\\\"stop\\\":512,\\\"mode\\\":2},{\\\"start\\\":513,\\\"stop\\\":522,\\\"mode\\\":3}," +
                "{\\\"start\\\":523,\\\"stop\\\":568,\\\"mode\\\":2},{\\\"start\\\":569,\\\"stop\\\":581," +
                "\\\"mode\\\":3},{\\\"start\\\":582,\\\"stop\\\":638,\\\"mode\\\":2},{\\\"start\\\":639," +
                "\\\"stop\\\":654,\\\"mode\\\":3},{\\\"start\\\":655,\\\"stop\\\":665,\\\"mode\\\":2}]," +
                "\\\"ed\\\":1589943900,\\\"wk\\\":0,\\\"wc\\\":0},\\\"tz\\\":\\\"28800\\\"," +
                "\\\"stp\\\":{\\\"runCal\\\":1,\\\"cal\\\":6,\\\"conAct\\\":0,\\\"stage\\\":[],\\\"ttl\\\":" + step + ",\\\"dis\\\":144,\\\"rn\\\":0,\\\"wk\\\":5,\\\"runDist\\\":4,\\\"ncal\\\":0},\\\"v\\\":5,\\\"goal\\\":8000}\",\"data\":[{\"stop\":1439,\"value\":\"WhQAUA0AUAAAUAAAUAAAUAAAUAAAWhQAUAYAcBEAUAYAUA8AUAsAUAYAUDIAUCQAUDkAUCkAUD4AUC0AUFcAUD8AUCkAUCEAUCwAUCsAUB4AUCQAUBsAUCcAUBQAUDcAUBoAUCYAUFcAUCAAUDkAUCEAWhQAWhQAWhQAUBAAUEgAUDsAUAgAWhQAUDwAUCEAUAIAUAsAUDoAUD8AWhQAWhQAWhQAWhQAWhQAWhQAAS0QEAsAWhQAAR8SEBcHYC4AUCoAUBMAUAIAUAYAUAsAUCsAUAUAUBIAUBIAUBsAUBgAUAoAUBsAUBUAUBkAUDIAUC0AUC4AUBAAWhQAUCsAUB8AUAIAUB8AUDUAUEEAUDUAUBkAUCYAUEoAUCYAUBIAUCAAUCkAUDAAUB4AUB0AUDEAUCUAUCgAUAQAWhQAUA8AUDwAUB8AUCUAUBQAUB4AUAUAWhQAUAAAUA8AUBkAUCgAUCwAUCkAUCgAYCIAYCIAYCgAUAoAWhQAUBwAWhQAUBoAUDkAUD4AYAkAYAYAWhQAWhQAUB4AWhQAUAQAUBcAUBAAUAUAWhQAUB0AcBYAehQAcBoAehQAehQAehQAcAMAcAMAehQAcAIAehQAcBIAcA0AehQAehQAcAsAcAYAcAEAcAoAehQAehQAcAwAehQAehQAehQAcAEAehQAehQAcAsAehQAehQAcA8AcBkAcAYAcBkAcC0AcAQAcBsAcAMAWhQAUAMAWhQAUBEAUAIAWhQAWhQAWhQAehQAehQAehQAehQAehQAehQAcAAAcB8AcBMAehQAehQAcDkAcBAAcAEAcAMAcAMAcCwAcA8AcAAAcAAAcCIAcAAAcCcAcB4AehQAcAkAehQAcCMAehQAehQAcAoAehQAehQAehQAcBgAcBgAcAkAehQAcAcAcCgAcBQAcA0AcAwAcCcAcCkAcAAAUAAAUAAAUB4AUBwAUAAAUAAAUCkAUBIAUBMAUCgAUA8AUBEAUD0AUCAAYAMAYCkAUBsAUB4AYCgAahQAUBkAWhQAWhQAUCAAUBcAUA8AUBAAUAcAUB8AUCEAUCMAUCkAYAMAYAAAUBsAUBEAUBgAUAUAUB0AUAAAUAAAUAAAUAAAUAAAUAQAUAAAUAAAUAAAUAAAWwAAUAAAcAAAcAAAcAAAcAAAcAAAcAAAcA0AcAAAcAAAcAAAcAIAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcA8AehQAehQAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAEAeRMAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAsAcAAAcAAAcAAAcAAAcAAAcAoAcAAAcBMAcAAAcAAAcAAAcAAAcAAAcAAAcA4AcAcAehQAehQAcAAAcAAAcAIAehQAehQAcAAAcAAAcAAAcAAAcAAAcAIAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcBcAehQAehQAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAehQAcAMAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcBUAeQAAcAAAcAAAcAAAcFgAcAAAcAAAcAAAcBkAeQAAcAAAcAAAcAAAcAAAcE0AcAQAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAeVAAehQAehQAcAAAcAAAcAAAcAAAcAUAeRwAUAAAUFUAUAAAUAAAUAAAUAAAUAAAUCMAeQAAcAAAcAAAcE0AUAAAUAAAUAAAUAAAUAAAUAAAcAAAcAAAcAAAcE4AcAAAcAAAcAAAcAAAcAgAcBAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAkAcAAAcAAAcAAAcAAAcBwAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAYAcBAAeQAAcB8AeQAAcAAAcAAAcAAAeSoAcAAAcAAAcAAAcAAAcAAAcAsAcAAAeScAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcCAAcAAAUAAAUAAAUAAAUAAAUAAAUBEAehQAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcBwAehQAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcBYAcAAAcAAAcAAAcAYAcAAAcAAAcCsAcAAAcAAAcAgAcAAAcAAAcBsAeRQAcAAAcAAAcAEAcAAAcAAAcAAAcAAAcAAAcAAAcA8AcAAAcAAAcBoAcAAAcAEAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAQAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcBIAcAAAcA0AcBAAcAAAcAAAcAAAcAAAehQAehQAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcCgAcAAAcBkAcAAAcB0AcAAAcAAAcBgAcAAAUAEAUBsAWhQAUB4AWhQAUCkAWQ8AUCsAUA0AWTUAXBAAWhQAUBMAUAQAUAcAUAoAUA8AUBkAUBcAUCoAUAIAUBQAWhQAWhQAUBIAUBQAUAcAWhQAUBYAWhQAUAgAWhQAWhQAUAkAUE0AUHUAAWMTEEcKYDoAYAgAUAMAWhQAUAUAUAYAUAkAUB4AUAsAUAIAUBMAWhQAAVQdAWAlEDYAYCQAUAQAUBgAUAgAUAUAUBQAUAIAWhQAUAkAUAMAUA4AWhQAehQAcAoAcAIAehQAcB0AcCcAUCsAUAEAUAgAUAoAUAIAUAsAUAIAWhQAWhQAUAgAUA0AWhQAUAYAWhQAUAEAWhQAWhQAUBAAUBQAUBIAUBcAUAoAYBAAYAIAAUkZAUglAVYSYBcAYAoAYCAAYAsAUBUAUB0AUBAAUBEAUCAAUBUAUBYAUA0AUB4AUBcAUBsAUBMAUBUAYAsAYAwAYAsAUB4AUBoAUBoAUBoAUBQAUAcAWhQAUBgAUBkAUBsAUBUAUBAAUCAAUCYAUB8AUB4AUBwAUAcAUBsAUBwAUBwAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAA\",\"did\":\"321123\",\"tz\":32,\"src\":17,\"start\":0}],\"data_hr\":\"\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\",\"summary_hr\":\"{\\\"ct\\\":0,\\\"id\\\":[]}\",\"date\":\"" + dateTimeNow + "\"}]";
            
            FormUrlEncodedContent postContent = new FormUrlEncodedContent(new Dictionary<string, string>()
            {
              {"data_json", data},
              {"userid", userid},
              {"device_type", "0"},
              {"last_sync_data_time", "1589917081"},
              {"last_deviceid", "DA932FFFFE8816E7"}
            });
            var temp = await httpClient.PostAsync("https://api-mifit-cn.huami.com/v1/data/band_data.json?&t=" + await GetTime(), postContent);
            if (temp.IsSuccessStatusCode)
            {
                JObject result = JObject.Parse(await temp.Content.ReadAsStringAsync());
                if (result.GetValue("code") is JValue jValue && result.GetValue("message") is JValue jValue1)
                {
                    if (jValue?.Value?.ToString() == "1" && jValue1?.Value?.ToString() == "success")
                        return true;
                }
            }
            return false;
        }
    }
}
