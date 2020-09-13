using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Text;

namespace FlashCommon
{

    public class DynamicDB : IDynamicDB
    {
        private ReplaySubject<IDatabase> db = new ReplaySubject<IDatabase>(1);

        public IObservable<IDatabase> CurrentDB
        {
            get { return db;  }
        }

        public void SetCurrentDB(IDatabase db)
        {
            this.db.OnNext(db);
        }
    }

}
