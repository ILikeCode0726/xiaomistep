﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.Intrinsics.X86;
using System.Security.Principal;
using xiaomistep.HelperFiles;

namespace xiaomistep.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StepChangeController : ControllerBase
    {
        ChangeStepHelper helper = new ChangeStepHelper();
        [HttpPost("Change")]
        public async Task<string> Change([FromForm]string acc, [FromForm] string pwd, [FromForm] string step)
        {
            if(!int.TryParse(step,out int ste))
            {
                return "步数错误";
            }
            if (!SingleTon.GetInstance().AddRecord(acc, ste))
            {
                //return "步数必须大于上一次";
            }
            var result = await helper.Start(acc, pwd, ste);
            if (result)
            {
                LogsHelper.Info("账号:" + acc + "执行成功");
                return "执行成功";
            }
            LogsHelper.Error("账号:" + acc + "执行失败");
            return "执行失败";
        }
        [HttpPost("AddAccountAuto")]
        public async Task<string> AddAccountAuto([FromForm] string acc, [FromForm] string pwd, [FromForm] string step)
        {
            if(int.TryParse(step,out int ste))
            {
                return await AutoHelper.GetInstence().AddAcc(acc, pwd, ste);
            }
            return "添加失败";
        }

        [HttpPost("DelAccountAuto")]
        public string DelAccountAuto([FromForm] string acc)
        {
            return AutoHelper.GetInstence().DelAcc(acc);
        }
        [HttpGet("GetAllAccountAuto")]
        public string GetAllAccountAuto()
        {
            string result = string.Empty;
            foreach (var item in AutoHelper.GetInstence().GetAllAcc())
            {
                result += item + "\n";
            }
             return result;
        }
    }
    
}
