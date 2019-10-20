﻿using SwarmPlatform.UI;

namespace ModrogClient.Interface
{
    class InterfaceElement : Element
    {
        public readonly Engine Engine;

        public InterfaceElement(Interface @interface, Element parent)
            : base(@interface.Desktop, null)
        {
            Engine = @interface.Engine;
            parent?.Add(this);
        }

    }
}