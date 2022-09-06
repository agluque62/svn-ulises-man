using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

using Utilities;

namespace UnitTesting
{
    [TestClass]
    public class IgmpTests
    {
        [TestMethod]
        public void TestMethod1()
        {
            var s = new IgmpSocket();
            Task.Delay(TimeSpan.FromSeconds(100)).Wait();
            s.Dispose();
        }
    }
}
