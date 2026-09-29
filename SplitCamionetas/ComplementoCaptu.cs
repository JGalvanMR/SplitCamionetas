using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Text;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using Java.Lang;
using Java.Util;
using SplitCamionetas.Modal;
using SplitCamionetas.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace SplitCamionetas
{
    [Activity(Label = "Complemento Capturar")]

    public partial class ComplementoCaptu : Activity, Android.Text.ITextWatcher
    {
        public static int valido = 0, veces = 0;
        public static string cvvehiculo, cvresponsable;
        public static string vehiculo, responsable;
        public string Nombre = "", Mtipo = "", MProd = "", MTar = "", MFol = "", mUser = "", mAutoriza = "", user = "", motfolade = "";
        public string cvecam = "", muser = "", mconcen = "1", Version = "7.5COMP";
        public static string AutoPed = "N";
        public static string EtiquetaExiste = "S", EtiquetaCapturada = "S";
        public static string HayExistencias = "S";
        public static string Surtidomayor = "S";
        public static string ValiFechacad = "S";
        public static string dondegenera = "";
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


        public static string imei = "";

        int Desactivarhabilitarreimprimir = 0;
        DataTable CatProd = new DataTable();

        //Declarar los datos de los items en el layout CapturarSplit
        EditText foliocaptura;
        TextView total;
        TextView pedidoencaptura;
        Button Guardar;

        TextView nosplit;


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

        #region VARIABLES PARA VALIDAR ETIQUETAS VERDES
        string SerialShippingContainerCode = "0000796631";
        string patron = @"^00007966310*([1-9]\d*).$";
        DataTable Foliosleidos = new DataTable();
        DataTable FoliosleidosPresplit = new DataTable();

        int total_caja_verde = 0;
        #endregion

        protected override void OnCreate(Bundle savedInstanceState)
        {
            Android.Telephony.TelephonyManager mTelephonyMgr;
            mTelephonyMgr = (Android.Telephony.TelephonyManager)GetSystemService(TelephonyService);
            //IMEI number  
            imei = mTelephonyMgr.DeviceId;

            cvvehiculo = Intent.GetStringExtra("cvcamioneta");
            cvresponsable = Intent.GetStringExtra("cvresponsable");
            vehiculo = Intent.GetStringExtra("camioneta");
            responsable = Intent.GetStringExtra("responsable");


            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.ComplementoCap);
            LoadConnection();
            TotCaj = 0;
            muser = SolicitarPed.responsable;
            cvecam = SolicitarPed.cvvehiculo;

            Eliminar_caja = FindViewById<CheckBox>(Resource.Id.Eliminar);

            foliocaptura = FindViewById<EditText>(Resource.Id.Foliocom);
            total = FindViewById<TextView>(Resource.Id.totalcapturadocom);
            pedidoencaptura = FindViewById<TextView>(Resource.Id.pedidoencapturacom);
            nosplit = FindViewById<TextView>(Resource.Id.splitcantidadcom);
            etiblanca = FindViewById<RadioButton>(Resource.Id.comradio_blanco);
            etiverde = FindViewById<RadioButton>(Resource.Id.comradio_verde);
            Guardar = FindViewById<Button>(Resource.Id.GuardarCapturadocom);
            Guardar.Click += BtnGuardar_Click;
            Guardar.Enabled = false;

            thisConnection.Open();
            string cadena = "Select prod_clave,prod_nombre from tb_cat_producto where estatus = 'A' AND (prod_tipo = 'PTP' OR prod_tipo = 'PTC')  order by LEN(prod_clave) DESC";
            //string cadena = "Select prod_clave,prod_nombre from tb_cat_producto where prod_tipo in ( 'PTP', 'PTC') and estatus='A' and len(prod_clave) >= 9 order by LEN(prod_clave) DESC";
            SqlDataAdapter da = new SqlDataAdapter(cadena, thisConnection);
            DataSet ds = new DataSet();
            da.Fill(ds, "CatProd");
            CatProd = ds.Tables["CatProd"];

            //Traer numero de split
            var quex = db.Table<Pedidos>();
            foreach (var captu in quex)
            {
                nosplit.Text = "Split Numero: " + NoSplit(captu.folio.ToString());
                pedidoencaptura.Text = "Pedido Actual: " + captu.folio.ToString();
            }

            //tERMINA TRAER NUMERO DE SPLIT





            thisConnection.Close();

            //consulta de Folio de Campo
            thisConnection.Open();
            cmnd = thisConnection.CreateCommand();
            cmnd.CommandText = "select inicio_campo from Tb_folio_campo";
            FolioCampo = Convert.ToInt32(cmnd.ExecuteScalar());


            cmnd = thisConnection.CreateCommand();
            cmnd.CommandText = "select sts_reetiquetado from Tb_Reetiquetadohabilitar";
            Desactivarhabilitarreimprimir = Convert.ToInt32(cmnd.ExecuteScalar());
            thisConnection.Close();


            //****************************************Inicio Lectura de QR**************************************************************************************


            foliocaptura.AddTextChangedListener(this);


            List<FlimStarInfo> lstFlimStar = productocapturado();
            var gvObject = FindViewById<GridView>(Resource.Id.gvCtr2com);
            gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);
            gvObject.ItemClick += new EventHandler<AdapterView.ItemClickEventArgs>(OnGridView_ItemClicked);

            total.Text = TotCaj.ToString("##0");



            //foliocaptura.KeyPress += onEditTextKeyPress;

        }



        public bool OnTouch(View v, MotionEvent e)
        {
            // Pass the event to the edit text to have the blinking cursor.
            v.OnTouchEvent(e);
            // Hide the input.
            var imm = ((InputMethodManager)v.Context.GetSystemService(Context.InputMethodService));
            imm?.HideSoftInputFromWindow(v.WindowToken, HideSoftInputFlags.None);
            return true;
        }


        private void BtnGuardar_Click(object sender, EventArgs e)
        {
            Guardar.Enabled = false;


            var progressDialog = ProgressDialog.Show(this, "Espere Por Favor...", "Guardando Split", true);


            new System.Threading.Thread(new ThreadStart(delegate
            {//LOAD METHOD TO GET ACCOUNT INFO

                db.Query<xLoteFinal>("delete from  [xLoteFinal]");
                db.Query<Pedidos>("UPDATE [Pedidos] SET surtido = '0'");
                thisConnection.Open();
                string mped = pedidoencaptura.Text.ToString().Trim();
                mped = mped.Replace("Pedido Actual: ", "");
                //Actualizacion de pedido leido por cada producto

                string mpedido = mped;
                //Actualizacion de pedido leido por cada producto

                db.Query<xLote>("UPDATE [xLote] SET Pedido = '" + mpedido + "'");


                var productoscapturados = db.Table<xLote>();
                foreach (var captu in productoscapturados)
                {
                    string mtip = "", mfol = "", mcod = "", mtar = "", mcaj = "", mdia = "", mmes = "", mfeccap = "";

                    mtip = captu.Tipo.ToString().Trim();
                    mfol = captu.Folio.ToString().Trim();
                    mcod = captu.Codigo.ToString().Trim();
                    mtar = captu.Tarima.ToString().Trim();
                    mcaj = captu.Cajas.ToString().Trim();
                    mdia = captu.diacad.ToString().Trim();
                    mmes = captu.mescad.ToString().Trim();
                    mfeccap = captu.fecha_captura.ToString().Trim();
                    string lectura = mtip + mfol + mcod + mtar + mcaj;
                    string nom = traenom(mcod);
                    var pedidos = db.Query<Pedidos>("SELECT * FROM [Pedidos] Where prod_clave = '" + mcod.ToString().Trim() + "'");
                    foreach (var pedisur in pedidos)
                    {
                        //if (mcod == pedisur.prod_clave) {
                        //mped = pedisur.surtido.ToString();
                        if (Convert.ToInt32(pedisur.surtido) < Convert.ToInt32(pedisur.pedido))
                        {
                            //mped = pedisur.folio.ToString();
                            db.Query<Pedidos>("UPDATE [Pedidos] SET surtido = '" + (Convert.ToInt32(pedisur.surtido) + 1) + "' WHERE prod_clave = '" + mcod.ToString() + "' AND Folio = '" + mped.ToString() + "'");
                            //db.Query<xLote>("UPDATE [xLote] SET Pedido = '" + mped + "' WHERE Tipo = '" + mtip.ToString() + "' AND Folio = '" + mfol.ToString() + "' AND Codigo = '" + mcod.ToString() + "' AND Tarima = '" + mtar.ToString() + "' AND Cajas = '" + mcaj.ToString() + "'");
                            break;
                        }
                        //}
                    }
                    string cadena = "insert into tb_det_Etiqueta(fecha,emb_folio, fecha_cap, Eti_Lectura, Eti_Recibo, Eti_Producto, Eti_Caja, Eti_TarIni, Eti_TarFin, Cve_Camioneta, FecCap, Version, Imei, Split, Estatus) " +
                                    "Values('" + System.DateTime.Now.ToString("dd/MM/yyyy") + "','" + mped + "','" + mfeccap + "','" + lectura + "','" + mfol + "','" + mcod + "','" + mcaj + "','" + mtar + "','" + mtar + "','" +
                                    cvecam + "','" + System.DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "','" + Version + "','" + imei + "', '" + nosplit.Text.Replace("Split Numero: ", "") + "', 'A' )";
                    SqlCommand cmd = new SqlCommand(cadena, thisConnection);
                    cmd.ExecuteNonQuery();

                    //Actualizacion de status en caso de que la etiqueta ya se haya leido previamente en el sistema de preesplit
                    string cadenaactualizar = "UPDATE Tb_Det_Etiqueta_Presplit SET Estatus = 'S' Where Eti_Lectura = '" + lectura + "'";
                    SqlCommand cmdactualizar = new SqlCommand(cadenaactualizar, thisConnection);
                    cmdactualizar.ExecuteNonQuery();

                    string hay = "N";
                    var lotesproducto = db.Query<xLoteFinal>("SELECT * FROM [xLoteFinal] Where tipo = '" + mtip + "' and Pedido = '" + mped + "' and Folio = '" + mfol + "' and Codigo = '" + mcod + "' and Tarima = '" + mtar + "'");
                    foreach (var lotesencontrado in lotesproducto)
                    {
                        db.Query<xLoteFinal>("UPDATE [xLoteFinal] SET Cajas = '" + (Convert.ToInt32(lotesencontrado.Cajas) + 1) + "' WHERE Tipo = '" + mtip + "' and Pedido = '" + mped + "' and Folio = '" + mfol + "' and Codigo = '" + mcod + "' and Tarima = '" + mtar + "'");
                        hay = "S";
                    }
                    if (hay == "N")
                    {
                        xLoteFinal LoteFinal = new xLoteFinal { Tipo = mtip, Pedido = mped, Folio = mfol, Codigo = mcod, Tarima = mtar, Cajas = "1", nombre = nom, diacad = mdia, mescad = mmes };
                        //Registra en la base de datos SQLite
                        db.Insert(LoteFinal);

                    }


                }
                var pedidoslote = db.Table<xLoteFinal>();
                foreach (var lotes in pedidoslote)
                {
                    var ampm = System.DateTime.Now.ToString("tt");
                    ampm = ampm.Replace(" ", "");
                    // AGREGO LOS REGISTROS EN LA TABLA DE LOS SPLIT PARA QUE ´PUEDAN SER CARGADOS EN LA CAMIONETA
                    string mnom = lotes.nombre.ToString().Trim();
                    mnom = mnom.Replace("'", " ");

                    string ordven = lotes.Pedido.ToString();
                    string tipord = "NAL";

                    if (Convert.ToInt32(lotes.Pedido.ToString().Trim()) < 300000)
                    {
                        ordven = "0" + lotes.Pedido.ToString();
                        tipord = "EXP";
                    }

                    string cadena = "Insert into tb_det_split(emb_folio, prod_clave, emb_tipo, no_lote, cajas, tarima, nom_prod, tipo_rec, estatus, LUGAR, TARINI, TARFIN, DIACAD, MESCAD, FECHA, HORA, NOM_CAPSPLIT, XCAJA) " +
                                    "Values('" + ordven.ToString() + "','" + lotes.Codigo.ToString() + "','" + tipord.Trim() + "','" + lotes.Folio.ToString() + "','" + lotes.Cajas.ToString() + "','" + nosplit.Text.Replace("Split Numero: ", "") + "',' " +
                                    mnom + "','" + lotes.Tipo.ToString() + "','A','Nacional','" + lotes.Tarima.ToString() + "','" +
                                    lotes.Tarima.ToString().ToString() + "','" + lotes.diacad.ToString() + "','" + lotes.mescad.ToString() + "','" + System.DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss") + " " + ampm + "','" +
                                    System.DateTime.Now.ToString("hh:mm") + " " + ampm + "','" + muser.Trim() + "','S')";
                    //MessageBox.Show(cadena);
                    SqlCommand cmd = new SqlCommand(cadena, thisConnection);
                    cmd.ExecuteNonQuery();
                    // ACTUALIZO LAS CAJAS SURTIDAS DE ACUERDO AL FOLIO CODIGO Y TARIMA 
                    if (lotes.Pedido.ToString().Trim().Length > 0)
                    {
                        if (lotes.Tipo.ToString() == "PTC")
                            cadena = "UPDATE TB_DET_TRAZABILIDAD SET SURTIDO = SURTIDO + " + lotes.Cajas.ToString() + " WHERE PROD_CLAVE = '" + lotes.Codigo.ToString() + "' AND RECIBO = '" + lotes.Folio.ToString() + "' " +
                                "AND TIPO = 'PTC' AND TARIMA = '" + Convert.ToInt32(lotes.Tarima.ToString()).ToString() + "' ";

                        else
                            cadena = "UPDATE TB_DET_ETI_FINAL SET CAJAS_SUR = CAJAS_SUR + " + lotes.Cajas.ToString() + " WHERE CVE_PROD = '" + lotes.Codigo.ToString().Trim() + "' AND FOLIO = '" + lotes.Folio.ToString() + "' " +
                                "AND TARIMA = '" + Convert.ToInt32(lotes.Tarima).ToString() + "' ";
                        cmd = new SqlCommand(cadena, thisConnection);
                        cmd.ExecuteNonQuery();
                    }
                }
                if (AutoPed == "S")
                {
                    var pedidosprod = db.Table<Pedidos>();
                    foreach (var prod in pedidosprod)
                    {
                        if (Convert.ToInt16(prod.pedido) != Convert.ToInt16(prod.surtido))
                            AgregaRegistroPedidoAuto(prod.folio.ToString(), "Prod: (" + prod.prod_clave.ToString() + ") " + prod.nombre.ToString().Trim() + " Ped:" + prod.pedido.ToString().Trim() + " Sur:" + prod.surtido.ToString().Trim());
                    }
                }
                AgregaProdXPedido();

                AgregaTempSplit();

                if (ValiFechacad == "N")
                {
                    AgregaDetaEtiAdelantado();
                }

                thisConnection.Close();



                Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                alertDialog.SetTitle(Html.FromHtml("<font color='#DF0101' size = 10>Informacion Almacenada</font>"));
                alertDialog.SetIcon(Resource.Drawable.exito);
                alertDialog.SetMessage(Html.FromHtml("<font color='#FFFFFF' size = 10>Información Grabada Correctamente!!! </font>"));
                alertDialog.SetCancelable(false);
                alertDialog.SetNeutralButton("Ok", delegate
                {
                    alertDialog.Dispose();
                    db.Query<Pedidos>("delete from  [Pedidos]");
                    db.Query<ConPedidos>("delete from  [ConPedidos]");
                    db.Query<xLote>("delete from  [xLote]");
                    db.Query<xLoteFinal>("delete from  [xLoteFinal]");
                    db.Query<xprod>("delete from  [xprod]");


                    Intent intent = new Intent(this, typeof(SolicitarPed));
                    intent.AddFlags(ActivityFlags.ClearTop);
                    Intent.AddFlags(ActivityFlags.SingleTop);
                    intent.PutExtra("cvcamioneta", cvvehiculo.ToString());
                    intent.PutExtra("cvresponsable", cvresponsable.ToString());
                    intent.PutExtra("camioneta", vehiculo.ToString());
                    intent.PutExtra("responsable", responsable.ToString());
                    StartActivity(intent);
                });
                RunOnUiThread(() => alertDialog.Show());

                RunOnUiThread(() => Toast.MakeText(this, "Split Almacenado Correctamente.", ToastLength.Long).Show()); //HIDE PROGRESS DIALOG 
                RunOnUiThread(() => progressDialog.Hide());
            })).Start();

        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.top_menu_captura, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        private void AgregaProdXPedido()
        {

            var pedidosproducto = db.Table<Pedidos>();
            foreach (var producto in pedidosproducto)
            {

                //Ver si hay registros del producto
                string Cadena = "Select CANTSURTIDO from TB_DET_SPLIT_PRODXPED where PDN_FOLIO = '" + producto.folio.ToString() + "' AND PROD_CLAVE = '" + producto.prod_clave.ToString() + "'";
                SqlCommand cmdx = new SqlCommand(Cadena, thisConnection);
                var valor = Convert.ToString(cmdx.ExecuteScalar());

                // termina ver si hay registros

                int total = traetotal(producto.prod_clave.ToString().Trim());

                string mnom = producto.nombre.ToString().Trim();
                mnom = mnom.Replace("'", " ");

                string cadena = "";

                if (valor == "")
                {
                    cadena = "INSERT INTO TB_DET_SPLIT_PRODXPED(FECHA,CVE_CAMIONETA,NOM_CAPSPLIT,PDN_FOLIO,PROD_CLAVE,PROD_NOMBRE,CANTPEDIDO,CANTSURTIDO) " +
                                "VALUES('" + System.DateTime.Now.ToString("dd/MM/yyyy") + "','" + cvecam + "','" + muser.Substring(0, 20) +
                                "','" + producto.folio.ToString() + "','" + producto.prod_clave.ToString() + "','" + mnom +
                                "','" + producto.pedido.ToString() + "','" + total + "')";
                }
                else
                {
                    cadena = "UPDATE TB_DET_SPLIT_PRODXPED  SET cantsurtido = '" + total + "' WHERE PDN_FOLIO = '" + producto.folio.ToString() + "' AND PROD_CLAVE = '" + producto.prod_clave.ToString() + "'";
                }


                SqlCommand cmd = new SqlCommand(cadena, thisConnection);
                cmd.ExecuteNonQuery();
            }
        }

        private void AgregaTempSplit()
        {

            var pedidosproducto = db.Query<Pedidos>("SELECT DISTINCT folio FROM [Pedidos]");
            foreach (var producto in pedidosproducto)
            {
                var ampmx = System.DateTime.Now.ToString("tt");
                ampmx = ampmx.Replace(" ", "");

                string cadena = "";

                if (Convert.ToInt32(nosplit.Text.Replace("Split Numero: ", "")) == 1)
                {
                    cadena = "INSERT INTO TB_TMP_PED(EMB_FOLIO, EMB_TIPO, STATUS, LUGAR, NOM_CAPSPLIT, FECHA) " +
                                "VALUES('" + producto.folio.ToString() + "', 'NAL', 'A', 'NAL', '" + muser.Trim() + "',  '" + System.DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss") + " " + ampmx + "')";

                    SqlCommand cmd = new SqlCommand(cadena, thisConnection);
                    cmd.ExecuteNonQuery();
                }


            }
        }

        private void AgregaRegistroPedidoAuto(string Mped, string mDet)
        {
            string cadena = "INSERT INTO TB_REGISTRO_MOVIMIENTOS(FECHA,NOM_COMPU,NOM_USU,TIPO_MOV,OP_CLAVE,FOLIO,DETALLE,SISTEMA,MOV_FOLIO) " +
                            "VALUES('" + System.DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "','CEL " + imei + "','" + mAutoriza.Trim() + "','A','7.10','" +
                            Mped + "','" + mDet + "','SPLITCA','" + Mped + "')";
            //MessageBox.Show(cadena);
            SqlCommand cmd = new SqlCommand(cadena, thisConnection);
            cmd.ExecuteNonQuery();
        }


        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (item.TitleFormatted.ToString() == "Limpiar")
            {

                total.Text = "000";
                TotCaj = 0;
                foliocaptura.Text = "";
                Guardar.Enabled = false;
                foliocaptura.RequestFocus();
                db.Query<xLote>("delete from  [xLote]");
                db.Query<xprod>("delete from  [xprod]");
                db.Query<Pedidos>("UPDATE [Pedidos] SET surtido = '0'");
                db.Query<ConPedidos>("UPDATE [ConPedidos] SET surtido = '0'");
                db.Query<ConPedidos>("Delete FROM [ConPedidos] WHERE pedido = '0'");

                ConsPedSurdos(pedidoencaptura.Text.Replace("Pedido Actual: ", ""));

                List<FlimStarInfo> lstFlimStar = detalle_pedido();
                lstFlimStar.Clear();
                var gvObject = FindViewById<GridView>(Resource.Id.gvCtr2com);
                gvObject.Adapter = new myGVItemAdapter(this, null);
                gvObject.Adapter = null;
                gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);
                gvObject.ItemClick += new EventHandler<AdapterView.ItemClickEventArgs>(OnGridView_ItemClicked);
                mconcen = "1";

                var quex = db.Table<Pedidos>();
                foreach (var captu in quex)
                {
                    ConsPedSur(captu.folio.ToString());
                }

                Toast.MakeText(this, "La informacion ha sido limpiada", ToastLength.Short).Show();

            }
            else if (item.TitleFormatted.ToString() == "Validar")
            {
                //Evitar apagar la pantalla*********************************************************************************************
                Context context = this;
                var pm = PowerManager.FromContext(context);
                var wakeLock = pm.NewWakeLock(WakeLockFlags.Full, "Validar");
                wakeLock.Acquire();
                //Adquirir el wakelock**************************************************************************************************

                var progressDialog = ProgressDialog.Show(this, "Espere Por Favor...", "Validando Informacion Capturada...", true);


                new System.Threading.Thread(new ThreadStart(delegate
                {//LOAD METHOD TO GET ACCOUNT INFO

                    try
                    {
                        dondegenera = "Iniciovalidar";
                        string existenproductos = "NO";
                        var existeCapturado = db.Table<xprod>();
                        foreach (var captu in existeCapturado)
                        {
                            existenproductos = "SI";
                        }

                        if (existenproductos == "SI")
                        {
                            if (validaestructuraetiqueta() == "SI")
                            {
                                db.Query<Mensajes>("delete from  [Mensajes]");
                                AutoPed = "N";
                                RunOnUiThread(() => Guardar.Enabled = false);

                                var validando = valida();
                                var producto = validaprod();
                                //var validandofec = validafecad();
                                var validandofec = validafecadMod();


                                if (Surtidomayor == "NR")
                                {
                                    RunOnUiThread(() => Guardar.Enabled = false);
                                    ImprimirDialogs(0);
                                }
                                else if (HayExistencias == "NE")
                                {
                                    ImprimirDialogs(0);
                                }
                                else
                                {
                                    if (EtiquetaCapturada == "N")
                                    {
                                        ImprimirDialogs(0);
                                    }
                                    else
                                    {
                                        if (EtiquetaExiste == "S")
                                        {
                                            if (producto == "S" && (validando == "S"))
                                            {
                                                RunOnUiThread(() => Guardar.Enabled = true);

                                                //if ((ValiFechacad == "N"))
                                                //{
                                                //    RunOnUiThread(() => Guardar.Enabled = false);

                                                //    et = new EditText(this);
                                                //    et.InputType = Android.Text.InputTypes.TextVariationPassword | Android.Text.InputTypes.ClassText;
                                                //    et.LongClickable = false;
                                                //    et.Hint = "Password";
                                                //    AlertDialog.Builder ad = new AlertDialog.Builder(this);
                                                //    ad.SetTitle("Autorizacion Folios Adelantados");
                                                //    ad.SetCancelable(false);
                                                //    ad.SetView(et);
                                                //    ad.SetPositiveButton(Html.FromHtml("<font face = 'Comic Sans MS, arial' color='#DF0101' size = '10'>Guardar</font>"), SaveName);
                                                //    ad.SetNegativeButton(Html.FromHtml("<font face = 'Comic Sans MS, arial' color='#DF0101' size = '10'>Cancelar</font>"), CancelAction);
                                                //    RunOnUiThread(() => ad.Show());
                                                //    //RunOnUiThread(() => fnShowCustomAlertDialogCancel());
                                                //}

                                            }
                                            else
                                            {
                                                RunOnUiThread(() => Guardar.Enabled = false);
                                            }

                                            if (validando != "S" || producto != "S")
                                            {
                                                /*if (producto == "N" || HayExistencias == "NE")
                                                {
                                                    RunOnUiThread(() => Guardar.Enabled = true);
                                                }*/

                                            }
                                            /*if ((Guardar.Enabled == true) && (ValiFechacad == "N"))
                                            {
                                                RunOnUiThread(() => Guardar.Enabled = false);
                                                RunOnUiThread(() => fnShowCustomAlertDialogCancel());
                                            }*/
                                            ImprimirDialogs(0);
                                        }
                                        else
                                        {
                                            ImprimirDialogs(0);
                                        }
                                    }
                                }
                                TotCaj = 0;
                                List<FlimStarInfo> lstFlimStar = detalle_lote();
                                var gvObject = FindViewById<GridView>(Resource.Id.gvCtr2com);
                                RunOnUiThread(() => gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar));
                                gvObject.ItemClick += new EventHandler<AdapterView.ItemClickEventArgs>(OnGridView_ItemClicked);
                                //insertarinfo();
                                RunOnUiThread(() => total.Text = TotCaj.ToString("##0"));

                            }
                        }
                        else
                        {
                            Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                            alertDialog.SetTitle(Html.FromHtml("<font color='#DF0101' size = 10>Sin Productos Capturados</font>"));
                            alertDialog.SetIcon(Resource.Drawable.no);
                            alertDialog.SetMessage(Html.FromHtml("<font color='#FFFFFF' size = 10>No existen productos capturados para validar</font>"));
                            alertDialog.SetCancelable(false);
                            alertDialog.SetNeutralButton("Ok", delegate
                            {
                                alertDialog.Dispose();
                            });
                            RunOnUiThread(() => alertDialog.Show());
                        }

                        mconcen = "1";

                        RunOnUiThread(() => Toast.MakeText(this, "Proceso Validado correctamente.", ToastLength.Long).Show()); //HIDE PROGRESS DIALOG 
                        RunOnUiThread(() => progressDialog.Hide());
                        wakeLock.Release();
                    }

                    catch (System.Exception ex)
                    {
                        SendMail("jgalvan@mrlucky.com.mx", "Error generado en la validacion COMPLEMENTO SPLIT CAMIONETAS " + ex, "Error En la Validacion + " + dondegenera);
                        Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                        alertDialog.SetTitle(Html.FromHtml("<font color='#DF0101' size = 10>Error en la validaciòn</font>"));
                        alertDialog.SetIcon(Resource.Drawable.no);
                        alertDialog.SetMessage(Html.FromHtml("<font color='#FFFFFF' size = 10>Ocurrio un error inesperado durante la validación, Favor de Validar nuevamente</font>"));
                        alertDialog.SetCancelable(false);
                        alertDialog.SetNeutralButton("Ok", delegate
                        {
                            alertDialog.Dispose();
                            Intent intent = new Intent(this, typeof(SolicitarPed));
                            intent.PutExtra("cvcamioneta", cvvehiculo.ToString());
                            intent.PutExtra("cvresponsable", cvresponsable.ToString());
                            intent.PutExtra("camioneta", vehiculo.ToString());
                            intent.PutExtra("responsable", responsable.ToString());
                            StartActivity(intent);
                            Finish();
                        });
                        RunOnUiThread(() => alertDialog.Show());

                        RunOnUiThread(() => Toast.MakeText(this, "Ocurrio un error en la validación", ToastLength.Long).Show()); //HIDE PROGRESS DIALOG 
                        RunOnUiThread(() => progressDialog.Hide());

                    }

                })).Start();

            }
            else
            {
                mconcen = "2";
                List<FlimStarInfo> lstFlimStar = detalle_pedido();
                var gvObject = FindViewById<GridView>(Resource.Id.gvCtr2com);
                gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);
                gvObject.ItemClick += new EventHandler<AdapterView.ItemClickEventArgs>(OnGridView_ItemClicked); //detalle_pedido
                Toast.MakeText(this, "Modo Concentrado Activado", ToastLength.Short).Show();
            }


            return base.OnOptionsItemSelected(item);
        }




        public void SendMail(string Dest, string mBody, string mAsunto)
        {
            MailMessage msg = new MailMessage();
            MailMessage email = new MailMessage();

            string[] destinatarios = Dest.Split(';');
            foreach (string destinos in destinatarios)
            {
                email.To.Add(new MailAddress(destinos));
            }
            //email.To.Add(new MailAddress("gcamacho@mrlucky.com.mx"));

            email.From = new MailAddress("jgalvan@mrlucky.com.mx"); //
            email.Subject = mAsunto; //"Mensaje de Prueba";
            email.Body = mBody;  //"Información de la factura";
            email.IsBodyHtml = true;
            email.Priority = MailPriority.Normal;



            SmtpClient smtp = new SmtpClient();
            smtp.Host = "mail1.mrlucky.com.mx";
            smtp.Port = 587;
            smtp.EnableSsl = true;
            smtp.UseDefaultCredentials = false;
            //smtp.Credentials = new NetworkCredential("dmunoz", "GuIraSis003$1234");
            smtp.Credentials = new NetworkCredential("jgalvan", "mnK3a2aN@1|Q21VV");

            try
            {
                smtp.Send(email);
                email.Dispose();
                RunOnUiThread(() => Toast.MakeText(this, "correo enviado exitosamente\r\n", ToastLength.Short).Show());
            }
            catch (System.Exception ex)
            {

                RunOnUiThread(() => Toast.MakeText(this, "correo no enviado\r\n" + ex.ToString(), ToastLength.Short).Show());
            }
        }

        private void ImprimirDialogs(int mensaje)
        {
            int mensajeactual = 0;
            var query = db.Table<Mensajes>();
            foreach (var captu in query)
            {
                if (mensajeactual == mensaje)
                {

                    if (captu.titulo.Trim() == "Existe un folio anterior disponible")
                    {
                        Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                        alertDialog.SetTitle(Html.FromHtml("<font color='#FCEC70' size = 10>" + captu.titulo.ToString() + "</font>"));
                        alertDialog.SetIcon(Resource.Drawable.warning);
                        alertDialog.SetMessage(Html.FromHtml("<font color='#E0F1FA' size = 10>" + captu.mensaje.ToString() + "</font>"));
                        alertDialog.SetCancelable(false);
                        alertDialog.SetNeutralButton("Ok", delegate
                        {
                            alertDialog.Dispose();
                            ImprimirDialogs(mensaje + 1);

                        });
                        RunOnUiThread(() => alertDialog.Show());

                    }
                    else if (captu.titulo.Trim() == "Etiqueta ya capturada")
                    {
                        Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                        alertDialog.SetTitle(Html.FromHtml("<font color='#62D9FF' size = 10>" + captu.titulo.ToString() + "</font>"));
                        alertDialog.SetIcon(Resource.Drawable.Info);
                        alertDialog.SetMessage(Html.FromHtml("<font color='#88D0FF' size = 10>" + captu.mensaje.ToString() + "</font>"));
                        alertDialog.SetCancelable(false);
                        alertDialog.SetNeutralButton("Ok", delegate
                        {
                            alertDialog.Dispose();
                            ImprimirDialogs(mensaje + 1);

                        });
                        RunOnUiThread(() => alertDialog.Show());

                    }
                    else if (captu.titulo.Trim() == "Error Surtido Mayor al Pedido")
                    {
                        Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                        alertDialog.SetTitle(Html.FromHtml("<font color='#27FF00' size = 10>" + captu.titulo.ToString() + "</font>"));
                        alertDialog.SetIcon(Resource.Drawable.radiactivo);
                        alertDialog.SetMessage(Html.FromHtml("<font color='#34FF4A' size = 10>" + captu.mensaje.ToString() + "</font>"));
                        alertDialog.SetCancelable(false);
                        alertDialog.SetNeutralButton("Ok", delegate
                        {
                            alertDialog.Dispose();
                            ImprimirDialogs(mensaje + 1);

                        });
                        RunOnUiThread(() => alertDialog.Show());
                    }
                    else if (captu.titulo.Trim() == "Tarima Surtida Completamente")
                    {
                        Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                        alertDialog.SetTitle(Html.FromHtml("<font color='#FABF57' size = 10>" + captu.titulo.ToString() + "</font>"));
                        alertDialog.SetIcon(Resource.Drawable.no);
                        alertDialog.SetMessage(Html.FromHtml("<font color='##FECB82' size = 10>" + captu.mensaje.ToString() + "</font>"));
                        alertDialog.SetCancelable(false);
                        alertDialog.SetNeutralButton("Ok", delegate
                        {
                            alertDialog.Dispose();
                            ImprimirDialogs(mensaje + 1);

                        });
                        RunOnUiThread(() => alertDialog.Show());

                    }
                    else
                    {
                        Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                        alertDialog.SetTitle(Html.FromHtml("<font color='#DF0101' size = 10>" + captu.titulo.ToString() + "</font>"));
                        alertDialog.SetIcon(Resource.Drawable.no);
                        alertDialog.SetMessage(Html.FromHtml("<font color='#FFFFFF' size = 10>" + captu.mensaje.ToString() + "</font>"));
                        alertDialog.SetCancelable(false);
                        alertDialog.SetNeutralButton("Ok", delegate
                        {
                            alertDialog.Dispose();
                            ImprimirDialogs(mensaje + 1);

                        });
                        RunOnUiThread(() => alertDialog.Show());
                    }
                }
                mensajeactual++;
            }
        }

        private void LoadConnection()
        {
            string folder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            string dbPath = System.IO.Path.Combine(folder, "Split_camionetas_Complemento.db");

            bool exist = File.Exists(dbPath);
            db = new SQLiteConnection(dbPath);

            if (!exist)
            {
                //Crea la tabla en base al modelo si es la primera vez
                db.CreateTable<Pedidos>();
                db.CreateTable<ConPedidos>();
                db.CreateTable<xLote>();
                db.CreateTable<xLoteFinal>();
                db.CreateTable<xprod>();
                db.CreateTable<Mensajes>();
                db.CreateTable<XLoteSug>();
            }

        }

        List<FlimStarInfo> listItem = new List<FlimStarInfo>();

        List<FlimStarInfo> detalle_pedido()
        {
            thisConnection.Open();
            listItem.Clear();

            var query = db.Table<ConPedidos>();
            foreach (var captu in query)
            {

                listItem.Add(new FlimStarInfo()
                {
                    Name = captu.nombre,
                    Age = "Pedidos: " + captu.pedido + " Surtido: " + captu.surtido,
                    ImageID = Resource.Drawable.producto
                });

            }

            mconcen = "2";

            //LbxCons.Font = new Font(LbxCons.Font.Name, 7);   ;
            thisConnection.Close();

            return listItem;
        }

        List<FlimStarInfo> detalle_lote()
        {
            thisConnection.Open();
            listItem.Clear();

            var query = db.Table<xLote>();
            foreach (var captu in query)
            {

                listItem.Add(new FlimStarInfo()
                {
                    Name = captu.nombre,
                    Age = "Folio: " + captu.Folio + " Tarima: " + captu.Tarima + " Caja: " + captu.Cajas + "Dia/Mes Caducidad:" + captu.diacad + "/" + captu.mescad,
                    ImageID = Resource.Drawable.producto
                });
                TotCaj++;
            }

            thisConnection.Close();

            return listItem;
        }


        List<FlimStarInfo> detalle_Surtido()
        {

            listItem.Clear();

            var query = db.Table<ConPedidos>();
            foreach (var captu in query)
            {

                listItem.Add(new FlimStarInfo()
                {
                    Name = captu.nombre,
                    Age = "Pedidos: " + captu.pedido + " Surtido: " + captu.surtido,
                    ImageID = Resource.Drawable.producto
                });

            }


            return listItem;
        }

        List<FlimStarInfo> productocapturado()
        {

            listItem.Clear();

            var query = db.Table<xprod>();
            foreach (var captu in query)
            {

                listItem.Add(new FlimStarInfo()
                {
                    Name = traenom(captu.Codigo.ToString().Trim()),
                    Age = "Recibo: " + captu.Folio + "Tarima: " + captu.Tarima + " Caja: " + captu.Cajas,
                    ImageID = Resource.Drawable.producto
                });

                TotCaj++;

            }


            return listItem;
        }



        private void OnGridView_ItemClicked(object sender, AdapterView.ItemClickEventArgs e)
        {

        }

        private string validaestructuraetiqueta()
        {
            dondegenera = "validaestructuraetiqueta";
            string ok = "SI";
            var productoscapturados = db.Table<xprod>();
            foreach (var captu in productoscapturados)
            {
                var mtip = captu.Tipo.ToString();
                var mfol = captu.Folio.ToString();
                var mcod = captu.Codigo.ToString();
                var mtar = captu.Tarima.ToString();
                var mcaj = captu.Cajas.ToString();
                var NOmprod = traenom(captu.Codigo.ToString());


                try
                {
                    int vfol = Convert.ToInt32(mfol);
                    int vtar = Convert.ToInt32(mtar);
                    int vcaj = Convert.ToInt32(mcaj);
                }
                catch (System.Exception ex)
                {
                    Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                    alertDialog.SetTitle(Html.FromHtml("<font color='#62D9FF' size = 10>Error en la estructura de Etiqueta</font>"));
                    alertDialog.SetIcon(Resource.Drawable.Info);
                    alertDialog.SetMessage(Html.FromHtml("<font color='#88D0FF' size = 10>La etiqueta del producto " + mcod + " - " + NOmprod + " Recibo: " + mfol + " / Tarima " + mtar + " / Caja: " + mcaj + " contiene un error en la tarima, recibo o folio, favor de informar al supervisor, validar la informacion, retirar y reetiquetar la caja y leer  la nueva etiqueta</font>"));
                    alertDialog.SetCancelable(false);
                    alertDialog.SetNeutralButton("Ok", delegate
                    {
                        alertDialog.Dispose();
                        //Borrado de Etiquetas capturadas
                        db.Query<xprod>("delete from[xprod] Where Tipo = '" + mtip + "' AND Folio = '" + mfol + "' AND Codigo = '" + mcod + "' AND Tarima = '" + mtar + "' AND Cajas = '" + mcaj + "'");
                        db.Query<ConPedidos>("UPDATE [ConPedidos] SET surtido = surtido - " + 1 + " WHERE prod_clave = '" + mcod.ToString().Trim() + "'");
                        //******************************

                    });
                    RunOnUiThread(() => alertDialog.Show());
                    ok = "NO";
                }


            }

            return ok;
        }

        private string valida()
        {
            dondegenera = "valida";
            HayExistencias = "S";
            EtiquetaExiste = "S";
            EtiquetaCapturada = "S";
            db.Query<xLote>("delete from  [xLote]");
            string ok = "S";
            int tot = 0, totok = 0;
            thisConnection.Open();
            string mtip = "", mfol = "", mcod = "", mtar = "", mcaj = "", mfeccap = "";
            string amtip = "", amfol = "", amcod = "", amtar = "", amcaj = "", amfeccap = "";
            var conta = 0;
            var productoscapturados = db.Table<xprod>();
            foreach (var captu in productoscapturados)
            {
                string er = "";
                mtip = captu.Tipo.ToString();
                mfol = captu.Folio.ToString();
                mcod = captu.Codigo.ToString();
                mtar = captu.Tarima.ToString();
                mcaj = captu.Cajas.ToString();
                mfeccap = captu.fecha_captura.ToString();

                string nom = traenom(captu.Codigo.ToString().Trim());
                string lectura = mtip + mfol + mcod + mtar + mcaj;
                string fechacap = ValidaCaja(lectura).Trim();
                string fechacappre = ValidaCajaPreesplit(lectura).Trim();
                if (fechacappre == "" && Desactivarhabilitarreimprimir != 0)
                {
                    /*Mensajes mensa = new Mensajes { titulo = "Etiqueta Sin Presplit", mensaje = "Error Etiqueta NO FUE CAPTURADA EN PRESPLIT!! " + "\n\r" + mtip + " | " + mfol + " | " + mcod + " | " + mtar + " | " + mcaj + "\n\r" + "Día " + fechacap + "\n\r" + nom + "\n\r Favor de INCLUIRLA EN PRESPLIT O ELIMINARLA DE LA LECTURA ACTUAL" };
                    db.Insert(mensa);
                    ok = "N";
                    er = "S";*/
                    try
                    {
                        string cadenaCompPreS = "INSERT INTO TB_REGISTRO_MOVIMIENTOS(FECHA,NOM_COMPU,NOM_USU,TIPO_MOV,OP_CLAVE,FOLIO,DETALLE,SISTEMA,MOV_FOLIO) " +
                            "VALUES(GETDATE(),'CEL " + imei + "','" + muser.Substring(0, 20) + "','COMPRES','SPLITCAMIONETAS','" + mfol + "', COMPLEMENTO - '" + lectura + "','SPLITCA','" + mfol + "')";
                        //MessageBox.Show(cadena);
                        SqlCommand cmdCompPreS = new SqlCommand(cadenaCompPreS, thisConnection);
                        cmdCompPreS.ExecuteNonQuery();
                    }
                    catch
                    {

                    }
                }
                if (fechacap.Length > 0)
                {
                    string EmbRespo = ValidaCajaRespon(lectura).Trim();

                    if (Desactivarhabilitarreimprimir == 0)
                    {
                        Mensajes mensa = new Mensajes { titulo = "Etiqueta ya capturada", mensaje = "Error Etiqueta YA FUE CAPTURADA!! " + "\n\r" + mtip + " | " + mfol + " | " + mcod + " | " + mtar + " | " + mcaj + "\n\r" + "Día " + fechacap + "\n\r" + nom + "\n\r" + EmbRespo + "Favor de Ir a Liberar su Reimpresion, Colocar la Nueva Etiqueta y Volver a Leer" };
                        db.Insert(mensa);

                        string reetiquetado = "insert into Tb_Det_Sol_Reetiquetado (Fecha, emb_folio, fecha_cap, Lectura, Recibo, Producto, Caja, TarIni, TarFin, Cve_Camioneta, Estatus, Obs, armador, autorizo, origen) values" +
                            " ('" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "', '',  GETDATE(), '" + lectura + "', '" + mfol + "', '" + mcod + "', '" + mcaj + "', '" + mtar + "', '" + mtar + "', '', 'A', 'SOLICITUD DE REIMPRESION POR ETIQUETA YA LEIDA CAMIONETAS', '" + responsable + "', '', 'EMB')";
                        SqlCommand cmd = new SqlCommand(reetiquetado, thisConnection);
                        cmd.ExecuteNonQuery();
                    }
                    else
                    {
                        Mensajes mensa = new Mensajes { titulo = "Etiqueta ya capturada", mensaje = "Error Etiqueta YA FUE CAPTURADA!! " + "\n\r" + mtip + " | " + mfol + " | " + mcod + " | " + mtar + " | " + mcaj + "\n\r" + "Día " + fechacap + "\n\r" + nom + "\n\r" + EmbRespo + "La Liberacion automatica, fue desactivada, favor de informar a personal de Camras frias" };
                        db.Insert(mensa);
                    }

                    //Borrado de Etiquetas capturadas
                    db.Query<xprod>("delete from[xprod] Where Tipo = '" + mtip + "' AND Folio = '" + mfol + "' AND Codigo = '" + mcod + "' AND Tarima = '" + mtar + "' AND Cajas = '" + mcaj + "'");
                    db.Query<ConPedidos>("UPDATE [ConPedidos] SET surtido = surtido - " + 1 + " WHERE prod_clave = '" + mcod.ToString().Trim() + "'");
                    //******************************
                    EtiquetaCapturada = "N";
                    ok = "N";
                    er = "S";

                }
                string cadena = "";

                int diascad = 14;
                if (nom.Contains("BETABEL"))
                {
                    diascad = 60;
                }
                else if (nom.Contains("AJO"))
                {
                    diascad = 180;
                }
                else if (nom.Contains("ADEREZO") || nom.Contains("VINAGRETA") || nom.Contains("QUESO"))
                {
                    diascad = 90;
                }

                if (mtip == "PTC")
                    cadena = "SELECT TOP(1) ETIQUETA AS PROD,SURTIDO,FECHA_CAD AS FECCAD, (CASE fecha_cad WHEN '' THEN  FORMAT( DATEADD(day, " + diascad + ", pti_fecha), 'dd/MM/yyyy', 'en-US' ) WHEN fecha_cad THEN fecha_cad END) AS fecha_cad FROM TB_DET_TRAZABILIDAD WHERE PROD_CLAVE = '" + mcod + "' AND RECIBO = '" + mfol + "' " +
                             "AND TIPO = '" + mtip + "' AND TARIMA = '" + Convert.ToInt32(mtar).ToString() + "' ";

                else
                    cadena = "SELECT TOP(1) NUM_CAJAS AS PROD, CAJAS_SUR AS SURTIDO,NUM_LOTE AS FECCAD, ISNULL(fechacad, FORMAT( DATEADD(day, " + diascad + ", fecha), 'yyyyMMdd', 'en-US' )) AS fecha_cad FROM TB_DET_ETI_FINAL WHERE CVE_PROD = '" + mcod + "' AND FOLIO = '" + mfol + "' " +
                        "AND TARIMA = '" + Convert.ToInt32(mtar).ToString() + "' ";

                SqlDataAdapter da = new SqlDataAdapter(cadena, thisConnection);
                DataSet ds = new DataSet();

                //MessageBox.Show(cadena); 
                da.Fill(ds, "Info");
                DataTable Info = ds.Tables["Info"];
                //MessageBox.Show(Info.Rows.Count.ToString()); 
                if (Info.Rows.Count == 0)
                {
                    ok = "N";
                    EtiquetaExiste = "N";

                    Mensajes mensa = new Mensajes { titulo = "Etiqueta No Existe", mensaje = "Error Etiqueta No Existe!! " + "\n\r" + mtip + " | " + mfol + " | " + mcod + " | " + mtar + " | " + mcaj + "\n\r" + "Día " + fechacap + "\n\r" + nom + " Informe al supervisor, Retire la caja, Reetiquete y Leala nuevamente" };
                    db.Insert(mensa);

                    db.Query<xprod>("delete from[xprod] Where Tipo = '" + mtip + "' AND Folio = '" + mfol + "' AND Codigo = '" + mcod + "' AND Tarima = '" + mtar + "' AND Cajas = '" + mcaj + "'");
                    db.Query<ConPedidos>("UPDATE [ConPedidos] SET surtido = surtido - " + 1 + " WHERE prod_clave = '" + mcod.ToString().Trim() + "'");

                    er = "S";
                    continue;
                }

                System.String diacaducidad = "";
                System.String mescaducidad = "";

                foreach (DataRow row in Info.Rows)
                {
                    int mP = Convert.ToInt32(row["PROD"]);
                    int mS = Convert.ToInt32(row["SURTIDO"]);
                    int cant = 0;

                    //TRAER LA CANTIDAD DE CAJAS EXISTENTES EN LA LECTURA
                    var query1 = db.Query<xprod>("SELECT * FROM [xprod] Where tipo = '" + mtip + "' and Folio = '" + mfol + "' and Codigo = '" + mcod + "' and Tarima = '" + mtar + "'");
                    foreach (var captu1 in query1)
                    {
                        cant = cant + 1;

                    }

                    if ((mS + cant) > mP)
                    {
                        ok = "N";
                        //LbxCap.SelectedIndex = i;
                        HayExistencias = "NE";
                        er = "S";

                        if (Desactivarhabilitarreimprimir == 0)
                        {
                            if (amtip != mtip || amfol != mfol || amcod != mcod || amtar != mtar)
                            {
                                //Mensajes mensa = new Mensajes { titulo = "Etiqueta Sin Existencias", mensaje = "Error en la Etiqueta Ya No Hay Existecia!!" + "\n\r" + mtip + " | " + mfol + " | " + mcod + " | " + mtar + " \n\r" + nom + "\n\r" + cant.ToString() };
                                //db.Insert(mensa);

                                Mensajes mensa = new Mensajes { titulo = "Tarima Surtida Completamente", mensaje = "La Cantidad a Surtir Supera Por " + ((mS + cant) - mP) + " Cajas Lo Producido, \n\r Produ: " + mP + " | Surt: " + mS + " | Leidos: " + cant + "\n\r" + mtip + " | " + mfol + " | " + mcod + " | " + mtar + " \n\r" + nom + " Favor de ir a Descargue y Habilitar sus folios de Esta orden" };
                                db.Insert(mensa);
                            }

                            //Agregar Solicitud de Habilitacion de Folios******************************************************************************************************
                            AgregaSolHabilitarFolios(mtip, mfol, mcod, nom, mtar, cant.ToString());
                            //Termina Agregar Solicitud de Habilitacion de Folios**********************************************************************************************
                            AgregaFolioSinExistencia(mtip, mfol, mcod, nom, mtar, cant.ToString());
                        }
                        else
                        {
                            if (amtip != mtip || amfol != mfol || amcod != mcod || amtar != mtar)
                            {
                                Mensajes mensa = new Mensajes { titulo = "Tarima Surtida Completamente", mensaje = "La Cantidad a Surtir Supera Por " + ((mS + cant) - mP) + " Cajas Lo Producido, \n\r Produ: " + mP + " | Surt: " + mS + " | Leidos: " + cant + "\n\r" + mtip + " | " + mfol + " | " + mcod + " | " + mtar + " \n\r" + nom + " La Liberacion Automatica esta Deshabilitada, favor de informar a personal de Camaras frias" };
                                db.Insert(mensa);
                            }
                        }


                    }
                    string feccad = "";

                    diacaducidad = traediafecad(row["feccad"].ToString(), mtip);
                    mescaducidad = traemesfecad(row["feccad"].ToString(), mtip);


                    //Validacion de fecha de caduciadad que debe venir*****************************************************************************************************

                    if (diacaducidad == "|")
                    {
                        diacaducidad = traediafecadrec(row["fecha_cad"].ToString(), mtip);
                    }

                    if (mescaducidad == "|")
                    {
                        mescaducidad = traemesfecadrec(row["fecha_cad"].ToString(), mtip);
                    }

                    xLote consecutivo = new xLote { Tipo = captu.Tipo.ToString(), Pedido = "", Folio = captu.Folio, Codigo = captu.Codigo, Tarima = captu.Tarima, Cajas = captu.Cajas, nombre = nom, diacad = diacaducidad.Trim(), mescad = mescaducidad.Trim(), fecha_captura = mfeccap };
                    //Registra en la base de datos SQLite
                    db.Insert(consecutivo);


                    totok++;
                }

                amtip = mtip;
                amfol = mfol;
                amcod = mcod;
                amtar = mtar;
                conta++;

                if (er == "S")
                {
                    tot++;
                }

            }

            if (tot > 0)
            {

                Mensajes mensa = new Mensajes { titulo = "Se detectaron etiquetas con ERROR", mensaje = "Se detectaron " + tot + " Etiquetas con error" };
                db.Insert(mensa);

            }

            thisConnection.Close();
            //List<FlimStarInfo> lstFlimStar = detalle_Surtido();
            //var gvObject = FindViewById<GridView>(Resource.Id.gvCtr2);
            //RunOnUiThread(() => gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar));
            //gvObject.ItemClick += new EventHandler<AdapterView.ItemClickEventArgs>(OnGridView_ItemClicked); //detalle_pedido


            RunOnUiThread(() => total.Text = totok.ToString("##0"));
            return ok;
        }

        private string ValidaCaja(string cadena)
        {
            /*string Cadena = "Select fecha_cap From tb_Det_Etiqueta " +
                           "Where Eti_Lectura = '" + cadena + "' AND Estatus != 'C'";*/
            string Cadena = "Select CONCAT(A.fecha_cap, '*', B.NOM_CAPSPLIT) AS datoscaptura From tb_Det_Etiqueta A LEFT JOIN tb_det_split B ON A.emb_folio = B.emb_folio AND A.Eti_TarIni = B.TARINI AND A.Eti_Producto = B.prod_clave AND A.Split = B.tarima Where A.Eti_Lectura = '" + cadena + "' AND A.Estatus NOT IN ('C', 'R')";
            SqlCommand cmd = new SqlCommand(Cadena, thisConnection);
            string Valor = Convert.ToString(cmd.ExecuteScalar());
            string[] valores = Valor.Split('*');
            Valor = "";
            if ((valores[0].ToString().Trim().Length > 0) && (valores[1].ToString().Trim().Length == 0))
            {
                cadena = "UPDATE tb_det_Etiqueta SET Estatus = 'C', Obs = 'Cancelacion de Etiqueta Por Error En Sistema' Where Eti_Lectura = '" + cadena + "' AND Estatus = 'A'";
                cmd = new SqlCommand(cadena, thisConnection);
                cmd.ExecuteNonQuery();
            }
            else if ((valores[0].ToString().Trim().Length > 0) && (valores[1].ToString().Trim().Length > 0))
            {
                Valor = valores[0].ToString().Trim();
            }
            return Valor;
        }


        private string ValidaEmb(string cadena)
        {
            string Cadena = "Select emb_folio From tb_Det_Etiqueta " +
                           "Where Eti_Lectura = '" + cadena + "'";
            SqlCommand cmd = new SqlCommand(Cadena, thisConnection);
            string Valor = Convert.ToString(cmd.ExecuteScalar());
            return Valor;
        }

        private string ValidaCajaRespon(string cadena)
        {
            string Cadena = "Select CONCAT(' CAPTURADO POR: ', RTRIM(B.NOM_CAPSPLIT), ' / EMBARQUE: ', A.emb_folio) AS datoscaptura From tb_Det_Etiqueta A LEFT JOIN tb_det_split B ON A.emb_folio = B.emb_folio AND A.Eti_TarIni = B.TARINI AND A.Eti_Producto = B.prod_clave AND A.Split = B.tarima Where A.Eti_Lectura = '" + cadena + "' AND A.Estatus NOT IN ('C', 'R')";
            SqlCommand cmd = new SqlCommand(Cadena, thisConnection);
            string Valor = Convert.ToString(cmd.ExecuteScalar());
            return Valor;
        }

        private string traenom(string cve)
        {
            string nom = "";
            foreach (DataRow row in CatProd.Select("prod_clave = '" + cve + "'"))
                nom = row["prod_nombre"].ToString().Trim();

            nom = nom.Replace("'", " ");
            return nom;
        }

        private void AgregaFolioSinExistencia(string mTi, string mFo, string mPr, string mNo, string mTa, string mCa)
        {
            string cadena = "INSERT INTO TB_DET_SPLIT_FOLIOSINEXIS(FECHA,FECHACAP,CVE_CAMIONETA,NOM_CAPSPLIT,TIPO,FOLIO,PROD_CLAVE,PROD_NOMBRE,TARIMA,CAJA) " +
                            "VALUES('" + DateTime.Now.ToString("dd/MM/yyyy") + "','" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "','" +
                            cvecam + "','" + muser.Substring(0, 20) + "','" + mTi + "','" + mFo + "','" + mPr + "','" + mNo + "','" + mTa + "','" + mCa + "')";
            //MessageBox.Show(cadena);
            SqlCommand cmd = new SqlCommand(cadena, thisConnection);
            cmd.ExecuteNonQuery();
        }

        private void AgregaSolHabilitarFolios(string mTi, string mFo, string mPr, string mNo, string mTa, string mCa)
        {
            string mped = "";
            //mped = mped.Replace("Pedido Actual: ", "");
            string cadena = "IF NOT EXISTS(SELECT emb_folio FROM tb_Det_Sol_Mod_inventario WHERE emb_folio = '" + mped + "' AND orden = '" + mFo + "' AND  id_codigo = '" + mPr + "' AND  tarima = '" + mTa + "' AND tipo = '" + mTi + "' AND estatus = 'A') INSERT INTO  tb_Det_Sol_Mod_inventario(emb_folio, orden, tipo, id_codigo, descrip, cajas_mod, fecha_cap, capturo, motivo, tarima, estatus) " +
                            "VALUES('" + mped + "','" + mFo + "','" + mTi + "','" + mPr + "','" + mNo + "','" + mCa + "',GETDATE(),'" + responsable + "','Folio Modificado Por intervension de Split Camionetas','" + mTa + "','A')";
            //MessageBox.Show(cadena);
            SqlCommand cmd = new SqlCommand(cadena, thisConnection);
            cmd.ExecuteNonQuery();
        }

        private string traediafecad(string fecha, string tipo)
        {
            fecha = fecha.Trim();
            string Cad = "|";
            int pos = 0;
            if (fecha.Trim().Length > 0)
            {
                if (tipo == "PTP")
                {
                    pos = fecha.Trim().IndexOf("FC");
                    Cad = fecha.Substring(pos + 5, 2);
                    //Cad = fecha.Substring(fecha.Length - 3, 2);
                }
                else
                {
                    Cad = fecha.Substring(0, 2);
                }

            }
            return Cad;
        }

        private string traemesfecad(string fecha, string tipo)
        {
            fecha = fecha.Trim();
            int pos = 0;
            string Cad = "|";
            if (fecha.Trim().Length > 0)
            {

                if (tipo == "PTP")
                {
                    pos = fecha.Trim().IndexOf("FC");
                    Cad = fecha.Substring(pos + 2, 3);
                    //Cad = fecha.Substring(fecha.Length - 6, 3);
                }
                else
                {
                    Cad = traemes(Convert.ToInt32(fecha.Substring(3, 2)));
                }
            }
            return Cad;
        }

        private string traediafecadrec(string fecha, string tipo)
        {
            fecha = fecha.Trim();
            string Cad = " | ";
            if (fecha.Trim().Length > 0)
            {
                if (tipo == "PTP")
                    Cad = fecha.Substring(fecha.Length - 2, 2);
                else
                    Cad = fecha.Substring(0, 2);
            }
            return Cad;
        }

        private string traemesfecadrec(string fecha, string tipo)
        {
            fecha = fecha.Trim();
            string Cad = " | ";
            if (fecha.Trim().Length > 0)
            {
                if (tipo == "PTP")
                    Cad = traemes(Convert.ToInt32(fecha.Substring(fecha.Length - 4, 2)));
                else
                    Cad = traemes(Convert.ToInt32(fecha.Substring(3, 2)));
            }
            return Cad;
        }


        private string validafecad()
        {
            string Valor = "";
            //Obtener los productos con su tipo de lo que se ha leido******************************************************************
            var productoscapturados = db.Query<xLote>("Select Tipo, Codigo, nombre FROM xLote GROUP BY Tipo, Codigo, nombre");
            db.Query<XLoteSug>("delete from[XLoteSug]");

            var allItems = db.Table<xLote>().ToList();
            int count = allItems.Count;
            int[] validados = new int[count + 1];
            int capturas = 0;
            foreach (var captu in productoscapturados)
            {
                int totalpro = 0;
                int totaldisponibles = 0;
                int totalusadas;
                int simulador = 0;
                int totaldis = 0;
                string fechaant = "";

                //traer el total de recibos vencidos para que no entren en la condicion
                var prodcapx = db.Query<xLote>("Select COUNT(ID) AS Cajas FROM xLote Where Codigo = '" + captu.Codigo.Trim() + "'");

                foreach (var capturadox in prodcapx)
                {
                    totalpro = Convert.ToInt32(capturadox.Cajas.ToString().Trim());

                }

                int resttotal = traerecibosvencidos(captu.Codigo.Trim(), captu.Tipo.Trim());

                totalpro = totalpro - resttotal;

                //Obtener los diferentes folios disponibles dependiendo el codigo y el tipo
                string todobien = "OK";
                int prod_cap = 0;
                int usadas = 0;
                int existefecant = 0;
                string cadena = "";
                string tipo = captu.Tipo.Trim();
                string prod = captu.Codigo.Trim();
                string diacadant = "";
                string mescadant = "";
                if (tipo == "PTC")
                {
                    cadena = "SELECT  (etiqueta - surtido) AS disponible, (CASE fecha_cad WHEN '' THEN  FORMAT( DATEADD(day, 15, pti_fecha), 'dd/MM/yyyy', 'en-US' ) WHEN fecha_cad THEN fecha_cad END) AS fecha_cad, (CASE fecha_cad WHEN '' THEN  FORMAT( DATEADD(day, 15, pti_fecha), 'yyyyMMdd', 'en-US' ) WHEN fecha_cad THEN FORMAT(convert(datetime,fecha_cad), 'yyyyMMdd', 'en-US' ) END) AS fecha_cadu, recibo, tarima FROM TB_DET_TRAZABILIDAD Inner JOIN tb_mstr_recepcion_pt ON rpt_recibo = recibo WHERE PROD_CLAVE = '" + prod + "' AND pti_estatus_sur = '' AND tipo = 'PTC' AND (rpt_tipo != 'TR' OR (rpt_tipo != 'TR' AND rpt_inventario = 'S')) AND rpt_estatus = '' AND  (etiqueta - surtido) > 0 Order By fecha_cadu";
                }
                else
                {
                    cadena = "SELECT (num_cajas - cajas_sur) AS disponible, ISNULL(fechacad, FORMAT( DATEADD(day, 15, fecha), 'yyyyMMdd', 'en-US' )) AS fecha_cad, folio AS recibo, tarima FROM tb_det_eti_final Inner JOIN tb_mstr_ordenes_prod ON folio = ordp_folio WHERE cve_prod = '" + prod + "' AND estatus_sur != 'S' AND ordp_estatus != 'C' AND (num_cajas - cajas_sur) > 0 Order By fechacad";
                }

                SqlDataAdapter da = new SqlDataAdapter(cadena, thisConnection);
                DataSet ds = new DataSet();
                da.Fill(ds, "xlotes");
                DataTable xlote = ds.Tables["xlotes"];


                //Recorrido de cada uno de los folios y la validacion correspondiente hacia lo que tengo capturado************************

                foreach (DataRow row in xlote.Rows)
                {

                    string Cadena = "Select Count(fecha) AS Total From Tb_Det_Etiqueta_Presplit " +
                                    "Where Eti_Recibo = '" + row["recibo"].ToString().Trim() + "' AND Eti_Producto = '" + captu.Codigo.Trim() + "' AND Eti_TarIni = '" + Convert.ToInt32(row["tarima"].ToString().Trim()) + "' AND Estatus = 'A'";

                    thisConnection.Open();
                    SqlCommand cmd = new SqlCommand(Cadena, thisConnection);
                    int TotalLeido = Convert.ToInt32(cmd.ExecuteScalar());
                    thisConnection.Close();

                    row["disponible"] = Convert.ToInt32(row["disponible"].ToString().Trim()) - TotalLeido;

                    if (Convert.ToInt32(row["disponible"]) > 0)
                    {
                        if (totalpro > 0)
                        {

                            string diacad = traediafecadrec(row["fecha_cad"].ToString().Trim(), tipo);
                            string mescad = traemesfecadrec(row["fecha_cad"].ToString().Trim(), tipo);
                            if ((diacadant == diacad && mescadant == mescad) || (diacadant == "" && mescadant == ""))
                            {
                                todobien = "OK";
                            }
                            else
                            {
                                if (totaldisponibles == 0)
                                {
                                    todobien = "OK";
                                }
                                else
                                {
                                    todobien = "NO";
                                }
                            }

                            if (todobien == "OK")
                            {
                                var prodcap = db.Query<xLote>("Select COUNT(ID) AS Cajas FROM xLote Where Codigo = '" + captu.Codigo.Trim() + "' AND Folio = '" + row["recibo"].ToString().Trim() + "'  AND CAST(Tarima as int) = '" + Convert.ToInt32(row["tarima"].ToString().Trim()) + "'");

                                foreach (var capturado in prodcap)
                                {
                                    usadas = Convert.ToInt32(capturado.Cajas.ToString().Trim());
                                    totaldis = Convert.ToInt32(row["disponible"].ToString().Trim()) - Convert.ToInt32(capturado.Cajas.ToString().Trim());
                                    simulador = simulador + totaldis;
                                    totalpro = totalpro - usadas;
                                    totaldisponibles = totaldisponibles + totaldis;
                                }

                                if (totaldis > 0)
                                {
                                    XLoteSug sugeridos = new XLoteSug { recibosug = row["recibo"].ToString().Trim(), fecrecsug = diacad + "/" + mescad, cveprod = prod, Tarima = row["tarima"].ToString().Trim(), Cajasdis = totaldis, Cajasusadas = usadas, foliomens = "" };
                                    db.Insert(sugeridos);
                                }
                                else
                                {
                                    XLoteSug sugeridos = new XLoteSug { recibosug = row["recibo"].ToString().Trim(), fecrecsug = diacad + "/" + mescad, cveprod = prod, Tarima = row["tarima"].ToString().Trim(), Cajasdis = 0, Cajasusadas = usadas, foliomens = "" };
                                    db.Insert(sugeridos);
                                }

                                diacadant = diacad;
                                mescadant = mescad;

                            }
                            else
                            {
                                var loteSug = db.Query<XLoteSug>("Select  *  FROM XLoteSug Where cveprod = '" + captu.Codigo.Trim() + "' AND cajasdis != 0 LIMIT 1");

                                foreach (var capturado in loteSug)
                                {
                                    string recibosug = capturado.recibosug;
                                    string fecrecsug = capturado.fecrecsug;
                                    string cveprod = capturado.cveprod;
                                    string tarima = capturado.Tarima;
                                    int cajasdis = capturado.Cajasdis;
                                    int cajasusadas = capturado.Cajasusadas;
                                    Mensajes mensa = new Mensajes { titulo = "Existe un folio anterior disponible", mensaje = "El recibo " + "\n\r" + capturado.recibosug.ToString().Trim() + " De la tarima  " + capturado.Tarima.Trim() + " Tiene  " + capturado.Cajasdis + " cajas disponibles del producto: " + captu.nombre.Trim() + " Con Fecha de Caducidad del" + capturado.fecrecsug };
                                    db.Insert(mensa);
                                    ValiFechacad = "N";
                                    db.Query<XLoteSug>("DELETE  FROM XLoteSug Where cveprod = '" + captu.Codigo.Trim() + "' AND cajasdis != 0 AND Cajasusadas <= 0");
                                    XLoteSug sugeridosact = new XLoteSug { recibosug = recibosug.ToString().Trim(), fecrecsug = fecrecsug, cveprod = cveprod, Tarima = tarima.ToString().Trim(), Cajasdis = cajasdis, Cajasusadas = cajasusadas, foliomens = "S" };
                                    db.Insert(sugeridosact);
                                    totalpro = 0;
                                }

                            }
                        }
                    }

                }


            }


            return Valor;


        }


        private string traemes(int mes)
        {
            string nom = "";
            switch (mes)
            {
                case 1: { nom = "ENE"; break; }
                case 2: { nom = "FEB"; break; }
                case 3: { nom = "MAR"; break; }
                case 4: { nom = "ABR"; break; }
                case 5: { nom = "MAY"; break; }
                case 6: { nom = "JUN"; break; }
                case 7: { nom = "JUL"; break; }
                case 8: { nom = "AGO"; break; }
                case 9: { nom = "SEP"; break; }
                case 10: { nom = "OCT"; break; }
                case 11: { nom = "NOV"; break; }
                case 12: { nom = "DIC"; break; }
            }
            return nom;
        }

        private string validaprod()
        {
            dondegenera = "validaprod";
            string ok = "S", nom = "";
            Surtidomayor = "S";

            var productoscapturadosx = db.Table<xprod>();

            var productoscapturados = db.Table<ConPedidos>();
            foreach (var captu in productoscapturados)
            {
                if (Convert.ToInt32(captu.pedido) > Convert.ToInt32(captu.surtido))
                {

                    Mensajes mensa = new Mensajes { titulo = "Error En el Producto", mensaje = "Producto " + captu.nombre.ToString() + "  Surtido es menor al Pedido" + "\n\r" + nom + "\n\r" + " Pedidos: " + captu.pedido.ToString() + "  Surtidos: " + captu.surtido.ToString() };
                    db.Insert(mensa);

                    //ok = "N";
                }
                else if (Convert.ToInt32(captu.pedido) < Convert.ToInt32(captu.surtido))
                {
                    Mensajes mensa2 = new Mensajes { titulo = "Error En el Producto", mensaje = "Producto " + captu.nombre.ToString() + " Surtido es Mayor al Pedido " + "\n\r" + nom + "\n\r" + " Pedidos: " + captu.pedido.ToString() + "  Surtidos: " + captu.surtido.ToString() + " Debe Iniciar la captura nuevamente" };
                    db.Insert(mensa2);

                    Surtidomayor = "NR";
                }
            }


            return ok;
        }

        void fnShowCustomAlertDialog()
        {
            //Inflate layout
            View view = LayoutInflater.Inflate(Resource.Layout.frmsupervisor, null);
            AlertDialog builder = new AlertDialog.Builder(this).Create();
            builder.SetView(view);
            builder.SetCanceledOnTouchOutside(false);
            EditText password = view.FindViewById<EditText>(Resource.Id.txtPassword);
            Button buttonaceptar = view.FindViewById<Button>(Resource.Id.btnLoginLL);
            Button button = view.FindViewById<Button>(Resource.Id.btnClearLL);
            button.Click += delegate
            {
                builder.Dismiss();

            };
            buttonaceptar.Click += delegate
            {
                thisConnection.Open();
                string cadena = "Select usuario,password From tb_Autoriza_OdeP Where password = '" + password.Text.Trim() + "' AND clave = 'EM'";
                SqlCommand cmd = new SqlCommand(cadena, thisConnection);
                mAutoriza = Convert.ToString(cmd.ExecuteScalar());
                if (mAutoriza.Trim().Length == 0)
                {
                    Toast.MakeText(this, "PASSWORD INCORRECTO!!!", ToastLength.Short).Show();
                    thisConnection.Close();
                }
                else
                {
                    thisConnection.Close();

                    AutoPed = "S";
                    Guardar.Enabled = true;
                    builder.Dismiss();
                }

            };
            builder.Show();
        }

        void fnShowCustomAlertDialogCancel()
        {
            //Inflate layout
            View view = LayoutInflater.Inflate(Resource.Layout.frmsupervisor, null);
            AlertDialog builder = new AlertDialog.Builder(this).Create();
            builder.SetView(view);
            builder.SetCanceledOnTouchOutside(false);
            TextView titulo = view.FindViewById<TextView>(Resource.Id.titleLogin);
            EditText password = view.FindViewById<EditText>(Resource.Id.txtPassword);
            Button buttonaceptar = view.FindViewById<Button>(Resource.Id.btnLoginLL);
            Button button = view.FindViewById<Button>(Resource.Id.btnClearLL);
            button.Click += delegate
            {
                builder.Dismiss();

            };
            titulo.Text = "Autorizacion Folios Adelantados";
            buttonaceptar.Click += delegate
            {
                thisConnection.Open();
                string cadena = "Select usuario,password From tb_Autoriza_OdeP Where password = '" + password.Text.Trim() + "' AND clave = 'EM'";
                SqlCommand cmd = new SqlCommand(cadena, thisConnection);
                mAutoriza = Convert.ToString(cmd.ExecuteScalar());
                if (mAutoriza.Trim().Length == 0)
                {
                    Toast.MakeText(this, "PASSWORD INCORRECTO!!!", ToastLength.Short).Show();
                    thisConnection.Close();
                }
                else
                {
                    thisConnection.Close();
                    Guardar.Enabled = true;
                    builder.Dismiss();
                }

            };
            builder.Show();
        }

        private string repetido(string mtip, string mfol, string mcod, string mtar, string mcaj)
        {
            string Ok = "N";
            var productoscapturados = db.Query<xprod>("Select * FROM xprod Where Codigo = '" + mcod + "' AND Folio = '" + mfol + "'  AND Tarima = '" + mtar + "' AND Cajas = '" + mcaj + "' AND Tipo = '" + mtip + "'");

            foreach (var captu in productoscapturados)
            {
                Ok = "S";
                break;
            }
            return Ok;
        }

        private int traetotalpedido(string mcod)
        {
            int total = 0;
            var productoscapturados = db.Query<ConPedidos>("Select * FROM ConPedidos Where prod_clave = '" + mcod + "'");
            foreach (var captu in productoscapturados)
            {
                total = Convert.ToInt32(captu.pedido);
                break;
            }
            return total;
        }

        private int traetotal(string mcod)
        {
            int total = 0;
            var productoscapturados = db.Query<ConPedidos>("Select * FROM ConPedidos Where prod_clave = '" + mcod + "'");
            foreach (var captu in productoscapturados)
            {
                total = Convert.ToInt32(captu.surtido);
                break;
            }
            return total;
        }


        private int traerecibosvencidos(string codigo, string tipo)
        {
            int total = 0;
            var productoscapturados = db.Query<xLote>("select Folio, Codigo, Tarima, Count(Cajas) AS Cajas FROM xLote Where Codigo = '" + codigo + "' AND Tipo = '" + tipo + "' Group by Folio, Codigo, Tarima");
            foreach (var captu in productoscapturados)
            {
                int total_recibo_cap = Convert.ToInt32(captu.Cajas);
                if (tipo == "PTC")
                    cadena = "SELECT ETIQUETA AS PROD,SURTIDO,FECHA_CAD AS FECCAD, pti_estatus_sur AS estatus_sur FROM TB_DET_TRAZABILIDAD WHERE PROD_CLAVE = '" + captu.Codigo.Trim() + "' AND RECIBO = '" + captu.Folio.Trim() + "' " +
                             "AND TIPO = '" + tipo + "' AND TARIMA = '" + Convert.ToInt32(captu.Tarima).ToString() + "' ";

                else
                    cadena = "SELECT NUM_CAJAS AS PROD, CAJAS_SUR AS SURTIDO,NUM_LOTE AS FECCAD, estatus_sur FROM TB_DET_ETI_FINAL WHERE CVE_PROD = '" + captu.Codigo.Trim() + "' AND FOLIO = '" + captu.Folio.Trim() + "' " +
                        "AND TARIMA = '" + Convert.ToInt32(captu.Tarima).ToString() + "' ";

                SqlDataAdapter da = new SqlDataAdapter(cadena, thisConnection);
                DataSet ds = new DataSet();

                //MessageBox.Show(cadena); 
                da.Fill(ds, "Info");
                DataTable Info = ds.Tables["Info"];
                //MessageBox.Show(Info.Rows.Count.ToString()); 

                foreach (DataRow row in Info.Rows)
                {
                    int mP = Convert.ToInt32(row["PROD"]);
                    int mS = Convert.ToInt32(row["SURTIDO"]);

                    if (((total_recibo_cap + mS) > mP) || (row["estatus_sur"].ToString().Trim() == "S"))
                    {
                        total = total + total_recibo_cap;
                        XLoteSug sugeridos = new XLoteSug { recibosug = captu.Folio.Trim(), fecrecsug = "/", cveprod = captu.Codigo.Trim(), Tarima = Convert.ToInt32(captu.Tarima).ToString(), Cajasdis = 0, Cajasusadas = total_recibo_cap };
                        db.Insert(sugeridos);
                    }
                }
            }
            return total;
        }

        void insertarinfo()
        {
            dondegenera = "inserinfo";
            thisConnection.Open();
            var pedidoscapturados = db.Table<Pedidos>();
            foreach (var captu in pedidoscapturados)
            {
                string cadena = "insert into Tb_Split_Pedidos(folio, prod_clave, nombre, pedido, surtido, fecha, veces) " +
                               "Values('" + captu.folio + "','" + captu.prod_clave + "','" + captu.nombre + "','" + captu.pedido + "','" + captu.surtido + "','" + System.DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "', '" + veces + "' )";
                SqlCommand cmd = new SqlCommand(cadena, thisConnection);
                cmd.ExecuteNonQuery();
            }

            var concentradocapturados = db.Table<ConPedidos>();
            foreach (var captu in concentradocapturados)
            {
                string cadena = "insert into Tb_Split_ConPedidos(prod_clave, nombre, pedido, surtido, fecha, veces) " +
                               "Values('" + captu.prod_clave + "','" + captu.nombre + "','" + captu.pedido + "','" + captu.surtido + "','" + System.DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "', '" + veces + "')";
                SqlCommand cmd = new SqlCommand(cadena, thisConnection);
                cmd.ExecuteNonQuery();
            }

            var relXlote = db.Table<xLote>();
            foreach (var captu in relXlote)
            {
                string cadena = "insert into Tb_split_xLote(Tipo, Pedido, Folio, Codigo, Tarima, Cajas, nombre, diacad, mescad, fecha, veces) " +
                               "Values('" + captu.Tipo + "','" + captu.Pedido + "','" + captu.Folio + "','" + captu.Codigo + "', '" + captu.Tarima + "', '" + captu.Cajas + "','" + captu.nombre + "','" + captu.diacad + "','" + captu.mescad + "', '" + System.DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "', '" + veces + "')";
                SqlCommand cmd = new SqlCommand(cadena, thisConnection);
                cmd.ExecuteNonQuery();
            }

            var relXprod = db.Table<xprod>();
            foreach (var captu in relXprod)
            {
                string cadena = "insert into Tb_split_xprod(Tipo, Folio, Codigo, Tarima, Cajas, fecha, veces) " +
                               "Values('" + captu.Tipo + "','" + captu.Folio + "','" + captu.Codigo + "', '" + captu.Tarima + "', '" + captu.Cajas + "','" + System.DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "', '" + veces + "')";
                SqlCommand cmd = new SqlCommand(cadena, thisConnection);
                cmd.ExecuteNonQuery();
            }

            thisConnection.Close();

            veces++;
        }

        private string NoSplit(string mped)
        {
            string Cadena = "Select MAX(tarima) from tb_det_split where emb_folio = '" + mped + "'";
            SqlCommand cmd = new SqlCommand(Cadena, thisConnection);
            string cad = Convert.ToString(cmd.ExecuteScalar());
            cad = (cad.Trim().Length == 0) ? "1" : (Convert.ToInt32(cad) + 1).ToString();
            return cad;
        }


        private void ConsPedSur(string mped)
        {
            thisConnection.Open();
            string cadena = "Select prod_clave as Codigo, nom_prod as Nombre, cant_ped as Pedido, 0 as Surtido from tb_ped_embarque Where emb_folio = '" + mped.Trim() + "' Order by nom_prod";
            SqlDataAdapter da = new SqlDataAdapter(cadena, thisConnection);
            DataSet ds = new DataSet();
            da.Fill(ds, "ConsPed");
            var ConsPed = ds.Tables["ConsPed"];
            cadena = "Select prod_clave, sum(cajas) as cajas from tb_det_split Where emb_folio = '" + mped.Trim() + "'" +
                     "And estatus != '' Group By prod_clave Order by prod_clave";
            da = new SqlDataAdapter(cadena, thisConnection);
            ds = new DataSet();
            da.Fill(ds, "PedSur");
            var PedSur = ds.Tables["PedSur"];
            int Cp = 0, Cs = 0, sur = 0;
            thisConnection.Close();
            foreach (DataRow Row in ConsPed.Rows)
            {
                sur = 0;
                foreach (DataRow row in PedSur.Select("prod_clave = '" + Row["Codigo"].ToString() + "'"))
                    sur = Convert.ToInt32(row["Cajas"]);

                db.Query<ConPedidos>("UPDATE [ConPedidos] SET surtido = '" + sur + "' WHERE prod_clave = '" + Row["Codigo"].ToString().Trim() + "'");

                Cp += Convert.ToInt32(Row["pedido"]);
                Cs += sur;
            }



        }

        private void ConsPedSurdos(string mped)
        {
            db.Query<ConPedidos>("UPDATE [ConPedidos] SET surtido = 0");
            thisConnection.Open();


            string Cadena = "Select SUM(a.pdn_num_unidades) AS Pedidos From tb_det_pedidos A, tb_Cat_producto B " +
                                "where a.pdn_folio = '" + mped.Trim() + "' and a.prod_clave = b.prod_clave";
            SqlCommand cmd = new SqlCommand(Cadena, thisConnection);
            int cantped = Convert.ToInt32(cmd.ExecuteScalar());




            string cadena = "Select * From tb_det_pedidos A, tb_Cat_producto B where a.pdn_folio = '" + mped.Trim() + "' and a.prod_clave = b.prod_clave";
            SqlDataAdapter da = new SqlDataAdapter(cadena, thisConnection);
            DataSet ds = new DataSet();
            da.Fill(ds, "ConsPed");
            var ConsPed = ds.Tables["ConsPed"];

            if (mped.Length > 0)
            {
                if (Convert.ToInt32(mped) < 300000)
                {
                    mped = "0" + mped;
                }
            }


            cadena = "Select prod_clave, sum(cajas) as cajas from tb_det_split Where emb_folio = '" + mped.Trim() + "'" +
                     " AND estatus != 'C' Group By prod_clave Order by prod_clave";
            da = new SqlDataAdapter(cadena, thisConnection);
            ds = new DataSet();
            da.Fill(ds, "PedSur");
            var PedSur = ds.Tables["PedSur"];
            int Cp = 0, Cs = 0, sur = 0;
            thisConnection.Close();
            foreach (DataRow Row in ConsPed.Rows)
            {
                sur = 0;
                foreach (DataRow row in PedSur.Select("prod_clave = '" + Row["prod_clave"].ToString() + "'"))
                    sur = Convert.ToInt32(row["Cajas"]);
                db.Query<Pedidos>("UPDATE [Pedidos] SET surtido = '" + sur + "' WHERE prod_clave = '" + Row["prod_clave"].ToString().Trim() + "'");
                db.Query<ConPedidos>("UPDATE [ConPedidos] SET surtido = '" + sur + "' WHERE prod_clave = '" + Row["prod_clave"].ToString().Trim() + "'");

                Cp += Convert.ToInt32(Row["pdn_num_unidades"]);
                Cs += sur;
            }


            //RECORRIDO SI HAY PRODUCTO CAPTURADO
            var quex = db.Table<xprod>();
            foreach (var captu in quex)
            {
                db.Query<Pedidos>("UPDATE [Pedidos] SET surtido = surtido + " + 1 + " WHERE prod_clave = '" + captu.Codigo.ToString().Trim() + "'");
                db.Query<ConPedidos>("UPDATE [ConPedidos] SET surtido = surtido + " + 1 + " WHERE prod_clave = '" + captu.Codigo.ToString().Trim() + "'");
            }

            //***********************************

        }

        private void AgregaDetaEtiAdelantado()
        {
            var recibosatrasusa = db.Query<XLoteSug>("Select * FROM [XLoteSug] Where Cajasusadas != 0 Order By recibosug, cveprod ASC");
            foreach (var recibos in recibosatrasusa)
            {
                if (recibos.Cajasusadas > 0)
                {
                    db.Query<xLote>("UPDATE [xLoteFinal] SET cajas = CAST(Cajas as int) - " + recibos.Cajasusadas + " WHERE Folio = '" + recibos.recibosug.ToString().Trim() + "' AND Codigo = '" + recibos.cveprod.ToString().Trim() + "' AND CAST(Tarima as int) = " + Convert.ToInt32(recibos.Tarima.ToString()));
                }
            }

            var productoscap = db.Query<xLoteFinal>("Select DISTINCT(Codigo) FROM [xLoteFinal] Order By Codigo ASC");
            foreach (var productos in productoscap)
            {
                var recibosatras = db.Query<XLoteSug>("Select  *  FROM XLoteSug Where cveprod = '" + productos.Codigo.Trim() + "' AND cajasdis != 0 AND foliomens = 'S' ORDER BY recibosug, Tarima LIMIT 1");
                foreach (var recibos in recibosatras)
                {

                    var folio = recibos.recibosug.Trim();
                    var producto = recibos.cveprod.Trim();
                    var tarima = recibos.Tarima.Trim();
                    var feccad = recibos.fecrecsug.Trim();

                    var recibosCapturados = db.Query<xLote>("Select * FROM [xLoteFinal] Where Codigo = '" + recibos.cveprod.ToString().Trim() + "' Order By Pedido, codigo ASC");
                    foreach (var reccapturado in recibosCapturados)
                    {
                        string fechacaducidadcapturado = reccapturado.diacad.Trim() + "/" + reccapturado.mescad.Trim();
                        if (reccapturado.Folio.Trim() != folio && reccapturado.Tarima.Trim() != tarima)
                        {
                            if (fechacaducidadcapturado != feccad)
                            {
                                string cadena = "insert into tb_det_folio_adelantado (responsable, fecha, emb_folio, recibo_cap, fecreccap, recibo_sug, fecrecsug, prod_clave, producto, cantidad, autorizo, tarimacap, tarimasug, imei, motivo) " +
                               "Values('" + responsable + "','" + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt") + "','" + reccapturado.Pedido + "', '" + reccapturado.Folio + "', '" + reccapturado.diacad + "/" + reccapturado.mescad + "','" + recibos.recibosug + "', '" + recibos.fecrecsug + "', '" + recibos.cveprod + "', '" + reccapturado.nombre + "', '" + reccapturado.Cajas + "', '" + mAutoriza.Trim() + "', '" + reccapturado.Tarima.Trim() + "', '" + recibos.Tarima.Trim() + "', '" + imei + "', '" + motfolade.Trim() + "')";
                                SqlCommand cmd = new SqlCommand(cadena, thisConnection);
                                cmd.ExecuteNonQuery();
                                break;
                            }
                        }
                    }


                    /*var recibosCapturados = db.Query<xLote>("Select * FROM [xLoteFinal] Where Codigo = '" + recibos.cveprod.ToString().Trim() + "' Order By Pedido, codigo ASC");
                    foreach (var reccapturado in recibosCapturados)
                    {
                        if (Convert.ToInt32(reccapturado.Cajas) > 0)
                        {
                            string cadena = "insert into tb_det_folio_adelantado (responsable, fecha, emb_folio, recibo_cap, fecreccap, recibo_sug, fecrecsug, prod_clave, producto, cantidad, autorizo, tarimacap, tarimasug) " +
                                "Values('" + responsable + "','" + DateTime.Now.ToString("dd/MM/yyyy") + "','" + reccapturado.Pedido + "', '" + reccapturado.Folio + "', '" + reccapturado.diacad + "/" + reccapturado.mescad + "','" + recibos.recibosug + "', '" + recibos.fecrecsug + "', '" + recibos.cveprod + "', '" + reccapturado.nombre + "', '" + reccapturado.Cajas + "', '" + mAutoriza.Trim() + "', '" + reccapturado.Tarima.Trim() + "', '" + recibos.Tarima.Trim() + "')";
                            SqlCommand cmd = new SqlCommand(cadena, thisConnection);
                            cmd.ExecuteNonQuery();
                        }
                    }*/
                }
            }
        }
        private string ValidaCajaPreesplit(string cadena)
        {
            string Cadena = "Select fecha_cap From Tb_Det_Etiqueta_Presplit " +
                          "Where Eti_Lectura = '" + cadena + "' AND Estatus = 'A'";
            SqlCommand cmd = new SqlCommand(Cadena, thisConnection);
            string Valor = Convert.ToString(cmd.ExecuteScalar());
            return Valor;
        }

        void ITextWatcher.AfterTextChanged(IEditable s)
        {
            //throw new NotImplementedException();
        }

        void ITextWatcher.BeforeTextChanged(ICharSequence s, int start, int count, int after)
        {
            //throw new NotImplementedException();
        }

        void ITextWatcher.OnTextChanged(ICharSequence s, int start, int before, int count)
        {
            if (mconcen == "2")
            {
                Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                alertDialog.SetTitle(Html.FromHtml("<font color='#DF0101' size = 10>Modo concentrado Activado</font>"));
                alertDialog.SetIcon(Resource.Drawable.no);
                alertDialog.SetMessage(Html.FromHtml("<font color='#FFFFFF' size = 10>Esta consultando el concentrado no se puede capturar codigo. </font>"));
                alertDialog.SetCancelable(false);
                alertDialog.SetNeutralButton("Ok", delegate
                {
                    alertDialog.Dispose();
                    foliocaptura.SetSelection(0, foliocaptura.Text.Length);
                    foliocaptura.RequestFocus();
                    valorfinal = foliocaptura.Text;
                });
                alertDialog.Show();
                return;


            } // esta consultando el concentrado no se puede capturar codigo

            string folio = foliocaptura.Text;
            Guardar.Enabled = false;

            if (folio != valorfinal && folio != "")
            {
                //var TxtCod = (EditText)sender;
                if (Eliminar_caja.Checked == true)
                {
                    eliminaretiquetablanca();

                }
                else
                {

                    if (etiblanca.Checked == true)
                    {
                        etiquetablanca();
                    }
                    else
                    {
                        //etiquetaverde();
                        etiquetasverde();

                    }

                }

            }
            else
            {
                foliocaptura.SetSelection(0, foliocaptura.Text.Length);
                foliocaptura.RequestFocus();
                valorfinal = foliocaptura.Text;
            }
        }


        public void eliminaretiquetablanca()
        {
            int pos = foliocaptura.Text.Trim().IndexOf("=");
            //MessageBox.Show(pos.ToString()); 
            if (pos == -1)
            {
                foliocaptura.SetSelection(0, foliocaptura.Text.Length);
                foliocaptura.RequestFocus();
                valorfinal = foliocaptura.Text;
                return;
            }
            foliocaptura.Text = foliocaptura.Text.Substring(pos + 1, foliocaptura.Text.Length - (pos + 1)).Trim();
            foliocaptura.Text = foliocaptura.Text.Replace("=", "");
            int tam = foliocaptura.Text.Length;
            string mcaj = "", mtar = "", mcod = "", mfol = "", mtip = "", Ent = "N";
            if (tam > 20) //Etiqueta de Campo que no es Aguilares y Proceso Planta
            {
                Int32 ValorFolio = Convert.ToInt32(foliocaptura.Text.Substring(0, 6));
                if (ValorFolio > FolioCampo) // Etiqueta de Campo
                    Ent = "S";
            }
            if (Ent == "N") // Valido si el PTP Planta o PTC de Aguilares
            {
                mcaj = foliocaptura.Text.Substring(tam - 3, 3);
                mtar = foliocaptura.Text.Substring(tam - 6, 3);
                int tam2 = tam - 6;
                mtip = "PTP";
                if (tam2 == 15) // Etiqueta de Aguilares	
                {
                    mfol = foliocaptura.Text.Substring(0, 5);
                    mcod = foliocaptura.Text.Substring(5, tam - 11);
                    mtip = "PTC";
                }
                else if (tam2 <= 14) // Etiqueta de Aguilares	
                {
                    mfol = foliocaptura.Text.Substring(0, 4);
                    mcod = foliocaptura.Text.Substring(4, tam - 10);
                    mtip = "PTC";
                }
                else
                {
                    mfol = foliocaptura.Text.Substring(0, 6);
                    mcod = foliocaptura.Text.Substring(6, tam - 12);
                }
                var nombreproducto = traenom(mcod); //Valido si existe el producto, si no quiere decir que es recibo de 6 digitos pero de produccion


                if (nombreproducto == "")
                {
                    mfol = foliocaptura.Text.Substring(0, 6);
                    mcod = foliocaptura.Text.Substring(6, tam - 12);
                    mtip = "PTP";
                }

                nombreproducto = traenom(mcod); //Valido si existe el producto, si no quiere decir que es recibo de 6 digitos

                if (nombreproducto == "")
                {
                    mcaj = foliocaptura.Text.Substring(tam - 2, 2);
                    mtar = foliocaptura.Text.Substring(tam - 4, 2);
                    mfol = foliocaptura.Text.Substring(0, 6);
                    mcod = foliocaptura.Text.Substring(6, tam - 10);
                    mtip = "PTC";
                }

                nombreproducto = traenom(mcod);

                if (nombreproducto == "")
                {
                    mcaj = foliocaptura.Text.Substring(tam - 3, 3);
                    mtar = foliocaptura.Text.Substring(tam - 6, 3);
                    mfol = foliocaptura.Text.Substring(0, 5);
                    mcod = foliocaptura.Text.Substring(5, tam - 11);
                    mtip = "PTC";
                }
            }
            else //Etiqueta de Campo que no es Aguilares
            {
                mcaj = foliocaptura.Text.Substring(tam - 3, 3);
                mtar = foliocaptura.Text.Substring(tam - 7, 2);
                mfol = foliocaptura.Text.Substring(0, 6);
                mcod = foliocaptura.Text.Substring(6, tam - 13);
                mtip = "PTC";
            }

            mcaj = mcaj.Trim();
            mtar = mtar.Trim();
            mfol = mfol.Trim();
            mcod = mcod.Trim();

            string cad = mtip + " | " + mfol + " | " + mcod + " | " + mtar + " | " + mcaj;
            if (repetido(mtip, mfol, mcod, mtar, mcaj) == "S")
            {
                xprod Pedidoscapturados = new xprod { Tipo = mtip, Folio = mfol, Codigo = mcod, Tarima = mtar, Cajas = mcaj, fecha_captura = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") };
                db.Insert(Pedidoscapturados);

                db.Query<xprod>("DELETE FROM xprod Where Codigo = '" + mcod + "' AND Folio = '" + mfol + "'  AND Tarima = '" + mtar + "' AND Cajas = '" + mcaj + "' AND Tipo = '" + mtip + "'");

                int totalx = traetotal(mcod);

                totalx = totalx - 1;

                string existeprod = "NO";
                var pedidos = db.Query<ConPedidos>("Select * FROM ConPedidos Where prod_clave = '" + mcod.ToString().Trim() + "'");

                foreach (var pedisur in pedidos)
                {
                    db.Query<ConPedidos>("UPDATE [ConPedidos] SET surtido = '" + totalx + "' WHERE prod_clave = '" + mcod.ToString() + "'");
                    existeprod = "SI";
                }

                if (totalx < 1)
                {
                    db.Query<ConPedidos>("Delete FROM ConPedidos Where prod_clave = '" + mcod.ToString().Trim() + "'");
                }


                TotCaj--;
                total.Text = TotCaj.ToString("##0");

                string nombreprod = traenom(mcod.ToString().Trim());

                foreach (var item in listItem.ToArray())
                {
                    string descrip = "Recibo: " + mfol + " Tarima: " + mtar + " Caja: " + mcaj;
                    if (item.Name == nombreprod.ToString().Trim() && item.Age == descrip)
                    {
                        listItem.Remove(item);
                    }
                }


                /*listItem.Remove(new FlimStarInfo()
                {
                    Name = traenom(mcod.ToString().Trim()),
                    Age = "Recibo: " + mfol + "Tarima: " + mtar + " Caja: " + mcaj,
                    ImageID = Resource.Drawable.producto
                });*/
            }
            foliocaptura.SetSelection(0, foliocaptura.Text.Length);
            foliocaptura.RequestFocus();
            valorfinal = foliocaptura.Text;



            List<FlimStarInfo> lstFlimStar = listItem;
            var gvObject = FindViewById<GridView>(Resource.Id.gvCtr2com);
            gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);
        }

        public void etiquetaverde()
        {
            int tam = foliocaptura.Text.Length;
            string mcaj = "", mtar = "", mcod = "", mfol = "", mtip = "", Ent = "N";
            if (foliocaptura.Text.Trim().Contains(" ") == true)
            {
                if (tam < 18)
                {
                    mtar = foliocaptura.Text.Substring(tam - 3, 3);
                    mfol = foliocaptura.Text.Substring(0, 4);
                    mcod = foliocaptura.Text.Replace(mfol, "");
                    mcod = mcod.Replace(mtar, "");
                    mtar = mtar.Replace(" ", "0");
                    mtip = "PTC";
                }
                else
                {
                    mtar = foliocaptura.Text.Substring(tam - 3, 3);
                    mfol = foliocaptura.Text.Substring(0, 6);
                    mcod = foliocaptura.Text.Replace(mfol, "");
                    mcod = mcod.Replace(mtar, "");
                    mtar = mtar.Replace(" ", "0");
                    mtip = "PTP";
                    if (mfol.Substring(0, 1) == "0")
                    {
                        mtip = "PTC";
                        mfol = Convert.ToInt32(mfol).ToString();
                    }
                }
            }
            else
            {
                string mtari = foliocaptura.Text.Substring(tam - 4, 4);
                mtar = foliocaptura.Text.Substring(tam - 4, 2);
                mfol = foliocaptura.Text.Substring(0, 6);
                mcod = foliocaptura.Text.Replace(mfol, "");
                mcod = mcod.Replace(mtari, "");
                mtip = "PTC";

            }


            DataTable Foliosleidos = new DataTable();
            string CadenaFolios = "Select Eti_Lectura, fecha_cap From tb_Det_Etiqueta " +
                           "WHERE (Eti_Producto = '" + mcod + "') AND (Eti_Recibo = '" + mfol + "') AND (Eti_TarIni = '" + mtar + "')";
            thisConnection.Open();
            SqlDataAdapter da = new SqlDataAdapter(CadenaFolios, thisConnection);
            DataSet ds = new DataSet();
            da.Fill(ds, "Foliosleidos");
            Foliosleidos = ds.Tables["Foliosleidos"];
            thisConnection.Close();

            DataTable FoliosleidosPresplit = new DataTable();
            string CadenaFoliospreesplit = "Select Eti_Lectura, fecha_cap From tb_Det_Etiqueta " +
                           "WHERE (Eti_Producto = '" + mcod + "') AND (Eti_Recibo = '" + mfol + "') AND (Eti_TarIni = '" + mtar + "')";
            thisConnection.Open();
            SqlDataAdapter dapre = new SqlDataAdapter(CadenaFoliospreesplit, thisConnection);
            DataSet dspre = new DataSet();
            dapre.Fill(dspre, "FoliosleidosPresplit");
            FoliosleidosPresplit = dspre.Tables["FoliosleidosPresplit"];
            thisConnection.Close();

            string cadenatarimacompleta = "";

            if (mtip == "PTP")
            {
                cadenatarimacompleta = "SELECT (num_cajas - CAJAS_SUR) AS DISPONIBLE FROM TB_DET_ETI_FINAL WHERE CVE_PROD = '" + mcod.Trim() + "' AND FOLIO = '" + mfol.Trim() + "' " +
            "AND TARIMA = '" + Convert.ToInt32(mtar.Trim()).ToString() + "' ";

            }
            else
            {
                cadenatarimacompleta = "SELECT (etiqueta - surtido) AS DISPONIBLE FROM TB_DET_TRAZABILIDAD WHERE PROD_CLAVE = '" + mcod.Trim() + "' AND RECIBO = '" + mfol.Trim() + "' " +
                 "AND TIPO = '" + mtip + "' AND TARIMA = '" + Convert.ToInt32(mtar.Trim()).ToString() + "' ";
            }

            thisConnection.Open();
            SqlCommand cmd = new SqlCommand(cadenatarimacompleta, thisConnection);
            int disponible = Convert.ToInt32(cmd.ExecuteScalar());

            thisConnection.Close();



            if (disponible > 0)
            {
                int total_caja_verde = 0;
                disponible++;
                int n = 1;
                int cajaactual = 1;
                while (n < disponible)
                {
                    if (cajaactual.ToString().Length == 1)
                    {
                        mcaj = "00" + cajaactual.ToString();
                    }
                    else
                    {
                        mcaj = "0" + cajaactual.ToString();
                    }

                    mtip = mtip.Trim();
                    mfol = mfol.Trim();
                    mcod = mcod.Trim();
                    mtar = mtar.Trim();
                    mcaj = mcaj.Trim();

                    string lectura = mtip + mfol + mcod + mtar + mcaj;
                    thisConnection.Open();
                    string fechacap = ValidaCajaEtiVerde(lectura, Foliosleidos).Trim();
                    string fechacappre = ValidaCajaPreesplitVerde(lectura, FoliosleidosPresplit).Trim();
                    thisConnection.Close();
                    if (fechacap.Length > 0)
                    {
                        cajaactual++;
                    }
                    else if (fechacappre.Length > 0)
                    {
                        cajaactual++;
                    }
                    else
                    {
                        string cad = mtip + " | " + mfol + " | " + mcod + " | " + mtar + " | " + mcaj;
                        if (repetido(mtip, mfol, mcod, mtar, mcaj) != "S")
                        {
                            xprod Pedidoscapturados = new xprod { Tipo = mtip, Folio = mfol, Codigo = mcod, Tarima = mtar, Cajas = mcaj, fecha_captura = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") };
                            db.Insert(Pedidoscapturados);

                            int totalx = traetotal(mcod);

                            totalx = totalx + 1;


                            var pedidos = db.Table<ConPedidos>();

                            string existeprod = "NO";
                            foreach (var pedisur in pedidos)
                            {
                                if (pedisur.prod_clave.ToString().Trim() == mcod.ToString().Trim())
                                {
                                    existeprod = "SI";
                                }
                            }


                            if (existeprod == "SI")
                            {
                                db.Query<ConPedidos>("UPDATE [ConPedidos] SET surtido = '" + totalx + "' WHERE prod_clave = '" + mcod.ToString() + "'");
                            }
                            else
                            {
                                ConPedidos ConsecutivosPedidos = new ConPedidos { prod_clave = mcod.ToString(), nombre = traenom(mcod.ToString().Trim()), pedido = 0, surtido = Convert.ToInt16(totalx) };
                                db.Insert(ConsecutivosPedidos);
                            }

                            cajaactual++;
                            total_caja_verde++;
                            TotCaj++;
                            total.Text = TotCaj.ToString("##0");
                            listItem.Add(new FlimStarInfo()
                            {
                                Name = traenom(mcod.ToString().Trim()),
                                Age = "Recibo: " + mfol + "Tarima: " + mtar + " Caja: " + mcaj,
                                ImageID = Resource.Drawable.producto
                            });

                        }

                        n++;
                    }

                }
                foliocaptura.SetSelection(0, foliocaptura.Text.Length);
                foliocaptura.RequestFocus();
                valorfinal = foliocaptura.Text;
                //iMPRESION DE MENSAJE QUE INDICARA CUANTO DE CADA TARIMA SE LOGRO CARGAR Y SIMULAR
                Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                alertDialog.SetTitle(Html.FromHtml("<font color='#55F721' size = 10>LECTURA POR TARIMA</font>"));
                alertDialog.SetIcon(Resource.Drawable.Info);
                alertDialog.SetMessage(Html.FromHtml("<font color='#9FFA7A' size = 10>Se han Capturado " + total_caja_verde + " Cajas,  Del Folio " + mfol + " De la tarima " + mtar + " Del Producto " + traenom(mcod.ToString().Trim()) + "</font>"));
                alertDialog.SetNeutralButton("Ok", delegate
                {
                    alertDialog.Dispose();
                });
                alertDialog.Show();

            }
            else
            {
                Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                alertDialog.SetTitle(Html.FromHtml("<font color='#55F721' size = 10>EXISTENCIA NO DISPONIBLE</font>"));
                alertDialog.SetIcon(Resource.Drawable.Info);
                alertDialog.SetMessage(Html.FromHtml("<font color='#9FFA7A' size = 10>La Tarima Actual No Cuenta con Existencia Disponible, Favor de Depurar los folios correspondientes y volver a leer</font>"));
                alertDialog.SetNeutralButton("Ok", delegate
                {
                    alertDialog.Dispose();
                });
                alertDialog.Show();
                foliocaptura.SetSelection(0, foliocaptura.Text.Length);
                foliocaptura.RequestFocus();
                valorfinal = foliocaptura.Text;
            }

            List<FlimStarInfo> lstFlimStar = listItem;
            var gvObject = FindViewById<GridView>(Resource.Id.gvCtr2com);
            gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);
        }

        public void etiquetasverde()
        {
            int tam = foliocaptura.Text.Length;
            string Vpti_clave = "", Bpti_clave = "";
            string mcaj = "", mtar = "", mcod = "", mfol = "", mtip = "", Ent = "N", mEtiqueta = "0";
            string Bcaj = "", Btar = "", Bcod = "", Bfol = "", Btip = "", BEnt = "N";
            string id_pallet = "";

            if (foliocaptura.Text.Trim().Length == 12)
            {
                string pti_famous = foliocaptura.Text.Trim();
                if (foliocaptura.Text.StartsWith("0"))
                {
                    pti_famous = foliocaptura.Text.TrimStart('0');
                }

                if (thisConnection.State == ConnectionState.Closed) { thisConnection.Open(); }
                string querySSCC = "select*from tb_det_trazabilidad where pti_famous='" + pti_famous + "'";
                SqlCommand sqlCommand = new SqlCommand(querySSCC);
                sqlCommand.Connection = thisConnection;
                SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();
                while (sqlDataReader.Read())
                {
                    Vpti_clave = sqlDataReader["pti_clave"].ToString().Trim();
                    mfol = sqlDataReader["recibo"].ToString().Trim();
                    mtar = sqlDataReader["tarima"].ToString().Trim();
                    mcod = sqlDataReader["prod_clave"].ToString().Trim();
                    mtip = sqlDataReader["tipo"].ToString().Trim();
                    mEtiqueta = sqlDataReader["etiqueta"].ToString().Trim();
                }
                if (thisConnection.State == ConnectionState.Open) { thisConnection.Close(); }
            }
            else if (foliocaptura.Text.Contains(SerialShippingContainerCode) == true)
            {
                Match match = Regex.Match(foliocaptura.Text, patron);
                id_pallet = match.Groups[1].Value;

                if (thisConnection.State == ConnectionState.Closed) { thisConnection.Open(); }
                string querySSCC = "select*from tb_det_trazabilidad where id_Pallet='" + id_pallet + "'";
                SqlCommand sqlCommand = new SqlCommand(querySSCC);
                sqlCommand.Connection = thisConnection;
                SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();
                while (sqlDataReader.Read())
                {
                    Vpti_clave = sqlDataReader["pti_clave"].ToString().Trim();
                    mfol = sqlDataReader["recibo"].ToString().Trim();
                    mtar = sqlDataReader["tarima"].ToString().Trim();
                    mcod = sqlDataReader["prod_clave"].ToString().Trim();
                    mtip = sqlDataReader["tipo"].ToString().Trim();
                    mEtiqueta = sqlDataReader["etiqueta"].ToString().Trim();
                }
                if (thisConnection.State == ConnectionState.Open) { thisConnection.Close(); }
            }
            else if (!Regex.IsMatch(foliocaptura.Text.Trim(), @"\s"))
            {
                if (thisConnection.State == ConnectionState.Closed) { thisConnection.Open(); }
                string querySSCC = "select*from tb_det_trazabilidad where pti_clave='" + foliocaptura.Text.Trim() + "'";
                SqlCommand sqlCommand = new SqlCommand(querySSCC);
                sqlCommand.Connection = thisConnection;
                SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();
                while (sqlDataReader.Read())
                {
                    Vpti_clave = sqlDataReader["pti_clave"].ToString().Trim();
                    mfol = sqlDataReader["recibo"].ToString().Trim();
                    mtar = sqlDataReader["tarima"].ToString().Trim();
                    mcod = sqlDataReader["prod_clave"].ToString().Trim();
                    mtip = sqlDataReader["tipo"].ToString().Trim();
                    mEtiqueta = sqlDataReader["etiqueta"].ToString().Trim();
                }
                if (thisConnection.State == ConnectionState.Open) { thisConnection.Close(); }
            }
            else if (foliocaptura.Text.Trim().Contains(" ") == true)
            {
                if (tam < 18)
                {
                    mtar = foliocaptura.Text.Substring(tam - 3, 3);
                    mfol = foliocaptura.Text.Substring(0, 5);
                    mcod = foliocaptura.Text.Replace(mfol, "");
                    mcod = mcod.Replace(mtar, "");
                    mtar = mtar.Replace(" ", "0");
                    mtip = "PTC";
                }
                else
                {
                    mtar = foliocaptura.Text.Substring(tam - 3, 3);
                    mfol = foliocaptura.Text.Substring(0, 6);
                    mcod = foliocaptura.Text.Replace(mfol, "");
                    mcod = mcod.Replace(mtar, "");
                    //mtar = mtar.Replace(" ", "0");
                    mtip = "PTP";
                    if (mfol.Substring(0, 1) == "0")
                    {
                        mtip = "PTC";
                        mfol = Convert.ToInt32(mfol).ToString();
                    }
                }
            }
            else
            {
                for (int i = 0; i < CatProd.Rows.Count; i++)
                {
                    string producto_clave = CatProd.Rows[i]["Prod_Clave"].ToString().Trim();
                    bool esta = foliocaptura.Text.Contains(producto_clave);

                    if (esta)
                    {
                        mcod = producto_clave;
                        break;
                    }
                }

                int posprod = foliocaptura.Text.Trim().IndexOf(mcod);
                mfol = foliocaptura.Text.Substring(0, posprod).Trim();
                string restocaptura = foliocaptura.Text.Replace(mfol, "").Replace(mcod, "");
                if (restocaptura.Length == 6)
                {
                    mtip = "PTC";
                    mtar = restocaptura.Substring(0, 3);
                }
                else
                {
                    mtip = "PTC";
                    //mtar = restocaptura.Substring(0, 2);
                    mtar = restocaptura.Trim();
                }

                /*string mtari = foliocaptura.Text.Substring(tam - 4, 4);
                mtar = foliocaptura.Text.Substring(tam - 4, 2);
                mfol = foliocaptura.Text.Substring(0, 6);
                mcod = foliocaptura.Text.Replace(mfol, "");
                mcod = mcod.Replace(mtari, "");
                mtip = "PTC";*/

            }
            mtip = mtip.Trim();
            mfol = mfol.Trim();
            mcod = mcod.Trim();
            mtar = mtar.Trim();

            if (mtip == "PTP")
            {
                mtar = mtar.PadLeft(3, '0');
            }
            else
            {
                mtar = mtar.PadLeft(2, '0');
            }


            #region VALIDA QUE LA ETIQUETA VERDE EXISTA
            if (mtip == "" || mfol == "" || mcod == "" || mtar == "")
            {
                Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                alertDialog.SetTitle(Html.FromHtml("<font color='#55F721' size = 10>EXISTENCIA NO DISPONIBLE</font>"));
                alertDialog.SetIcon(Resource.Drawable.nota);
                alertDialog.SetMessage(Html.FromHtml("<font color='#9FFA7A' size = 10>La Tarima Actual No Cuenta con Existencia Disponible, Favor de Depurar los folios correspondientes y volver a leer</font>"));
                alertDialog.SetNeutralButton("Ok", delegate
                {
                    alertDialog.Dispose();
                });
                alertDialog.Show();
                foliocaptura.SetSelection(0, foliocaptura.Text.Length);
                foliocaptura.RequestFocus();
                valorfinal = foliocaptura.Text;
                return;
            }
            #endregion


            string CadenaFolios = "Select Eti_Lectura, fecha_cap From tb_Det_Etiqueta " +
                                   "WHERE (Eti_Producto = '" + mcod + "') AND (Eti_Recibo = '" + mfol + "') AND (Eti_TarIni = " + Convert.ToInt32(mtar) + ") AND Estatus = 'A'";
            if (thisConnection.State == ConnectionState.Closed) { thisConnection.Open(); }
            SqlDataAdapter da = new SqlDataAdapter(CadenaFolios, thisConnection);
            DataSet ds = new DataSet();
            da.Fill(ds, "Foliosleidos");


            Foliosleidos = ds.Tables["Foliosleidos"];
            thisConnection.Close();


            string CadenaFoliospreesplit = "Select Eti_Lectura, fecha_cap From Tb_Det_Etiqueta_Presplit " +
                           "WHERE (Eti_Producto = '" + mcod + "') AND (Eti_Recibo = '" + mfol + "') AND (Eti_TarIni = " + Convert.ToInt32(mtar) + ") AND Estatus IN ('A', 'S')";
            thisConnection.Open();
            SqlDataAdapter dapre = new SqlDataAdapter(CadenaFoliospreesplit, thisConnection);
            DataSet dspre = new DataSet();
            dapre.Fill(dspre, "FoliosleidosPresplit");
            FoliosleidosPresplit = dspre.Tables["FoliosleidosPresplit"];
            thisConnection.Close();

            string cadenatarimacompleta = "";

            if (mtip == "PTP")
            {
                cadenatarimacompleta = "SELECT (num_cajas - CAJAS_SUR) AS DISPONIBLE FROM TB_DET_ETI_FINAL WHERE CVE_PROD = '" + mcod.Trim() + "' AND FOLIO = '" + mfol.Trim() + "' " +
            "AND TARIMA = '" + Convert.ToInt32(mtar.Trim()).ToString() + "' ";

            }
            else
            {
                cadenatarimacompleta = "SELECT (etiqueta - surtido) AS DISPONIBLE FROM TB_DET_TRAZABILIDAD WHERE PROD_CLAVE = '" + mcod.Trim() + "' AND RECIBO = '" + mfol.Trim() + "' " +
                 "AND TIPO = '" + mtip + "' AND TARIMA = '" + Convert.ToInt32(mtar.Trim()).ToString() + "' ";
            }

            thisConnection.Open();
            SqlCommand cmd = new SqlCommand(cadenatarimacompleta, thisConnection);
            int disponible = Convert.ToInt32(cmd.ExecuteScalar());

            thisConnection.Close();

            //string strEti_Lectura = "SELECT COUNT(*) as Eti_Lectura FROM (SELECT Eti_Lectura FROM tb_Det_Etiqueta WHERE Eti_Producto = '" + mcod.Trim() + "' AND Eti_Recibo = '" + mfol.Trim() + "' AND Eti_TarIni = '" + Convert.ToInt32(mtar.Trim()).ToString() + "' AND Estatus = 'A' UNION SELECT Eti_Lectura FROM Tb_Det_Etiqueta_Presplit WHERE Eti_Producto = '" + mcod.Trim() + "' AND Eti_Recibo = '" + mfol.Trim() + "' AND Eti_TarIni = '" + Convert.ToInt32(mtar.Trim()).ToString() + "' AND Estatus IN ('A', 'S')) AS Eti_Lectura\r\n";
            string strEti_Lectura = "SELECT sum(CAJA) AS CAJAS FROM (SELECT COUNT(Eti_Lectura) AS CAJA FROM tb_Det_Etiqueta WHERE Eti_Producto = '" + mcod.Trim() + "' AND Eti_Recibo = '" + mfol.Trim() + "' AND Eti_TarIni = " + Convert.ToInt32(mtar.Trim()) + " AND Estatus = 'A' UNION ALL SELECT COUNT(Eti_Lectura) as caja FROM Tb_Det_Etiqueta_Presplit WHERE Eti_Producto = '" + mcod.Trim() + "' AND Eti_Recibo = '" + mfol.Trim() + "' AND Eti_TarIni = " + Convert.ToInt32(mtar.Trim()) + " AND Estatus = 'S' UNION ALL SELECT SUM(cajas) as caja FROM tb_det_split WHERE prod_clave = '" + mcod.Trim() + "' AND no_lote = '" + mfol.Trim() + "' AND TARINI = '" + mtar + "' AND Estatus = 'A')   AS Eti_Lectura";
            thisConnection.Open();
            SqlCommand cmdEti_Lectura = new SqlCommand(strEti_Lectura, thisConnection);
            int totalEti_Lectura = Convert.ToInt32(cmdEti_Lectura.ExecuteScalar());

            thisConnection.Close();




            if ((disponible > 0) || (totalEti_Lectura < Convert.ToInt32(mEtiqueta)))
            {
                total_caja_verde = 0;
                disponible++;
                int n = 1;
                int cajaactual = 1;
                while (n < disponible)
                {
                    if (cajaactual.ToString().Length == 1)
                    {
                        mcaj = "00" + cajaactual.ToString();
                    }
                    else if (cajaactual.ToString().Length == 2)
                    {
                        mcaj = "0" + cajaactual.ToString();
                    }
                    else
                    {
                        mcaj = cajaactual.ToString();
                    }

                    mtip = mtip.Trim();
                    mfol = mfol.Trim();
                    mcod = mcod.Trim();
                    mtar = mtar.Trim();
                    mcaj = mcaj.Trim();



                    string lectura = mtip + mfol + mcod + mtar + mcaj;
                    //string lectura = Btip + captura;
                    thisConnection.Open();
                    string fechacap = ValidaCajaEtiVerde(lectura, Foliosleidos).Trim();
                    //string fechacappre = ValidaCajaPreesplitVerde(lectura, FoliosleidosPresplit).Trim();
                    thisConnection.Close();
                    if (fechacap.Length > 0)
                    {
                        cajaactual++;
                    }
                    /*else if (fechacappre.Length > 0)
                    {
                        cajaactual++;
                    }*/
                    else
                    {
                        string cad = mtip + " | " + mfol + " | " + mcod + " | " + mtar + " | " + mcaj;
                        if (repetido(mtip, mfol, mcod, mtar, mcaj) != "S" || disponible > 0)
                        {
                            string lectura2 = mtip + mfol + mcod + mtar + mcaj;
                            lectura2 = lectura2.Trim();

                            try
                            {
                                xprod Pedidoscapturados = new xprod { Tipo = mtip, Folio = mfol, Codigo = mcod, Tarima = mtar, Cajas = mcaj, fecha_captura = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"), tipo_captura = "V", Lecturabd = lectura2 };
                                db.Insert(Pedidoscapturados);

                                int totalx = traetotal(mcod);

                                totalx = totalx + 1;


                                var pedidos = db.Table<ConPedidos>();

                                string existeprod = "NO";
                                foreach (var pedisur in pedidos)
                                {
                                    if (pedisur.prod_clave.ToString().Trim() == mcod.ToString().Trim())
                                    {
                                        existeprod = "SI";
                                    }
                                }


                                if (existeprod == "SI")
                                {
                                    db.Query<ConPedidos>("UPDATE [ConPedidos] SET surtido = '" + totalx + "' WHERE prod_clave = '" + mcod.ToString() + "'");
                                }
                                else
                                {
                                    ConPedidos ConsecutivosPedidos = new ConPedidos { prod_clave = mcod.ToString(), nombre = traenom(mcod.ToString().Trim()), pedido = 0, surtido = Convert.ToInt16(totalx) };
                                    db.Insert(ConsecutivosPedidos);
                                }

                                //cajaactual++;
                                total_caja_verde++;
                                TotCaj++;
                                total.Text = TotCaj.ToString("##0");



                                listItem.Add(new FlimStarInfo()
                                {
                                    Name = traenom(mcod.ToString().Trim()),
                                    Age = "Recibo: " + mfol + " Tarima: " + mtar + " Caja: " + mcaj,
                                    ImageID = Resource.Drawable.producto
                                });
                            }
                            catch
                            {
                                Toast.MakeText(this, "Duplicidad Evitada", ToastLength.Short).Show();
                            }
                        }
                        cajaactual++;
                        n++;
                    }
                }
                foliocaptura.SetSelection(0, foliocaptura.Text.Length);
                foliocaptura.RequestFocus();
                valorfinal = foliocaptura.Text;
                //iMPRESION DE MENSAJE QUE INDICARA CUANTO DE CADA TARIMA SE LOGRO CARGAR Y SIMULAR
                Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                alertDialog.SetTitle(Html.FromHtml("<font color='#55F721' size = 10>LECTURA POR TARIMA</font>"));
                alertDialog.SetIcon(Resource.Drawable.nota);
                alertDialog.SetMessage(Html.FromHtml("<font color='#9FFA7A' size = 10>Se han Capturado " + total_caja_verde + " Cajas,  Del Folio " + mfol + " De la tarima " + mtar + " Del Producto " + traenom(mcod.ToString().Trim()) + "</font>"));
                alertDialog.SetNeutralButton("Ok", delegate
                {
                    alertDialog.Dispose();
                });
                alertDialog.Show();
            }
            else
            {
                Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                alertDialog.SetTitle(Html.FromHtml("<font color='#55F721' size = 10>EXISTENCIA NO DISPONIBLE</font>"));
                alertDialog.SetIcon(Resource.Drawable.nota);
                alertDialog.SetMessage(Html.FromHtml("<font color='#9FFA7A' size = 10>La Tarima Actual No Cuenta con Existencia Disponible, Favor de Depurar los folios correspondientes y volver a leer</font>"));
                alertDialog.SetNeutralButton("Ok", delegate
                {
                    alertDialog.Dispose();
                });
                alertDialog.Show();
                foliocaptura.SetSelection(0, foliocaptura.Text.Length);
                foliocaptura.RequestFocus();
                valorfinal = foliocaptura.Text;
            }
            List<FlimStarInfo> lstFlimStar = listItem;
            var gvObject = FindViewById<GridView>(Resource.Id.gvCtr2com);
            gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);
        }

        private string ValidaCajaEtiVerde(string cadena, DataTable foliosleidos)
        {
            /*string Cadena = "Select fecha_cap From tb_Det_Etiqueta " +
                           "Where Eti_Lectura = '" + cadena + "' AND Estatus != 'C'";
            SqlCommand cmd = new SqlCommand(Cadena, thisConnection);*/
            string Valor = "";

            DataRow[] datos = foliosleidos.Select("Eti_Lectura = '" + cadena + "'");

            if (datos.Length > 0)
            {
                Valor = datos[0].ItemArray[1].ToString();
            }

            return Valor;

        }

        private string ValidaCajaPreesplitVerde(string cadena, DataTable foliosleidos)
        {
            /*string Cadena = "Select fecha_cap From tb_Det_Etiqueta " +
                           "Where Eti_Lectura = '" + cadena + "' AND Estatus != 'C'";
            SqlCommand cmd = new SqlCommand(Cadena, thisConnection);*/
            string Valor = "";

            DataRow[] datos = foliosleidos.Select("Eti_Lectura = '" + cadena + "'");

            if (datos.Length > 0)
            {
                Valor = datos[0].ItemArray[1].ToString();
            }

            return Valor;

        }

        private void etiquetablanca()
        {
            int pos = foliocaptura.Text.Trim().IndexOf("=");
            //MessageBox.Show(pos.ToString()); 
            if (pos == -1)
            {
                foliocaptura.SetSelection(0, foliocaptura.Text.Length);
                foliocaptura.RequestFocus();
                valorfinal = foliocaptura.Text;
                return;
            }
            foliocaptura.Text = foliocaptura.Text.Substring(pos + 1, foliocaptura.Text.Length - (pos + 1)).Trim();
            foliocaptura.Text = foliocaptura.Text.Replace("=", "");
            int tam = foliocaptura.Text.Length;
            string mcaj = "", mtar = "", mcod = "", mfol = "", mtip = "", Ent = "N";

            for (int i = 0; i < CatProd.Rows.Count; i++)
            {
                string producto_clave = CatProd.Rows[i]["Prod_Clave"].ToString().Trim();
                bool esta = foliocaptura.Text.Contains(producto_clave);

                if (esta)
                {
                    mcod = producto_clave;
                    break;
                }
            }

            int posprod = foliocaptura.Text.Trim().IndexOf(mcod);
            mfol = foliocaptura.Text.Substring(0, posprod).Trim();
            mtip = "PTP";
            string restocaptura = foliocaptura.Text.Replace(mfol, "").Replace(mcod, "");
            if (restocaptura.Length == 6)
            {
                if (mfol.Length == 5)
                {
                    mtip = "PTC";
                }
                mcaj = restocaptura.Substring(3, 3);
                mtar = restocaptura.Substring(0, 3);
            }
            else
            {
                mtip = "PTC";
                mcaj = restocaptura.Substring(4, 3);
                mtar = restocaptura.Substring(0, 2);
            }


            /*
            if (tam == 21) //Etiqueta de Campo que no es Aguilares y Proceso Planta
            {
                Int32 ValorFolio = Convert.ToInt32(foliocaptura.Text.Substring(0, 6));
                if (ValorFolio > FolioCampo) // Etiqueta de Campo
                    Ent = "S";
            }
            else if(tam > 20) //Etiqueta de Campo que no es Aguilares y Proceso Planta
            { 
                Int32 ValorFolio = Convert.ToInt32(foliocaptura.Text.Substring(0, 6));
                if (ValorFolio > FolioCampo) // Etiqueta de Campo
                { // Etiqueta de Campo
                    Ent = "S";
                }
                else
                {
                    mcaj = foliocaptura.Text.Substring(tam - 3, 3);
                    mtar = foliocaptura.Text.Substring(tam - 7, 2);
                    mfol = foliocaptura.Text.Substring(0, 6);
                    mcod = foliocaptura.Text.Substring(6, tam - 13);
                    mtip = "PTC";
                    if (traenom(mcod) != "")
                    {
                        Ent = "S";
                    }
                }
            }
            if (Ent == "N") // Valido si el PTP Planta o PTC de Aguilares
            {
                mcaj = foliocaptura.Text.Substring(tam - 3, 3);
                mtar = foliocaptura.Text.Substring(tam - 6, 3);
                int tam2 = tam - 6;
                mtip = "PTP";
                if (tam2 == 15) // Etiqueta de Aguilares	
                {
                    mfol = foliocaptura.Text.Substring(0, 5);
                    mcod = foliocaptura.Text.Substring(5, tam - 11);
                    mtip = "PTC";
                }
                else if (tam2 <= 14) // Etiqueta de Aguilares	
                {
                    mfol = foliocaptura.Text.Substring(0, 4);
                    mcod = foliocaptura.Text.Substring(4, tam - 10);
                    mtip = "PTC";
                }
                else
                {
                    mfol = foliocaptura.Text.Substring(0, 6);
                    mcod = foliocaptura.Text.Substring(6, tam - 12);
                }
                var nombreproducto = traenom(mcod); //Valido si existe el producto, si no quiere decir que es recibo de 6 digitos


                if (nombreproducto == "")
                {
                    mfol = foliocaptura.Text.Substring(0, 6);
                    mcod = foliocaptura.Text.Substring(6, tam - 12);
                    mtip = "PTP";
                }

                nombreproducto = traenom(mcod); //Valido si existe el producto, si no quiere decir que es recibo de 6 digitos


                if (nombreproducto == "")
                {
                    mcaj = foliocaptura.Text.Substring(tam - 2, 2);
                    mtar = foliocaptura.Text.Substring(tam - 4, 2);
                    mfol = foliocaptura.Text.Substring(0, 6);
                    mcod = foliocaptura.Text.Substring(6, tam - 10);
                    mtip = "PTC";
                }

                nombreproducto = traenom(mcod);

                if (nombreproducto == "")
                {
                    mcaj = foliocaptura.Text.Substring(tam - 3, 3);
                    mtar = foliocaptura.Text.Substring(tam - 6, 3);
                    mfol = foliocaptura.Text.Substring(0, 5);
                    mcod = foliocaptura.Text.Substring(5, tam - 11);
                    mtip = "PTC";
                }
            }
            else //Etiqueta de Campo que no es Aguilares
            {
                mcaj = foliocaptura.Text.Substring(tam - 3, 3);
                mtar = foliocaptura.Text.Substring(tam - 7, 2);
                mfol = foliocaptura.Text.Substring(0, 6);
                mcod = foliocaptura.Text.Substring(6, tam - 13);
                mtip = "PTC";
            }*/
            mtip = mtip.Trim();
            mfol = mfol.Trim();
            mcod = mcod.Trim();
            mtar = mtar.Trim();
            mcaj = mcaj.Trim();
            string cad = mtip + " | " + mfol + " | " + mcod + " | " + mtar + " | " + mcaj;
            if (repetido(mtip, mfol, mcod, mtar, mcaj) != "S")
            {
                xprod Pedidoscapturados = new xprod { Tipo = mtip, Folio = mfol, Codigo = mcod, Tarima = mtar, Cajas = mcaj, fecha_captura = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") };
                db.Insert(Pedidoscapturados);

                int totalx = traetotal(mcod);

                totalx = totalx + 1;


                string existeprod = "NO";
                var pedidos = db.Query<ConPedidos>("Select * FROM ConPedidos Where prod_clave = '" + mcod.ToString().Trim() + "'");

                foreach (var pedisur in pedidos)
                {
                    db.Query<ConPedidos>("UPDATE [ConPedidos] SET surtido = '" + totalx + "' WHERE prod_clave = '" + mcod.ToString() + "'");
                    existeprod = "SI";
                }


                if (existeprod == "NO")
                {
                    ConPedidos ConsecutivosPedidos = new ConPedidos { prod_clave = mcod.ToString(), nombre = traenom(mcod.ToString().Trim()), pedido = 0, surtido = Convert.ToInt16(totalx) };
                    db.Insert(ConsecutivosPedidos);
                }


                TotCaj++;
                total.Text = TotCaj.ToString("##0");

                listItem.Add(new FlimStarInfo()
                {
                    Name = traenom(mcod.ToString().Trim()),
                    Age = "Recibo: " + mfol + " Tarima: " + mtar + " Caja: " + mcaj,
                    ImageID = Resource.Drawable.producto
                });
            }
            foliocaptura.SetSelection(0, foliocaptura.Text.Length);
            foliocaptura.RequestFocus();
            valorfinal = foliocaptura.Text;

            List<FlimStarInfo> lstFlimStar = listItem;
            var gvObject = FindViewById<GridView>(Resource.Id.gvCtr2com);
            gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);
        }

        private string validafecadMod()
        {
            dondegenera = "validafecMod";
            string Valor = "";
            ValiFechacad = "S";
            //Obtener los productos con su tipo de lo que se ha leido******************************************************************
            var productoscapturados = db.Query<xLote>("Select Tipo, Codigo, nombre FROM xLote GROUP BY Tipo, Codigo, nombre");
            db.Query<XLoteSug>("delete from[XLoteSug]");

            var allItems = db.Table<xLote>().ToList();
            int count = allItems.Count;
            int[] validados = new int[count + 1];
            int capturas = 0;
            foreach (var captu in productoscapturados)
            {
                int totalpro = 0;
                int totaldisponibles = 0;
                int totalusadas;
                int simulador = 0;
                int totaldis = 0;
                string fechaant = "";
                int totaldisreal = 0;
                int totalprodsimulado = 0;

                //traer el total de recibos vencidos para que no entren en la condicion
                var prodcapx = db.Query<xLote>("Select COUNT(ID) AS Cajas FROM xLote Where Codigo = '" + captu.Codigo.Trim() + "'");

                foreach (var capturadox in prodcapx)
                {
                    totalpro = Convert.ToInt32(capturadox.Cajas.ToString().Trim());
                }

                int resttotal = traerecibosvencidos(captu.Codigo.Trim(), captu.Tipo.Trim());

                totalpro = totalpro - resttotal;
                totalprodsimulado = totalpro;

                //Obtener los diferentes folios disponibles dependiendo el codigo y el tipo
                string todobien = "OK";
                int prod_cap = 0;
                int usadas = 0;
                int existefecant = 0;
                string cadena = "";
                string tipo = captu.Tipo.Trim();
                string prod = captu.Codigo.Trim();
                string diacadant = "";
                string mescadant = "";
                string prodnom = captu.nombre.Trim();
                int diascad = 14;
                if (prodnom.Contains("BETABEL"))
                {
                    diascad = 60;
                }
                else if (prodnom.Contains("AJO"))
                {
                    diascad = 180;
                }
                else if (prodnom.Contains("ADEREZO") || prodnom.Contains("VINAGRETA") || prodnom.Contains("QUESO"))
                {
                    diascad = 90;
                }


                if (tipo == "PTC")
                {
                    cadena = "SELECT  (etiqueta - surtido) AS disponible, (CASE fecha_cad WHEN '' THEN  FORMAT( DATEADD(day, " + diascad + ", pti_fecha), 'dd/MM/yyyy', 'en-US' ) WHEN fecha_cad THEN fecha_cad END) AS fecha_cad, (CASE fecha_cad WHEN '' THEN  FORMAT( DATEADD(day, " + diascad + ", pti_fecha), 'yyyyMMdd', 'en-US' ) WHEN fecha_cad THEN FORMAT(convert(datetime,fecha_cad), 'yyyyMMdd', 'en-US' ) END) AS fecha_cadu, recibo, tarima FROM TB_DET_TRAZABILIDAD Inner JOIN tb_mstr_recepcion_pt ON rpt_recibo = recibo WHERE PROD_CLAVE = '" + prod + "' AND pti_estatus_sur = '' AND tipo = 'PTC' AND (rpt_tipo != 'TR' OR (rpt_tipo != 'TR' AND rpt_inventario = 'S')) AND rpt_estatus = '' AND  (etiqueta - surtido) > 0 Order By fecha_cadu";
                }
                else
                {
                    cadena = "SELECT (num_cajas - cajas_sur) AS disponible, ISNULL(fechacad, FORMAT( DATEADD(day, " + diascad + ", fecha), 'yyyyMMdd', 'en-US' )) AS fecha_cad, folio AS recibo, tarima FROM tb_det_eti_final Inner JOIN tb_mstr_ordenes_prod ON folio = ordp_folio WHERE cve_prod = '" + prod + "' AND estatus_sur != 'S' AND ordp_estatus != 'C' AND (num_cajas - cajas_sur) > 0 Order By fecha_cad";
                }

                SqlDataAdapter da = new SqlDataAdapter(cadena, thisConnection);
                DataSet ds = new DataSet();
                da.Fill(ds, "xlotes");
                DataTable xlote = ds.Tables["xlotes"];
                //Recorrido de cada uno de los folios y la validacion correspondiente hacia lo que tengo capturado************************

                string foliosAnt = "";

                foreach (DataRow row in xlote.Rows)
                {
                    int total_prod_simula = totalprodsimulado;
                    string Cadena = "Select Count(fecha) AS Total From Tb_Det_Etiqueta_Presplit " +
                                    "Where Eti_Recibo = '" + row["recibo"].ToString().Trim() + "' AND Eti_Producto = '" + captu.Codigo.Trim() + "' AND Eti_TarIni = '" + Convert.ToInt32(row["tarima"].ToString().Trim()) + "' AND Estatus = 'A'";

                    thisConnection.Open();
                    SqlCommand cmd = new SqlCommand(Cadena, thisConnection);
                    int TotalLeido = Convert.ToInt32(cmd.ExecuteScalar());
                    int usadasant = 0;
                    thisConnection.Close();

                    row["disponible"] = Convert.ToInt32(row["disponible"].ToString().Trim()) - TotalLeido;

                    if (Convert.ToInt32(row["disponible"]) > 0)
                    {
                        if (totalpro > 0)
                        {

                            string diacad = traediafecadrec(row["fecha_cad"].ToString().Trim(), tipo);
                            string mescad = traemesfecadrec(row["fecha_cad"].ToString().Trim(), tipo);

                            var prodcap = db.Query<xLote>("Select COUNT(ID) AS Cajas FROM xLote Where Codigo = '" + captu.Codigo.Trim() + "' AND Folio = '" + row["recibo"].ToString().Trim() + "'  AND CAST(Tarima as int) = '" + Convert.ToInt32(row["tarima"].ToString().Trim()) + "'");

                            foreach (var capturado in prodcap)
                            {

                                usadas = Convert.ToInt32(capturado.Cajas.ToString().Trim());
                                usadasant = usadas;
                                totaldis = Convert.ToInt32(row["disponible"].ToString().Trim()) - Convert.ToInt32(capturado.Cajas.ToString().Trim());
                                simulador = simulador + totaldis;
                                totalpro = totalpro - usadas;
                                totaldisponibles = totaldisponibles + totaldis;
                                totaldisreal = totaldis;
                            }

                            if (totaldis > 0 && totalpro > 0)
                            {
                                var prodcapfecad = db.Query<xLote>("Select COUNT(ID) AS Cajas FROM xLote Where Codigo = '" + captu.Codigo.Trim() + "' AND diacad = '" + diacad + "'AND mescad = '" + mescad + "'");

                                foreach (var capturado in prodcapfecad)
                                {
                                    usadas = Convert.ToInt32(capturado.Cajas.ToString().Trim()) - usadasant;
                                    totaldis = Convert.ToInt32(totaldis) - Convert.ToInt32(capturado.Cajas.ToString().Trim());
                                    simulador = simulador + totaldis;
                                    totalpro = totalpro - usadas;
                                    totaldisponibles = totaldisponibles + totaldis;
                                }
                            }

                            if (totaldis > 0)
                            {
                                if (totalpro > 0)
                                {
                                    XLoteSug sugeridos = new XLoteSug { recibosug = row["recibo"].ToString().Trim(), fecrecsug = diacad + "/" + mescad, cveprod = prod, Tarima = row["tarima"].ToString().Trim(), Cajasdis = totaldisreal, Cajasusadas = usadas, foliomens = "" };
                                    db.Insert(sugeridos);

                                    break;
                                }
                            }

                            //diacadant = diacad;
                            //mescadant = mescad;
                        }
                        else
                        {
                            break;
                        }
                    }

                }

                var loteSug = db.Query<XLoteSug>("Select  *  FROM XLoteSug Where cveprod = '" + captu.Codigo.Trim() + "' AND cajasdis != 0 LIMIT 1");
                foreach (var capturado in loteSug)
                {
                    string recibosug = capturado.recibosug;
                    string fecrecsug = capturado.fecrecsug;
                    string cveprod = capturado.cveprod;
                    string tarima = capturado.Tarima;
                    int cajasdis = capturado.Cajasdis;
                    int cajasusadas = capturado.Cajasusadas;
                    Mensajes mensa = new Mensajes { titulo = "Existe un folio anterior disponible", mensaje = "El recibo " + "\n\r" + capturado.recibosug.ToString().Trim() + " De la tarima  " + capturado.Tarima.Trim() + " Tiene  " + capturado.Cajasdis + " cajas disponibles del producto: " + captu.nombre.Trim() + " Con Fecha de Caducidad del" + capturado.fecrecsug };
                    db.Insert(mensa);
                    ValiFechacad = "N";

                    XLoteSug sugeridosact = new XLoteSug { recibosug = recibosug.ToString().Trim(), fecrecsug = fecrecsug, cveprod = cveprod, Tarima = tarima.ToString().Trim(), Cajasdis = cajasdis, Cajasusadas = cajasusadas, foliomens = "S" };
                    db.Insert(sugeridosact);
                    totalpro = 0;
                }


            }


            return Valor;


        }

        private void CancelAction(object sender, DialogClickEventArgs e)
        {
            return;
        }

        private void SaveName(object sender, DialogClickEventArgs e)
        {
            //nombre_recibido = et.Text.Trim().ToUpper();

            thisConnection.Open();
            string cadena = "Select usuario,password From tb_Autoriza_OdeP Where password = '" + et.Text.Trim().ToUpper() + "' AND clave = 'EM' AND obs = 'Autoriza Caducidad Camionetas'";
            SqlCommand cmd = new SqlCommand(cadena, thisConnection);
            mAutoriza = Convert.ToString(cmd.ExecuteScalar());
            if (mAutoriza.Trim().Length == 0)
            {
                Toast.MakeText(this, "PASSWORD INCORRECTO!!!", ToastLength.Short).Show();
                thisConnection.Close();
            }
            else
            {
                thisConnection.Close();
                AutoPed = "S";
                Guardar.Enabled = true;
                return;
            }

        }
    }
}