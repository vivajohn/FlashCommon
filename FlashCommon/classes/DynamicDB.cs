using CustomExtensions;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;

namespace FlashCommon
{

    public class DynamicDB : IDynamicDB
    {
        private Func<IDatabase> GetDb;
        private ReplaySubject<IDatabase> odb = new ReplaySubject<IDatabase>(1);

        public bool IsConnected { get; set; } = false;

        public IObservable<IDatabase> CurrentDB
        {
            get { return odb; }
        }

        public IObservable<IDatabase> SingleDB
        {
            get { return CurrentDB.Once(); }
        }

        public void SetCurrentDB(Func<IDatabase> fn)
        {
            GetDb = fn;
        }

        public void Connect()
        {
            IsConnected = true;
            odb.OnNext(GetDb());
        }

        public void Disconnect()
        {
            IsConnected = false;
            odb.OnCompleted();
            odb = new ReplaySubject<IDatabase>(1);
        }
    }

}
