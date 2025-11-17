using System.ComponentModel.DataAnnotations;

namespace MaterialBalance.API.Request;

/// <summary>
/// todo: Узлы нам пока не нужны, их вообще нет в природе.
/// Мы получаем набор потоков, из которых собираем граф, вот там будут узлы.
/// </summary>
public class Flow
{
    /// <summary>
    /// Id потока
    /// </summary>
    [Required]
    public Guid Id { get; set; }
    
    /// <summary>
    /// Id потока из которого выходит текущий поток
    /// </summary>
    [Required]
    public Guid SourceNodeId { get; set; }
    
    /// <summary>
    /// Id потока в который входит поток
    /// </summary>
    [Required]
    public Guid TargetNodeId { get; set; }
    
    /// <summary>
    /// Тип потока
    /// </summary>
    [Required]
    public FlowType Type { get; set; }
    
    /// <summary>
    /// Нижняя граница значения потока
    /// </summary>
    [Required]
    public double LowerBound {get; set;}
    
    /// <summary>
    /// Верхняя граница
    /// </summary>
    [Required]
    public double UpperBound {get; set;}

}