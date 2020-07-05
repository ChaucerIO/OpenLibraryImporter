using Microsoft.AspNetCore.Mvc;

namespace Chaucer.Backend.Controllers
{
    [ApiVersion("1")]
    [ApiController]
    [Route("v{version:apiVersion}/[controller]")]
    public class Borrow
    {
        [HttpGet]
        public long CollectionCountAsync()
        {
            return 42;
        }
    }
}