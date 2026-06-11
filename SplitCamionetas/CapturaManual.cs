using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using SplitCamionetas.Modal;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Views.InputMethods;
using SQLite;
using SplitCamionetas.Models;
using System.IO;
using Android.Text;
using Java.Util;
using Android.Icu.Text;
using System.Threading;
using Java.Lang;
using System.Net.Mail;
using System.Net;


namespace SplitCamionetas
{

    [Activity(Label = "Captura Manual")]

    public partial class CapturaManual : Activity
    {
        public static int valido = 0, veces = 0;
        public static string cvvehiculo, cvresponsable;
        public static string vehiculo, responsable;
        public string Nombre = "", Mtipo = "", MProd = "", MTar = "", MFol = "", mUser = "", mAutoriza = "", user = "", motfolade = "";
        public string cvecam = "", muser = "", mconcen = "1", Version = "7.5";
        public static string AutoPed = "N";
        public static string EtiquetaExiste = "S";
        public static string dondegenera = "";
        public static string HayExistencias = "S", EtiquetaCapturada = "S";
        public static string Surtidomayor = "S", ValiFechacad = "S";
        public static SQLiteConnection db;
        SqlConnection thisConnection = new SqlConnection(MainActivity.cadenaConexion);
        SqlDataAdapter da;
        DataSet ds = new DataSet();
        SqlCommand cmnd = new SqlCommand();
        SqlCommand cmnd1 = new SqlCommand();
        SqlDataReader reader1;
        public static DataTable det_pedidos = new DataTable("det_pedidos");
        public static DataTable det_pedidos2 = new DataTable("det_pedidos2");
        public static DataTable productos_leidos = new DataTable("productos_leidos");
        string query = "", prod_clave = "", folio = "", tipo = "", cadena = "", prod_nombre = "";
        int tarima = 0, caja = 0, tarimaf = 0;
        bool find = false;
        ArrayAdapter<System.String> comboAdapter;
        System.String[] strFrutas;

        //folio de campo
        int FolioCampo = 0;


        int Desactivarhabilitarreimprimir = 0;


        public static string imei = "";


        DataTable CatProd = new DataTable();

        //Declarar los datos de los items en el layout CapturarSplit
        EditText foliocaptura;
        TextView total;
        Button Guardar;

        Int32 TotCaj;


        //CheckBox Eliminar Caja
        CheckBox Eliminar_caja;


        string valorfinal = "";


        //Datos supervisor
        EditText supervisor;
        EditText passwordsupervisor;

        //Radio button
        RadioButton etiblanca;
        RadioButton etiverde;

        EditText et;










    }
}