using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Common;
using Newtonsoft.Json;

namespace WebAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class TwinCATController : ControllerBase
    {
        int InternalPort = 8529;
        [HttpGet]
        public ActionResult<TCRequest> Get(TCRequest request)
        {
            using (HttpClient client = new HttpClient())
            {
                var internal_request = "http://localhost:" + InternalPort.ToString() + "/" + JsonConvert.SerializeObject(request);

                var t = client.GetStringAsync(internal_request);
                t.Wait();
                var internal_response = t.Result;

                return Ok(internal_response);
            }
        }

        [HttpPost]
        public ActionResult<TCRequest> Post(TCRequest request)
        {
            using (HttpClient client = new HttpClient())
            {
                var internal_request = "http://localhost:" + InternalPort.ToString() + "/" + JsonConvert.SerializeObject(request);

                var t = client.GetStringAsync(internal_request);
                t.Wait();
                var internal_response = t.Result;

                return Ok(internal_response);
            }
        }
    }
}
