using System;
using System.Collections.Generic;
using System.Text;

namespace FlashCommon
{
    // This is useful for making switching between databases easy do to
    public interface IDynamicDB
    {
        IObservable<IDatabase> CurrentDB { get; }

        // This returns a non-repeating instance of IDatabase
        // (Returns CurrentDB.FirstAsync())
        IObservable<IDatabase> SingleDB { get; }

        void SetCurrentDB(Func<IDatabase> fn);

        void Connect();

        void Disconnect();

        bool IsConnected { get; set; }
    }

}
