using Newtonsoft.Json;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using System.Security.Principal;
using xiaomistep.Models;

namespace xiaomistep.HelperFiles
{
    /// <summary>
    /// 自动执行帮助类
    /// </summary>
    public class AutoHelper
    {
        private static string filePath = AppDomain.CurrentDomain.BaseDirectory + "Temp.txt";
        /// <summary>
        /// 文件路径
        /// </summary>
        public static string FilePath { get { return filePath; } }

        private static AutoHelper? instence;//唯一实例

        private static object o = new object();//锁

        private static List<AccountModel> accountModels = new List<AccountModel>();//账号信息



        private AutoHelper()
        {

        }
        /// <summary>
        /// 对自动刷步数集合中的项进行刷步数
        /// </summary>
        private async Task Do()
        {
            DateTime now = PlayNugetPackage.TimeHelper.Now;
            //每天九点钟之后执行自动刷步数
            if (now.Hour < 9 )
            {
                return;
            }
            List< AccountModel > tempModels=new List<AccountModel>(accountModels);
            
            while (tempModels.Count > 0)
            {
                now = PlayNugetPackage.TimeHelper.Now;
                var temp = tempModels[0];
                tempModels.RemoveAt(0);
                if (temp == null || temp.Account == null || temp.Step == null || temp.Password == null)
                    continue;
                //步数加上当前的号数，避免每天一样，支付宝不刷新
                var step = (temp.Step ?? 18001) + now.Day;
                if (RecordHelper.GetInstence().CheckRecord(temp.Account ?? "", step, now))
                {
                    var result = await new ChangeStepHelper().Start(temp.Account ?? "", temp.Password ?? "", step);
                    if (result)
                    {
                        LogsHelper.Info("账号:" + temp.Account + "自动执行成功(ps:步数=设置步数+当前几号)，步数：" + step);
                        await Task.Delay(new TimeSpan(0, 2, 0));//2分钟执行一个账号
                    }
                    else
                    {
                        LogsHelper.Error("账号:" + temp.Account + "自动执行失败");
                        await Task.Delay(new TimeSpan(0,2, 0));//2分钟执行一个账号
                    }
                }
            }
        }
        /// <summary>
        /// 初始化
        /// </summary>
        public void Init()
        {
            if (!System.IO.File.Exists(FilePath))
            {
                System.IO.File.Create(FilePath).Close();
            }
            string str = System.IO.File.ReadAllText(FilePath);
            accountModels = JsonConvert.DeserializeObject<List<AccountModel>>(str) ?? new List<AccountModel>();

            Task.Run(async () =>
            {
                while (true)
                {
                    await Do();
                    await Task.Delay(new TimeSpan(1, 0, 0));
                }
            });
        }
        /// <summary>
        /// 获取唯一实例
        /// </summary>
        /// <returns></returns>
        public static AutoHelper GetInstence()
        {
            if (instence == null)
            {
                lock (o)
                {
                    if (instence == null)
                    {
                        instence = new AutoHelper();
                    }
                }
            }
            return instence;
        }
        /// <summary>
        /// 添加自动执行账号
        /// </summary>
        /// <param name="acc"></param>
        /// <param name="pwd"></param>
        /// <param name="step"></param>
        /// <returns></returns>
        public async Task<string> AddAcc(string acc, string pwd, int step)
        {
            var now = PlayNugetPackage.TimeHelper.Now;
            bool isEmail = false;
            if (acc.Contains("@"))
            {
                isEmail = true;
            }
            var accountModel = accountModels.FirstOrDefault(m => m.Account == acc&&pwd==m.Password);
            if (accountModel != null)
            {
                accountModel.Step = step;
                System.IO.File.WriteAllText(filePath, JsonConvert.SerializeObject(accountModels));
                LogsHelper.Info("账号:" + acc + "步数更新成功");
                return "账号:" + acc + "步数更新成功";
            }
            if (string.IsNullOrEmpty(await ChangeStepHelper.LoginAndGetCode(acc, pwd, isEmail)))
            {
                LogsHelper.Error("账号或者密码有误，添加失败");
                return "账号或者密码有误，添加失败";
            }
            accountModels.Add(new AccountModel() { Account = acc, Password = pwd, Step = step });
            LogsHelper.Info("账号:" + acc + "添加成功");
            System.IO.File.WriteAllText(filePath, JsonConvert.SerializeObject(accountModels));

            if (RecordHelper.GetInstence().CheckRecord(acc, step, now))
            {
                var result = await new ChangeStepHelper().Start(acc, pwd, step);
                if (result)
                {
                    LogsHelper.Info("账号:" + acc + "执行成功(ps:步数=设置步数+当前几号)，步数：" + step);
                }
                else
                {
                    LogsHelper.Error("账号:" + acc + "执行失败");
                }
            }

            return "账号:" + acc + "添加成功";
        }
        /// <summary>
        /// 删除账号
        /// </summary>
        /// <param name="acc"></param>
        /// <returns></returns>
        public string DelAcc(string acc, string pwd)
        {
            var accountModel = accountModels.FirstOrDefault(m => m.Account == acc);
            if (accountModel != null)
            {
                if (accountModel.Password != pwd)
                {
                    return "账号:" + acc + "删除失败,密码错误";
                }
                accountModels.Remove(accountModel);
                System.IO.File.WriteAllText(filePath, JsonConvert.SerializeObject(accountModels));
                LogsHelper.Info("账号:" + acc + "删除成功");
                return "账号:" + acc + "删除成功";
            }
            LogsHelper.Error("账号:" + acc + "删除失败，无此账号");
            return "账号:" + acc + "删除失败，无此账号";
        }

        public IList<string> GetAllAcc()
        {
            List<string> acc = new List<string>();
            foreach (var item in accountModels)
            {
                acc.Add("账号：" + item.Account + "---步数：" + item.Step);
            }
            return acc;
        }
    }
}
