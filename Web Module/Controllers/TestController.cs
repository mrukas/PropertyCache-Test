using Microsoft.AspNetCore.Mvc;

namespace Web_Module.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        [HttpGet]
        public string GetValue()
        {
            return "Response from external assembly.";
        }
    }
}
