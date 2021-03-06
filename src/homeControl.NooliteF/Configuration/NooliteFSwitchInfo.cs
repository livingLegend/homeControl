﻿using homeControl.Domain;

namespace homeControl.NooliteF.Configuration
{
    internal class NooliteFSwitchInfo
    {
        public byte Channel { get; set; }
        
        public byte[] StatusChannelIds { get; set; }
        
        public SwitchId SwitchId { get; set; }
        
        public string Description { get; set; }
        
        public byte FullPowerLevel { get; set; }
        public byte ZeroPowerLevel { get; set; }
        
        public bool UseF { get; set; }
    }
}