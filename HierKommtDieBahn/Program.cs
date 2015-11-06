using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace de.offenesthueringen.data.DieBahnKommt
{

    public class Point : IEquatable<Point>
    {

        public double X { get; private set; }
        public double Y { get; private set; }


        public Point(double x, double y)
        {
            this.X = x;
            this.Y = y;
        }

        public Boolean Equals(Point other)
        {

            if (this.X != other.X)
                return false;

            if (this.Y != other.Y)
                return false;

            return true;

        }

    }


    public class Program
    {

        /// <summary>
        /// Uses the Douglas Peucker algorithim to reduce the number of points.
        /// </summary>
        /// <param name="Points">The points.</param>
        /// <param name="Tolerance">The tolerance.</param>
        /// <returns></returns>
        public static IList<Point> DouglasPeuckerReduction(IList<Point> Points, double Tolerance)
        {

            if (Points == null || Points.Count < 3)
                return Points;

            int firstPoint = 0;
            int lastPoint = Points.Count - 1;
            var pointIndexsToKeep = new List<int>();

            // Add the first and last index to the keepers
            pointIndexsToKeep.Add(firstPoint);
            pointIndexsToKeep.Add(lastPoint);

            // The first and the last point can not be the same
            while (Points[firstPoint].Equals(Points[lastPoint]))
                lastPoint--;

            DouglasPeuckerReduction(Points, firstPoint, lastPoint, Tolerance, ref pointIndexsToKeep);

            var returnPoints = new List<Point>();
            pointIndexsToKeep.Sort();
            foreach (int index in pointIndexsToKeep)
            {
                returnPoints.Add(Points[index]);
            }

            return returnPoints;
        }


        /// <summary>
        /// Douglases the peucker reduction.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="firstPoint">The first point.</param>
        /// <param name="lastPoint">The last point.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <param name="pointIndexsToKeep">The point indexs to keep.</param>
        private static void DouglasPeuckerReduction(IList<Point> points, int firstPoint, int lastPoint, double tolerance, ref List<int> pointIndexsToKeep)
        {
            double maxDistance = 0;
            int indexFarthest = 0;

            for (int index = firstPoint; index < lastPoint; index++)
            {
                double distance = PerpendicularDistance(points[firstPoint], points[lastPoint], points[index]);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    indexFarthest = index;
                }
            }

            if (maxDistance > tolerance && indexFarthest != 0)
            {
                //Add the largest point that exceeds the tolerance
                pointIndexsToKeep.Add(indexFarthest);

                DouglasPeuckerReduction(points, firstPoint, indexFarthest, tolerance, ref pointIndexsToKeep);
                DouglasPeuckerReduction(points, indexFarthest, lastPoint, tolerance, ref pointIndexsToKeep);
            }

        }

        /// <summary>
        /// The distance of a point from a line made from point1 and point2.
        /// </summary>
        /// <param name="pt1">The PT1.</param>
        /// <param name="pt2">The PT2.</param>
        /// <param name="p">The p.</param>
        /// <returns></returns>
        public static double PerpendicularDistance(Point Point1, Point Point2, Point Point)
        {
            //Area = |(1/2)(x1y2 + x2y3 + x3y1 - x2y1 - x3y2 - x1y3)|   *Area of triangle
            //Base = √((x1-x2)²+(x1-x2)²)                               *Base of Triangle*
            //Area = .5*Base*H                                          *Solve for height
            //Height = Area/.5/Base

            double area = Math.Abs(.5 * (Point1.X * Point2.Y + Point2.X * Point.Y + Point.X * Point1.Y - Point2.X * Point1.Y - Point.X * Point2.Y - Point1.X * Point.Y));
            double bottom = Math.Sqrt(Math.Pow(Point1.X - Point2.X, 2) + Math.Pow(Point1.Y - Point2.Y, 2));
            double height = area / bottom * 2;

            return height;
        }



        public static Boolean PolyContainsPoint(IEnumerable<Point>  Polygon,
                                                Point               PointToCheck)
        {

            var inside = false;

            // An imaginary closing segment is implied,
            // so begin testing with that.
            var v1 = Polygon.Last();//[Polygon.Count - 1];

            foreach (var PointOfPolygon in Polygon)
            {

                var d1 = (PointToCheck.Y - PointOfPolygon.Y) * (v1.X - PointOfPolygon.X);
                var d2 = (PointToCheck.X - PointOfPolygon.X) * (v1.Y - PointOfPolygon.Y);

                if (PointToCheck.Y < v1.Y)
                {
                    // V1 below ray
                    if (PointOfPolygon.Y <= PointToCheck.Y)
                    {

                        // V0 on or above ray
                        // Perform intersection test
                        if (d1 > d2)
                            inside = !inside; // Toggle state

                    }
                }

                else if (PointToCheck.Y < PointOfPolygon.Y)
                {

                    // V1 is on or above ray, V0 is below ray
                    // Perform intersection test
                    if (d1 < d2)
                        inside = !inside; // Toggle state

                }

                v1 = PointOfPolygon; //Store previous endpoint as next startpoint

            }

            return inside;

        }


        public static void Main(string[] args)
        {

            // BoundingBox Thüringen
            // 51.6490678544 9.8778443239 50.2042330625 12.6531964048
            // http://api.openstreetmap.org/api/0.6/relation/62366

            var Thüringen           = JObject.Parse(File.ReadAllText("Thüringen.geojson"))["features"][0]["geometry"]["coordinates"][0].Children().Select(v => new Point(v[0].Value<Double>(), v[1].Value<Double>())).ToList();
            var ThüringenSimple     = DouglasPeuckerReduction(Thüringen, 0.01);

            var file = new StreamWriter("Thueringensimple.geojson");
            file.WriteLine(@"var Thueringensimple = JSON.parse('{ \");
            file.WriteLine(@"  ""type"": ""FeatureCollection"", \");
            file.WriteLine(@"  ""features"": [ \");
            file.WriteLine(@"    { \");
            file.WriteLine(@"      ""type"": ""Feature"", \");
            file.WriteLine(@"      ""properties"": { }, \");
            file.WriteLine(@"      ""geometry"": { \");
            file.WriteLine(@"        ""type"": ""Polygon"", \");
            file.WriteLine(@"        ""coordinates"": [[ \");

            foreach (var line in ThüringenSimple.Take(ThüringenSimple.Count() - 1))
                file.WriteLine(@"[ " + line.X + @", " + line.Y + @" ], \");

            file.WriteLine(@"[ " + ThüringenSimple.Last().X + @", " + ThüringenSimple.Last().Y + @" ] \");

            file.WriteLine(@"        ]] \");
            file.WriteLine(@"      } \");
            file.WriteLine(@"    } \");
            file.WriteLine(@"  ] \");
            file.WriteLine(@"}');");

            file.Close();

            Console.WriteLine("Thüringen: " + Thüringen.Count() + " => " + ThüringenSimple.Count());

            var Streckenabschnitte  = JObject.Parse(File.ReadAllText("connectivity_2015_09.geojson"))["features"].Children();
            var BBoxFiltered        = Streckenabschnitte.
                                          Where(feature => feature["geometry"]["coordinates"].Children().Select(v => v[0].Value<Double>()).All(lng =>  9.8778443239 < lng && lng < 12.6531964048)).
                                          Where(feature => feature["geometry"]["coordinates"].Children().Select(v => v[1].Value<Double>()).All(lat => 50.2042330625 < lat && lat < 51.6490678544));
            var RectangleFiltered   = BBoxFiltered.
                                          Where(feature => feature["geometry"]["coordinates"].Children().
                                                               Select(v => new Point(v[0].Value<Double>(), v[1].Value<Double>())).
                                                               Any   (p => PolyContainsPoint(ThüringenSimple, p)));

            Console.WriteLine("Streckenabschnitte: " + Streckenabschnitte.Count() + " => " + BBoxFiltered.Count() + " => " + RectangleFiltered.Count());





            var Streckenabschnitte2 = new JObject(new JProperty("type",      "FeatureCollection"),
                                                  new JProperty("features",  new JArray(RectangleFiltered)));

            var sb = new StringBuilder("var Streckenabschnitte = JSON.parse('");

            foreach (var line in Streckenabschnitte2.ToString().Split(new String[] { Environment.NewLine }, StringSplitOptions.None))
                sb.AppendLine(line + " \\");

            sb.AppendLine("');");

            File.WriteAllText("connectivity_Thüringen_2015_09.geojson", sb.ToString());

            // {
            //
            //     "type": "Feature",
            //
            //     "geometry": {
            //         "type": "LineString",
            //         "coordinates": [
            //             [ 11.06535,           49.44201          ],
            //             [ 11.07071,           49.4427           ],
            //             [ 11.07126,           49.4428           ],
            //             [ 11.07177,           49.44295          ],
            //             [ 11.071865002158022, 49.44298166738601 ]
            //         ]
            //     },
            //
            //     "properties": {
            //         "all_stability":                 0.961,
            //         "all_measurements":              1157,
            //         "all,no4g_stability":            0.974,
            //         "all,no4g_measurements":         567,
            //
            //         "t-mobile_stability":            0.929,
            //         "t-mobile_measurements":         437,
            //         "t-mobile,no4g_stability":       0.971,
            //         "t-mobile,no4g_measurements":    188,
            //
            //         "vodafone_stability":            0.981,
            //         "vodafone_measurements":         364,
            //         "vodafone,no4g_stability":       0.98,
            //         "vodafone,no4g_measurements":    205,
            //
            //         "e-plus_stability":              0.981,
            //         "e-plus_measurements":           112,
            //         "e-plus,no4g_stability":         0.981,
            //         "e-plus,no4g_measurements":      94,
            //
            //         "o2_stability":                  0.979,
            //         "o2_measurements":               244,
            //         "o2,no4g_stability":             0.957,
            //         "o2,no4g_measurements":          81
            //     }
            //
            // }


        }

    }

}
