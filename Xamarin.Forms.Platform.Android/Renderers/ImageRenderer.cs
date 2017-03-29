using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using Android.Graphics;
using Android.Views;
using AImageView = Android.Widget.ImageView;
using Xamarin.Forms.Internals;
using System.Threading;

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

		protected async override void OnElementChanged(ElementChangedEventArgs<Image> e)
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

		protected async override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
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
		// TODO Set up a TryUpdateBitmap equivalent for Windows, iOS

		protected virtual async Task TryUpdateBitmap(Image previous = null)
		{
			try
			{
				await UpdateBitmap(previous);
			}
			catch(Exception ex)
			{
				Log.Warning("Xamarin.Forms.Platform.Android.ImageRenderer", "Error updating bitmap: {0}", ex);
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
					// TODO hartez Some of the test cases don't throw an exception, they just return null
					// This is because the BitmapFactory methods return null if they can't decode.

					bitmap = await handler.LoadImageAsync(source, Context);
				}
				catch (TaskCanceledException)
				{
				}

				// TODO verify that when this is down here, you don't get double exceptions
				if(bitmap == null)
				{
					// Could not decode the bitmap, throw an exception to indicate 
					// the data wasn't decodable
					throw new InvalidDataException($"Could not load Bitmap from source {source}");
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