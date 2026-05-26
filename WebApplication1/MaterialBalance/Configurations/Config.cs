namespace MaterialBalance.Configurations;

public class Config
{
    /// <summary>
    /// Строка подключения к БД
    /// </summary>
    public string ConnectionString { get; set; }
    
    /// <summary>
    /// Название БД
    /// </summary>
    public string DatabaseName  { get; set; }
}