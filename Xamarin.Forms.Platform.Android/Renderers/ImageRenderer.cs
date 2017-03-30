using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using Android.Graphics;
using Android.Views;
using AImageView = Android.Widget.ImageView;
using Xamarin.Forms.Internals;

namespace Xamarin.Forms.Platform.Android
{
	public class ImageRenderer : ViewRenderer<Image, AImageView>
	{
		bool _isDisposed;
		readonly MotionEventHelper _motionEventHelper = new MotionEventHelper();

		public ImageRenderer()
		{
			AutoPackage = false;
		}

		protected override void Dispose(bool disposing)
		{
			if (_isDisposed)
				return;

			_isDisposed = true;

			base.Dispose(disposing);
		}

		protected override AImageView CreateNativeControl()
		{
			return new FormsImageView(Context);
		}

		protected override async void OnElementChanged(ElementChangedEventArgs<Image> e)
		{
			base.OnElementChanged(e);

			if (e.OldElement == null)
			{
				var view = CreateNativeControl();
				SetNativeControl(view);
			}

			_motionEventHelper.UpdateElement(e.NewElement);

			await TryUpdateBitmap(e.OldElement);

			UpdateAspect();
		}

		protected override async void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);

			if (e.PropertyName == Image.SourceProperty.PropertyName)
				await TryUpdateBitmap();
			else if (e.PropertyName == Image.AspectProperty.PropertyName)
				UpdateAspect();
		}

		void UpdateAspect()
		{
			AImageView.ScaleType type = Element.Aspect.ToScaleType();
			Control.SetScaleType(type);
		}

		// TODO hartez Write up an example of a custom renderer with alternate handling of these errors
		// TODO hartez Set up a TryUpdateBitmap equivalent for Windows

		protected virtual async Task TryUpdateBitmap(Image previous = null)
		{
			// TODO hartez 2017/03/29 18:00:25 add blurb here	

			try
			{
				await UpdateBitmap(previous);
			}
			catch (Exception ex)
			{
				Log.Warning("Xamarin.Forms.Platform.Android.ImageRenderer", "Error updating bitmap: {0}", ex);
			}
			finally
			{
				((IImageController)Element).SetIsLoading(false);
			}
		}

		protected async Task UpdateBitmap(Image previous = null)
		{
			if (Device.IsInvokeRequired)
				throw new InvalidOperationException("Image Bitmap must not be updated from background thread");

			if (previous != null && Equals(previous.Source, Element.Source))
				return;

			((IImageController)Element).SetIsLoading(true);

			var formsImageView = Control as FormsImageView;
			formsImageView?.SkipInvalidate();

			Control.SetImageResource(global::Android.Resource.Color.Transparent);

			ImageSource source = Element.Source;
			Bitmap bitmap = null;
			IImageSourceHandler handler;

			if (source != null && (handler = Internals.Registrar.Registered.GetHandler<IImageSourceHandler>(source.GetType())) != null)
			{
				try
				{
					bitmap = await handler.LoadImageAsync(source, Context);

					if (bitmap == null)
					{
						// If we were trying to load an image which doesn't exist (either locally or remotely), we'll get back a null Bitmap
						// If the image data is found but is invalid, we'll also get back a null Bitmap. So this is as helpful as we can be.
						Log.Warning(nameof(ImageRenderer), "Could not find image or image file is invalid: {0}", source);
					}
				}
				catch (TaskCanceledException)
				{
					((IImageController)Element).SetIsLoading(false);
				}
			}

			if (Element == null || !Equals(Element.Source, source))
			{
				bitmap?.Dispose();
				return;
			}

			if (!_isDisposed)
			{
				if (bitmap == null && source is FileImageSource)
					Control.SetImageResource(ResourceManager.GetDrawableByName(((FileImageSource)source).File));
				else
					Control.SetImageBitmap(bitmap);

				bitmap?.Dispose();

				((IImageController)Element).SetIsLoading(false);
				((IVisualElementController)Element).NativeSizeChanged();
			}
		}

		public override bool OnTouchEvent(MotionEvent e)
		{
			if (base.OnTouchEvent(e))
				return true;

			return _motionEventHelper.HandleMotionEvent(Parent);
		}
	}
}