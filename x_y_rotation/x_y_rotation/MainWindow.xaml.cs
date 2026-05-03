using System.Buffers.Text;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using HelixToolkit.Wpf;

namespace x_y_rotation;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
/// 
public record Continent(string Name, string CsvPath);
public enum ProjectionType
{Central, Lambert}


public partial class MainWindow : Window
{
   
    private readonly List<Continent> continents = new()
{
    new("Összes kontinens", string.Empty),
    new("Európa", @"D:\Dzseni\Egyetem\szakdolgozat_vetuletek\Europe.csv"),
    new("Afrika", @"D:\Dzseni\Egyetem\szakdolgozat_vetuletek\Africa.csv"),
    new("Antarktisz", @"D:\Dzseni\Egyetem\szakdolgozat_vetuletek\Antarctica.csv"),
    new("Ázsia", @"D:\Dzseni\Egyetem\szakdolgozat_vetuletek\Asia.csv"),
    new("Ausztrália", @"D:\Dzseni\Egyetem\szakdolgozat_vetuletek\Australia.csv"),
    new("Észak-Amerika", @"D:\Dzseni\Egyetem\szakdolgozat_vetuletek\NorthAmerica.csv"),
    new("Dél-Amerika", @"D:\Dzseni\Egyetem\szakdolgozat_vetuletek\SouthAmerica.csv"),
    new("Geodéziai buffer",@"D:\Dzseni\Egyetem\szakdolgozat_vetuletek\buffer_simple.csv")
};
    const double R_central = 6.371;

    private ProjectionType currentProjection = ProjectionType.Central;

    private PointCollection continentLatLon = null!;
    private  Point3DCollection continentECEF = null!;

    private readonly LinesVisual3D projected = new();


    private double centerLon;
    private double centerLat;
    private double deltaLon;
    private double deltaLat;
    public MainWindow()
    {
        InitializeComponent();

        ContinentBox.ItemsSource = continents;
        ContinentBox.SelectedIndex = 0;

        rightPort.Children.Add(projected);

        EuropeEcefLine.Fill = Brushes.Black;
        EuropeEcefLine.Diameter = 0.05;


        projected.Color = Colors.Black;
        projected.Thickness = 1.0;

        A1.Fill = Brushes.Black;
        A2.Fill = Brushes.Black;
        A3.Fill = Brushes.Black;
        A4.Fill = Brushes.Black;

    }
    private void ContinentChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ContinentBox.SelectedItem is not Continent c)
            return;

        if (string.IsNullOrEmpty(c.CsvPath))
        {
            LoadAllContinents();
        }
        else
        {
            LoadContinent(c);
        }
    }

    private void ProjectionChanged(object sender, SelectionChangedEventArgs e)
    {
        currentProjection = ProjectionBox.SelectedIndex switch
        {
            1 => ProjectionType.Lambert,
            _ => ProjectionType.Central
        };

        RecalculateProjection();
    }
    private void SliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        deltaLat = element_x.Value * Math.PI / 180;
        deltaLon = element_y.Value * Math.PI / 180;

        double lon = centerLon + deltaLon;
        double lat = centerLat + deltaLat;

        ApplyPlaneTransform(lon, lat);

        RecalculateProjection();
        UpdateCornerArrows();
    }

    private void LoadContinent(Continent continent)
    {

        continentLatLon = GetGeodeticsAsRadian(continent.CsvPath);

        Point3DCollection ecef = new(continentLatLon.Count);

        foreach (Point point in continentLatLon)
        {
            double lat = point.Y;
            double lon = point.X;

            double sinLat = Math.Sin(lat);
            double cosLat = Math.Cos(lat);
            double sinLon = Math.Sin(lon);
            double cosLon = Math.Cos(lon);

            ecef.Add(new Point3D(
                R_central * cosLat * cosLon,
                R_central * cosLat * sinLon,
                R_central * sinLat
            ));
        }
        ecef.Freeze();
        continentECEF = ecef;

        EuropeEcefLine.Path = continentECEF;
        RecalculateProjection();

        var (avgLon, avgLat) = GetAverageLatLon(continentLatLon);
        centerLon = avgLon;
        centerLat = avgLat;

        deltaLon = 0;
        deltaLat = 0;

        element_x.Value = 0;
        element_y.Value = 0;

        ApplyPlaneTransform(centerLon, centerLat);
        RecalculateProjection();
        UpdateCornerArrows();

        Vector3D normal = new(
            Math.Cos(centerLat) * Math.Cos(centerLon),
            Math.Cos(centerLat) * Math.Sin(centerLon),
            Math.Sin(centerLat)
         );

        var planeRotation = GetRotationFromZ(normal);

        Point3D origin = new(
            normal.X * R_central,
            normal.Y * R_central,
            normal.Z * R_central
        );

        ApplyPlaneTransform(centerLon, centerLat);
        RecalculateProjection();

    }

    private void LoadAllContinents()
    {
        List<Point> latLonAll = new();
        List<Point3D> ecefAll = new();

        foreach (var cont in continents)
        {
            if (string.IsNullOrEmpty(cont.CsvPath))
                continue;

            var latLon = GetGeodeticsAsRadian(cont.CsvPath);

            foreach (Point p in latLon)
            {
                latLonAll.Add(p);

                double lat = p.Y;
                double lon = p.X;

                double sinLat = Math.Sin(lat);
                double cosLat = Math.Cos(lat);
                double sinLon = Math.Sin(lon);
                double cosLon = Math.Cos(lon);

                ecefAll.Add(new Point3D(
                    R_central * cosLat * cosLon,
                    R_central * cosLat * sinLon,
                    R_central * sinLat
                ));
            }

            latLonAll.Add(new Point(double.NaN, double.NaN));
            ecefAll.Add(new Point3D(double.NaN, double.NaN, double.NaN));
        }

        continentLatLon = new PointCollection(latLonAll);
        continentECEF = new Point3DCollection(ecefAll);

        continentLatLon.Freeze();
        continentECEF.Freeze();

        EuropeEcefLine.Path = continentECEF;

        centerLon = 0;
        centerLat = 0;

        deltaLon = 0;
        deltaLat = 0;

        element_x.Value = 0;
        element_y.Value = 0;

        ApplyPlaneTransform(centerLon, centerLat);
        RecalculateProjection();
        UpdateCornerArrows();
    }
    private void ApplyPlaneTransform(double lon, double lat)
    {
        Vector3D normal = new(
            Math.Cos(lat) * Math.Cos(lon),
            Math.Cos(lat) * Math.Sin(lon),
            Math.Sin(lat)
        );

        ImagePlane.Transform = new Transform3DGroup
        {
            Children =
        {
            new RotateTransform3D(
                new QuaternionRotation3D(GetRotationFromZ(normal))
            ),
            new TranslateTransform3D(
                normal.X * R_central,
                normal.Y * R_central,
                normal.Z * R_central)
        }
        };
    }
    void UpdateCornerArrows()
    {
        var planeTransform = ImagePlane.Transform.Value;

        double halfwidth = ImagePlane.Width / 2;
        double halfheight = ImagePlane.Length / 2;

        Point3D origin = new(0, 0, 0);

        Point3D[] planeCornersLocal =
        {
        new Point3D(-halfwidth,-halfheight, 0),
        new Point3D( halfwidth,-halfheight, 0),
        new Point3D( halfwidth,halfheight, 0),
        new Point3D(-halfwidth,halfheight, 0),
    };
        ArrowVisual3D[] arrows = { A1, A2, A3, A4 };

        for (int i = 0; i < arrows.Length; i++)
        {
            arrows[i].Origin = origin;
            arrows[i].Point2 = planeTransform.Transform(planeCornersLocal[i]);
        }

    }
    private Point3DCollection GetCentralProjection(double lam0, double phi0)
    {
        Point3DCollection projectedPoints = new(continentLatLon.Count);

        foreach (Point point in continentLatLon)
        {
            // x = cos(φ) · sin(λ − λ0) / (sin(φ0) · sin(φ) + cos(φ0) · cos(φ) · cos(λ − λ0))
            // y = (cos(φ0) · sin(φ) − sin(φ0) · cos(φ) · cos(λ − λ0)) / (sin(φ0) · sin(φ) + cos(φ0) · cos(φ) · cos(λ − λ0))

            const double m = 0.2;

            double phi = point.Y;
            double lam = point.X;
            double dlam = lam - lam0;

            double denom =
                Math.Sin(phi0) * Math.Sin(phi) +
                Math.Cos(phi0) * Math.Cos(phi) * Math.Cos(dlam);

            if (denom <= 0)
            {
                projectedPoints.Add(new Point3D(double.NaN, double.NaN, 0));
                continue;
            }

            double x = (R_central / m) * (Math.Cos(phi) * Math.Sin(dlam) / denom);
            double y = (R_central / m) * ((Math.Cos(phi0) * Math.Sin(phi) - Math.Sin(phi0) * Math.Cos(phi) * Math.Cos(dlam)) / denom);

            projectedPoints.Add(new Point3D(x, y, 0d));
        }

        projectedPoints.Freeze();
        return projectedPoints;
    }
    private Point3DCollection LambertProjection(double lam0, double phi0)
    {
        Point3DCollection projectedPoints = new(continentLatLon.Count);

        foreach (Point point in continentLatLon)
        {
            double phi = point.Y;
            double lam = point.X;
            double dlam = lam - lam0;

            double cosc =
                Math.Sin(phi0) * Math.Sin(phi) +
                Math.Cos(phi0) * Math.Cos(phi) * Math.Cos(dlam);

            if (cosc <= 0)
            {
                projectedPoints.Add(new Point3D(double.NaN, double.NaN, 0));
                continue;
            }
            
            double k = Math.Sqrt(2.0 / (1.0 + cosc));

            double x =
                R_central * k * Math.Cos(phi) * Math.Sin(dlam);

            double y =
                R_central * k *
                (Math.Cos(phi0) * Math.Sin(phi)
               - Math.Sin(phi0) * Math.Cos(phi) * Math.Cos(dlam));

            projectedPoints.Add(new Point3D(x, y, 0));
        }

        projectedPoints.Freeze();
        return projectedPoints;
    }

    private void RecalculateProjection()
    {
        if (continentLatLon == null)
            return;

        double lam0 = centerLon + deltaLon;
        double phi0 = centerLat + deltaLat;
        Point3DCollection raw = currentProjection switch
        {
            ProjectionType.Lambert =>  LambertProjection(lam0, phi0), _ =>
            GetCentralProjection(lam0, phi0)
        };

        var clipped = ClipProjection(raw);

        Point3DCollection segments = new();

        for (int i = 0; i < clipped.Count - 1; i++)
        {
            segments.Add(clipped[i]);
            segments.Add(clipped[i + 1]);
        }

        segments.Freeze();
        projected.Points = segments;
    }

    private static PointCollection GetGeodeticsAsRadian(string path)
    {
        PointCollection points = new();

        foreach (var line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var parts = line.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);

            double lon = double.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture);
            double lat = double.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);

            lon = double.DegreesToRadians(lon);
            lat = double.DegreesToRadians(lat);

            points.Add(new Point(lon, lat));
        }

        points.Freeze();
        return points;
    }
    private static (double lon, double lat) GetAverageLatLon(PointCollection latLon)
    {
        double x = 0, y = 0, z = 0;

        foreach (Point p in latLon)
        {
            double lat = p.Y;
            double lon = p.X;

            x += Math.Cos(lat) * Math.Cos(lon);
            y += Math.Cos(lat) * Math.Sin(lon);
            z += Math.Sin(lat);
        }

        double r = Math.Sqrt(x * x + y * y);
        double avgLon = Math.Atan2(y, x);
        double avgLat = Math.Atan2(z, r);

        return (avgLon, avgLat);
    }
    private static Quaternion GetRotationFromZ(Vector3D targetDirection)
    {
        Vector3D zAxis = new(0, 0, 1);
        targetDirection.Normalize();

        Vector3D rotationAxis = Vector3D.CrossProduct(zAxis, targetDirection);

        if (rotationAxis.Length < 0.000001)
            return Quaternion.Identity;

        double rotationAngle = Vector3D.AngleBetween(zAxis, targetDirection);
        rotationAxis.Normalize();

        return new Quaternion(rotationAxis, rotationAngle);
    }

    private static bool InsideSquare(Point3D point, double halfSize)
    {
        return Math.Abs(point.X) <= halfSize &&
               Math.Abs(point.Y) <= halfSize;
    }

    private Point3DCollection ClipProjection(Point3DCollection input)
    {
        double half = (double)FindResource("RightPanelSize") / 2;

        Point3DCollection result = new();

        foreach (var point in input)
        {
            if (InsideSquare(point, half))
                result.Add(point);
            else
                result.Add(new Point3D(double.NaN, double.NaN, 0));
        }
        result.Freeze();
        return result;
    }

}
