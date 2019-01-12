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
using System.Collections.ObjectModel;

namespace UWP_Mapa_Bing
{
    /// <summary>
    /// Página vacía que se puede usar de forma independiente o a la que se puede navegar dentro de un objeto Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public Geopoint miPosiciónActual;
        Color[] colors = { Colors.Blue, Colors.Red , Colors.Orange , Colors.Yellow , Colors.White };
        String[] maps2D = { "Aerial" , "RoadDark", "RoadLight" };
        String[] maps3D = { "Aerial3DWithRoads","Road","Terrain" };
        MapStyle[] estilos3DMapa = { MapStyle.Aerial3DWithRoads, MapStyle.Road, MapStyle.Terrain };

        ObservableCollection<String> mapas2d = new ObservableCollection<String>();
        ObservableCollection<String> mapas3d = new ObservableCollection<String>();

        public MainPage()
        {
            this.InitializeComponent();

            ColorsCombo.SelectedIndex = 1;//Establezco el color inicial a Rojo

            //Añado los Items de los combos de los mapas 2D y 3D
            foreach (String data in maps2D)
            {
                mapas2d.Add(data);
            }

            foreach (String data in maps3D)
            {
                mapas3d.Add(data);
            }
           
            /*Antes de que su aplicación pueda acceder a la ubicación del usuario, debe llamar al método RequestAccessAsync.
             * En ese momento, su aplicación debe estar en primer plano y se debe llamar a RequestAccessAsync desde el subproceso 
             * de la interfaz de usuario. Hasta que el usuario otorgue el permiso de su aplicación a su ubicación, esta no podrá 
             * acceder a los datos de ubicación.

            Obtenga la ubicación actual del dispositivo (si la ubicación está disponible) utilizando el método GetGeopositionAsync de
            la clase Geolocator. Para obtener el Geopunto correspondiente, use la propiedad Punto de la geocoordinada de la posición 
            geográfica. Para obtener más información, consulte Obtener la ubicación actual.*/
            // Set your current location.

            currentLocation();
            this.SizeChanged += MainPage_SizeChanged;//evento lanzado cuando la pantalla cambia de tamaño
            this.Loaded += Page_Loaded;//evento lanzado cuando la pantalla ha sido cargada (posterior a la función Main)
        }

        private async void ColocarPOIs(Geopoint posPOI, String textPOI, Boolean destino)
        {
            /*POIs: según la documentación de windows para UWP, se pueden añadir POI (Points Of Interest) al mapa. Ademas permite personalizar
                     los POIs cambiando su imagen por defecto. Entre los tipos de POIs que se pueden añadir, están las imágenes (no confundir con
                     personallizar la imagen dce un POI), objetos 3D, shapes (para señalizar zonas del mapa con imagenes personalizadas por nosotros)
                     , lineas, y XAML (por ejemplo botones, tablas, CARDs, por ejemplo para mostrar una CARD con información y botones para realizar
                     una acción sobre un sitio concreto) https://docs.microsoft.com/en-us/windows/uwp/maps-and-location/display-poi*/
            var MyLandmarks = new List<MapElement>();


            //Si no se especifíca la Altitud, la imagen del POI será colocada en la superficie
            var newIcon = new MapIcon
            {
                Location = posPOI,//localización sobre la que se mostrará en el Mapa
                NormalizedAnchorPoint = new Point(0.5, 1.0),//El punto de anclaje es el punto en el MapIcon que se coloca en el punto en el
                                                            //MapControl especificado por la propiedad Ubicación (Posición 2D sobre el Mapa)
                ZIndex = 0,//Layer
                Title = textPOI//Si el título no se ve, es por el zoom del Mapa, tal cual dice la documentación
            };

            //Se recomienda un tamaño por debajo de los 100 px, ya que si no el POI será muy grande
            if (destino == false)
                newIcon.Image = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/poi_position.png"));
            else
            {
                newIcon.Image = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/destinoPOI.png"));
            }

            MyLandmarks.Add(newIcon);

            var LandmarksLayer = new MapElementsLayer
            {
                ZIndex = 1,
                MapElements = MyLandmarks
            };

            MapControl.Layers.Add(LandmarksLayer);              
        }

        #region Eventos
        //Evento llamado cuando la pantalla ha sido cargada. Lo uso para establecer el Index de los los combos de los mapas, ya que al añadir sus items del combo en la función Main, no se puede establecer
        //su index en ella porque su valor aun es null. Por ello, los establezco en esta función, que es llamada posteriormente
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //Estilo del mapa inicial 2d, correspondiente al index 1 de combo2d
            MapControl.StyleSheet = MapStyleSheet.RoadDark();//Apariencia del Mapa. Por lo que dice la documentación, se pueden personalizar los colores
                                                             //de cada tipo de apariencia del mapa, para poder cambiarlos y crear nuestras propias apariencias https://docs.microsoft.com/en-us/windows/uwp/maps-and-location/elements-of-map-style-sheet
            combo2d.SelectedIndex = 1;
            combo3d.SelectedIndex = 0;
            Debug.WriteLine(combo2d.Width + " " + combo3d.Width);
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

            Panel_Info_Ruta.Width = width / 3;
            Panel_Info_Ruta.Height = (height / 4) * 3;

            Panel_Rutas.Height = (height / 4);
            Panel_Rutas.Width = (width / 3) * 2;

            Panel_Botones.Width = width / 3;
            Panel_Botones.Height = (height / 4);

            MapControl.Width = (width / 3) * 2;
            MapControl.Height = (height / 4) * 3;
        }
        #endregion

        #region Posicion Inicial
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
                    //Un BasicGeoposition se compone de la Longitud y Latitud de un Point -> new BasicGeoposition { .Point.Position.Latitude, .Point.Position.Longitude}

                    // Coloco el mapa en la posición del usuario, y establezco sus valores iniciales
                    MapControl.Center = miPosiciónActual;
                    MapControl.ZoomLevel = 12;//20 lo mas cercano, 1 lo mas alejado
                    MapControl.Heading = 0;//de 0 a 360 la rotación circular del mapa
                    MapControl.LandmarksVisible = true;//true if 3D buildings are displayed on the map; otherwise, false.
                    MapControl.DesiredPitch = 65;//Inclinación del mapa
                    //MapControl.TrafficFlowVisible = true;//Flujo de tráfico
                    //MapControl.PedestrianFeaturesVisible = true;//

                    ColocarPOIs(miPosiciónActual, "Posición Actual", false);
                    break;

                case GeolocationAccessStatus.Denied:
                    // Si entra aquí, para que pueda acceder a la Localización, en el dispositvo hay que activar la Localización/Ubicación en la Configuración de Privacidad
                    Debug.Write("Acceso denegado a Geolocator");
                    break;

                case GeolocationAccessStatus.Unspecified:
                    Debug.Write("Geolocator unespecificado");
                    break;
            }
        }
        #endregion

        #region Visualización de los Mapas
        //Función para visualizar el StreetView
        private async void click_StretView (object sender, RoutedEventArgs e)
        {
            // Comprobación de si el dispositivo soporta Streetside
            if (MapControl.IsStreetsideSupported)
            {
                StreetsidePanorama panoramaNearCity = await StreetsidePanorama.FindNearbyAsync(miPosiciónActual);

                // Si el panorama existe, se crea el StreetView y se establece en el mapa para ser visualizado
                if (panoramaNearCity != null)
                {
                    StreetsideExperience stretView = new StreetsideExperience(panoramaNearCity);
                    stretView.OverviewMapVisible = true;
                    MapControl.CustomExperience = stretView;
                }
            }
            else
            {
                // Si el dispositivo no soporta Streetside, aparece el siguiente mensaje de dialogo
                ContentDialog viewNotSupportedDialog = new ContentDialog()
                {
                    Title = "StretView no soportado",
                    Content = "La vista StretView no es soportada en tu dispositvo.",
                    PrimaryButtonText = "OK"
                };
                await viewNotSupportedDialog.ShowAsync();
            }
        }

        //Función que establece el mapa con mapas 3D según el valor de su combo
        private async void map_tresD (object sender, RoutedEventArgs e)
        {
            if (MapControl.Is3DSupported)
            {
                // Se establece el mapa mediante el valor actual del combo3D
                MapControl.Style = estilos3DMapa[combo3d.SelectedIndex];

                // Se crea la escena del Mapa 3D
                MapScene hwScene = MapScene.CreateFromLocationAndRadius(miPosiciónActual,
                                                                                     80, // cuantos metros se ve alrededor
                                                                                     0, //grados con respecto al Norte
                                                                                     60); //inclinación del mapa
                // Se establece la animación con la que aparecerá el mapa 3D
                await MapControl.TrySetSceneAsync(hwScene, MapAnimationKind.Linear);
            }
            else
            {
                // Si no se soporta el modo 3D, se muestra un dialogo
                ContentDialog viewNotSupportedDialog = new ContentDialog()
                {
                    Title = "3D no soportado",
                    Content = "La vista 3D del Mapa no es soportada en tu dispositvo.",
                    PrimaryButtonText = "OK"
                };
                await viewNotSupportedDialog.ShowAsync();
            }
        }

        //Función que establece el mapa con mapas 2D según el valor de su combo
        private void click_MapaNormal(object sender, RoutedEventArgs e)
        {
            switch (combo2d.SelectedIndex)
            {
                case 0:
                    MapControl.StyleSheet = MapStyleSheet.Aerial();
                    break;
                case 1:
                    MapControl.StyleSheet = MapStyleSheet.RoadDark();
                    break;
                case 2:
                    MapControl.StyleSheet = MapStyleSheet.RoadLight();
                    break;
            }
        }
        #endregion

        #region Calculo de la ruta
        private async void click_CalcularRuta(object sender, RoutedEventArgs e)
        {
            if (text_Origen.Text != "" && text_Destino.Text != "")
            {
                try
                {
                    // Obtenemos la dirección inicial mediante Geocode
                    Geopoint startLocation = await geocodeDireccion(text_Origen.Text);

                    // Obtenemos la dirección destino mediante Geocode
                    Geopoint endLocation = await geocodeDireccion(text_Destino.Text);

                    MapRouteFinderResult routeResult = null;

                    if (toggle_Desplazamiento.IsOn == true)
                    {
                        // Para conseguir la ruta entre 2 destinos especificados Coche
                        routeResult = await MapRouteFinder.GetDrivingRouteAsync(
                                      startLocation,
                                      endLocation,
                                      MapRouteOptimization.Time,
                                      MapRouteRestrictions.None);
                    }
                    else
                    {
                        // Para conseguir la ruta entre 2 destinos especificados Andando. Suele Fallar
                        routeResult = await MapRouteFinder.GetWalkingRouteAsync(
                                     startLocation,
                                      endLocation);
                    }



                    if (routeResult.Status == MapRouteFinderStatus.Success)
                    {
                        System.Text.StringBuilder routeInfo = new System.Text.StringBuilder();

                        // Se muestra la información obtenida de la ruta
                        string tiempo = string.Format("{0} horas {1} min", routeResult.Route.EstimatedDuration.Hours, routeResult.Route.EstimatedDuration.Minutes);
                        routeInfo.Append("\nTiempo en minutos = " + tiempo);
                        routeInfo.Append("\nTotal Km = " + (routeResult.Route.LengthInMeters / 1000).ToString());

                        // Se muestra las direcciones de la ruta
                        routeInfo.Append("\n\nDIRECCIONES\n");

                        foreach (MapRouteLeg leg in routeResult.Route.Legs)
                        {
                            foreach (MapRouteManeuver maneuver in leg.Maneuvers)
                            {
                                routeInfo.AppendLine(maneuver.InstructionText);
                                Debug.WriteLine(maneuver.InstructionText);
                            }
                        }

                        // Mostramos la información de la ruta en el Texto asignado para ver las indicaciones de la ruta
                        TextIndicacionesRuta.Text = routeInfo.ToString();

                        // Usamos la ruta para inicializar MapRouteView
                        MapRouteView viewOfRoute = new MapRouteView(routeResult.Route);
                        viewOfRoute.RouteColor = colors[ColorsCombo.SelectedIndex];
                        viewOfRoute.OutlineColor = Colors.Red;

                        // Añadimos el nuevo MapRouteView a las rutas del Mapa
                        if (MapControl.Routes.Count > 0)//Si ya hay mostrada una ruta, la limpio (ejemplo: misma ruta con distinto color)
                            MapControl.Routes.Clear();

                        MapControl.Routes.Add(viewOfRoute);

                        // Mostramos la ruta en el Mapa
                        await MapControl.TrySetViewBoundsAsync(
                              routeResult.Route.BoundingBox,
                              null,
                              Windows.UI.Xaml.Controls.Maps.MapAnimationKind.None);

                        //Coloco los POIs de Inicio y Destino
                        ColocarPOIs(endLocation, "Destino", true);
                        ColocarPOIs(startLocation, "Inicio", true);
                    }
                    else
                    {
                        TextIndicacionesRuta.Text = "A ocurrido un problema al cargar el mapa: " + routeResult.Status.ToString();
                    }
                }
                catch {
                    TextIndicacionesRuta.Text = "Error obteniendo una de las posiciones especificadas (DIRECCIÓN NO ENCONTRADA)";
                    Debug.WriteLine("Error haciendo obteniendo la posición de una dirección mediante Geocode (DIRECCIÓN NO ENCONTRADA) ");
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

        private async Task<Geopoint> geocodeDireccion(String addressToGeocode)
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

            Geopoint valor_retorno = result.Locations[0].Point;
         
            return valor_retorno;
        }
        #endregion

        #region Click Posición Actual
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
        #endregion
    }
}
