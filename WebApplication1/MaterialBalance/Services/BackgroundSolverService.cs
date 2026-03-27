using System.Data.Common;
using System.Text;
using MaterialBalance.Configurations;
using MaterialBalance.Interfaces;
using Microsoft.Extensions.Options;
using Npgsql;
using MaterialBalance.API.Request;

namespace MaterialBalance.Services;

public class BackgroundSolverService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    
    public BackgroundSolverService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var dbProvider = scope.ServiceProvider.GetRequiredService<IDBProvider>();
                    
                    if (await dbProvider.HasProcessingSolverTask())
                    {
                        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                        continue;
                    }

                    var task = await dbProvider.GetPendingSolverTask();
                    if (task == null)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                        continue;
                    }

                    await dbProvider.UpdateSolverTaskStatus(task.Id, StatusType.Processing);

                    await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

                    await dbProvider.UpdateSolverTaskStatus(task.Id, StatusType.Completed, ResultType.Solved);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
    
}