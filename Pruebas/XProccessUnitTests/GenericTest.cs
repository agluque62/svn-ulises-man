using System;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using U5kManServer;
using U5kManServer.Procesos;

namespace XProccessUnitTests
{
    public class GenericTest
    {
        [Fact]
        public void HttpServiceTest()
        {
            var httpS = new RuntimeHttpService();
            var res = httpS.Get("http://11.21.91.130:8080/std", TimeSpan.FromSeconds(5)).Result;
            Assert.True(res.Success == false);
            res = httpS.Get("http://www.google.es", TimeSpan.FromSeconds(5)).Result;
            Assert.True(res.Success == true);
            res = httpS.Get("http://11.21.91.1:9999/pepe", TimeSpan.FromSeconds(5)).Result;
            Assert.True(res.Success == false);
            res = httpS.Get("http://1.1.365.1:8999/pepe", TimeSpan.FromSeconds(5)).Result;
            Assert.True(res.Success == false);
        }
    }
}
