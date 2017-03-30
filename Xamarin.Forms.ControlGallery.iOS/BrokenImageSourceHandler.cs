using System;
using System.Threading;
using System.Threading.Tasks;
using UIKit;
using Xamarin.Forms.Platform.iOS;

namespace Xamarin.Forms.ControlGallery.iOS
{
	public sealed class BrokenImageSourceHandler : IImageSourceHandler
	{
		public Task<UIImage> LoadImageAsync(ImageSource imagesource, CancellationToken cancelationToken = new CancellationToken(),
			float scale = 1)
		{
			throw new Exception("Fail");
		}
	}
}
