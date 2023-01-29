using xiaomistep.HelperFiles;

LogsHelper.Init();
AutoHelper.GetInstence().Init();
SingleTon.GetInstance().Init();


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseAuthorization();

app.MapControllers();

app.Run();
