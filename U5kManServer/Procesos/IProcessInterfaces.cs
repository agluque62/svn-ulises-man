﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;

using Lextm.SharpSnmpLib;


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

    public interface IProcessSnmp
    {
        event EventHandler<TrapBus.TrapEventArgs> TrapReceived;
        IList<Variable> GetData(object from, IList<Variable> variables);
    }
    public class RuntimeSnmpService : IDisposable, IProcessSnmp
    {
        public event EventHandler<TrapBus.TrapEventArgs> TrapReceived;

        public IList<Variable> GetData(object from, IList<Variable> variables)
        {
            if (from is stdPos)
            {
                var pos = from as stdPos;
                return new SnmpClient().Get(VersionCode.V2,
                    new IPEndPoint(IPAddress.Parse(pos.ip), pos.snmpport),
                    new OctetString("public"), variables,
                    pos.SnmpTimeout, pos.SnmpReintentos);
            }
            throw new NotImplementedException();
        }

        public RuntimeSnmpService() 
        {
            trapSubscrition = TrapBus.Subscribe((trap) => SafeRaiseEvent(trap));
        }
        void SafeRaiseEvent(TrapBus.TrapEventArgs e)
        {
            if (TrapReceived != null)
                TrapReceived(this, e);
        }
        public void Dispose()
        {
            TrapBus.Unsubscribe(trapSubscrition);
            TrapReceived = null;
        }
        object trapSubscrition = default;
    }
}
