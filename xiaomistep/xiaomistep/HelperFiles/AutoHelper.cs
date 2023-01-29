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
        private static string logsPath = AppDomain.CurrentDomain.BaseDirectory + "logs.txt";
        /// <summary>
        /// 文件路径
        /// </summary>
        public static string FilePath { get { return filePath; } }

        private static AutoHelper? instence;//唯一实例

        private static object o = new object();//锁

        private static List<AccountModel> accountModels=new List<AccountModel>();//账号信息

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
            if (!System.IO.File.Exists(logsPath))
            {
                System.IO.File.Create(logsPath).Close();
            }
            string str=System.IO.File.ReadAllText(FilePath);
            accountModels = JsonConvert.DeserializeObject<List<AccountModel>>(str) ?? new List<AccountModel>();

            Task.Run(async () =>
            {
                while (true)
                {
                    foreach (var item in accountModels)
                    {
                        if (item.Account == null || item.Step == null || item.Password == null)
                            continue;
                        if (SingleTon.GetInstance().AddRecord(item.Account, item.Step??18001))
                        {
                            var result = await new ChangeStepHelper().Start(item.Account, item.Password, item.Step ?? 18001);
                            if (result)
                            {
                                LogsHelper.Info("账号:" + item.Account + "执行成功");
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

        public async void AddAcc(string acc,string pwd,int step)
        {
            if (SingleTon.GetInstance().AddRecord(acc, step))
            {
                var result= await new ChangeStepHelper().Start(acc, pwd, step);
                if (result)
                {
                    LogsHelper.Info("账号:" + acc + "执行成功");
                }
            }
            accountModels.Add(new AccountModel() { Account = acc, Password = pwd, Step = step });
        }
    }
}
