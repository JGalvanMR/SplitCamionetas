using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Locations;
using Android.Net;
using Android.OS;
using Android.Support.V4.Content;
using Android.Telephony;
using Android.Text;
using Android.Widget;
using Java.Net;
using Java.Util;
using Org.Json;
using System;
using System.Collections;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Android;

namespace SplitCamionetas
{
    [Activity(Label = "SplitCamionetas", Theme = "@android:style/Theme.Material", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, ScreenOrientation = ScreenOrientation.Portrait)]
    public class MainActivity : Activity
    {
        public static string cadenaConexion = "Persist Security Info=False;user id=sa; password=Gabira2026$;Initial Catalog =GAB_Irapuato; server=tcp:189.206.160.206,2352; Connect Timeout = 130";
        //public static string cadenaConexion = "Persist Security Info=False;user id=sa; password=Gabira2026$;Initial Catalog =GAB_Irapuato; server=tcp:192.168.123.6,1433; Connect Timeout = 130";

        public static int captura = 0;
        SqlCommand cmnd = new SqlCommand();
        SqlDataReader reader;
        SqlCommand cmnd1 = new SqlCommand();
        SqlDataReader reader1;
        String[] strFrutas;
        ArrayAdapter<String> comboAdapter;
        SqlDataAdapter da;
        SqlDataAdapter da1;
        public static DataTable camionetas = new DataTable("camionetas");
        public static DataTable responsables = new DataTable("responsables");
        public static DataTable vehiculos = new DataTable("vehiculos");
        public static DataTable version = new DataTable("version");
        public static DataTable formulario = new DataTable("formulario");
        string query = "";
        DataSet ds = new DataSet();
        DataSet ds1 = new DataSet();
        public static string vehiculo = "";
        public static string responsablesplit = "";
        public static string imei = "";
        SqlConnection thisConnection;


        //Variables del servicio Web
        Context context;
        Java.Lang.Runnable listener;
        //private static string INFO_FILE = "http://192.168.123.4:81/EmbarquesApk/APK_SplitCamionetas/version.txt";
        private static string INFO_FILE = "http://189.206.160.206:81/EmbarquesApk/APK_SplitCamionetas/version.txt";
        private int currentVersionCode;
        private string currentVersionName;
        private int latestVersionCode;
        private string latestVersionName;
        private string downloadURL;
        TextView versionapp;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);
            Button log = FindViewById<Button>(Resource.Id.btnlogin);
            log.Click += Btnlogin_Click;

            //LLenado Spinner 1

            Spinner spinner = FindViewById<Spinner>(Resource.Id.spinner1);
            System.Collections.ArrayList listaFrutas = new System.Collections.ArrayList();
            thisConnection = new SqlConnection(cadenaConexion);

            thisConnection.Open();
            //Buscar el numero del que parte si es campo o proceso
            cmnd = thisConnection.CreateCommand();

            versionapp = FindViewById<TextView>(Resource.Id.versinapp);

            query = "select * FROM tb_cat_vehiculos Where estatus = 'A'";
            da = new SqlDataAdapter(query, thisConnection);
            da.Fill(ds, "camionetas");
            camionetas = ds.Tables["camionetas"];
            thisConnection.Close();

            strFrutas = new String[camionetas.Rows.Count + 1];
            strFrutas[0] = "Seleccione un vehiculo";
            for (int i = 1; i <= camionetas.Rows.Count; i++)
            {
                int x = i - 1;
                strFrutas[i] = camionetas.Rows[x]["descripcion"].ToString();
            }
            Collections.AddAll(listaFrutas, strFrutas);
            comboAdapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, strFrutas);
            spinner.Adapter = comboAdapter;
            spinner.ItemSelected += new EventHandler<AdapterView.ItemSelectedEventArgs>(spinner_ItemSelected);

            //Llenado Spinner 2

            query = "SELECT cve_capsplit, nom_capsplit FROM TB_RESPON_SPLIT Where status = 'C' ORDER BY NOM_CAPSPLIT";
            da = new SqlDataAdapter(query, thisConnection);
            da.Fill(ds, "responsables");
            responsables = ds.Tables["responsables"];
            thisConnection.Close();

            Spinner spinner2 = FindViewById<Spinner>(Resource.Id.spinner2);
            System.Collections.ArrayList listaFrutas2 = new System.Collections.ArrayList();

            strFrutas = new String[responsables.Rows.Count + 1];
            strFrutas[0] = "Seleccione un Responsable";
            for (int i = 1; i <= responsables.Rows.Count; i++)
            {
                int x = i - 1;
                strFrutas[i] = responsables.Rows[x]["nom_capsplit"].ToString();
            }


            Collections.AddAll(listaFrutas2, strFrutas);
            comboAdapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, strFrutas);
            spinner2.Adapter = comboAdapter;
            spinner2.ItemSelected += new EventHandler<AdapterView.ItemSelectedEventArgs>(spinner_ItemSelected2);

            //Inicio de Validacion de Actualizacion *******************************************************************

            try
            {
                getData();
            }
            catch
            {

            }

            versionapp.Text = "Split Camionetas - Versión: " + currentVersionName;

            if (isNewVersionAvailable())
            {
                //Crea mensaje con datos de versión.
                string msj = "Nueva Version Disponible: " + isNewVersionAvailable();
                msj += "\nVersion Actual: " + currentVersionName + "(" + currentVersionCode + ")";
                msj += "\nUltima Version: " + latestVersionName + "(" + latestVersionCode + ")";
                msj += "\nDesea Actualizar?";
                //Crea ventana de alerta.
                Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                alertDialog.SetTitle(Html.FromHtml("<font color='#DF0101' size = 10>Actualizacion Disponible"));
                alertDialog.SetIcon(Resource.Drawable.update);
                alertDialog.SetMessage(Html.FromHtml("<font color='#000000' size = 10>" + msj + "</font>"));
                alertDialog.SetPositiveButton(Html.FromHtml("<font face = 'Comic Sans MS, arial' color='#DF0101' size = '10'>Sí</font>"), SaveAction);
                alertDialog.SetNegativeButton(Html.FromHtml("<font face = 'Comic Sans MS, arial' color='#DF0101' size = '10'>No</font>"), CancelaAction);
                alertDialog.SetCancelable(false);
                alertDialog.Create();
                alertDialog.Show();



                //Muestra la ventana esperando respuesta.

            }



            //Termino de Validacion de Actualizacion*********************************************************************************************************

        }

        private void CancelaAction(object sender, DialogClickEventArgs e)
        {
            Finish();
        }

        private void SaveAction(object sender, DialogClickEventArgs e)
        {
            downloadApp();
        }

        void Btnlogin_Click(object sender, EventArgs e)
        {
            if (responsablesplit == "Seleccione un Responsable")
            {
                Toast.MakeText(this, "Por favor, asegurese de seleccionar un responsable y volver a intentarlo", ToastLength.Long).Show();
                return;
            }

            if (vehiculo == "Seleccione un vehiculo")
            {
                Toast.MakeText(this, "Por favor, asegurese de seleccionar un vehiculo y volver a intentarlo", ToastLength.Long).Show();
                return;
            }

            var camioneta = "";
            if (camionetas.Rows.Count != 0)
            {
                for (int i = 0; i < camionetas.Rows.Count; i++)
                {
                    if (camionetas.Rows[i]["descripcion"].ToString() == vehiculo)
                    {
                        camioneta = camionetas.Rows[i]["clave"].ToString();
                    }
                }
            }
            else
            {
                Toast.MakeText(this, "Por favor, Seleccione un vehiculo", ToastLength.Long).Show();
                return;
            }

            var responsable = "";
            if (responsables.Rows.Count != 0)
            {
                for (int i = 0; i < responsables.Rows.Count; i++)
                {
                    if (responsables.Rows[i]["nom_capsplit"].ToString() == responsablesplit)
                    {
                        responsable = responsables.Rows[i]["cve_capsplit"].ToString();
                    }
                }
            }
            else
            {
                Toast.MakeText(this, "Por favor, Seleccione un responsable", ToastLength.Long).Show();
                return;
            }


            //******************************************************


            Intent intent = new Intent(this, typeof(SolicitarPed));
            intent.PutExtra("cvcamioneta", camioneta.ToString());
            intent.PutExtra("cvresponsable", responsable.ToString());
            intent.PutExtra("camioneta", vehiculo.ToString());
            intent.PutExtra("responsable", responsablesplit.ToString());
            StartActivity(intent);
        }

        private void spinner_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            Spinner spinner = (Spinner)sender;
            vehiculo = spinner.GetItemAtPosition(e.Position).ToString();
        }

        private void spinner_ItemSelected2(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            Spinner spinner = (Spinner)sender;
            responsablesplit = spinner.GetItemAtPosition(e.Position).ToString();
        }

        private void getData()
        {
            try
            {
                context = this;
                // Datos locales
                System.Console.WriteLine("AutoUpdater", "GetData");
                Android.Content.PM.PackageInfo pckginfo = context.PackageManager.GetPackageInfo(context.PackageName, 0);

                currentVersionCode = pckginfo.VersionCode;
                currentVersionName = pckginfo.VersionName;

                // Datos remotos
                string data = downloadHttp(new URL(INFO_FILE));
                JSONObject json = new JSONObject(data.ToString());
                latestVersionCode = json.GetInt("versionCode");
                latestVersionName = json.OptString("versionName");
                downloadURL = json.GetString("downloadURL");
                System.Console.WriteLine("AutoUpdate", "Datos obtenidos con éxito");
            }
            catch (JSONException e)
            {
                Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                alertDialog.SetTitle(Html.FromHtml("<font color='#0068b3' size = 10>SE HA PRODUCIDO UNA EXCEPCION</font>"));
                alertDialog.SetIcon(Resource.Drawable.Info);
                alertDialog.SetMessage(Html.FromHtml("<font color='#ed174f' size = 10>Ha habido un error con el JSON" + e + "</font>"));
                alertDialog.SetNeutralButton("Ok", delegate { alertDialog.Dispose(); });
                alertDialog.Show();

                System.Console.WriteLine("AutoUpdate", "Ha habido un error con el JSON", e);
            }
            catch (Android.Content.PM.PackageManager.NameNotFoundException e)
            {
                Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                alertDialog.SetTitle(Html.FromHtml("<font color='#0068b3' size = 10>SE HA PRODUCIDO UNA EXCEPCION</font>"));
                alertDialog.SetIcon(Resource.Drawable.Info);
                alertDialog.SetMessage(Html.FromHtml("<font color='#ed174f' size = 10>Ha habido un error con el packete :S" + e + "</font>"));
                alertDialog.SetNeutralButton("Ok", delegate { alertDialog.Dispose(); });
                alertDialog.Show();

                System.Console.WriteLine("AutoUpdate", "Ha habido un error con el packete :S", e);
            }
            catch (System.IO.IOException e)
            {
                Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                alertDialog.SetTitle(Html.FromHtml("<font color='#0068b3' size = 10>SE HA PRODUCIDO UNA EXCEPCION</font>"));
                alertDialog.SetIcon(Resource.Drawable.Info);
                alertDialog.SetMessage(Html.FromHtml("<font color='#ed174f' size = 10>Ha habido un error con la descarga" + e + "</font>"));
                alertDialog.SetNeutralButton("Ok", delegate { alertDialog.Dispose(); });
                alertDialog.Show();

                System.Console.WriteLine("AutoUpdate", "Ha habido un error con la descarga", e);
            }
        }

        private static string downloadHttp(URL url)
        {
            // Codigo de coneccion, Irrelevante al tema.

            StrictMode.ThreadPolicy policy = new StrictMode.ThreadPolicy.Builder().PermitAll().Build();
            StrictMode.SetThreadPolicy(policy);
            HttpURLConnection c = (HttpURLConnection)url.OpenConnection();

            c.RequestMethod = "GET";
            c.ReadTimeout = (15 * 1000);
            c.UseCaches = false;
            c.Connect();
            Java.IO.BufferedReader reader = new Java.IO.BufferedReader(new Java.IO.InputStreamReader(c.InputStream));
            Java.Lang.StringBuilder stringBuilder = new Java.Lang.StringBuilder();
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                stringBuilder.Append(line + "\n");
            }
            return stringBuilder.ToString();
        }
        public bool isNewVersionAvailable()
        {
            return latestVersionCode > currentVersionCode;
        }

        private string downloadApp()
        {

            var progressDialog = ProgressDialog.Show(this, "Espere Por Favor...", "Descargando Actualizacion", true);
            new System.Threading.Thread(new System.Threading.ThreadStart(delegate
            {//LOAD METHOD TO GET ACCOUNT INFO
                try
                {
                    var pathToNewFolder = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/SplitCamionetas";
                    Directory.CreateDirectory(pathToNewFolder);

                    string archivo = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/SplitCamionetas/SplitCamionetas.apk";

                    var webClient = new WebClient();
                    webClient.DownloadFileCompleted += (s, ex) =>
                    {
                        Java.IO.File toInstall = new Java.IO.File(archivo);
                        Android.Net.Uri downloadUri = FileProvider.GetUriForFile(context, context.ApplicationContext.PackageName + ".provider", toInstall);

                        Intent intent = new Intent(Intent.ActionView);
                        intent.SetDataAndType(downloadUri, "application/vnd.android.package-archive");
                        intent.SetFlags(ActivityFlags.NewTask);
                        intent.AddFlags(ActivityFlags.GrantReadUriPermission);
                        StartActivity(intent);
                        Finish();


                        //RunOnUiThread(() => Toast.MakeText(this, "Aplicacion Actualizada.", ToastLength.Long).Show()); //HIDE PROGRESS DIALOG 
                        //RunOnUiThread(() => progressDialog.Hide());
                        //Intent intentx = new Intent(Intent.ActionView);
                        //intentx.SetDataAndType(Android.Net.Uri.FromFile(new Java.IO.File(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/SplitCamionetas/SplitCamionetas.apk")), "application/vnd.android.package-archive");
                        //intentx.SetFlags(ActivityFlags.NewTask);
                        //StartActivity(intentx);
                        //Finish();

                    };

                    var folder = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/SplitCamionetas";
                    //webClient.DownloadFileAsync(new System.Uri("http://192.168.123.4:81/EmbarquesApk/APK_SplitCamionetas/SplitCamionetas.apk"), folder + "/SplitCamionetas.apk");
                    webClient.DownloadFileAsync(new System.Uri("http://189.206.160.206:81/EmbarquesApk/APK_SplitCamionetas/SplitCamionetas.apk"), folder + "/SplitCamionetas.apk");
                }
                catch (System.IO.IOException e)
                {
                    RunOnUiThread(() => progressDialog.Hide());
                    RunOnUiThread(() => Toast.MakeText(this, e.ToString(), ToastLength.Long).Show()); //HIDE PROGRESS DIALOG 

                }


            })).Start();






            return "1";
        }
    }
}

