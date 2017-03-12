﻿using homeControl.ClientServerShared;

namespace homeControl.ClientApi.Server
{
    internal interface IMessageWriterPipeline
    {
        void PostMessage(IClientMessage message);
    }
}