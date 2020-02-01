﻿using System;
using Unity.Labs.MARS.Data;

namespace Unity.Labs.MARS
{
    public interface IUsesGeoLocation
    {
    }

    public static class IUsesGeoLocationMethods
    {
        public delegate bool TryGetGeoLocationDelegate(out GeographicCoordinate coordinate);
        public static TryGetGeoLocationDelegate TryGetGeoLocationAction { private get; set; }

        public static Func<bool> TryStartServiceFunction { private get; set; }

        public static Action<Action<GeographicCoordinate>> SubscribeGeoLocationChangedAction { private get; set; }
        public static Action<Action<GeographicCoordinate>> UnsubscribeGeoLocationChangedAction { private get; set; }

        public static bool TryStartService()
        {
            return TryStartServiceFunction();
        }

        public static bool TryGetGeoLocation(this IUsesGeoLocation obj, out GeographicCoordinate coordinate)
        {
            return TryGetGeoLocationAction(out coordinate);
        }

        public static void SubscribeGeoLocationChanged(this IUsesGeoLocation obj, Action<GeographicCoordinate> onChangeAction)
        {
            SubscribeGeoLocationChangedAction(onChangeAction);
        }

        public static void UnsubscribeGeoLocationChanged(this IUsesGeoLocation obj, Action<GeographicCoordinate> onChangeAction)
        {
            UnsubscribeGeoLocationChangedAction(onChangeAction);
        }
    }
}
