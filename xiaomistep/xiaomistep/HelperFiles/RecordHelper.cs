using Newtonsoft.Json;
using xiaomistep.Models;

namespace xiaomistep.HelperFiles
{
    public class RecordHelper
    {
        private static string filePath = AppDomain.CurrentDomain.BaseDirectory + "record.txt";

        private static RecordHelper? instence;//唯一实例

        private static object o = new object();//锁

        private static List<AccountModel> records = new List<AccountModel>();//记录

        private RecordHelper()
        {

        }


        /// <summary>
        /// 初始化
        /// </summary>
        public void Init()
        {
            if (!System.IO.File.Exists(filePath))
            {
                System.IO.File.Create(filePath).Close();
            }
            string str = System.IO.File.ReadAllText(filePath);
            records = JsonConvert.DeserializeObject<List<AccountModel>>(str) ?? new List<AccountModel>();

            //List<AccountModel> accountModels = new List<AccountModel>();
            //accountModels.Add(new AccountModel() { Account = "1", Password = "1", Step = 1, Time = DateTime.UtcNow.AddHours(8) });
            //accountModels.Add(new AccountModel() { Account = "2", Password = "2", Step = 2, Time = DateTime.UtcNow.AddHours(9).AddSeconds(44) });
            //accountModels.Add(new AccountModel() { Account = "3", Password = "3", Step = 3, Time = DateTime.UtcNow.AddHours(10).AddSeconds(20) });
            //accountModels.Add(new AccountModel() { Account = "3", Password = "3", Step = 4, Time = DateTime.UtcNow.AddHours(10).AddSeconds(22) });
            //accountModels.Add(new AccountModel() { Account = "3", Password = "3", Step = 6, Time = DateTime.UtcNow.AddHours(9).AddSeconds(1) });
            //accountModels.Add(new AccountModel() { Account = "4", Password = "4", Step = 4, Time = DateTime.UtcNow.AddHours(11).AddSeconds(30) });


            //清理记录，防止文件越来越大
            Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(new TimeSpan(1, 0, 0));

                    List<AccountModel> rTemp=new List<AccountModel>();
                    foreach (var item in records)
                    {
                        if (item.Time.HasValue)
                        {
                            if (item.Time.Value.ToUniversalTime().Ticks < TimeHelper.DateNow.ToUniversalTime().Ticks)
                            {
                                rTemp.Add(item);
                            }
                        }
                    }

                    foreach (var item in rTemp)
                    {
                        records.Remove(item);
                    }
                    File.WriteAllText(filePath, JsonConvert.SerializeObject(records));
                }
            });
        }

        /// <summary>
        /// 获取唯一实例
        /// </summary>
        /// <returns></returns>
        public static RecordHelper GetInstence()
        {
            if (instence == null)
            {
                lock (o)
                {
                    if (instence == null)
                    {
                        instence = new RecordHelper();
                    }
                }
            }
            return instence;
        }

        /// <summary>
        /// 检查当天是否已经刷步数
        /// </summary>
        /// <param name="acc"></param>
        /// <param name="step"></param>
        /// <returns></returns>
        public bool CheckRecord(string acc, int step)
        {
            DateTime time = TimeHelper.DateTimeNow;

            var firstR = records.Where(m => m.Account == acc).MaxBy(m => m.Time);//最新的记录
            if (firstR == null)
            {
                return true;
            }
            if(firstR.Time.HasValue&& DateTime.Equals(firstR.Time.Value.Date, time.Date)&&firstR.Step<step)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 添加一条记录
        /// </summary>
        /// <param name="acc">账号</param>
        /// <param name="step">步数</param>
        public void AddRecord(string acc, int step)
        {
            DateTime time = TimeHelper.DateTimeNow;

            var firstR = records.Where(m => m.Account == acc).MaxBy(m => m.Time);//最新的记录
            if (firstR == null)
            {
                records.Add(new AccountModel() { Account = acc, Step = step, Time = time });
            }
            else
            {
                firstR.Time = time;
                firstR.Step = step;
            }
            File.WriteAllText(filePath, JsonConvert.SerializeObject(records));
        }
    }
}
