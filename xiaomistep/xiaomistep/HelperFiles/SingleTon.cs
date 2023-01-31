using System.Collections.Generic;

namespace xiaomistep.HelperFiles
{
    public class SingleTon
    {
        private static SingleTon? instence;
        private static object o = new object();
        IList<(string, int,DateTime)> records = new List<(string, int, DateTime)>();
        private SingleTon()
        {

        }

        public void Init()
        {
            Task.Run(async() =>
            {
                while (true)
                {
                    DateTime time = DateTime.Now;
                    if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                    {
                        time = DateTime.Now;
                    }
                    else if (Environment.OSVersion.Platform == PlatformID.Unix)
                    {
                        time = DateTime.Now.AddHours(8);
                    }
                    time = DateTime.Parse(time.ToString("D"));

                    IList < (string, int, DateTime) > temp=new List<(string, int, DateTime)>();
                    foreach (var item in records)
                    {
                        if(item.Item3!= time)
                        {
                            temp.Add(item);
                        }
                    }
                    foreach (var item in temp)
                    {
                        records.Remove(item);
                    }
                    await Task.Delay(1000);
                }
            });
        }

        public static SingleTon GetInstance()
        {
            if (instence == null)
            {
                lock (o)
                {
                    if(instence== null)
                    {
                        instence = new SingleTon();
                    }
                }
            }
            return instence;
        }
        /// <summary>
        /// 添加当天的记录
        /// </summary>
        /// <param name="acc"></param>
        /// <param name="step"></param>
        /// <returns></returns>
        public bool AddRecord(string acc,int step)
        {
            if (string.IsNullOrEmpty(acc))
            {
                return false;
            }
            if (step <= 0)
            {
                return false;
            }
            DateTime time = DateTime.Now;
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                time = DateTime.Now;
            }
            else if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                time = DateTime.Now.AddHours(8);
            }
            //六点钟之前不执行
            if (time.Hour < 6)
            {
                return false;
            }
            time = DateTime.Parse(time.ToString("D"));
            if (records.Where(m => m.Item1 == acc && m.Item2 >= step && m.Item3 == time).Count() > 0)
                return false;
            records.Add((acc,step, time));
            return true;
        }
    }
}
