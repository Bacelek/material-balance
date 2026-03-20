using MaterialBalance.Configurations;
using MaterialBalance.Interfaces;
using MaterialBalance.Providers;
using MaterialBalance.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IDBProvider, PostgresBdProvider>();
builder.Services.AddScoped<IGraphService, GraphService>();

builder.Services.Configure<Config>(builder.Configuration.GetSection(nameof(Config)));

WebApplication app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

if (app.Environment.IsDevelopment())
{
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();


app.Run();