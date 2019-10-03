﻿using DeepSwarmClient.UI;

namespace DeepSwarmClient.Interface
{
    public class StyledTextButton : TextButton
    {
        public StyledTextButton(Element parent) : base(parent)
        {
            Padding = 8;
            Flow = Flow.Shrink;
            BackgroundPatch = new TexturePatch(0x4444aaff);
        }
    }
}
