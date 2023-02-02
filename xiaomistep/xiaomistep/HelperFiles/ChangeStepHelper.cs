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
            var result= await ChangeStep(apptoken, temp.Item2, step);
            //执行成功增加记录
            if (result)
            {
                RecordHelper.GetInstence().AddRecord(account, step);
            }
            return result;
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
            long last_sync_data_time =TimeHelper.Get1970ToNowSeconds  - 13081;//13081=3小时38分钟1秒，now-1970.1.1在往前推几小时

            HttpClient httpClient = CreatHttpClient(new List<(string, string)> { ("UserAgent", "Mozilla/5.0 (iPhone; CPU iPhone OS 13_4_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148 MicroMessenger/7.0.12(0x17000c2d) NetType/WIFI Language/zh_CN"), ("apptoken", app_token) });
            string dateTimeNow = DateTime.UtcNow.AddHours(8).ToString("yyyy-M-d");
            string data1 = "[{\"data_hr\":\"\\/\\/\\/\\/\\/\\/9L\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/Vv\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/0v\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/9e\\/\\/\\/\\/\\/0n\\/a\\/\\/\\/S\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/0b\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/1FK\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/R\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/9PTFFpaf9L\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/R\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/0j\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/9K\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/Ov\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/zf\\/\\/\\/86\\/zr\\/Ov88\\/zf\\/Pf\\/\\/\\/0v\\/S\\/8\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/Sf\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/z3\\/\\/\\/\\/\\/\\/0r\\/Ov\\/\\/\\/\\/\\/\\/S\\/9L\\/zb\\/Sf9K\\/0v\\/Rf9H\\/zj\\/Sf9K\\/0\\/\\/N\\/\\/\\/\\/0D\\/Sf83\\/zr\\/Pf9M\\/0v\\/Ov9e\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/S\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/zv\\/\\/z7\\/O\\/83\\/zv\\/N\\/83\\/zr\\/N\\/86\\/z\\/\\/Nv83\\/zn\\/Xv84\\/zr\\/PP84\\/zj\\/N\\/9e\\/zr\\/N\\/89\\/03\\/P\\/89\\/z3\\/Q\\/9N\\/0v\\/Tv9C\\/0H\\/Of9D\\/zz\\/Of88\\/z\\/\\/PP9A\\/zr\\/N\\/86\\/zz\\/Nv87\\/0D\\/Ov84\\/0v\\/O\\/84\\/zf\\/MP83\\/zH\\/Nv83\\/zf\\/N\\/84\\/zf\\/Of82\\/zf\\/OP83\\/zb\\/Mv81\\/zX\\/R\\/9L\\/0v\\/O\\/9I\\/0T\\/S\\/9A\\/zn\\/Pf89\\/zn\\/Nf9K\\/07\\/N\\/83\\/zn\\/Nv83\\/zv\\/O\\/9A\\/0H\\/Of8\\/\\/zj\\/PP83\\/zj\\/S\\/87\\/zj\\/Nv84\\/zf\\/Of83\\/zf\\/Of83\\/zb\\/Nv9L\\/zj\\/Nv82\\/zb\\/N\\/85\\/zf\\/N\\/9J\\/zf\\/Nv83\\/zj\\/Nv84\\/0r\\/Sv83\\/zf\\/MP\\/\\/\\/zb\\/Mv82\\/zb\\/Of85\\/z7\\/Nv8\\/\\/0r\\/S\\/85\\/0H\\/QP9B\\/0D\\/Nf89\\/zj\\/Ov83\\/zv\\/Nv8\\/\\/0f\\/Sv9O\\/0ZeXv\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/1X\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/9B\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/TP\\/\\/\\/1b\\/\\/\\/\\/\\/\\/0\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/9N\\/\\/\\/\\/\\/\\/\\/\\/\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\\/v7+\",\"date\":\""+ dateTimeNow + "\",\"data\":[{\"start\":0,\"stop\":1439,\"value\":\"UA8AUBQAUAwAUBoAUAEAYCcAUBkAUB4AUBgAUCAAUAEAUBkAUAwAYAsAYB8AYB0AYBgAYCoAYBgAYB4AUCcAUBsAUB8AUBwAUBIAYBkAYB8AUBoAUBMAUCEAUCIAYBYAUBwAUCAAUBgAUCAAUBcAYBsAYCUAATIPYD0KECQAYDMAYB0AYAsAYCAAYDwAYCIAYB0AYBcAYCQAYB0AYBAAYCMAYAoAYCIAYCEAYCYAYBsAYBUAYAYAYCIAYCMAUB0AUCAAUBYAUCoAUBEAUC8AUB0AUBYAUDMAUDoAUBkAUC0AUBQAUBwAUA0AUBsAUAoAUCEAUBYAUAwAUB4AUAwAUCcAUCYAUCwKYDUAAUUlEC8IYEMAYEgAYDoAYBAAUAMAUBkAWgAAWgAAWgAAWgAAWgAAUAgAWgAAUBAAUAQAUA4AUA8AUAkAUAIAUAYAUAcAUAIAWgAAUAQAUAkAUAEAUBkAUCUAWgAAUAYAUBEAWgAAUBYAWgAAUAYAWgAAWgAAWgAAWgAAUBcAUAcAWgAAUBUAUAoAUAIAWgAAUAQAUAYAUCgAWgAAUAgAWgAAWgAAUAwAWwAAXCMAUBQAWwAAUAIAWgAAWgAAWgAAWgAAWgAAWgAAWgAAWgAAWREAWQIAUAMAWSEAUDoAUDIAUB8AUCEAUC4AXB4AUA4AWgAAUBIAUA8AUBAAUCUAUCIAUAMAUAEAUAsAUAMAUCwAUBYAWgAAWgAAWgAAWgAAWgAAWgAAUAYAWgAAWgAAWgAAUAYAWwAAWgAAUAYAXAQAUAMAUBsAUBcAUCAAWwAAWgAAWgAAWgAAWgAAUBgAUB4AWgAAUAcAUAwAWQIAWQkAUAEAUAIAWgAAUAoAWgAAUAYAUB0AWgAAWgAAUAkAWgAAWSwAUBIAWgAAUC4AWSYAWgAAUAYAUAoAUAkAUAIAUAcAWgAAUAEAUBEAUBgAUBcAWRYAUA0AWSgAUB4AUDQAUBoAXA4AUA8AUBwAUA8AUA4AUA4AWgAAUAIAUCMAWgAAUCwAUBgAUAYAUAAAUAAAUAAAUAAAUAAAUAAAUAAAUAAAUAAAWwAAUAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAeSEAeQ8AcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcBcAcAAAcAAAcCYOcBUAUAAAUAAAUAAAUAAAUAUAUAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcCgAeQAAcAAAcAAAcAAAcAAAcAAAcAYAcAAAcBgAeQAAcAAAcAAAegAAegAAcAAAcAcAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcCkAeQAAcAcAcAAAcAAAcAwAcAAAcAAAcAIAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcCIAeQAAcAAAcAAAcAAAcAAAcAAAeRwAeQAAWgAAUAAAUAAAUAAAUAAAUAAAcAAAcAAAcBoAeScAeQAAegAAcBkAeQAAUAAAUAAAUAAAUAAAUAAAUAAAcAAAcAAAcAAAcAAAcAAAcAAAegAAegAAcAAAcAAAcBgAeQAAcAAAcAAAcAAAcAAAcAAAcAkAegAAegAAcAcAcAAAcAcAcAAAcAAAcAAAcAAAcA8AeQAAcAAAcAAAeRQAcAwAUAAAUAAAUAAAUAAAUAAAUAAAcAAAcBEAcA0AcAAAWQsAUAAAUAAAUAAAUAAAUAAAcAAAcAoAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAYAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcBYAegAAcAAAcAAAegAAcAcAcAAAcAAAcAAAcAAAcAAAeRkAegAAegAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAEAcAAAcAAAcAAAcAUAcAQAcAAAcBIAeQAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcBsAcAAAcAAAcBcAeQAAUAAAUAAAUAAAUAAAUAAAUBQAcBYAUAAAUAAAUAoAWRYAWTQAWQAAUAAAUAAAUAAAcAAAcAAAcAAAcAAAcAAAcAMAcAAAcAQAcAAAcAAAcAAAcDMAeSIAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcAAAcBQAeQwAcAAAcAAAcAAAcAMAcAAAeSoAcA8AcDMAcAYAeQoAcAwAcFQAcEMAeVIAaTYAbBcNYAsAYBIAYAIAYAIAYBUAYCwAYBMAYDYAYCkAYDcAUCoAUCcAUAUAUBAAWgAAYBoAYBcAYCgAUAMAUAYAUBYAUA4AUBgAUAgAUAgAUAsAUAsAUA4AUAMAUAYAUAQAUBIAASsSUDAAUDAAUBAAYAYAUBAAUAUAUCAAUBoAUCAAUBAAUAoAYAIAUAQAUAgAUCcAUAsAUCIAUCUAUAoAUA4AUB8AUBkAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAAfgAA\",\"tz\":32,\"did\":\"DA932FFFFE8816E7\",\"src\":24}],\"summary\":\"{\\\"v\\\":6,\\\"slp\\\":{\\\"st\\\":1628296479,\\\"ed\\\":1628296479,\\\"dp\\\":0,\\\"lt\\\":0,\\\"wk\\\":0,\\\"usrSt\\\":-1440,\\\"usrEd\\\":-1440,\\\"wc\\\":0,\\\"is\\\":0,\\\"lb\\\":0,\\\"to\\\":0,\\\"dt\\\":0,\\\"rhr\\\":0,\\\"ss\\\":0},\\\"stp\\\":{\\\"ttl\\\":"+step+",\\\"dis\\\":10627,\\\"cal\\\":510,\\\"wk\\\":41,\\\"rn\\\":50,\\\"runDist\\\":7654,\\\"runCal\\\":397,\\\"stage\\\":[{\\\"start\\\":327,\\\"stop\\\":341,\\\"mode\\\":1,\\\"dis\\\":481,\\\"cal\\\":13,\\\"step\\\":680},{\\\"start\\\":342,\\\"stop\\\":367,\\\"mode\\\":3,\\\"dis\\\":2295,\\\"cal\\\":95,\\\"step\\\":2874},{\\\"start\\\":368,\\\"stop\\\":377,\\\"mode\\\":4,\\\"dis\\\":1592,\\\"cal\\\":88,\\\"step\\\":1664},{\\\"start\\\":378,\\\"stop\\\":386,\\\"mode\\\":3,\\\"dis\\\":1072,\\\"cal\\\":51,\\\"step\\\":1245},{\\\"start\\\":387,\\\"stop\\\":393,\\\"mode\\\":4,\\\"dis\\\":1036,\\\"cal\\\":57,\\\"step\\\":1124},{\\\"start\\\":394,\\\"stop\\\":398,\\\"mode\\\":3,\\\"dis\\\":488,\\\"cal\\\":19,\\\"step\\\":607},{\\\"start\\\":399,\\\"stop\\\":414,\\\"mode\\\":4,\\\"dis\\\":2220,\\\"cal\\\":120,\\\"step\\\":2371},{\\\"start\\\":415,\\\"stop\\\":427,\\\"mode\\\":3,\\\"dis\\\":1268,\\\"cal\\\":59,\\\"step\\\":1489},{\\\"start\\\":428,\\\"stop\\\":433,\\\"mode\\\":1,\\\"dis\\\":152,\\\"cal\\\":4,\\\"step\\\":238},{\\\"start\\\":434,\\\"stop\\\":444,\\\"mode\\\":3,\\\"dis\\\":2295,\\\"cal\\\":95,\\\"step\\\":2874},{\\\"start\\\":445,\\\"stop\\\":455,\\\"mode\\\":4,\\\"dis\\\":1592,\\\"cal\\\":88,\\\"step\\\":1664},{\\\"start\\\":456,\\\"stop\\\":466,\\\"mode\\\":3,\\\"dis\\\":1072,\\\"cal\\\":51,\\\"step\\\":1245},{\\\"start\\\":467,\\\"stop\\\":477,\\\"mode\\\":4,\\\"dis\\\":1036,\\\"cal\\\":57,\\\"step\\\":1124},{\\\"start\\\":478,\\\"stop\\\":488,\\\"mode\\\":3,\\\"dis\\\":488,\\\"cal\\\":19,\\\"step\\\":607},{\\\"start\\\":489,\\\"stop\\\":499,\\\"mode\\\":4,\\\"dis\\\":2220,\\\"cal\\\":120,\\\"step\\\":2371},{\\\"start\\\":500,\\\"stop\\\":511,\\\"mode\\\":3,\\\"dis\\\":1268,\\\"cal\\\":59,\\\"step\\\":1489},{\\\"start\\\":512,\\\"stop\\\":522,\\\"mode\\\":1,\\\"dis\\\":152,\\\"cal\\\":4,\\\"step\\\":238}]},\\\"goal\\\":8000,\\\"tz\\\":\\\"28800\\\"}\",\"source\":24,\"type\":0}]";
            FormUrlEncodedContent postContent = new FormUrlEncodedContent(new Dictionary<string, string>()
            {
              {"data_json", data1},
              {"userid", userid},
              {"device_type", "0"},
              {"last_sync_data_time", last_sync_data_time.ToString()},
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
