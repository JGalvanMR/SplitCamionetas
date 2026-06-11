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

namespace SplitCamionetas
{
    [Activity(Label = "Ingresar Pedido")]
    public partial class SolicitarPed : Activity
    {
        public static string cvvehiculo, cvresponsable;
        public static string vehiculo, responsable;
        public string Nombre = "", Mtipo = "", MProd = "", MTar = "", MFol = "", mUser = "", user = "";
        public string Mtipo2 = "", MProd2 = "", MTar2 = "", MFol2 = "", CveCam = "", mOp = "A", Version = "";
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
        ArrayAdapter<String> comboAdapter;
        String[] strFrutas;



        //traer los datos e id de cada uno de los elementos de la vista
        EditText pedido;
        TextView detalleped;
        Spinner Pedidos;
        Button capturar;




        protected override void OnCreate(Bundle savedInstanceState)
        {
            string contenido = "";
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.SolicitarPedidos);
            LoadConnection();


            //Declaracion de los id de cada elemento
            pedido = FindViewById<EditText>(Resource.Id.agregarpedido);
            Pedidos = FindViewById<Spinner>(Resource.Id.pedidos);
            capturar = FindViewById<Button>(Resource.Id.button1);
            capturar.Click += Btnlogin_Click;
            capturar.Enabled = false;
            Pedidos.Enabled = false;

            //Recuperar datos de la pantalla anterior
            cvvehiculo = Intent.GetStringExtra("cvcamioneta");
            cvresponsable = Intent.GetStringExtra("cvresponsable");
            vehiculo = Intent.GetStringExtra("camioneta");
            responsable = Intent.GetStringExtra("responsable");

            TextView usuario = FindViewById<TextView>(Resource.Id.usuario);
            usuario.Text = responsable.Trim() + " - " + cvvehiculo.Trim();


            //*** Buscar Pedidos si se cerro mal el programa*************************************************************
            int cantidad_spinnere = 0;
            var queryinicial = db.Query<Pedidos>("SELECT DISTINCT folio FROM [Pedidos]");


            foreach (var captu1 in queryinicial)
            {

                if (valida_pedido(pedido.Text.Trim()) != 0)
                {
                    db.Query<Pedidos>("delete from  [Pedidos]");
                    db.Query<ConPedidos>("delete from  [ConPedidos]");
                    db.Query<xLote>("delete from  [xLote]");
                    db.Query<xLoteFinal>("delete from  [xLoteFinal]");
                    db.Query<xprod>("delete from  [xprod]");
                }
                else
                {
                    cantidad_spinnere = cantidad_spinnere + 1;
                }
            }

            if (cantidad_spinnere > 0)
            {
                var querye = db.Query<Pedidos>("SELECT DISTINCT folio FROM [Pedidos]");
                Spinner spinner1 = FindViewById<Spinner>(Resource.Id.pedidos);
                System.Collections.ArrayList listaFrutas = new System.Collections.ArrayList();

                strFrutas = new String[cantidad_spinnere + 1];
                strFrutas[0] = "Seleccione un folio";
                int x = 1;

                foreach (var captu in querye)
                {
                    strFrutas[x] = captu.folio.ToString();
                    x = x + 1;
                }


                Collections.AddAll(listaFrutas, strFrutas);
                comboAdapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, strFrutas);
                spinner1.Adapter = comboAdapter;
                spinner1.ItemSelected += new EventHandler<AdapterView.ItemSelectedEventArgs>(spinner_ItemSelected);
                capturar.Enabled = true;
                spinner1.Enabled = true;
            }



            //Termina busqueda de pedidos *********************************************************************************




            pedido.EditorAction += (sender, e) =>
            {

                if (e.ActionId == ImeAction.Done || e.ActionId == ImeAction.Next)
                {

                    string hay = "N";
                    string Cadena = "";
                    if (mOp == "C")
                    {

                        List<FlimStarInfo> lstFlimStar = ConsPed(pedido.Text.Trim());
                        var gvObject = FindViewById<GridView>(Resource.Id.gvCtrl);
                        gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);
                        gvObject.ItemClick += new EventHandler<AdapterView.ItemClickEventArgs>(OnGridView_ItemClicked); //detalle_pedido
                        return;
                    }
                    else
                    {

                        var queryqe = db.Table<Pedidos>();
                        foreach (var captu in queryqe)
                        {
                            if (captu.folio == pedido.Text.Trim())
                            {
                                hay = "S";
                                Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                                alertDialog.SetTitle(Html.FromHtml("<font color='#DF0101' size = 10>Pedido ya agregado para captura</font>"));
                                alertDialog.SetIcon(Resource.Drawable.no);
                                alertDialog.SetCancelable(false);
                                alertDialog.SetMessage(Html.FromHtml("<font color='#FFFFFF' size = 10>El pedido : " + folio + " ya se agrego para capturar</font>"));
                                alertDialog.SetNeutralButton("Ok", delegate
                                {
                                    alertDialog.Dispose();
                                });
                                alertDialog.Show();
                                pedido.Text = "";
                                pedido.RequestFocus();
                                return;
                            }
                        }

                        int resuvalped = valida_pedido(pedido.Text.Trim());

                        if (resuvalped == 1)
                        {
                            Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                            alertDialog.SetTitle(Html.FromHtml("<font color='#DF0101' size = 10>Pedido ya capturado</font>"));
                            alertDialog.SetIcon(Resource.Drawable.no);
                            alertDialog.SetCancelable(false);
                            alertDialog.SetMessage(Html.FromHtml("<font color='#FFFFFF' size = 10>El pedido : " + folio + " ya ha sido capturado</font>"));
                            alertDialog.SetNeutralButton("Ok", delegate
                            {
                                alertDialog.Dispose();
                            });
                            alertDialog.Show();
                            pedido.Text = "";
                            pedido.RequestFocus();
                            //return;

                        }
                        else if (resuvalped == 2)
                        {
                            Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                            alertDialog.SetTitle(Html.FromHtml("<font color='#DF0101' size = 10>Pedido ya Cerrado</font>"));
                            alertDialog.SetIcon(Resource.Drawable.no);
                            alertDialog.SetCancelable(false);
                            alertDialog.SetMessage(Html.FromHtml("<font color='#FFFFFF' size = 10>El pedido : " + folio + " ya ha sido capturado y Cerrado</font>"));
                            alertDialog.SetNeutralButton("Ok", delegate
                            {
                                alertDialog.Dispose();
                            });
                            alertDialog.Show();
                            pedido.Text = "";
                            pedido.RequestFocus();
                            return;
                        }
                        else if (resuvalped == 3)
                        {
                            Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                            alertDialog.SetTitle(Html.FromHtml("<font color='#DF0101' size = 10>Factura No Es De Camionetas</font>"));
                            alertDialog.SetIcon(Resource.Drawable.no);
                            alertDialog.SetCancelable(false);
                            alertDialog.SetMessage(Html.FromHtml("<font color='#FFFFFF' size = 10>La Factura: " + folio + " No Corresponde a Sistema Split Camionetas</font>"));
                            alertDialog.SetNeutralButton("Ok", delegate
                            {
                                alertDialog.Dispose();
                            });
                            alertDialog.Show();
                            pedido.Text = "";
                            pedido.RequestFocus();
                            return;
                        }
                        else if (resuvalped == 4)
                        {
                            Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                            alertDialog.SetTitle(Html.FromHtml("<font color='#FA993E' size = 10>Pedido Cancelado</font>"));
                            alertDialog.SetIcon(Resource.Drawable.warning);
                            alertDialog.SetCancelable(false);
                            alertDialog.SetMessage(Html.FromHtml("<font color='#FAC73E' size = 10>El pedido: " + folio + " Esta Cancelado y no se puede cargar</font>"));
                            alertDialog.SetNeutralButton("Ok", delegate
                            {
                                alertDialog.Dispose();
                            });
                            alertDialog.Show();
                            pedido.Text = "";
                            pedido.RequestFocus();
                            return;

                        }
                        else if (resuvalped == 5)
                        {
                            Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                            alertDialog.SetTitle(Html.FromHtml("<font color='#FA993E' size = 10>Pedido No Es De Camionetas</font>"));
                            alertDialog.SetIcon(Resource.Drawable.warning);
                            alertDialog.SetCancelable(false);
                            alertDialog.SetMessage(Html.FromHtml("<font color='#FAC73E' size = 10>El pedido: " + folio + " No Corresponde a Sistema Split Camionetas</font>"));
                            alertDialog.SetNeutralButton("Ok", delegate
                            {
                                alertDialog.Dispose();
                            });
                            alertDialog.Show();
                            pedido.Text = "";
                            pedido.RequestFocus();
                            return;

                        }
                        else if (resuvalped == 6)
                        {
                            Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                            alertDialog.SetTitle(Html.FromHtml("<font color='#FA993E' size = 10>Pedido No Asignado A " + cvvehiculo.Trim() + "</font>"));
                            alertDialog.SetIcon(Resource.Drawable.warning);
                            alertDialog.SetCancelable(false);
                            alertDialog.SetMessage(Html.FromHtml("<font color='#FAC73E' size = 10>El pedido: " + folio + " No se puede Cargar a esta camioneta porque la orden esta asignada a otra</font>"));
                            alertDialog.SetNeutralButton("Ok", delegate
                            {
                                alertDialog.Dispose();
                            });
                            alertDialog.Show();
                            pedido.Text = "";
                            pedido.RequestFocus();
                            return;

                        }
                        else if (resuvalped == 7)
                        {
                            Android.App.AlertDialog.Builder alertDialogx = new Android.App.AlertDialog.Builder(this);
                            alertDialogx.SetTitle(Html.FromHtml("<font color='#FA993E' size = 10>Pedido Con Split</font>"));
                            alertDialogx.SetIcon(Resource.Drawable.warning);
                            alertDialogx.SetCancelable(false);
                            alertDialogx.SetMessage(Html.FromHtml("<font color='#FAC73E' size = 10>El pedido: " + pedido.Text.Trim() + " No se puede Cargar a esta camioneta porque la orden tiene un split asignado a Otra, Si desea Puede Cancelar el Split Previo e intentar de nuevo</font>"));
                            alertDialogx.SetNeutralButton("Ok", delegate
                            {
                                alertDialogx.Dispose();
                            });
                            alertDialogx.Show();
                            pedido.Text = "";
                            pedido.RequestFocus();
                            return;

                        }

                        if (hay == "N")
                        {
                            thisConnection.Open();
                            Cadena = "Select a.pdn_folio,a.prod_clave,b.prod_nombre,a.pdn_num_unidades From tb_det_pedidos A, tb_Cat_producto B " +
                                "where a.pdn_folio = '" + pedido.Text.Trim() + "' and a.prod_clave = b.prod_clave";
                            SqlDataAdapter da = new SqlDataAdapter(Cadena, thisConnection);
                            DataSet ds = new DataSet();
                            da.Fill(ds, "Ped");
                            DataTable Ped = ds.Tables["Ped"];
                            hay = "N";
                            thisConnection.Close();

                            if (Ped.Rows.Count == 0)
                            {
                                Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                                alertDialog.SetTitle(Html.FromHtml("<font color='#DF0101' size = 10>Pedido Inexistente</font>"));
                                alertDialog.SetIcon(Resource.Drawable.no);
                                alertDialog.SetCancelable(false);
                                alertDialog.SetMessage(Html.FromHtml("<font color='#FFFFFF' size = 10>El pedido: " + pedido.Text.Trim() + " No Existe o No se ha dado de alta</font>"));
                                alertDialog.SetNeutralButton("Ok", delegate
                                {
                                    alertDialog.Dispose();
                                });
                                alertDialog.Show();
                                pedido.Text = "";
                                pedido.RequestFocus();
                                return;
                            }


                            foreach (DataRow row in Ped.Rows)
                            {
                                string mnom = row["prod_nombre"].ToString().Trim();
                                mnom = mnom.Replace("'", " ");

                                Pedidos Pedidoscapturados = new Pedidos { folio = row["pdn_folio"].ToString().Trim(), prod_clave = row["prod_clave"].ToString().Trim(), nombre = mnom, pedido = Convert.ToInt32(row["pdn_num_unidades"]), surtido = 0 };
                                //Registra en la base de datos SQLite
                                db.Insert(Pedidoscapturados);


                                var encontrado = 0;
                                var query = db.Table<ConPedidos>();
                                foreach (var captu in query)
                                {
                                    if (captu.prod_clave.ToString().Trim() == row["prod_clave"].ToString().Trim())
                                    {
                                        encontrado = 1;
                                        var total = Convert.ToInt16(row["pdn_num_unidades"]) + Convert.ToInt16(captu.pedido);
                                        db.Query<ConPedidos>("UPDATE [ConPedidos] SET pedido = '" + total + "' WHERE prod_clave = '" + captu.prod_clave.ToString().Trim() + "'");
                                    }
                                }

                                if (encontrado == 0)
                                {

                                    ConPedidos consecutivo = new ConPedidos { prod_clave = row["prod_clave"].ToString().Trim(), nombre = mnom, pedido = Convert.ToInt32(row["pdn_num_unidades"]), surtido = 0 };
                                    //Registra en la base de datos SQLite
                                    db.Insert(consecutivo);

                                }

                                hay = "S";
                            }
                            if (hay == "S")
                            {
                                int cantidad_spinner = 0;
                                var query1 = db.Query<Pedidos>("SELECT DISTINCT folio FROM [Pedidos]");
                                foreach (var captu1 in query1)
                                {
                                    cantidad_spinner = cantidad_spinner + 1;
                                }


                                var query = db.Query<Pedidos>("SELECT DISTINCT folio FROM [Pedidos]");
                                Spinner spinner = FindViewById<Spinner>(Resource.Id.pedidos);
                                System.Collections.ArrayList listaFrutas = new System.Collections.ArrayList();
                                spinner.Enabled = true;

                                strFrutas = new String[cantidad_spinner + 1];
                                strFrutas[0] = "Seleccione un folio";
                                int x = 1;

                                foreach (var captu in query)
                                {

                                    strFrutas[x] = captu.folio.ToString();
                                    x = x + 1;
                                }


                                Collections.AddAll(listaFrutas, strFrutas);
                                comboAdapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, strFrutas);
                                spinner.Adapter = comboAdapter;
                                spinner.ItemSelected += new EventHandler<AdapterView.ItemSelectedEventArgs>(spinner_ItemSelected);
                                pedido.Text = "";
                                pedido.RequestFocus();
                                capturar.Enabled = true;
                                Toast.MakeText(this, "Pedido agregado Correctamente", ToastLength.Short).Show();
                            }
                            thisConnection.Close();

                        }
                        else
                        {
                            e.Handled = false;
                        }

                    }

                    LoadConnection();


                }
            };



        }

        private void OnGridView_ItemClicked(object sender, AdapterView.ItemClickEventArgs e)
        {

        }


        public int valida_pedido(string validar)
        {
            int valor = 0;
            thisConnection.Open();
            string Cadena = "Select emb_folio from tb_det_split Where emb_folio = '" + validar + "' AND estatus != 'C'";
            SqlCommand cmd = new SqlCommand(Cadena, thisConnection);
            string Emb = Convert.ToString(cmd.ExecuteScalar());
            thisConnection.Close();
            if (Emb.Trim().Length > 0)
            {

                valor = 1;
                return valor;
            }

            thisConnection.Open();
            Cadena = "Select prov_clave from tb_mstr_pedidos_nal Where pdn_folio = '" + validar + "' AND pdn_surtido = 'S'";
            SqlCommand cmdx = new SqlCommand(Cadena, thisConnection);
            string Embx = Convert.ToString(cmdx.ExecuteScalar());
            thisConnection.Close();

            if (Embx.Trim().Length > 0)
            {
                thisConnection.Open();
                Cadena = "Select emb_folio from  tb_mstr_embarque Where emb_folio = '" + validar + "'";
                SqlCommand cmdexisteembarque = new SqlCommand(Cadena, thisConnection);
                string Embxexisteembarque = Convert.ToString(cmdexisteembarque.ExecuteScalar());
                thisConnection.Close();
                if (Embxexisteembarque.Trim().Length > 0)
                {

                    valor = 2;
                    return valor;
                }
                else
                {
                    thisConnection.Open();
                    string cadena = "UPDATE tb_mstr_pedidos_nal SET pdn_surtido = '' WHERE pdn_folio = '" + validar + "'";
                    SqlCommand cmdActualizaEmb = new SqlCommand(cadena, thisConnection);
                    cmdActualizaEmb.ExecuteNonQuery();
                    thisConnection.Close();
                }
            }


            thisConnection.Open();
            Cadena = "Select prov_clave from tb_mstr_facturas_nal Where pdn_folio = '" + validar + "'";
            cmdx = new SqlCommand(Cadena, thisConnection);
            Embx = Convert.ToString(cmdx.ExecuteScalar());
            thisConnection.Close();

            if (Embx.Trim().Length > 0)
            {
                if (Embx.Trim() != "MRLUCKY")
                {
                    valor = 3;
                    return valor;
                }
            }


            thisConnection.Open();
            Cadena = "Select pdn_folio from tb_mstr_pedidos_nal Where pdn_folio = '" + validar + "' AND pdn_estatus = 'C'";
            cmdx = new SqlCommand(Cadena, thisConnection);
            Embx = Convert.ToString(cmdx.ExecuteScalar());
            thisConnection.Close();
            if (Embx.Trim().Length > 0)
            {
                valor = 4;
                return valor;
            }

            thisConnection.Open();
            Cadena = "Select prov_clave from tb_mstr_pedidos_nal Where pdn_folio = '" + validar + "'";
            cmdx = new SqlCommand(Cadena, thisConnection);
            Embx = Convert.ToString(cmdx.ExecuteScalar());
            thisConnection.Close();
            if (Embx.Trim() != "MRLUCKY")
            {
                valor = 5;
                return valor;
            }


            thisConnection.Open();
            string CadenaPedido = "Select cve_auto from tb_mstr_facturas_nal Where pdn_folio = '" + validar + "'";
            SqlCommand cmdxi = new SqlCommand(CadenaPedido, thisConnection);
            string Embxi = Convert.ToString(cmdxi.ExecuteScalar()).Trim();
            thisConnection.Close();
            if (Embxi.Trim().Length > 0)
            {
                if (Embxi != cvvehiculo.Trim())
                {
                    valor = 6;
                    return valor;
                }
            }
            else
            {
                thisConnection.Open();
                string CadenaPedidosplit = "Select TOP(1) Cve_Camioneta from Tb_Det_Etiqueta Where emb_folio = '" + pedido.Text.Trim() + "' AND estatus != 'C'";
                SqlCommand cmdxii = new SqlCommand(CadenaPedidosplit, thisConnection);
                string Embxii = Convert.ToString(cmdxii.ExecuteScalar()).Trim();
                thisConnection.Close();

                if (Embxii.Trim().Length > 0)
                {
                    if (Embxi != cvvehiculo.Trim())
                    {
                        valor = 7;
                        return valor;
                    }

                }
                else
                {
                    Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                    alertDialog.SetTitle(Html.FromHtml("<font color='#5DCDD4' size = 10>Pedido Sin Factura</font>"));
                    alertDialog.SetIcon(Resource.Drawable.exito);
                    alertDialog.SetCancelable(false);
                    alertDialog.SetMessage(Html.FromHtml("<font color='#5FBDD5' size = 10>El pedido: " + pedido.Text.Trim() + " Se Asignará a la Camioneta Actual (" + cvvehiculo.Trim() + ")</font>"));
                    alertDialog.SetNeutralButton("Ok", delegate
                    {
                        alertDialog.Dispose();
                    });
                    alertDialog.Show();
                }

            }

            return valor;

        }



        List<FlimStarInfo> listItem = new List<FlimStarInfo>();

        List<FlimStarInfo> GetFlimStarInformation()
        {
            throw new NotImplementedException();
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.top_menus, menu);
            return base.OnCreateOptionsMenu(menu);
        }


        private void spinner_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            Spinner spinner = (Spinner)sender;
            var folio = spinner.GetItemAtPosition(e.Position).ToString();

            List<FlimStarInfo> lstFlimStar = detalle_pedido(folio.Trim(), "Individual");
            var gvObject = FindViewById<GridView>(Resource.Id.gvCtrl);
            gvObject.Adapter = new myGVItemAdapter(this, null);
            gvObject.Adapter = null;
            gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);
            gvObject.ItemClick += new EventHandler<AdapterView.ItemClickEventArgs>(OnGridView_ItemClicked);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (Convert.ToString(item.TitleFormatted) == "Nuevo")
            {
                Pedidos.Enabled = false;
                pedido.Text = "";
                pedido.RequestFocus();
                capturar.Enabled = false;


                List<FlimStarInfo> lstFlimStar = detalle_pedido(folio.Trim(), "Individual");
                lstFlimStar.Clear();
                var gvObject = FindViewById<GridView>(Resource.Id.gvCtrl);
                gvObject.Adapter = new myGVItemAdapter(this, null);
                gvObject.Adapter = null;
                gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);
                gvObject.ItemClick += new EventHandler<AdapterView.ItemClickEventArgs>(OnGridView_ItemClicked);



                Spinner spinner = FindViewById<Spinner>(Resource.Id.pedidos);

                spinner.Adapter = null;



                db.Query<Pedidos>("delete from  [Pedidos]");
                db.Query<ConPedidos>("delete from  [ConPedidos]");
                db.Query<xLote>("delete from  [xLote]");
                db.Query<xLoteFinal>("delete from  [xLoteFinal]");
                db.Query<xprod>("delete from  [xprod]");
                mOp = "A";
                Toast.MakeText(this, "Modo Captura Activado", ToastLength.Short).Show();




            }
            else if (Convert.ToString(item.TitleFormatted) == "Consultar")
            {
                capturar.Enabled = false;
                Pedidos.Enabled = false;
                pedido.Text = "";
                mOp = "C";

                Spinner spinner = FindViewById<Spinner>(Resource.Id.pedidos);
                spinner.Adapter = null;


                List<FlimStarInfo> lstFlimStar = detalle_pedido(folio.Trim(), "Individual");
                lstFlimStar.Clear();
                var gvObject = FindViewById<GridView>(Resource.Id.gvCtrl);
                gvObject.Adapter = new myGVItemAdapter(this, null);
                gvObject.Adapter = null;
                gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);
                gvObject.ItemClick += new EventHandler<AdapterView.ItemClickEventArgs>(OnGridView_ItemClicked);


                db.Query<Pedidos>("delete from  [Pedidos]");
                db.Query<ConPedidos>("delete from  [ConPedidos]");
                db.Query<xLote>("delete from  [xLote]");
                db.Query<xLoteFinal>("delete from  [xLoteFinal]");
                db.Query<xprod>("delete from  [xprod]");
                Toast.MakeText(this, "Modo Consulta Activado", ToastLength.Short).Show();
            }
            else if (Convert.ToString(item.TitleFormatted) == "Cancelar")
            {
                fnShowCustomAlertDialog();
            }
            else if (Convert.ToString(item.TitleFormatted) == "Complemento")
            {
                Intent intent = new Intent(this, typeof(ComplementoSolPed));
                intent.PutExtra("cvcamioneta", cvvehiculo.ToString());
                intent.PutExtra("cvresponsable", cvresponsable.ToString());
                intent.PutExtra("camioneta", vehiculo.ToString());
                intent.PutExtra("responsable", responsable.ToString());
                StartActivity(intent);
            }
            else
            {
                if (mOp == "C")
                {
                    Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                    alertDialog.SetTitle(Html.FromHtml("<font color='#DF0101' size = 10>Modo Consulta Activado</font>"));
                    alertDialog.SetIcon(Resource.Drawable.no);
                    alertDialog.SetMessage(Html.FromHtml("<font color='#FFFFFF' size = 10>Para realizar el acumulado, debe estar en modo captura.\r\nFavor de validar que el modo captura esta activado</font>"));
                    alertDialog.SetNeutralButton("Ok", delegate
                    {
                        alertDialog.Dispose();
                    });
                    alertDialog.Show();
                    return base.OnOptionsItemSelected(item);
                }


                List<FlimStarInfo> lstFlimStar = detalle_pedido(pedido.Text.Trim(), "Acumulado");
                var gvObject = FindViewById<GridView>(Resource.Id.gvCtrl);
                gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);
                gvObject.ItemClick += new EventHandler<AdapterView.ItemClickEventArgs>(OnGridView_ItemClicked); //detalle_pedido
            }

            return base.OnOptionsItemSelected(item);
        }


        //Cargar conexion de base de datos sqlite
        private void LoadConnection()
        {
            string folder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            string dbPath = System.IO.Path.Combine(folder, "Split_camionetas.db");

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
            }
            else
            {
                try
                {
                    db.Query<xLote>("Select * FROM XLoteSug");
                }
                catch (SQLiteException e)
                {

                    string errorsqlite = e.ToString().Trim();
                    errorsqlite = errorsqlite.Substring(24, 23);
                    if (errorsqlite == "no such table: XLoteSug")
                    {
                        db.CreateTable<XLoteSug>();
                    }
                }
            }


        }

        List<FlimStarInfo> ConsPed(string mped)
        {
            thisConnection.Open();
            listItem.Clear();
            string contenido = "";
            //thisConnection.Open();
            string cadena = "Select DISTINCT A.prod_clave from tb_det_split AS A  JOIN tb_cat_producto AS B ON A.prod_clave = B.prod_clave Where A.emb_folio = '" + pedido.Text.Trim() + "' Order by A.prod_clave";
            SqlDataAdapter da = new SqlDataAdapter(cadena, thisConnection);
            DataSet ds = new DataSet();
            da.Fill(ds, "ConsPed");
            DataTable ConsPed = ds.Tables["ConsPed"];

            foreach (DataRow Row in ConsPed.Rows)
            {
                string texto = "";
                string producto = "";
                string cadena2 = "Select A.no_lote, A.prod_clave, A.tarima, A.cajas, B.prod_nombre from tb_det_split AS A  JOIN tb_cat_producto AS B ON A.prod_clave = B.prod_clave Where A.emb_folio = '" + pedido.Text.Trim() + "' AND A.prod_clave = '" + Row["prod_clave"].ToString().Trim() + "' Order by A.tarima, A.prod_clave, A.no_lote";


                SqlDataAdapter dai = new SqlDataAdapter(cadena2, thisConnection);
                DataSet dsi = new DataSet();

                dai.Fill(dsi, "ConsPedi");
                DataTable ConsPedi = dsi.Tables["ConsPedi"];
                foreach (DataRow Rowi in ConsPedi.Rows)
                {
                    producto = Rowi["prod_nombre"].ToString().Trim();
                    texto = texto + "Lote: " + Rowi["no_lote"].ToString().Trim() + " Tarima: " + Rowi["tarima"].ToString().Trim() + " Surtido: " + Rowi["cajas"].ToString().Trim() + System.Environment.NewLine;

                }

                listItem.Add(new FlimStarInfo()
                {
                    Name = producto,
                    Age = texto,
                    ImageID = Resource.Drawable.producto
                });
            }


            //LbxCons.Font = new Font(LbxCons.Font.Name, 7);   ;
            thisConnection.Close();

            return listItem;
        }



        List<FlimStarInfo> detalle_pedido(string mped, string mov)
        {
            thisConnection.Open();
            listItem.Clear();

            if (mov != "Acumulado")
            {


                var query = db.Table<Pedidos>();
                foreach (var captu in query)
                {
                    if (captu.folio == mped)
                    {
                        listItem.Add(new FlimStarInfo()
                        {
                            Name = captu.nombre,
                            Age = "Pedidos: " + captu.pedido + " Surtido: " + captu.surtido,
                            ImageID = Resource.Drawable.producto
                        });
                    }
                }

            }
            else
            {

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

            }

            //LbxCons.Font = new Font(LbxCons.Font.Name, 7);   ;
            thisConnection.Close();

            return listItem;
        }

        void Btnlogin_Click(object sender, EventArgs e)
        {
            Intent intent = new Intent(this, typeof(capturar_split));
            intent.PutExtra("cvcamioneta", cvvehiculo.ToString());
            intent.PutExtra("cvresponsable", cvresponsable.ToString());
            intent.PutExtra("camioneta", vehiculo.ToString());
            intent.PutExtra("responsable", responsable.ToString());
            StartActivity(intent);
        }


        public override Boolean OnKeyDown(Keycode keyCode, KeyEvent e)
        {

            if (keyCode == Keycode.Back)
            {
                Intent intent = new Intent(this, typeof(MainActivity));
                intent.AddFlags(ActivityFlags.ClearTop);
                Intent.AddFlags(ActivityFlags.SingleTop);
                //intent.PutExtra("cvcamioneta", cvvehiculo.ToString());
                StartActivity(intent);
            }
            return true;
        }

        void fnShowCustomAlertDialog()
        {
            //Inflate layout
            View view = LayoutInflater.Inflate(Resource.Layout.frmsupervisor, null);
            AlertDialog builder = new AlertDialog.Builder(this).Create();
            builder.SetView(view);
            builder.SetCanceledOnTouchOutside(false);
            //EditText usuario = view.FindViewById<EditText>(Resource.Id.txtUsername);
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
                string cadena = "Select usuario From tb_Autoriza_OdeP Where clave = 'EM' and password = '" + password.Text.Trim() + "' AND Obs = 'Autoriza Caducidad Camionetas'";
                SqlCommand cmd = new SqlCommand(cadena, thisConnection);
                var mAutoriza = Convert.ToString(cmd.ExecuteScalar());
                if (mAutoriza.Trim().Length == 0)
                {
                    Toast.MakeText(this, "USUARIO Y PASSWORD INCORRECTO!!!", ToastLength.Short).Show();
                    thisConnection.Close();
                }
                else
                {
                    thisConnection.Close();
                    Intent intent = new Intent(this, typeof(CancelarSplit));
                    intent.PutExtra("respcancel", mAutoriza.ToString().Trim());
                    StartActivity(intent);
                    builder.Dismiss();
                }

            };
            builder.Show();
        }

    }
}