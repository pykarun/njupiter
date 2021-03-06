#region Copyright & License
/*
	Copyright (c) 2005-2011 nJupiter

	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to deal
	in the Software without restriction, including without limitation the rights
	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
	copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in
	all copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
	THE SOFTWARE.
*/
#endregion

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Web;
using System.Web.UI;
using System.Web.Hosting;
using System.Reflection;
using System.Globalization;

using nJupiter.Configuration;

namespace nJupiter.Web.UI {

	public class StreamImage : Page {

		#region IFileToStream Interface
		public interface IFileToStream {
			#region Properties
			string Name { get; }
			string MimeType { get; }
			bool Exists { get; }
			DateTime LastModified { get; }
			#endregion

			#region Methods
			Stream OpenStream();
			#endregion
		}
		#endregion

		#region Default IFileToStream Implementation
		private sealed class FileToStreamImpl : IFileToStream {
			#region Members
			private readonly VirtualFile virtualFile;
			private readonly FileInfo fileInfo;
			#endregion

			#region Properties
			private string Extension { get { return Path.GetExtension(this.Name); } }
			public string Name { get { return this.fileInfo != null ? this.fileInfo.Name : (this.virtualFile != null ? this.virtualFile.Name : null); } }
			public string MimeType { get { return "image/" + this.Extension.Substring(1); } }
			public bool Exists { get { return this.fileInfo != null ? this.fileInfo.Exists : this.virtualFile != null; } }

			public DateTime LastModified {
				get {
					if(this.fileInfo != null) {
						return this.fileInfo.LastAccessTimeUtc;
					}
					DateTime dateNow = DateTime.Now;
					return new DateTime(dateNow.Year, dateNow.Month, dateNow.Day, dateNow.Hour, 0, 0);
				}
			}
			#endregion

			#region Constructors
			public FileToStreamImpl(string path) {
				path = HttpUtility.UrlDecode(path);
				string filePath = HttpContextHandler.Instance.Current.Server.MapPath(path);
				if(File.Exists(filePath)) {
					this.fileInfo = new FileInfo(filePath);
				} else if(HostingEnvironment.VirtualPathProvider.FileExists(path)) {
					this.virtualFile = HostingEnvironment.VirtualPathProvider.GetFile(path);
				}
			}
			#endregion

			#region Methods
			public Stream OpenStream() {
				if(this.fileInfo != null) {
					return this.fileInfo.OpenRead();
				}
				return this.virtualFile.Open();
			}
			#endregion
		}
		#endregion

		#region Methods
		public virtual IFileToStream GetFileToStream(string path) {

			IFileToStream file = null;
			try {
				file = GetFileToStreamInternal(path);
			} catch(FileNotFoundException) { }
			if(file == null)
				file = new FileToStreamImpl(path);
			return file;
		}

		private static IFileToStream GetFileToStreamInternal(string path) {
			const string section = "fileToStream";
			const string assemblypath = "assemblyPath";
			const string assembly = "assembly";
			const string type = "type";

			IConfig config = ConfigRepository.Instance.GetConfig(true);
			if(config != null && config.ContainsKey(section)) {
				return (IFileToStream)GetInstance(
					config.GetValue(section, assemblypath),
					config.GetValue(section, assembly),
					config.GetValue(section, type),
					new object[] { path });
			}
			return null;
		}

		private static object GetInstance(string assemblyPath, string assemblyName, string typeName, object[] prams) {
			Assembly assembly;
			if(!string.IsNullOrEmpty(assemblyPath)) {
				assembly = Assembly.LoadFrom(assemblyPath);
			} else if(assemblyName == null || assemblyName.Length.Equals(0) ||
				Assembly.GetExecutingAssembly().GetName().Name.Equals(assemblyName)) {
				assembly = Assembly.GetExecutingAssembly();
				//Load current assembly
			} else {
				assembly = Assembly.Load(assemblyName);
				// Late binding to an assembly on disk (current directory)
			}
			return assembly.CreateInstance(
				typeName, false,
				BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly |
				BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.ExactBinding,
				null, prams, null, null);
		}
		#endregion

		#region Overridden Methods
		protected override void OnLoad(EventArgs e) {
			// See if client has a cached version of the image
			string ifModifiedSince = this.Request.Headers.Get("If-Modified-Since");

			string reqPath = this.Request.QueryString["path"];
			string path = reqPath ?? string.Empty;

			IFileToStream fileToStream = this.GetFileToStream(path);
			if(fileToStream.Exists) {
				//Get the last modified time for the current file
				//Handle the situation where we get a LastModified that is in the future
				DateTime now = DateTime.Now;
				DateTime lastModifiedTime = fileToStream.LastModified > now ? now : fileToStream.LastModified;

				//Check to see if it is a conditional HTTP GET.
				if(ifModifiedSince != null) {
					//This is a conditional HTTP GET request. Compare the strings.
					try {
						DateTime incrementalIndexTime = DateTime.Parse(ifModifiedSince, DateTimeFormatInfo.InvariantInfo).ToUniversalTime();
						// Has to do a string compare because of the resolution
						if(incrementalIndexTime.ToString(DateTimeFormatInfo.InvariantInfo) ==
							lastModifiedTime.ToString(DateTimeFormatInfo.InvariantInfo)) {
							// If the file has not been modifed, send a not changed status
							this.Response.StatusCode = 304;
							this.Response.End();
						}
					} catch(FormatException) {
					}
				}

				string reqWidth = Page.Request.QueryString["width"];
				string reqHeight = Page.Request.QueryString["height"];
				string reqAllowEnlarging = Page.Request.QueryString["allowEnlarging"];
				string reqAllowStretching = Page.Request.QueryString["allowStretching"];

				int width = 0;
				int height = 0;
				bool allowEnlarging = reqAllowEnlarging != null && string.Compare(reqAllowEnlarging, "true", true, CultureInfo.InvariantCulture) == 0;
				bool allowStretching = reqAllowStretching != null && string.Compare(reqAllowStretching, "true", true, CultureInfo.InvariantCulture) == 0;

				IConfig config = ConfigRepository.Instance.GetSystemConfig();
				SmoothingMode smoothingMode = SmoothingMode.Default;
				if(config.ContainsKey("imageScaleConfig", "smoothingMode")) {
					smoothingMode = (SmoothingMode)Enum.Parse(typeof(SmoothingMode), config.GetValue("imageScaleConfig", "smoothingMode"), true);
				}
				InterpolationMode interpolationMode = InterpolationMode.Default;
				if(config.ContainsKey("imageScaleConfig", "interpolationMode")) {
					interpolationMode = (InterpolationMode)Enum.Parse(typeof(InterpolationMode), config.GetValue("imageScaleConfig", "interpolationMode"), true);
				}
				PixelOffsetMode pixelOffsetMode = PixelOffsetMode.Default;
				if(config.ContainsKey("imageScaleConfig", "pixelOffsetMode")) {
					pixelOffsetMode = (PixelOffsetMode)Enum.Parse(typeof(PixelOffsetMode), config.GetValue("imageScaleConfig", "pixelOffsetMode"), true);
				}
				CompositingQuality compositingQuality = CompositingQuality.Default;
				if(config.ContainsKey("imageScaleConfig", "compositingQuality")) {
					compositingQuality = (CompositingQuality)Enum.Parse(typeof(CompositingQuality), config.GetValue("imageScaleConfig", "compositingQuality"), true);
				}
				bool useEmbeddedColorManagement = false;
				if(config.ContainsKey("imageScaleConfig", "useEmbeddedColorManagement")) {
					useEmbeddedColorManagement = config.GetValue<bool>("imageScaleConfig", "useEmbeddedColorManagement");
				}

				try {
					width = reqWidth == null ? width : int.Parse(reqWidth, NumberFormatInfo.InvariantInfo);
				} catch(FormatException) { }
				try {
					height = reqHeight == null ? height : int.Parse(reqHeight, NumberFormatInfo.InvariantInfo);
				} catch(FormatException) { }

				using(Stream fileStream = fileToStream.OpenStream()) {
					ImageScale.ResizeFlags resizeFlags = ImageScale.ResizeFlags.None;
					if(allowEnlarging) {
						resizeFlags = resizeFlags | ImageScale.ResizeFlags.AllowEnlarging;
					}
					if(allowStretching) {
						resizeFlags = resizeFlags | ImageScale.ResizeFlags.AllowStretching;
					}

					this.Response.Clear();
					try {
						ImageScale.Resize(fileStream, this.Response.OutputStream, width, height, resizeFlags, smoothingMode, interpolationMode, pixelOffsetMode, compositingQuality, useEmbeddedColorManagement);
						this.Response.AddHeader("Content-Disposition", "inline;filename=\"" + fileToStream.Name + "\"");
						this.Response.ContentType = fileToStream.MimeType;
						this.Response.Cache.SetLastModified(lastModifiedTime);
						//The following lines enable downlevel caching in server or browser cache. But not in proxies.
						this.Response.Cache.SetCacheability(HttpCacheability.Public);
						//Set the expiration time for the downlevel cache
						this.Response.Cache.SetExpires(DateTime.Now.AddMinutes(5));
						this.Response.Cache.SetValidUntilExpires(true);
						this.Response.Cache.VaryByParams["*"] = true;
					} catch(OutOfMemoryException ex) {
						// If not image
						throw new FileNotFoundException(ex.Message, ex);
					} catch(FileNotFoundException) {
						// If file not found
						// We know that the file exists so we throw a forbidden request exception
						throw new HttpException(403, "Forbidden");
					}
				}
			} else {
				this.Response.Clear();
				throw new HttpException(404, "Not Found");
			}
		}
		#endregion
	}

	/// <summary>
	/// Class that handles scaling of images.
	/// </summary>
	public static class ImageScale {
		#region Enums
		[Flags]
		public enum ResizeFlags {
			None = 0,
			AllowEnlarging = 1,
			AllowStretching = 2
		}
		#endregion

		#region Constructors
		#endregion

		#region Methods
		/// <summary>
		/// Resizes an image to a new size. Resizing neither stretches nor enlarges a picture by default.
		/// </summary>
		/// <param name="originalImage">The original image to resize</param>
		/// <param name="newWidth">The new width of the image. Set to 0 to let the height decide.</param>
		/// <param name="newHeight">The new height of the image. Set to 0 to let the width decide.</param>
		/// <returns>A memory stream containing the resized image.
		/// Returns null if the original file was neither an image nor found.
		/// </returns>
		public static MemoryStream Resize(Image originalImage, int newWidth, int newHeight) {
			return Resize(originalImage, newWidth, newHeight, SmoothingMode.Default, InterpolationMode.Default, PixelOffsetMode.Default, CompositingQuality.Default);
		}

		/// <summary>
		/// Resizes an image to a new size. Resizing neither stretches nor enlarges a picture by default.
		/// </summary>
		/// <param name="originalImage">The original image to resize</param>
		/// <param name="newWidth">The new width of the image. Set to 0 to let the height decide.</param>
		/// <param name="newHeight">The new height of the image. Set to 0 to let the width decide.</param>
		/// <param name="smoothingMode">Specifies the smoothing mode.</param>
		/// <param name="interpolationMode">Specifies the interpolation mode.</param>
		/// <param name="pixelOffsetMode">Specifies the pixel offset mode.</param>
		/// <returns>A memory stream containing the resized image.
		/// Returns null if the original file was neither an image nor found.
		/// </returns>
		public static MemoryStream Resize(Image originalImage, int newWidth, int newHeight, SmoothingMode smoothingMode, InterpolationMode interpolationMode, PixelOffsetMode pixelOffsetMode, CompositingQuality compositingQuality) {
			MemoryStream ms = new MemoryStream();
			Resize(originalImage, ms, newWidth, newHeight, ResizeFlags.None, smoothingMode, interpolationMode, pixelOffsetMode, compositingQuality);
			return ms;
		}

		/// <summary>
		/// Resizes an image to a new size. Resizing neither stretches nor enlarges a picture by default.
		/// </summary>
		/// <param name="originalImage">The original image to resize</param>
		/// <param name="outputStream">The stream containing the resized image.</param>
		/// <param name="newWidth">The new width of the image. Set to 0 to let the height decide.</param>
		/// <param name="newHeight">The new height of the image. Set to 0 to let the width decide.</param>
		public static void Resize(Image originalImage, Stream outputStream, int newWidth, int newHeight) {
			Resize(originalImage, outputStream, newWidth, newHeight, SmoothingMode.Default, InterpolationMode.Default, PixelOffsetMode.Default, CompositingQuality.Default);
		}

		/// <summary>
		/// Resizes an image to a new size. Resizing neither stretches nor enlarges a picture by default.
		/// </summary>
		/// <param name="originalImage">The original image to resize</param>
		/// <param name="outputStream">The stream containing the resized image.</param>
		/// <param name="newWidth">The new width of the image. Set to 0 to let the height decide.</param>
		/// <param name="newHeight">The new height of the image. Set to 0 to let the width decide.</param>
		/// <param name="smoothingMode">Specifies the smoothing mode.</param>
		/// <param name="interpolationMode">Specifies the interpolation mode.</param>
		/// <param name="pixelOffsetMode">Specifies the pixel offset mode.</param>
		public static void Resize(Image originalImage, Stream outputStream, int newWidth, int newHeight, SmoothingMode smoothingMode, InterpolationMode interpolationMode, PixelOffsetMode pixelOffsetMode, CompositingQuality compositingQuality) {
			Resize(originalImage, outputStream, newWidth, newHeight, ResizeFlags.None, smoothingMode, interpolationMode, pixelOffsetMode, compositingQuality);
		}

		/// <summary>
		/// Resizes an image to a new size. Resizing neither stretches nor enlarges a picture by default.
		/// </summary>
		/// <param name="originalImage">The original image to resize</param>
		/// <param name="newWidth">The new width of the image. Set to 0 to let the height decide.</param>
		/// <param name="newHeight">The new height of the image. Set to 0 to let the width decide.</param>
		/// <param name="resizeFlags">Specify the different flags in the ResizeFlags enumeration to deviate from the default behaviour.</param>
		/// <returns>A memory stream containing the resized image.
		/// Returns null if the original file was neither an image nor found.
		/// </returns>
		public static MemoryStream Resize(Image originalImage, int newWidth, int newHeight, ResizeFlags resizeFlags) {
			return Resize(originalImage, newWidth, newHeight, resizeFlags, SmoothingMode.Default, InterpolationMode.Default, PixelOffsetMode.Default, CompositingQuality.Default);
		}

		/// <summary>
		/// Resizes an image to a new size. Resizing neither stretches nor enlarges a picture by default.
		/// </summary>
		/// <remarks>Set both newWidth and newHeight to 0 to prevent resizing.</remarks>
		/// <param name="originalImage">The original image to resize</param>
		/// <param name="newWidth">The new width of the image. Set to 0 to let the height decide.</param>
		/// <param name="newHeight">The new height of the image. Set to 0 to let the width decide.</param>
		/// <param name="resizeFlags">Specify the different flags in the ResizeFlags enumeration to deviate from the default behaviour.</param>
		/// <param name="smoothingMode">Specifies the smoothing mode.</param>
		/// <param name="interpolationMode">Specifies the interpolation mode.</param>
		/// <param name="pixelOffsetMode">Specifies the pixel offset mode.</param>
		/// <returns>A memory stream containing the resized image.
		/// Returns null if the original file was neither an image nor found.
		/// </returns>
		public static MemoryStream Resize(Image originalImage, int newWidth, int newHeight, ResizeFlags resizeFlags, SmoothingMode smoothingMode, InterpolationMode interpolationMode, PixelOffsetMode pixelOffsetMode, CompositingQuality compositingQuality) {
			MemoryStream ms = new MemoryStream();
			Resize(originalImage, ms, newWidth, newHeight, resizeFlags, smoothingMode, interpolationMode, pixelOffsetMode, compositingQuality);
			return ms;
		}

		/// <summary>
		/// Resizes an image to a new size. Resizing neither stretches nor enlarges a picture by default.
		/// </summary>
		/// <param name="originalImage">The original image to resize</param>
		/// <param name="outputStream">The stream containing the resized image.</param>
		/// <param name="newWidth">The new width of the image. Set to 0 to let the height decide.</param>
		/// <param name="newHeight">The new height of the image. Set to 0 to let the width decide.</param>
		/// <param name="resizeFlags">Specify the different flags in the ResizeFlags enumeration to deviate from the default behaviour.</param>
		public static void Resize(Image originalImage, Stream outputStream, int newWidth, int newHeight, ResizeFlags resizeFlags) {
			Resize(originalImage, outputStream, newWidth, newHeight, resizeFlags, SmoothingMode.Default, InterpolationMode.Default, PixelOffsetMode.Default, CompositingQuality.Default);
		}

		/// <summary>
		/// Resizes an image to a new size. Resizing neither stretches nor enlarges a picture by default.
		/// </summary>
		/// <param name="originalImage">The original image to resize</param>
		/// <param name="outputStream">The stream containing the resized image.</param>
		/// <param name="newWidth">The new width of the image. Set to 0 to let the height decide.</param>
		/// <param name="newHeight">The new height of the image. Set to 0 to let the width decide.</param>
		/// <param name="resizeFlags">Specify the different flags in the ResizeFlags enumeration to deviate from the default behaviour.</param>
		/// <param name="smoothingMode">Specifies the smoothing mode.</param>
		/// <param name="interpolationMode">Specifies the interpolation mode.</param>
		/// <param name="pixelOffsetMode">Specifies the pixel offset mode.</param>
		public static void Resize(Image originalImage, Stream outputStream, int newWidth, int newHeight, ResizeFlags resizeFlags, SmoothingMode smoothingMode, InterpolationMode interpolationMode, PixelOffsetMode pixelOffsetMode, CompositingQuality compositingQuality) {
			if(originalImage == null) {
				throw new ArgumentNullException("originalImage");
			}

			Size originalSize = originalImage.PhysicalDimension.ToSize();
			Size newSize;
			if((resizeFlags & ResizeFlags.AllowStretching) > 0) {
				newSize = GetNonProportionalImageSize(originalSize, newWidth, newHeight, (resizeFlags & ResizeFlags.AllowEnlarging) > 0);
			} else {
				newSize = GetProportionalImageSize(originalSize, newWidth, newHeight, (resizeFlags & ResizeFlags.AllowEnlarging) > 0);
			}

			using(MemoryStream memoryStream = new MemoryStream()) {
				if(newSize.Equals(originalSize)) {
					// Keep original size. Only make a copy
					originalImage.Save(memoryStream, new ImageFormat(originalImage.RawFormat.Guid));
				} else {
					// Create new pic.
					using(Bitmap bitmap = new Bitmap(newSize.Width, newSize.Height)) {
						bitmap.SetResolution(originalImage.HorizontalResolution, originalImage.VerticalResolution);
						using(Graphics graphics = Graphics.FromImage(bitmap)) {
							graphics.SmoothingMode = smoothingMode;
							graphics.InterpolationMode = interpolationMode;
							graphics.PixelOffsetMode = pixelOffsetMode;
							graphics.CompositingQuality = compositingQuality;
							graphics.DrawImage(originalImage,
								new Rectangle(0, 0, bitmap.Width, bitmap.Height),
								new Rectangle(0, 0, originalImage.Width, originalImage.Height),
								GraphicsUnit.Pixel);
							bitmap.Save(memoryStream, originalImage.RawFormat);
						}
					}
				}
				memoryStream.WriteTo(outputStream);
			}
		}

		/// <summary>
		/// Resizes an image to a new size. Resizing neither stretches nor enlarges a picture by default.
		/// </summary>
		/// <param name="imagePath">The full path of the image to resize. E.g. 'c:/WebFolder/upload/sample.jpg'</param>
		/// <param name="newWidth">The new width of the image. Set to 0 to let the height decide.</param>
		/// <param name="newHeight">The new height of the image. Set to 0 to let the width decide.</param>
		/// <returns>A memory stream containing the resized image.
		/// Returns null if the original file was neither an image nor found.
		/// </returns>
		public static MemoryStream Resize(string imagePath, int newWidth, int newHeight) {
			return Resize(imagePath, newWidth, newHeight, SmoothingMode.Default, InterpolationMode.Default, PixelOffsetMode.Default, CompositingQuality.Default, false);
		}

		/// <summary>
		/// Resizes an image to a new size. Resizing neither stretches nor enlarges a picture by default.
		/// </summary>
		/// <param name="imagePath">The full path of the image to resize. E.g. 'c:/WebFolder/upload/sample.jpg'</param>
		/// <param name="newWidth">The new width of the image. Set to 0 to let the height decide.</param>
		/// <param name="newHeight">The new height of the image. Set to 0 to let the width decide.</param>
		/// <param name="smoothingMode">Specifies the smoothing mode.</param>
		/// <param name="interpolationMode">Specifies the interpolation mode.</param>
		/// <param name="pixelOffsetMode">Specifies the pixel offset mode.</param>
		/// <returns>A memory stream containing the resized image.
		/// Returns null if the original file was neither an image nor found.
		/// </returns>
		public static MemoryStream Resize(string imagePath, int newWidth, int newHeight, SmoothingMode smoothingMode, InterpolationMode interpolationMode, PixelOffsetMode pixelOffsetMode, CompositingQuality compositingQuality, bool useEmbeddedColorManagement) {
			MemoryStream ms = new MemoryStream();
			Resize(imagePath, ms, newWidth, newHeight, ResizeFlags.None, smoothingMode, interpolationMode, pixelOffsetMode, compositingQuality, useEmbeddedColorManagement);
			return ms;
		}

		/// <summary>
		/// Resizes an image to a new size. Resizing neither stretches nor enlarges a picture by default.
		/// </summary>
		/// <param name="imagePath">The full path of the image to resize. E.g. 'c:/WebFolder/upload/sample.jpg'</param>
		/// <param name="outputStream">The stream containing the resized image.</param>
		/// <param name="newWidth">The new width of the image. Set to 0 to let the height decide.</param>
		/// <param name="newHeight">The new height of the image. Set to 0 to let the width decide.</param>
		public static void Resize(string imagePath, Stream outputStream, int newWidth, int newHeight) {
			Resize(imagePath, outputStream, newWidth, newHeight, SmoothingMode.Default, InterpolationMode.Default, PixelOffsetMode.Default, CompositingQuality.Default, false);
		}

		/// <summary>
		/// Resizes an image to a new size. Resizing neither stretches nor enlarges a picture by default.
		/// </summary>
		/// <param name="imagePath">The full path of the image to resize. E.g. 'c:/WebFolder/upload/sample.jpg'</param>
		/// <param name="outputStream">The stream containing the resized image.</param>
		/// <param name="newWidth">The new width of the image. Set to 0 to let the height decide.</param>
		/// <param name="newHeight">The new height of the image. Set to 0 to let the width decide.</param>
		/// <param name="smoothingMode">Specifies the smoothing mode.</param>
		/// <param name="interpolationMode">Specifies the interpolation mode.</param>
		/// <param name="pixelOffsetMode">Specifies the pixel offset mode.</param>
		public static void Resize(string imagePath, Stream outputStream, int newWidth, int newHeight, SmoothingMode smoothingMode, InterpolationMode interpolationMode, PixelOffsetMode pixelOffsetMode, CompositingQuality compositingQuality, bool useEmbeddedColorManagement) {
			Resize(imagePath, outputStream, newWidth, newHeight, ResizeFlags.None, smoothingMode, interpolationMode, pixelOffsetMode, compositingQuality, useEmbeddedColorManagement);
		}

		/// <summary>
		/// Resizes an image to a new size. Resizing neither stretches nor enlarges a picture by default.
		/// </summary>
		/// <param name="imagePath">The full path of the image to resize. E.g. 'c:/WebFolder/upload/sample.jpg'</param>
		/// <param name="newWidth">The new width of the image. Set to 0 to let the height decide.</param>
		/// <param name="newHeight">The new height of the image. Set to 0 to let the width decide.</param>
		/// <param name="resizeFlags">Specify the different flags in the ResizeFlags enumeration to deviate from the default behaviour.</param>
		/// <returns>A memory stream containing the resized image.
		/// Returns null if the original file was neither an image nor found.
		/// </returns>
		public static MemoryStream Resize(string imagePath, int newWidth, int newHeight, ResizeFlags resizeFlags) {
			return Resize(imagePath, newWidth, newHeight, resizeFlags, SmoothingMode.Default, InterpolationMode.Default, PixelOffsetMode.Default, CompositingQuality.Default, false);
		}

		/// <summary>
		/// Resizes an image to a new size. Resizing neither stretches nor enlarges a picture by default. 
		/// </summary>
		/// <remarks>Set both newWidth and newHeight to 0 to prevent resizing.</remarks>
		/// <param name="imagePath">The full path of the image to resize. E.g. 'c:/WebFolder/upload/sample.jpg'</param>
		/// <param name="newWidth">The new width of the image. Set to 0 to let the height decide.</param>
		/// <param name="newHeight">The new height of the image. Set to 0 to let the width decide.</param>
		/// <param name="resizeFlags">Specify the different flags in the ResizeFlags enumeration to deviate from the default behaviour.</param>
		/// <param name="smoothingMode">Specifies the smoothing mode.</param>
		/// <param name="interpolationMode">Specifies the interpolation mode.</param>
		/// <param name="pixelOffsetMode">Specifies the pixel offset mode.</param>
		/// <returns>A memory stream containing the resized image.
		/// Returns null if the original file was neither an image nor found.
		/// </returns>
		public static MemoryStream Resize(string imagePath, int newWidth, int newHeight, ResizeFlags resizeFlags, SmoothingMode smoothingMode, InterpolationMode interpolationMode, PixelOffsetMode pixelOffsetMode, CompositingQuality compositingQuality, bool useEmbeddedColorManagement) {
			try {
				MemoryStream ms = new MemoryStream();
				Resize(imagePath, ms, newWidth, newHeight, resizeFlags, smoothingMode, interpolationMode, pixelOffsetMode, compositingQuality, useEmbeddedColorManagement);
				return ms;
			} catch(FileNotFoundException) {
				// If file not found
			} catch(OutOfMemoryException) {
				// If not image
			}
			return null;
		}

		/// <summary>
		/// Resizes an image to a new size. Resizing neither stretches nor enlarges a picture by default.
		/// </summary>
		/// <param name="imagePath">The full path of the image to resize. E.g. 'c:/WebFolder/upload/sample.jpg'</param>
		/// <param name="outputStream">The stream containing the resized image.</param>
		/// <param name="newWidth">The new width of the image. Set to 0 to let the height decide.</param>
		/// <param name="newHeight">The new height of the image. Set to 0 to let the width decide.</param>
		/// <param name="resizeFlags">Specify the different flags in the ResizeFlags enumeration to deviate from the default behaviour.</param>
		public static void Resize(string imagePath, Stream outputStream, int newWidth, int newHeight, ResizeFlags resizeFlags) {
			Resize(imagePath, outputStream, newWidth, newHeight, resizeFlags, SmoothingMode.Default, InterpolationMode.Default, PixelOffsetMode.Default, CompositingQuality.Default, false);
		}

		/// <summary>
		/// Resizes an image to a new size. Resizing neither stretches nor enlarges a picture by default.
		/// </summary>
		/// <param name="imagePath">The full path of the image to resize. E.g. 'c:/WebFolder/upload/sample.jpg'</param>
		/// <param name="outputStream">The stream containing the resized image.</param>
		/// <param name="newWidth">The new width of the image. Set to 0 to let the height decide.</param>
		/// <param name="newHeight">The new height of the image. Set to 0 to let the width decide.</param>
		/// <param name="resizeFlags">Specify the different flags in the ResizeFlags enumeration to deviate from the default behaviour.</param>
		/// <param name="smoothingMode">Specifies the smoothing mode.</param>
		/// <param name="interpolationMode">Specifies the interpolation mode.</param>
		/// <param name="pixelOffsetMode">Specifies the pixel offset mode.</param>
		public static void Resize(string imagePath, Stream outputStream, int newWidth, int newHeight, ResizeFlags resizeFlags, SmoothingMode smoothingMode, InterpolationMode interpolationMode, PixelOffsetMode pixelOffsetMode, CompositingQuality compositingQuality, bool useEmbeddedColorManagement) {
			using(Image origImage = Image.FromFile(imagePath, useEmbeddedColorManagement)) {
				Resize(origImage, outputStream, newWidth, newHeight, resizeFlags, smoothingMode, interpolationMode, pixelOffsetMode, compositingQuality);
			}
		}

		/// <summary>
		/// Resizes an image to a new size. Resizing neither stretches nor enlarges a picture by default.
		/// </summary>
		/// <param name="imageStream">The stream containing the image to resize.</param>
		/// <param name="newWidth">The new width of the image. Set to 0 to let the height decide.</param>
		/// <param name="newHeight">The new height of the image. Set to 0 to let the width decide.</param>
		/// <returns>A memory stream containing the resized image.
		/// Returns null if the original file was neither an image nor found.
		/// </returns>
		public static MemoryStream Resize(Stream imageStream, int newWidth, int newHeight) {
			return Resize(imageStream, newWidth, newHeight, SmoothingMode.Default, InterpolationMode.Default, PixelOffsetMode.Default, CompositingQuality.Default, false);
		}

		/// <summary>
		/// Resizes an image to a new size. Resizing neither stretches nor enlarges a picture by default.
		/// </summary>
		/// <param name="imageStream">The stream containing the image to resize.</param>
		/// <param name="newWidth">The new width of the image. Set to 0 to let the height decide.</param>
		/// <param name="newHeight">The new height of the image. Set to 0 to let the width decide.</param>
		/// <param name="smoothingMode">Specifies the smoothing mode.</param>
		/// <param name="interpolationMode">Specifies the interpolation mode.</param>
		/// <param name="pixelOffsetMode">Specifies the pixel offset mode.</param>
		/// <returns>A memory stream containing the resized image.
		/// Returns null if the original file was neither an image nor found.
		/// </returns>
		public static MemoryStream Resize(Stream imageStream, int newWidth, int newHeight, SmoothingMode smoothingMode, InterpolationMode interpolationMode, PixelOffsetMode pixelOffsetMode, CompositingQuality compositingQuality, bool useEmbeddedColorManagement) {
			MemoryStream ms = new MemoryStream();
			Resize(imageStream, ms, newWidth, newHeight, ResizeFlags.None, smoothingMode, interpolationMode, pixelOffsetMode, compositingQuality, useEmbeddedColorManagement);
			return ms;
		}

		/// <summary>
		/// Resizes an image to a new size. Resizing neither stretches nor enlarges a picture by default.
		/// </summary>
		/// <param name="imageStream">The stream containing the image to resize.</param>
		/// <param name="outputStream">The stream containing the resized image.</param>
		/// <param name="newWidth">The new width of the image. Set to 0 to let the height decide.</param>
		/// <param name="newHeight">The new height of the image. Set to 0 to let the width decide.</param>
		public static void Resize(Stream imageStream, Stream outputStream, int newWidth, int newHeight) {
			Resize(imageStream, outputStream, newWidth, newHeight, SmoothingMode.Default, InterpolationMode.Default, PixelOffsetMode.Default, CompositingQuality.Default, false);
		}

		/// <summary>
		/// Resizes an image to a new size. Resizing neither stretches nor enlarges a picture by default.
		/// </summary>
		/// <param name="imageStream">The stream containing the image to resize.</param>
		/// <param name="outputStream">The stream containing the resized image.</param>
		/// <param name="newWidth">The new width of the image. Set to 0 to let the height decide.</param>
		/// <param name="newHeight">The new height of the image. Set to 0 to let the width decide.</param>
		/// <param name="smoothingMode">Specifies the smoothing mode.</param>
		/// <param name="interpolationMode">Specifies the interpolation mode.</param>
		/// <param name="pixelOffsetMode">Specifies the pixel offset mode.</param>
		public static void Resize(Stream imageStream, Stream outputStream, int newWidth, int newHeight, SmoothingMode smoothingMode, InterpolationMode interpolationMode, PixelOffsetMode pixelOffsetMode, CompositingQuality compositingQuality, bool useEmbeddedColorManagement) {
			Resize(imageStream, outputStream, newWidth, newHeight, ResizeFlags.None, smoothingMode, interpolationMode, pixelOffsetMode, compositingQuality, useEmbeddedColorManagement);
		}

		/// <summary>
		/// Resizes an image to a new size. Resizing neither stretches nor enlarges a picture by default.
		/// </summary>
		/// <param name="imageStream">The stream containing the image to resize.</param>
		/// <param name="newWidth">The new width of the image. Set to 0 to let the height decide.</param>
		/// <param name="newHeight">The new height of the image. Set to 0 to let the width decide.</param>
		/// <param name="resizeFlags">Specify the different flags in the ResizeFlags enumeration to deviate from the default behaviour.</param>
		/// <returns>A memory stream containing the resized image.
		/// Returns null if the original file was neither an image nor found.
		/// </returns>
		public static MemoryStream Resize(Stream imageStream, int newWidth, int newHeight, ResizeFlags resizeFlags) {
			return Resize(imageStream, newWidth, newHeight, resizeFlags, SmoothingMode.Default, InterpolationMode.Default, PixelOffsetMode.Default, CompositingQuality.Default, false);
		}

		/// <summary>
		/// Resizes an image to a new size. Resizing neither stretches nor enlarges a picture by default. 
		/// </summary>
		/// <remarks>Set both newWidth and newHeight to 0 to prevent resizing.</remarks>
		/// <param name="imageStream">The stream containing the image to resize.</param>
		/// <param name="newWidth">The new width of the image. Set to 0 to let the height decide.</param>
		/// <param name="newHeight">The new height of the image. Set to 0 to let the width decide.</param>
		/// <param name="resizeFlags">Specify the different flags in the ResizeFlags enumeration to deviate from the default behaviour.</param>
		/// <param name="smoothingMode">Specifies the smoothing mode.</param>
		/// <param name="interpolationMode">Specifies the interpolation mode.</param>
		/// <param name="pixelOffsetMode">Specifies the pixel offset mode.</param>
		/// <returns>A memory stream containing the resized image.
		/// Returns null if the original file was neither an image nor found.
		/// </returns>
		public static MemoryStream Resize(Stream imageStream, int newWidth, int newHeight, ResizeFlags resizeFlags, SmoothingMode smoothingMode, InterpolationMode interpolationMode, PixelOffsetMode pixelOffsetMode, CompositingQuality compositingQuality, bool useEmbeddedColorManagement) {
			try {
				MemoryStream ms = new MemoryStream();
				Resize(imageStream, ms, newWidth, newHeight, resizeFlags, smoothingMode, interpolationMode, pixelOffsetMode, compositingQuality, useEmbeddedColorManagement);
				return ms;
			} catch(OutOfMemoryException) {
				// If not image
			}
			return null;
		}

		/// <summary>
		/// Resizes an image to a new size. Resizing neither stretches nor enlarges a picture by default.
		/// </summary>
		/// <param name="imageStream">The stream containing the image to resize.</param>
		/// <param name="outputStream">The stream containing the resized image.</param>
		/// <param name="newWidth">The new width of the image. Set to 0 to let the height decide.</param>
		/// <param name="newHeight">The new height of the image. Set to 0 to let the width decide.</param>
		/// <param name="resizeFlags">Specify the different flags in the ResizeFlags enumeration to deviate from the default behaviour.</param>
		public static void Resize(Stream imageStream, Stream outputStream, int newWidth, int newHeight, ResizeFlags resizeFlags) {
			Resize(imageStream, outputStream, newWidth, newHeight, resizeFlags, SmoothingMode.Default, InterpolationMode.Default, PixelOffsetMode.Default, CompositingQuality.Default, false);
		}

		/// <summary>
		/// Resizes an image to a new size. Resizing neither stretches nor enlarges a picture by default.
		/// </summary>
		/// <param name="imageStream">The stream containing the image to resize.</param>
		/// <param name="outputStream">The stream containing the resized image.</param>
		/// <param name="newWidth">The new width of the image. Set to 0 to let the height decide.</param>
		/// <param name="newHeight">The new height of the image. Set to 0 to let the width decide.</param>
		/// <param name="resizeFlags">Specify the different flags in the ResizeFlags enumeration to deviate from the default behaviour.</param>
		/// <param name="smoothingMode">Specifies the smoothing mode.</param>
		/// <param name="interpolationMode">Specifies the interpolation mode.</param>
		/// <param name="pixelOffsetMode">Specifies the pixel offset mode.</param>
		public static void Resize(Stream imageStream, Stream outputStream, int newWidth, int newHeight, ResizeFlags resizeFlags, SmoothingMode smoothingMode, InterpolationMode interpolationMode, PixelOffsetMode pixelOffsetMode, CompositingQuality compositingQuality, bool useEmbeddedColorManagement) {
			using(Image origImage = Image.FromStream(imageStream, useEmbeddedColorManagement)) {
				Resize(origImage, outputStream, newWidth, newHeight, resizeFlags, smoothingMode, interpolationMode, pixelOffsetMode, compositingQuality);
			}
		}
		#endregion

		#region Helper Methods
		/// <summary>
		/// Calculates a proportional file size.		
		/// </summary>		
		/// <param name="origPhysSize">Original size of image.</param>
		/// <param name="newWidth">Widht requested. Set to 0 to let height decide.</param>
		/// <param name="newHeight">Height requested. Set to 0 to let width decide.</param>
		/// <param name="allowEnlarge">Set to true to allow an image to become larger than original.</param>
		/// <returns>Calculated size.</returns>
		public static Size GetProportionalImageSize(Size origPhysSize, int newWidth, int newHeight, bool allowEnlarge) {
			if(newWidth == 0 && newHeight == 0) {
				return origPhysSize;
			}

			Size newSize;
			float ratio = (float)origPhysSize.Width / origPhysSize.Height;

			if((origPhysSize.Width >= origPhysSize.Height && newHeight != 0 && (newHeight * ratio < newWidth || newWidth == 0)) ||
				(origPhysSize.Width < origPhysSize.Height && !(newWidth != 0 && (newWidth < newHeight * ratio || newHeight == 0)))) {
				newSize = new Size((int)Math.Ceiling(newHeight * ratio), newHeight);
			} else {
				newSize = new Size(newWidth, (int)Math.Ceiling(newWidth / ratio));
			}

			if(((newSize.Height > origPhysSize.Height || newSize.Width > origPhysSize.Width) && allowEnlarge) ||
				newSize.Width < origPhysSize.Width || newSize.Height < origPhysSize.Height) {
				return newSize;
			}
			return origPhysSize;
		}
		public static Size GetNonProportionalImageSize(Size origPhysSize, int newWidth, int newHeight, bool allowEnlarge) {
			return (newWidth > origPhysSize.Width || newHeight > origPhysSize.Height) && !allowEnlarge ?
			origPhysSize : new Size(newWidth == 0 ? origPhysSize.Width : newWidth, newHeight == 0 ? origPhysSize.Height : newHeight);
		}
		#endregion
	}
}
