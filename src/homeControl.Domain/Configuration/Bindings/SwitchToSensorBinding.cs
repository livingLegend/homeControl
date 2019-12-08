﻿using System;
using System.Diagnostics;

namespace homeControl.Domain.Configuration.Bindings
{
    [DebuggerDisplay("SwitchId::SensorId")]
    public abstract class SwitchToSensorBinding: IEquatable<SwitchToSensorBinding>
    {
        public SwitchId SwitchId { get; set; }
        public SensorId SensorId { get; set; }

        public bool Equals(SwitchToSensorBinding other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return SwitchId.Equals(other.SwitchId) && SensorId.Equals(other.SensorId);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is SwitchToSensorBinding && Equals((SwitchToSensorBinding)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (SwitchId.GetHashCode() * 397) ^ SensorId.GetHashCode();
            }
        }

        public static bool operator ==(SwitchToSensorBinding left, SwitchToSensorBinding right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(SwitchToSensorBinding left, SwitchToSensorBinding right)
        {
            return !Equals(left, right);
        }
    }
}
