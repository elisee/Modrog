﻿using SwarmPlatform.Graphics;
using System;

namespace SwarmPlatform.UI
{
    public class Panel : Element
    {
        public Action OnValidate;

        public Panel(Element parent) : this(parent.Desktop, parent) { }
        public Panel(Element parent, TexturePatch backgroundPatch) : this(parent.Desktop, parent, backgroundPatch) { }
        public Panel(Desktop desktop, Element parent = null) : base(desktop, parent) { }

        public Panel(Desktop desktop, Element parent, TexturePatch backgroundPatch) : base(desktop, parent)
        {
            BackgroundPatch = backgroundPatch;
        }

        public override Element HitTest(int x, int y) => base.HitTest(x, y) ?? (LayoutRectangle.Contains(x, y) ? this : null);

        public override void Validate()
        {
            if (OnValidate != null) OnValidate();
            else base.Validate();
        }
    }
}
