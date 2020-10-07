using FlashCommon;
using System;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;

namespace CustomExtensions
{
    public static class RxExtensions
    {
        // Returns the long name of the enum for display purposes
        public static string Description(this DBNames val)
        {
            DescriptionAttribute[] attributes = (DescriptionAttribute[])val
               .GetType()
               .GetField(val.ToString())
               .GetCustomAttributes(typeof(DescriptionAttribute), false);
            return attributes.Length > 0 ? attributes[0].Description : string.Empty;
        }

        // 'Unit' is used in case where the observable returns no data, it just
        // signals that an event has occurred.
        public static IObservable<Unit> AsUnit<T>(this IObservable<T> obs)
        {
            return obs.Select(x => Unit.Default);
        }

        // Fires once and disposes of the subscription
        public static IObservable<T> Once<T>(this IObservable<T> obs)
        {
            var s = new ReplaySubject<T>(1);
            IDisposable o = null;
            o = obs.Subscribe(x =>
            {
                s.OnNext(x);
                s.OnCompleted();
                o?.Dispose();
            });
            return s;
        }

        public static IObservable<Unit> AsUnit(this Task t)
        {
            return t.ToObservable().AsUnit();
        }
    }
}
