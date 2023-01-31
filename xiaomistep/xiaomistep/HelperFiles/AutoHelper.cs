using Newtonsoft.Json;
using System.Runtime.Intrinsics.X86;
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
                    foreach (var item in accountModels)
                    {
                        if (item.Account == null || item.Step == null || item.Password == null)
                            continue;
                        if (SingleTon.GetInstance().AddRecord(item.Account, item.Step ?? 18001))
                        {
                            var result = await new ChangeStepHelper().Start(item.Account, item.Password, item.Step ?? 18001);
                            if (result)
                            {
                                LogsHelper.Info("账号:" + item.Account + "自动执行成功");
                            }
                        }
                    }
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
            if (SingleTon.GetInstance().AddRecord(acc, step))
            {
                var result = await new ChangeStepHelper().Start(acc, pwd, step);
                if (result)
                {
                    LogsHelper.Info("账号:" + acc + "步数修改执行成功");
                }
            }
            accountModels.Add(new AccountModel() { Account = acc, Password = pwd, Step = step });
            LogsHelper.Info("账号:" + acc + "添加成功");
            System.IO.File.WriteAllText(filePath, JsonConvert.SerializeObject(accountModels));
            return "账号:" + acc + "添加成功";
        }
        /// <summary>
        /// 删除账号
        /// </summary>
        /// <param name="acc"></param>
        /// <returns></returns>
        public string DelAcc(string acc)
        {
            var accountModel = accountModels.FirstOrDefault(m => m.Account == acc);
            if(accountModel != null)
            {
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
