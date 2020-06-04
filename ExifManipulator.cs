using ImageMagick;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExifManipulationLibrary
{
    public class ExifManipulator
    {
        private static void DecimalDeg2Triple(double degrees, out int d, out int m, out double s)
        {
            d = (int)degrees;
            m = (int)((degrees - d) * 60);
            s = ((((degrees - d) * 60) - m) * 60);
        }

        public static bool SaveGPS2Image(string src_imageURI, string dst_imageURI, double lat, double lon, double alt)
        {
            bool bResult = false;
            try
            {
                int dlat, mlat, dlon, mlon;
                double slat, slon;

                DecimalDeg2Triple(lat, out dlat, out mlat, out slat);
                DecimalDeg2Triple(lon, out dlon, out mlon, out slon);

                Rational[] latitude = new Rational[] { new Rational(dlat), new Rational(mlat), new Rational(slat) };
                Rational[] longitude = new Rational[] { new Rational(dlon), new Rational(mlon), new Rational(slon) };
                Rational altitude = new Rational(alt);
                //Rational orientation = new Rational(0);

                // Read image from file
                MagickImage image = new MagickImage(src_imageURI);

                // Retrieve the exif information
                ExifProfile profile = image.GetExifProfile();

                // Check if image contains an exif profile
                if (profile == null)
                {
                    profile = new ExifProfile();

                    image.AddProfile(profile);
                    image.Write(@".\without_m.jpg");
                }

                // Write GPS data
                profile.SetValue(ExifTag.GPSLatitude, latitude);
                //Rational latRef = new Rational(dlat < 0 ? "S" : "N");
                string latRef = dlat < 0 ? "S" : "N";
                profile.SetValue(ExifTag.GPSLatitudeRef, latRef);

                profile.SetValue(ExifTag.GPSLongitude, longitude);
                //Rational lonRef = new Rational(dlon < 0 ? 'W' : 'E');
                string lonRef = dlon < 0 ? "W" : "E";
                profile.SetValue(ExifTag.GPSLongitudeRef, lonRef);

                profile.SetValue(ExifTag.GPSAltitude, altitude);

                profile.SetValue(ExifTag.Orientation, (UInt16)1);

                //profile.SetValue(ExifTag.Or, (UInt16)1);

                image.AddProfile(profile);
                image.Write(dst_imageURI);

                bResult = true;
            }
            catch (Exception e)
            {
                bResult = false;
            }

            return bResult;
        }
    }
}
