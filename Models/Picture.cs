using Starfield_Interactive_Smart_Slate.Models.Entities;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Starfield_Interactive_Smart_Slate.Models
{
    public class Picture
    {
        public int PictureID { get; set; }
        public Uri? PictureUri { get; set; }
        public BitmapImage? PictureBitmap { get; set; }
        public Uri? ThumbnailUri { get; set; }
        public BitmapImage? ThumbnailBitmap { get; set; }
        public bool IsPlaceholder { get; set; } = false;
        public bool Corrupted { get; set; } = false;

        private static string PicturesFolder = "Pictures";
        private static string DeletedFolder = "Deleted";
        private static string ThumbnailPostfix = "_thumbnail";

        public Picture(int pictureID, Uri pictureUri)
        {
            PictureID = pictureID;
            PictureUri = pictureUri;

            if (File.Exists(pictureUri.LocalPath))
            {
                BitmapImage pictureBitmap;
                using (FileStream stream = File.OpenRead(pictureUri.LocalPath))
                {
                    pictureBitmap = new BitmapImage();
                    pictureBitmap.BeginInit();
                    pictureBitmap.CacheOption = BitmapCacheOption.OnLoad;
                    pictureBitmap.StreamSource = stream;
                    pictureBitmap.EndInit();
                    PictureBitmap = pictureBitmap;
                }

                // see if thumbnail exists
                var pictureLocalPath = pictureUri.LocalPath;
                var filePathWithoutExtension = Path.Combine(
                    Path.GetDirectoryName(pictureLocalPath),
                    Path.GetFileNameWithoutExtension(pictureLocalPath)
                );
                var thumbnailFileName = filePathWithoutExtension + ThumbnailPostfix;
                var thumbnailFilePath = Path.ChangeExtension(thumbnailFileName, Path.GetExtension(pictureLocalPath));

                if (File.Exists(thumbnailFilePath))
                {
                    ThumbnailUri = new Uri(thumbnailFilePath);
                    using (FileStream stream = new FileStream(thumbnailFilePath, FileMode.Open))
                    {
                        var thumbnailBitmap = new BitmapImage();
                        thumbnailBitmap.BeginInit();
                        thumbnailBitmap.CacheOption = BitmapCacheOption.OnLoad;
                        thumbnailBitmap.StreamSource = stream;
                        thumbnailBitmap.EndInit();
                        ThumbnailBitmap = thumbnailBitmap;
                    }
                }
                else
                {
                    ThumbnailBitmap = pictureBitmap;
                }
            }
            else
            {
                Corrupted = true;
            }
        }

        public Picture()
        {
            IsPlaceholder = true;
        }

        public override bool Equals(object? obj)
        {
            if (obj is Picture)
            {
                return PictureID == ((Picture)obj).PictureID;
            }
            else
            {
                return false;
            }
        }

        public Picture DeepCopy()
        {
            if (IsPlaceholder)
            {
                return new Picture();
            }
            else
            {
                return new Picture(PictureID, PictureUri);
            }
        }

        // import either a picture Uri or direct BitmapSource
        public static Uri ImportPicture(CelestialBody celestialBody, Entity entity, Uri? picture = null, BitmapSource? directSource = null)
        {
            // create folders if needed
            var baseFolder = DatabaseInitializer.UserDatabaseFolder();
            var picturesFolder = Path.Combine(baseFolder, PicturesFolder);
            if (!Directory.Exists(picturesFolder))
            {
                Directory.CreateDirectory(picturesFolder);
            }

            var solarSystemFolder = Path.Combine(picturesFolder, RemoveDisallowedCharacters(celestialBody.SystemName));
            if (!Directory.Exists(solarSystemFolder))
            {
                Directory.CreateDirectory(solarSystemFolder);
            }

            var celestialBodyFolder = Path.Combine(solarSystemFolder, RemoveDisallowedCharacters(celestialBody.BodyName));
            if (!Directory.Exists(celestialBodyFolder))
            {
                Directory.CreateDirectory(celestialBodyFolder);
            }

            // build internal file name
            string fileName = entity.Name;
            fileName += $"_{DateTime.Now:yyyy_MM_dd_HHmmss}";
            fileName = RemoveDisallowedCharacters(fileName);

            // add unique modifier if same-name file exists
            var destinationFileName = Path.Combine(celestialBodyFolder, fileName);
            var destinationExtension = picture != null ? Path.GetExtension(picture.LocalPath) : ".png";
            var destinationFilePath = Path.ChangeExtension(destinationFileName, destinationExtension);
            var attempt = 0;
            while (Path.Exists(destinationFilePath))
            {
                attempt++;
                destinationFileName = Path.Combine(celestialBodyFolder, fileName + $"({attempt})");
                destinationFilePath = Path.ChangeExtension(destinationFileName, destinationExtension);
            }

            var destinationUri = new Uri(destinationFilePath);

            if (picture != null)
            {
                File.Copy(picture.LocalPath, destinationFilePath);
                CreateThumbnail(picture, destinationFileName);
            }
            else
            {
                using (FileStream stream = new FileStream(destinationFilePath, FileMode.Create))
                {
                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(directSource));
                    encoder.Save(stream);
                }
                CreateThumbnail(destinationUri, destinationFileName);
            }

            return destinationUri;
        }

        private static void CreateThumbnail(Uri picture, string destinationFileName)
        {
            // create thumbnail sized version for grid display
            double maxDimension = 200;
            var originalImage = new BitmapImage(picture);
            double originalWidth = originalImage.Width;
            double originalHeight = originalImage.Height;
            double scaleRatio;

            if (originalWidth > maxDimension || originalHeight > maxDimension)
            {
                // convert shorter edge to maxDimension
                if (originalWidth > originalHeight)
                {
                    scaleRatio = maxDimension / originalHeight;
                }
                else
                {
                    scaleRatio = maxDimension / originalWidth;
                }
                TransformedBitmap resizedImage = new TransformedBitmap(originalImage, new ScaleTransform(scaleRatio, scaleRatio));
                BitmapSource bitmapSource = BitmapFrame.Create(resizedImage);
                BitmapEncoder encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

                var thumbnailName = destinationFileName + ThumbnailPostfix;
                var thumbnailPath = Path.ChangeExtension(thumbnailName, Path.GetExtension(picture.LocalPath));

                using (FileStream stream = new FileStream(thumbnailPath, FileMode.Create))
                {
                    encoder.Save(stream);
                }
            }
        }

        public void MoveToDeletedFolder()
        {
            var picturePath = PictureUri?.LocalPath;
            if (File.Exists(picturePath))
            {
                var pictureFolder = Path.GetDirectoryName(picturePath);
                var deletedFolder = Path.Combine(pictureFolder, DeletedFolder);

                if (!Directory.Exists(deletedFolder))
                {
                    Directory.CreateDirectory(deletedFolder);
                }

                var deletedPath = Path.Combine(deletedFolder, Path.GetFileName(picturePath));

                File.Move(picturePath, deletedPath);
            }

            var thumbnailPath = ThumbnailUri?.LocalPath;
            if (File.Exists(thumbnailPath))
            {
                var thumbnailFolder = Path.GetDirectoryName(thumbnailPath);
                var deletedFolder = Path.Combine(thumbnailFolder, DeletedFolder);

                if (!Directory.Exists(deletedFolder))
                {
                    Directory.CreateDirectory(deletedFolder);
                }

                var deletedPath = Path.Combine(deletedFolder, Path.GetFileName(thumbnailPath));

                File.Move(thumbnailPath, deletedPath);
            }
        }

        private static string RemoveDisallowedCharacters(string fileName)
        {
            // Disallowed in Windows file names
            string pattern = "[\\/:*?\"<>|]";
            return Regex.Replace(fileName, pattern, "");
        }
    }
}
