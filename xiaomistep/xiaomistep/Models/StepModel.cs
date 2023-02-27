namespace xiaomistep.Models
{
    public class StepModel
    {
        /// <summary>
        /// 总步数
        /// </summary>
        public int ttl { get; set; }
        /// <summary>
        /// 总公里数
        /// </summary>
        public int dis { get; set; }
        /// <summary>
        /// 总能量
        /// </summary>
        public int cal { get; set; }
        public int wk { get; set; }
        public int rn { get; set; }
        public int runDist { get; set; }
        public int runCal { get; set; }
        public List<StepModelItem> stage { get; set; } = new List<StepModelItem>();
        private StepModel()
        {

        }


        public static StepModel SetStep(int step)
        {
            StepModel result = new StepModel();
            result.ttl = step;
            //上一次的运动
            StepModelItem? last = null;
            Random random = new Random();

            while (step > 0)
            {
                StepModelItem item = StepModelItem.GetModelItem(last);
                if (step < 1000)
                {
                    item.step = step;
                    item.mode = 1;
                    item.dis = (int)(step * (random.Next(70, 80) / 100.0));
                    item.cal = random.Next(4, 20);
                    result.stage.Add(item);
                    step = 0;
                    break;
                }

                int tempStep = 1000;
                switch (item.mode)
                {
                    case 1:
                        tempStep = random.Next(200, 1000);
                        item.dis = (int)(tempStep * (random.Next(70, 80) / 100.0));
                        item.cal = random.Next(4, 20);
                        item.step = tempStep;
                        step = step - tempStep;
                        result.stage.Add(item);
                        break;
                    case 3:
                        tempStep = random.Next(1000, 2000);
                        item.dis = (int)(tempStep * (random.Next(85, 90) / 100.0));
                        item.cal = random.Next(50, 100);
                        item.step = tempStep;
                        step = step - tempStep;
                        result.stage.Add(item);
                        break;
                    case 4:
                        tempStep = random.Next(2000, 3500);
                        item.dis = (int)(tempStep * (random.Next(85, 90) / 100.0));
                        item.cal = random.Next(80, 150);
                        item.step = tempStep;
                        step = step - tempStep;
                        result.stage.Add(item);
                        break;
                }

                last = item;
            }

            int dis = 0;
            int cal = 0;
            foreach (var item in result.stage)
            {
                dis += item.dis;
                cal += item.cal;
            }
            result.dis = dis;
            result.cal = cal;


            result.rn = 50;
            result.wk = 41;
            result.runDist = 7654;
            result.runCal = 397;

            return result;
        }
    }
    public class StepModelItem
    {
        /// <summary>
        /// 开始运动的时间
        /// </summary>
        public int start { get; set; }
        /// <summary>
        /// 结束运动的时间
        /// </summary>
        public int stop { get; set; }
        /// <summary>
        /// 运动方式 1：慢走 3：快走 4：跑步
        /// </summary>
        public int mode { get; set; }
        /// <summary>
        /// 公里数
        /// </summary>
        public int dis { get; set; }
        /// <summary>
        /// 消耗的能量
        /// </summary>
        public int cal { get; set; }
        /// <summary>
        /// 本次运动步数
        /// </summary>
        public int step { get; set; }

        private StepModelItem()
        {

        }
        public static StepModelItem GetModelItem(StepModelItem? stepModelItem = null)
        {
            StepModelItem result = new StepModelItem();
            if (stepModelItem == null)
            {
                //表示从5点27开始
                result.start = 327;
            }
            else
            {
                result.start = stepModelItem.stop + 1;
            }
            result.stop = new Random().Next(1, 20) + result.start;

            int[] modes = new int[] { 1, 3, 4, 3, 4, 4 };
            result.mode = modes[new Random().Next(modes.Length)];

            return result;
        }
    }
}
