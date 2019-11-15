using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DatingAPI.Controllers
{
    public class BaseController : ControllerBase
    {
        protected bool ValidateAuthenticationUserId(int userId)
        {
            return userId == int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
        }
    }
}