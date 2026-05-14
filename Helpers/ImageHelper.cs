namespace Diploma.Helpers
{
    public static class ImageHelper
    {
        public static BitmapImage BytesToImage(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0) return null!;

            var image = new BitmapImage();
            using var mem = new MemoryStream(imageData);
            mem.Position = 0;
            image.BeginInit();
            image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = null;
            image.StreamSource = mem;
            image.EndInit();
            image.Freeze();
            return image;
        }

        public static byte[] ImageToBytes(BitmapImage image)
        {
            using var memStream = new MemoryStream();
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));
            encoder.Save(memStream);
            return memStream.ToArray();
        }
    }
}