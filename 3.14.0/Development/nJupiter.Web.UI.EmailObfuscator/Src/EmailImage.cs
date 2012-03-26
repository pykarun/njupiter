#region Copyright & License
/*
	Copyright (c) 2005-2010 nJupiter

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
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace nJupiter.Web.UI.EmailObfuscator {

	internal sealed class EmailImage : IDisposable {

		private readonly	Font	font		= new Font(FontFamily.GenericSerif, 16, FontStyle.Underline, GraphicsUnit.Pixel);
		private readonly	Color	fontColor	= System.Drawing.ColorTranslator.FromHtml("#0000ff");
		private				string	text;
		private				bool	disposed;

		public string Email { get { return this.text; } set { this.text = value; } }

		#region Methods
		public void RenderImage(Stream stream) {
			
			Bitmap bmp;
			int width;
			int height;

			using(bmp = new Bitmap(1, 1, PixelFormat.Format32bppArgb)) { //Create a 1x1 bitmap to use to messure the size of a string
				Graphics g = Graphics.FromImage(bmp);
				g.PageUnit = GraphicsUnit.Pixel;
				SizeF textSize = g.MeasureString(this.text, this.font);
				width = (int)textSize.Width;
				height = (int)textSize.Height;
			}

			using(bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb)){ //Create the real bitmap and render the string
				using(Graphics g = Graphics.FromImage(bmp)) {
					Rectangle rectangle = new Rectangle(0, 0, width, height);
					g.SmoothingMode = SmoothingMode.None;
					g.PageUnit = GraphicsUnit.Pixel;
					g.FillRectangle(Brushes.White, rectangle); // We make the background white, which is going to be transparent later
					g.DrawString(this.text, this.font, Brushes.Black, 0,0, StringFormat.GenericDefault); // Since the text has only one color and we rerender the image later we can write it in black
				}
				using(Bitmap final = MakeTransparent(bmp)) { //Convert bitmap to transparent gif
					final.Save(stream, ImageFormat.Gif); //And save to stream
				}
			}
		}

		// More info here http://support.microsoft.com/default.aspx?scid=kb;EN-US;Q319061
		private Bitmap MakeTransparent(Image image) {
			
			const int nColors = 2;
			int width	= image.Width;
			int height	= image.Height;

			Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format8bppIndexed); //Create a bitmap with Format8bppIndexed witch is the color table for GIF-images

			ColorPalette pal;
			using(Bitmap b = new Bitmap(1, 1, PixelFormat.Format1bppIndexed)) { //Make a 2 colored indexed palette
				pal = b.Palette;
			}

			pal.Entries[0] = Color.FromArgb(255, this.fontColor);
			pal.Entries[1] = Color.FromArgb(0, 255, 255, 255); //Set the last color to alpha 0 to make it transparent

			bitmap.Palette = pal; //Set the palette to our new bitmap

			using(Bitmap bmpCopy = new Bitmap(width, height, PixelFormat.Format32bppArgb)) { //Copy the original bitmap into an indexed bitmap and use the palette above

				using(Graphics g = Graphics.FromImage(bmpCopy)) {
					g.SmoothingMode	= SmoothingMode.None;
					g.PageUnit		= GraphicsUnit.Pixel;
					g.DrawImage(image, 0, 0, width, height);
				}

				Rectangle rectangle = new Rectangle(0, 0, width, height);
				BitmapData bitmapData = bitmap.LockBits(rectangle, ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

				IntPtr pixels = bitmapData.Scan0;

				unsafe {
					byte* pBits;
					if(bitmapData.Stride > 0)
						pBits = (byte*)pixels.ToPointer();
					else
						pBits = (byte*)pixels.ToPointer() + bitmapData.Stride * (height - 1);

					uint stride = (uint)Math.Abs(bitmapData.Stride);

					for(uint row = 0; row < height; ++row) {
						for(uint col = 0; col < width; ++col) {
							byte* p8BppPixel = pBits + row * stride + col;
							Color pixel = bmpCopy.GetPixel((int)col, (int)row);
							double luminance = (pixel.R * 0.299) +
								(pixel.G * 0.587) +
								(pixel.B * 0.114);
							*p8BppPixel = (byte)(luminance * (nColors - 1) / 255 + 0.5);
						}
					}
				}
				bitmap.UnlockBits(bitmapData);
			}
			return bitmap;
		}
		#endregion


		#region IDisposable Members
		public void Dispose() {
			Dispose(true);
		}

		private void Dispose(bool disposing) {
			if(!this.disposed) {

				if(this.font != null){
					this.font.Dispose();
				}
				
				// Suppress finalization of this disposed instance.
				if (disposing) 
					GC.SuppressFinalize(this);

				this.disposed = true;
			}
		}

		~EmailImage(){
			Dispose(false);
		}
		#endregion
	}
}