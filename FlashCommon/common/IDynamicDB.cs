using System;
using System.Collections.Generic;
using System.Text;

namespace FlashCommon
{
    // This is useful for making switching between databases easy do to
    public interface IDynamicDB
    {
        IObservable<IDatabase> CurrentDB { get; }

        void SetCurrentDB(IDatabase db);
    }

}
