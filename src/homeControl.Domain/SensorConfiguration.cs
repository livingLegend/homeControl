﻿using System.Diagnostics;

namespace homeControl.Domain
{
    [DebuggerDisplay("SensorId")]
    public sealed class SensorConfiguration
    {
        public SensorId SensorId { get; set; }
    }
}