using System;

namespace Unity.Labs.MARS
{
    public enum ReservedDataIDs
    {
        Start = -1000,
        /// <summary> Data ID representing the environment immediately surrounding the user / device </summary>
        ImmediateEnvironment,
        /// <summary> Data ID representing the local user/device </summary>
        LocalUser,
        /// <summary> Data ID representing no choice being made </summary>
        Unset = -1,
        /// <summary> Data ID representing an invalid value </summary>
        Invalid = int.MinValue, 
    }
}
