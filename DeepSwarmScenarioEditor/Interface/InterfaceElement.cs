﻿using DeepSwarmPlatform.UI;

namespace DeepSwarmScenarioEditor.Interface
{
    class InterfaceElement : Element
    {
        public readonly Engine Engine;

        public InterfaceElement(Interface @interface, Element parent) : base(@interface.Desktop, parent)
        {
            Engine = @interface.Engine;
        }

    }
}