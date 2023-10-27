using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace U5kManServer.Procesos
{
    public interface IProcessData
    {
        U5kManStdData Data { get; }
        bool IsMaster { get; }
    }

    public class RunTimeData : IProcessData
    {
        public U5kManStdData Data => U5kManService.GlobalData;

        public bool IsMaster => U5kManService._Master;
    }

}
