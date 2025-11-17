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
}