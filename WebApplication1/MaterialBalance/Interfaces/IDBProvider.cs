using MaterialBalance.API.Request;


namespace MaterialBalance.Interfaces;

public interface IDBProvider
{
    /// <summary>
    /// Создание БД и таблиц
    /// </summary>
    /// <returns></returns>
    Task Initialize();
    
    
    /// <summary>
    /// Заполнение таблицы flows.
    /// </summary>
    /// <param name="flows"></param>
    /// <returns></returns>
    Task AddFlows(IEnumerable<Flow> flows);
    
    /// <summary>
    /// Удаление записей из таблицы flows.
    /// </summary>
    /// <param name="flowsId"></param>
    /// <returns></returns>
    Task DeleteFlows(IEnumerable<Guid> flowsId);
    
    /// <summary>
    /// Получение потоков по ID
    /// </summary>
    /// <param name="flowsId"></param>
    /// <returns></returns>
    Task<IEnumerable<Flow>> GetFlows(IEnumerable<Guid> flowsId);
    
    Task<Guid> CreateSolverTask(IEnumerable<Guid> flowsId);
    Task<SolverTask?> GetSolverTask(Guid? taskId);
    Task<SolverTask?> GetPendingSolverTask();  
    Task<bool> HasProcessingSolverTask();  
    Task UpdateSolverTaskStatus(Guid taskId, StatusType status, SolverResult? result = null);
}