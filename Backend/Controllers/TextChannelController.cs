using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Intalk.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TextChannelController
    {
        
    }
}