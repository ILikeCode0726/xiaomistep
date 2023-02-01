using xiaomistep.HelperFiles;

LogsHelper.Init();
AutoHelper.GetInstence().Init();
RecordHelper.GetInstence().Init();


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseAuthorization();

app.MapControllers();

#region ����HTML��̬ҳ��Ϊ����ҳ��   ɾ�� launchSettings.json �� launchUrl
DefaultFilesOptions defaultFilesOptions = new DefaultFilesOptions();
defaultFilesOptions.DefaultFileNames.Clear();
defaultFilesOptions.DefaultFileNames.Add("Index.html");
app.UseDefaultFiles(defaultFilesOptions);
app.UseStaticFiles();
#endregion

app.Run();
