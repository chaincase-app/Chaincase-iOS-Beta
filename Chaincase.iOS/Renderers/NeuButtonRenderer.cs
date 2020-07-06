using System;
using CoreAnimation;
using CoreGraphics;
using UIKit;
using Chaincase.Controls;
using Chaincase.iOS.Renderers;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

// Modified from github.com/sthewissen's code released under MIT licence
// https://gist.github.com/sthewissen/cd339a2e5c86c0173f8634174fb8da68#file-neucontentviewrenderer-cs
[assembly: ExportRenderer(typeof(NeuButton), typeof(NeuButtonRenderer))]
namespace Chaincase.iOS.Renderers
{
    public class NeuButtonRenderer : ButtonRenderer
    {
        private CAShapeLayer _shadowLayer;

        protected override void OnElementChanged(ElementChangedEventArgs<Button> e)
        {
            base.OnElementChanged(e);

            if (e.NewElement != null && Control != null)
            {
                Control.Layer.MasksToBounds = false;
                Control.Layer.ShadowColor = Color.FromHex("#dfe4ee").ToCGColor();
                Control.Layer.CornerRadius = e.NewElement.CornerRadius;
                Control.Layer.ShadowOffset = new CGSize(0, 4);
                Control.Layer.ShadowOpacity = 1f;
                Control.Layer.ShadowRadius = 8;

                Control.TouchUpInside += OnButtonTouchUpInside;
                Control.TouchDown += OnButtonTouchDown;

                _shadowLayer = new CAShapeLayer
                {
                    Frame = Control.Bounds,
                    BackgroundColor = Color.FromHex("#f1f3f6").ToCGColor(),
                    ShadowColor = Color.White.ToCGColor(),
                    CornerRadius = e.NewElement.CornerRadius,
                    ShadowOffset = new CGSize(0, -4.0),
                    ShadowOpacity = 1,
                    ShadowRadius = 4
                };

                Control.Layer.InsertSublayerBelow(_shadowLayer, Control.ImageView?.Layer);
            }
        }

        private void OnButtonTouchDown(object sender, EventArgs e)
        {
            Control.Layer.ShadowOffset = new CGSize(0, height: -4);
            Control.Layer.Sublayers[0].ShadowOffset = new CGSize(0, 4);
            Control.ContentEdgeInsets = new UIEdgeInsets(0, 4, 0, 0);
        }

        private void OnButtonTouchUpInside(object sender, EventArgs e)
        {
            Control.Layer.ShadowOffset = new CGSize(0, height: 4);
            Control.Layer.Sublayers[0].ShadowOffset = new CGSize(0, -4);
            Control.ContentEdgeInsets = new UIEdgeInsets(0, 0, 4, 0);
        }

        public override void Draw(CGRect rect)
        {
            _shadowLayer.Frame = Bounds;
            base.Draw(rect);
        }
    }
}
