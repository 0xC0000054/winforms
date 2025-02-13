﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Windows.Forms.ButtonInternal
{
    internal class RadioButtonStandardAdapter : RadioButtonBaseAdapter
    {
        internal RadioButtonStandardAdapter(ButtonBase control) : base(control) { }

        internal override void PaintUp(PaintEventArgs e, CheckState state)
        {
            if (Control.Appearance == Appearance.Button)
            {
                ButtonAdapter.PaintUp(e, Control.Checked ? CheckState.Checked : CheckState.Unchecked);
            }
            else
            {
                ColorData colors = PaintRender(e).Calculate();
                LayoutData layout = Layout(e).Layout();
                PaintButtonBackground(e, Control.ClientRectangle, null);

                PaintImage(e, layout);
                DrawCheckBox(e, layout);
                AdjustFocusRectangle(layout);
                PaintField(e, layout, colors, colors.WindowText, true);
            }
        }

        internal override void PaintDown(PaintEventArgs e, CheckState state)
        {
            if (Control.Appearance == Appearance.Button)
            {
                ButtonAdapter.PaintDown(e, Control.Checked ? CheckState.Checked : CheckState.Unchecked);
            }
            else
            {
                PaintUp(e, state);
            }
        }

        internal override void PaintOver(PaintEventArgs e, CheckState state)
        {
            if (Control.Appearance == Appearance.Button)
            {
                ButtonAdapter.PaintOver(e, Control.Checked ? CheckState.Checked : CheckState.Unchecked);
            }
            else
            {
                PaintUp(e, state);
            }
        }

        private new ButtonStandardAdapter ButtonAdapter
        {
            get
            {
                return ((ButtonStandardAdapter)base.ButtonAdapter);
            }
        }

        protected override ButtonBaseAdapter CreateButtonAdapter()
        {
            return new ButtonStandardAdapter(Control);
        }

        protected override LayoutOptions Layout(PaintEventArgs e)
        {
            LayoutOptions layout = CommonLayout();
            layout.HintTextUp = false;
            layout.DotNetOneButtonCompat = !Application.RenderWithVisualStyles;

            if (Application.RenderWithVisualStyles)
            {
                ButtonBase b = Control;
                using var screen = GdiCache.GetScreenHdc();
                layout.CheckSize = RadioButtonRenderer.GetGlyphSize(
                    screen,
                    RadioButtonRenderer.ConvertFromButtonState(GetState(), b.MouseIsOver),
                    b.HandleInternal).Width;
            }
            else
            {
                layout.CheckSize = (int)(layout.CheckSize * GetDpiScaleRatio());
            }

            return layout;
        }
    }
}
