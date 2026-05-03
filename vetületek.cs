//using System.IO;
//using System.Text;
//using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Data;
//using System.Windows.Documents;
//using System.Windows.Input;
//using System.Windows.Media;
//using System.Windows.Media.Imaging;
//using System.Windows.Media.Media3D;
//using System.Windows.Navigation;
//using System.Windows.Shapes;
//using System.Xml.Schema;
//using HelixToolkit.Wpf;
//using Path = System.IO.Path;

//namespace x_y_rotation;

///// <summary>
///// Interaction logic for MainWindow.xaml
///// </summary>
//public partial class MainWindow : Window
//{
//    public MainWindow()
//    {
//        InitializeComponent();

//        const string path = @"C:\Users\dzsen\Dzseni\Földrajz\Szakdolgozat_vetületek\Europe (1).csv";

//        Point3DCollection europe = new Point3DCollection();

//        foreach (string row in File.ReadLines(path))
//        {
//            string[] values = row.Split(',');

//            double lon = double.Parse(values[0], System.Globalization.CultureInfo.InvariantCulture);
//            double lat = double.Parse(values[1], System.Globalization.CultureInfo.InvariantCulture);

//            (double lat_sin, double lat_cos) = double.SinCos(double.DegreesToRadians(lat));
//            (double lon_sin, double lon_cos) = double.SinCos(double.DegreesToRadians(lon));

//            double x = 6.371 * (lat_cos * lon_cos);
//            double y = 6.371 * (lat_cos * lon_sin);
//            double z = 6.371 * (lat_sin);


//            //𝑋= (𝑅+ℎ)∙cos_lat∙cos_lon
//            //𝑌= (𝑅+ℎ)∙cos_lat∙sin_lon
//            //𝑍= (𝑅+ℎ)∙sin_lat,

//            europe.Add(new Point3D(x, y, z));
//        }
//        europe.Freeze();

//        Europe.Path = europe;

//    }
//    private void element_x_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
//    {
//        Point3D origin = rectangleVisual3D.Transform.Transform(rectangleVisual3D.Origin);
//        double lam = double.Atan2(origin.Y, origin.X);
//        double phi = double.Atan2(origin.Z, double.Hypot(origin.X, origin.Y));
//    }
//}