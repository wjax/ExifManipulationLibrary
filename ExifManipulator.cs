using ExifLib;
using ExifManipulationLibrary.Extensions;
using ImageMagick;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ExifManipulationLibrary
{
    public class ExifManipulator
    {
        private static readonly Regex _nullDateTimeMatcher = new Regex(@"^[\s0]{4}[:\s][\s0]{2}[:\s][\s0]{5}[:\s][\s0]{2}[:\s][\s0]{2}$");
        private const string _subSecPattern = @"^[0-9]+$";

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
                IExifProfile profile = image.GetExifProfile();

                // Check if image contains an exif profile
                if (profile == null)
                {
                    profile = new ExifProfile();

                    image.SetProfile(profile);
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
                profile.SetValue(ExifTag.GPSAltitudeRef, (byte)(alt > 0 ? 0 : 1));

                profile.SetValue(ExifTag.Orientation, (UInt16)1);

                //profile.SetValue(ExifTag.Or, (UInt16)1);

                image.SetProfile(profile);
                image.Write(dst_imageURI);

                bResult = true;
            }
            catch (Exception e)
            {
                bResult = false;
            }

            return bResult;
        }

        public static bool GetDateTimeDigitized(string filePath, bool assumeUTC, out DateTime result)
        {
            bool valid = false;
            result = DateTime.MinValue;
            double secondsDateTime = 0;

            if (filePath != null && System.IO.File.Exists(filePath))
            {
                using (var image = new MagickImage(filePath))
                {
                    // Retrieve the exif information
                    var profile = image.GetExifProfile();

                    // Check if image contains an exif profile
                    if (profile is null)
                        System.Diagnostics.Debug.WriteLine("Image does not contain exif information.");
                    else
                    {
                        // Write all values to the console
                        var dateTimeDigitized = profile.GetValue(ExifTag.DateTimeDigitized);
                        var subSeconds = profile.GetValue(ExifTag.SubsecTimeDigitized);

                        if (dateTimeDigitized != null && ToDateTime(dateTimeDigitized.Value, assumeUTC, out result))
                        {
                            if (subSeconds != null && double.TryParse(subSeconds.Value, out double dSubSeconds))
                                secondsDateTime = dSubSeconds / 100;

                            result = result.AddSeconds(secondsDateTime);
                            valid = true;
                        }
                        else
                            valid = false;

                    }
                }
            }

            return valid;
        }

        public static bool GetDateTimeDigitizedFast(string filePath, bool assumeUTC, out DateTime result)
        {
            bool valid = false;
            result = DateTime.MinValue;

            if (filePath != null && System.IO.File.Exists(filePath))
            {
                // Instantiate the reader
                using (ExifReader reader = new ExifReader(filePath))
                {
                    // Extract the tag data using the ExifTags enumeration
                    if (reader.GetTagValue(ExifTags.DateTimeDigitized, out DateTime datePictureTaken))
                    {
                        if (reader.GetTagValue(ExifTags.SubsecTimeDigitized, out string subSec))
                            datePictureTaken = AddSubsecTimeDigitized(datePictureTaken, subSec);

                        if (assumeUTC)
                            result = datePictureTaken.AsUtc();
                        else
                            result = datePictureTaken.AsLocal();

                        valid = true;
                    }
                    else
                        throw new ArgumentException("No Date info in EXIF");
                }
            }
            else
                throw new ArgumentException("Path does not exist");

            return valid;
        }
        
        private static DateTime AddSubsecTimeDigitized (DateTime datePictureTaken, string subSec)
        {
            if (subSec != null && Regex.IsMatch(subSec, _subSecPattern))
            {
                subSec = $"0.{subSec.Trim()}";
                double dSubSeconds = double.Parse(subSec, CultureInfo.InvariantCulture);
                return datePictureTaken.AddSeconds(dSubSeconds);
            }
            
            return datePictureTaken;
        }

        private static bool ToDateTime(string str, bool assumeUTC, out DateTime result)
        {
            // From page 28 of the Exif 2.2 spec (http://www.exif.org/Exif2-2.PDF): 

            // "When the field is left blank, it is treated as unknown ... When the date and time are unknown, 
            // all the character spaces except colons (":") may be filled with blank characters"
            if (string.IsNullOrEmpty(str) || _nullDateTimeMatcher.IsMatch(str))
            {
                result = DateTime.MinValue;
                return false;
            }

            // Do Conversion
            DateTimeStyles style = assumeUTC ? DateTimeStyles.AssumeUniversal : DateTimeStyles.AssumeLocal;

            // There are 2 types of date - full date/time stamps, and plain dates. Dates are 10 characters long.
            if (str.Length == 10)
            {
                result = DateTime.ParseExact(str, "yyyy:MM:dd", CultureInfo.InvariantCulture, style);
                return true;
            }

            // "The format is "YYYY:MM:DD HH:MM:SS" with time shown in 24-hour format, and the date and time separated by one blank character [20.H].
            result = DateTime.ParseExact(str, "yyyy:MM:dd HH:mm:ss", CultureInfo.InvariantCulture, style);

            if (assumeUTC)
                result = result.ToUniversalTime();
            else
                result = result.ToLocalTime();

            return true;
        }
    }
}
