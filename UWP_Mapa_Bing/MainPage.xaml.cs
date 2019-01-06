using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Services.Maps;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Diagnostics;
using Windows.Storage.Streams;
using Windows.UI;
using System.Threading.Tasks;

// La plantilla de elemento Página en blanco está documentada en https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0xc0a

namespace UWP_Mapa_Bing
{
    /// <summary>
    /// Página vacía que se puede usar de forma independiente o a la que se puede navegar dentro de un objeto Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public Geopoint miPosiciónActual;

        public MainPage()
        {
            this.InitializeComponent();

            MapControl.StyleSheet = MapStyleSheet.RoadDark();//Apariencia del Mapa. Por lo que dice la documentación, se pueden personalizar los colores
                                                             //de cada tipo de apariencia del mapa, para poder cambiarlos y crear nuestras propias apariencias https://docs.microsoft.com/en-us/windows/uwp/maps-and-location/elements-of-map-style-sheet

            /*Antes de que su aplicación pueda acceder a la ubicación del usuario, debe llamar al método RequestAccessAsync.
             * En ese momento, su aplicación debe estar en primer plano y se debe llamar a RequestAccessAsync desde el subproceso 
             * de la interfaz de usuario. Hasta que el usuario otorgue el permiso de su aplicación a su ubicación, esta no podrá 
             * acceder a los datos de ubicación.

            Obtenga la ubicación actual del dispositivo (si la ubicación está disponible) utilizando el método GetGeopositionAsync de
            la clase Geolocator. Para obtener el Geopunto correspondiente, use la propiedad Punto de la geocoordinada de la posición 
            geográfica. Para obtener más información, consulte Obtener la ubicación actual.*/
            // Set your current location.

            currentLocation();
            this.SizeChanged += MainPage_SizeChanged;
        }

        //Evento que controla el tamaño de la pantalla, para redimensionar los elementos que hay en ella. Como la App está diseñada para Tablets en modo Landscape, no controlo otras resoluciones. Pero si quisiera
        //ver la App con otros diseñis sin cargar otra Page, podríamos usar este evento para cargar diferentes VisualState u otras elementos que permitan mostrar
        //otro diseño creado en el mismo fichero XAML, para modificar la apariencia de la Page mostrada.
        private void MainPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //Width y Heigth de la pantalla
            var bounds = Window.Current.Bounds;
            double height = bounds.Height;
            double width = bounds.Width;

            TextIndicacionesRuta.Width = width / 3;
            TextIndicacionesRuta.Height = (height / 4) * 3;

            Panel_Rutas.Height = (height / 4);
            Panel_Rutas.Width = (width / 3) * 2;

            Panel_Botones.Width = width / 3;
            Panel_Botones.Height = (height / 4);

            MapControl.Width = (width / 3) * 2;
            MapControl.Height = (height / 4) * 3;
        }

        private async void currentLocation()
        {
            var accessStatus = await Geolocator.RequestAccessAsync();
            switch (accessStatus)
            {
                case GeolocationAccessStatus.Allowed:

                    // Localización actual del Usuario
                    Geolocator geolocator = new Geolocator();
                    geolocator.DesiredAccuracy = PositionAccuracy.High;
                    Geoposition pos = await geolocator.GetGeopositionAsync();
                    miPosiciónActual = pos.Coordinate.Point;

                    // Reverse geocode the specified geographic location.
                    MapLocationFinderResult result =
                          await MapLocationFinder.FindLocationsAtAsync(miPosiciónActual);

                    // If the query returns results, display the name of the town
                    // contained in the address of the first result.
                    if (result.Status == MapLocationFinderStatus.Success)
                    {
                        Debug.WriteLine( "Ciudad " + result.Locations[0].Address.Town + ", Calle " + result.Locations[0].Address.Street + " Portal " + result.Locations[0].Address.StreetNumber);
                    }

                    Debug.WriteLine("Altitud: " + miPosiciónActual.Position.Altitude + ", Latitud " + miPosiciónActual.Position.Latitude);

                    // Set the map location.
                    MapControl.Center = miPosiciónActual;
                    MapControl.ZoomLevel = 12;//20 lo mas cercano, 1 lo mas alejado
                    MapControl.Heading = 0;//de 0 a 360 la rotación circular del mapa
                    MapControl.LandmarksVisible = true;//true if 3D buildings are displayed on the map; otherwise, false.
                    MapControl.DesiredPitch = 65;//Inclinación del mapa
                    //MapControl.TrafficFlowVisible = true;//Flujo de tráfico
                    //MapControl.PedestrianFeaturesVisible = true;//
                    //MapControl.WatermarkMode = MapWatermarkMode.On;

                    /*POIs: según la documentación de windows para UWP, se pueden añadir POI (Points Of Interest) al mapa. Ademas permite personalizar
                     los POIs cambiando su imagen por defecto. Entre los tipos de POIs que se pueden añadir, están las imágenes (no confundir con
                     personallizar la imagen dce un POI), objetos 3D, shapes (para señalizar zonas del mapa con imagenes personalizadas por nosotros)
                     , lineas, y XAML (por ejemplo botones, tablas, CARDs, por ejemplo para mostrar una CARD con información y botones para realizar
                     una acción sobre un sitio concreto) https://docs.microsoft.com/en-us/windows/uwp/maps-and-location/display-poi*/
                    var MyLandmarks = new List<MapElement>();

                    //Si no se especifíca la Altitud, la imagen del POI será colocada en la superficie
                    var spaceNeedleIcon = new MapIcon
                    {
                        Location = miPosiciónActual,//localización sobre la que se mostrará en el Mapa
                        NormalizedAnchorPoint = new Point(0.5, 1.0),//El punto de anclaje es el punto en el MapIcon que se coloca en el punto en el
                        //MapControl especificado por la propiedad Ubicación (Posición 2D sobre el Mapa)
                        ZIndex = 0,//Layer
                        Title = "Posición Actual"//Si el título no se ve, es por el zoom del Mapa, tal cual dice la documentación
                    };

                    //Se recomienda un tamaño por debajo de los 100 px, ya que si no el POI será muy grande
                    spaceNeedleIcon.Image = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/poi_position.png"));

                    MyLandmarks.Add(spaceNeedleIcon);

                    var LandmarksLayer = new MapElementsLayer
                    {
                        ZIndex = 1,
                        MapElements = MyLandmarks
                    };

                    MapControl.Layers.Add(LandmarksLayer);
                    break;

                case GeolocationAccessStatus.Denied:
                    //Para que pueda acceder a la Localización, en el dispositvo activar la Localización/Ubicación en la Configuración de Privacidad
                    Debug.Write("Acceso denegado a Geolocator");
                    // Handle the case  if access to location is denied.
                    break;

                case GeolocationAccessStatus.Unspecified:
                    Debug.Write("Unespecificado Geolocator");
                    // Handle the case if  an unspecified error occurs.
                    break;
            }
        }

        private async void showStreetsideView (object sender, RoutedEventArgs e)
        {
            // Check if Streetside is supported.
            if (MapControl.IsStreetsideSupported)
            {
                // Find a panorama near Avenue Gustave Eiffel.
                //BasicGeoposition cityPosition = new BasicGeoposition() { Latitude = 40.389763, Longitude = -3.629439 };
                Geopoint cityCenter = miPosiciónActual;
                StreetsidePanorama panoramaNearCity = await StreetsidePanorama.FindNearbyAsync(cityCenter);

                // Set the Streetside view if a panorama exists.
                if (panoramaNearCity != null)
                {
                    // Create the Streetside view.
                    StreetsideExperience ssView = new StreetsideExperience(panoramaNearCity);
                    ssView.OverviewMapVisible = true;
                    MapControl.CustomExperience = ssView;
                }
            }
            else
            {
                // If Streetside is not supported
                ContentDialog viewNotSupportedDialog = new ContentDialog()
                {
                    Title = "Streetside is not supported",
                    Content = "\nStreetside views are not supported on this device.",
                    PrimaryButtonText = "OK"
                };
                await viewNotSupportedDialog.ShowAsync();
            }
        }

        private async void map_tresD (object sender, RoutedEventArgs e)
        {
            if (MapControl.Is3DSupported)
            {
                // Set the aerial 3D view.
                MapControl.Style = MapStyle.Aerial3DWithRoads;

                // Specify the location.
                //BasicGeoposition hwGeoposition = new BasicGeoposition() { Latitude = 43.773251, Longitude = 11.255474 };
                Geopoint hwPoint = miPosiciónActual;

                // Create the map scene.
                MapScene hwScene = MapScene.CreateFromLocationAndRadius(hwPoint,
                                                                                     80, /* show this many meters around */
                                                                                     0, /* looking at it to the North*/
                                                                                     60 /* degrees pitch */);
                // Set the 3D view with animation.
                await MapControl.TrySetSceneAsync(hwScene, MapAnimationKind.Bow);
            }
            else
            {
                // If 3D views are not supported, display dialog.
                ContentDialog viewNotSupportedDialog = new ContentDialog()
                {
                    Title = "3D is not supported",
                    Content = "\n3D views are not supported on this device.",
                    PrimaryButtonText = "OK"
                };
                await viewNotSupportedDialog.ShowAsync();
            }
        }

        private async void click_CalcularRuta(object sender, RoutedEventArgs e)
        {
            if (text_Origen.Text != "" && text_Destino.Text != "")
            {
                //Posición inicial de la ruta
                BasicGeoposition startLocation = await geocodeButton(text_Origen.Text);

                // End at the city of Seattle, Washington.
                BasicGeoposition endLocation = await geocodeButton(text_Destino.Text);

                // Para conseguir la ruta entre 2 destinos especificados Coche
                MapRouteFinderResult routeResult =
                      await MapRouteFinder.GetDrivingRouteAsync(
                      new Geopoint(startLocation),
                      new Geopoint(endLocation),
                      MapRouteOptimization.Time,
                      MapRouteRestrictions.None);

                // Para conseguir la ruta entre 2 destinos especificados Andando. Suele Fallar
                /*MapRouteFinderResult routeResult =
                      await MapRouteFinder.GetWalkingRouteAsync(
                      new Geopoint(startLocation),
                      new Geopoint(endLocation));*/

                if (routeResult.Status == MapRouteFinderStatus.Success)
                {
                    System.Text.StringBuilder routeInfo = new System.Text.StringBuilder();

                    // Se muestra la información obtenida de la ruta
                    routeInfo.Append("Total estimated time (minutes) = " + routeResult.Route.EstimatedDuration.TotalMinutes.ToString());
                    routeInfo.Append("\nTotal length(kilometers) = " + (routeResult.Route.LengthInMeters / 1000).ToString());

                    // Se muestra las direcciones de la ruta
                    routeInfo.Append("\n\nDIRECTIONS\n");

                    foreach (MapRouteLeg leg in routeResult.Route.Legs)
                    {
                        foreach (MapRouteManeuver maneuver in leg.Maneuvers)
                        {
                            routeInfo.AppendLine(maneuver.InstructionText);
                            Debug.WriteLine(maneuver.InstructionText);
                        }
                    }

                    // Load the text box.
                    TextIndicacionesRuta.Text = routeInfo.ToString();

                    // Use the route to initialize a MapRouteView.
                    MapRouteView viewOfRoute = new MapRouteView(routeResult.Route);
                    viewOfRoute.RouteColor = Colors.Blue;
                    viewOfRoute.OutlineColor = Colors.Red;

                    // Add the new MapRouteView to the Routes collection
                    // of the MapControl.
                    MapControl.Routes.Add(viewOfRoute);

                    // Fit the MapControl to the route.
                    await MapControl.TrySetViewBoundsAsync(
                          routeResult.Route.BoundingBox,
                          null,
                          Windows.UI.Xaml.Controls.Maps.MapAnimationKind.None);
                }
                else
                {
                    TextIndicacionesRuta.Text = "A problem occurred: " + routeResult.Status.ToString();
                }
            }
            else
            {
                ContentDialog noWifiDialog = new ContentDialog
                {
                    Title = "No has rellenado todos los campos",
                    Content = "Rellena los campos Origen y Destino para poder calcular una ruta.",
                    CloseButtonText = "Ok"
                };

                ContentDialogResult result = await noWifiDialog.ShowAsync();
            }
        }

        private async Task<BasicGeoposition> geocodeButton(String addressToGeocode)
        {
            // Se busca la dirección o lugar específicado, se establece un punto cercano como referencia (en este caso la ubicación inicial del usuario al ejecutar la App)
            MapLocationFinderResult result = await MapLocationFinder.FindLocationsAsync(
                                            addressToGeocode,
                                            miPosiciónActual,
                                            3);

            //Si el resultado obtenido ha sido correcto, y si el array de localizaciones es mayor que 0
            if (result.Status == MapLocationFinderStatus.Success)
            {
                if (result.Locations.Count > 0)
                    Debug.WriteLine("result = (" + result.Locations[0].Point.Position.Latitude.ToString() + "," + result.Locations[0].Point.Position.Longitude.ToString() + ")");
                else
                    Debug.WriteLine("No ha encontrado nada el Geocode");               
            }

            BasicGeoposition valor_retorno = new BasicGeoposition();
            valor_retorno.Latitude = result.Locations[0].Point.Position.Latitude;
            valor_retorno.Longitude = result.Locations[0].Point.Position.Longitude;

            return valor_retorno;
        }

        private async void click_PosActual(object sender, RoutedEventArgs e)
        {
            // Reverse geocode the specified geographic location.
            MapLocationFinderResult result =
                  await MapLocationFinder.FindLocationsAtAsync(miPosiciónActual);

            // If the query returns results, display the name of the town
            // contained in the address of the first result.
            if (result.Status == MapLocationFinderStatus.Success)
            {
                text_Origen.Text = result.Locations[0].Address.Town + ", " + result.Locations[0].Address.Street + " " + result.Locations[0].Address.StreetNumber.ToString();
            }
        }

    }
}
