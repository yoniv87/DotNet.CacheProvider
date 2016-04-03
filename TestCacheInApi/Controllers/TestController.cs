using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using CacheProvider.Filters;

namespace TestCacheInApi.Controllers
{
    public class TestController : ApiController
    {
        [CustomCache("onemin","*","*")]
        public string GetExampleData()
        {
            return DateTime.Now.ToString("HH:mm:ss.fff");
        }
    }
}
