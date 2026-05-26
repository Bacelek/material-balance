using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;

namespace MaterialBalance.API.Request;

public enum StatusType
{
    Pending = 0,
    Processing = 1,
    Completed = 2
}