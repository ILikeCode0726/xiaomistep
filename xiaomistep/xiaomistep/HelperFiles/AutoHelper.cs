using Newtonsoft.Json;
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
        private async void Do()
        {
            for (int i = 0; i < accountModels.Count; i++)
            {
                if (accountModels[i] == null||accountModels[i].Account == null || accountModels[i].Step == null || accountModels[i].Password == null)
                    continue;
                if (RecordHelper.GetInstence().CheckRecord(accountModels[i].Account, accountModels[i].Step ?? 18001))
                {
                    var result = await new ChangeStepHelper().Start(accountModels[i].Account, accountModels[i].Password, accountModels[i].Step ?? 18001);
                    if (result)
                    {
                        LogsHelper.Info("账号:" + accountModels[i].Account + "自动执行成功");
                    }
                    else
                    {
                        LogsHelper.Error("账号:" + accountModels[i].Account + "自动执行失败");
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
                    //每天五点钟之后执行自动刷步数
                    if (TimeHelper.DateTimeNow.Hour > 5)
                    {
                        Do();
                    }
                    await Task.Delay(new TimeSpan(0, 30, 0));
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
            bool isEmail = false;
            if (acc.Contains("@"))
            {
                isEmail = true;
            }
            var accountModel = accountModels.FirstOrDefault(m => m.Account == acc);
            if (accountModel != null && accountModel.Step >= step)
            {
                LogsHelper.Error("账号:" + acc + "添加失败，该账号已存在并且新的步数小于之前的");
                return "账号已存在并且步数小于之前的";
            }
            else if (accountModel != null && accountModel.Step < step)
            {
                accountModel.Step = step;
                System.IO.File.WriteAllText(filePath, JsonConvert.SerializeObject(accountModels));
                LogsHelper.Info("账号:" + acc + "步数更新成功");
                return "账号:" + acc + "步数更新成功";
            }
            if(string.IsNullOrEmpty(await ChangeStepHelper.LoginAndGetCode(acc, pwd, isEmail)))
            {
                LogsHelper.Error("账号或者密码有误，添加失败");
                return "账号或者密码有误，添加失败";
            }
            accountModels.Add(new AccountModel() { Account = acc, Password = pwd, Step = step });
            Do();
            LogsHelper.Info("账号:" + acc + "添加成功");
            System.IO.File.WriteAllText(filePath, JsonConvert.SerializeObject(accountModels));
            return "账号:" + acc + "添加成功";
        }
        /// <summary>
        /// 删除账号
        /// </summary>
        /// <param name="acc"></param>
        /// <returns></returns>
        public string DelAcc(string acc,string pwd)
        {
            var accountModel = accountModels.FirstOrDefault(m => m.Account == acc);
            if(accountModel != null)
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
            return accountModels.Select(m => m.Account??string.Empty).ToList();
        }
    }
}
