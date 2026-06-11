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

namespace SplitCamionetas
{
    [Activity(Label = "Cancelar Split")]
    public partial class CancelarSplit : Activity
    {
        public static string crcancelar, split, pedidocancelar;
        public static SQLiteConnection db;
        SqlConnection thisConnection = new SqlConnection(MainActivity.cadenaConexion);
        SqlDataAdapter da;
        DataSet ds = new DataSet();
        SqlCommand cmnd = new SqlCommand();
        SqlCommand cmnd1 = new SqlCommand();

        EditText pedidocan;
        TextView cansplit;
        TextView usuario;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            crcancelar = Intent.GetStringExtra("respcancel");
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.CancelarSplit);

            pedidocan = FindViewById<EditText>(Resource.Id.pedidocancelar);
            usuario = FindViewById<TextView>(Resource.Id.usercancel);
            cansplit = FindViewById<TextView>(Resource.Id.SplitCargados);

            LoadConnection();

            pedidocan.EditorAction += (sender, e) =>
            {
                if (e.ActionId == ImeAction.Done || e.ActionId == ImeAction.Next)
                {
                    int resuvalped = valida_pedido(pedidocan.Text.Trim());

                    if (resuvalped == 2)
                    {
                        Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                        alertDialog.SetTitle(Html.FromHtml("<font color='#DF0101' size = 10>Pedido ya Cerrado</font>"));
                        alertDialog.SetIcon(Resource.Drawable.no);
                        alertDialog.SetCancelable(false);
                        alertDialog.SetMessage(Html.FromHtml("<font color='#FFFFFF' size = 10>El pedido : " + pedidocan.Text.Trim() + " ya ha sido capturado y Cerrado</font>"));
                        alertDialog.SetNeutralButton("Ok", delegate
                        {
                            db.Query<Mensajes>("delete from  [Mensajes] Where titulo = 'Error Al Guardar' AND mensaje = '" + pedidocan.Text.Trim() + "'");
                            alertDialog.Dispose();
                        });
                        alertDialog.Show();
                        pedidocan.Text = "";
                        pedidocan.RequestFocus();
                        return;
                    }
                    else if (resuvalped == 3)
                    {
                        Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                        alertDialog.SetTitle(Html.FromHtml("<font color='#DF0101' size = 10>Factura No Es De Camionetas</font>"));
                        alertDialog.SetIcon(Resource.Drawable.no);
                        alertDialog.SetCancelable(false);
                        alertDialog.SetMessage(Html.FromHtml("<font color='#FFFFFF' size = 10>La Factura: " + pedidocan.Text.Trim() + " No Corresponde a Sistema Split Camionetas</font>"));
                        alertDialog.SetNeutralButton("Ok", delegate
                        {
                            alertDialog.Dispose();
                        });
                        alertDialog.Show();
                        pedidocan.Text = "";
                        pedidocan.RequestFocus();
                        return;
                    }
                    else if (resuvalped == 4)
                    {
                        Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                        alertDialog.SetTitle(Html.FromHtml("<font color='#FA993E' size = 10>Pedido Cancelado</font>"));
                        alertDialog.SetIcon(Resource.Drawable.warning);
                        alertDialog.SetCancelable(false);
                        alertDialog.SetMessage(Html.FromHtml("<font color='#FAC73E' size = 10>El pedido: " + pedidocan.Text.Trim() + " Esta Cancelado y no se puede cargar</font>"));
                        alertDialog.SetNeutralButton("Ok", delegate
                        {
                            alertDialog.Dispose();
                        });
                        alertDialog.Show();
                        pedidocan.Text = "";
                        pedidocan.RequestFocus();
                        return;

                    }
                    else if (resuvalped == 5)
                    {
                        Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                        alertDialog.SetTitle(Html.FromHtml("<font color='#FA993E' size = 10>Pedido No Es De Camionetas</font>"));
                        alertDialog.SetIcon(Resource.Drawable.warning);
                        alertDialog.SetCancelable(false);
                        alertDialog.SetMessage(Html.FromHtml("<font color='#FAC73E' size = 10>El pedido: " + pedidocan.Text.Trim() + " No Corresponde a Sistema Split Camionetas</font>"));
                        alertDialog.SetNeutralButton("Ok", delegate
                        {
                            alertDialog.Dispose();
                        });
                        alertDialog.Show();
                        pedidocan.Text = "";
                        pedidocan.RequestFocus();
                        return;

                    }

                    List<FlimStarInfo> lstFlimStar = ConsSplit();
                    var gvObject = FindViewById<GridView>(Resource.Id.gvCtrCancel);
                    gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);
                    gvObject.ItemClick += new EventHandler<AdapterView.ItemClickEventArgs>(OnGridView_ItemClicked); ; //detalle_pedido

                    pedidocan.SetSelection(0, pedidocan.Text.Length);
                    pedidocan.RequestFocus();

                }
            };

        }

        public int valida_pedido(string validar)
        {
            int valor = 0;

            thisConnection.Open();
            string Cadena = "Select prov_clave from tb_mstr_pedidos_nal Where pdn_folio = '" + validar + "' AND pdn_surtido = 'S'";
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
                    //cmdActualizaEmb.ExecuteNonQuery();
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

            return valor;

        }

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


        private void OnGridView_ItemClicked(object sender, AdapterView.ItemClickEventArgs e)
        {
            split = e.View.FindViewById<TextView>(Resource.Id.txtName).Text;
            split = split.Replace("Split Numero: ", "");

            pedidocancelar = pedidocan.Text.Trim();

            Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
            alertDialog.SetTitle(Html.FromHtml("<font color='#DF0101' size = 10>Cancelar Split</font>"));
            alertDialog.SetIcon(Resource.Drawable.question);
            alertDialog.SetMessage(Html.FromHtml("<font color='#000000' size = 10>¿Desea Cancelar el Splir Numero " + split + "?</font>"));
            alertDialog.SetPositiveButton(Html.FromHtml("<font face = 'Comic Sans MS, arial' color='#DF0101' size = '10'>Sí</font>"), SaveAction);
            alertDialog.SetNegativeButton(Html.FromHtml("<font face = 'Comic Sans MS, arial' color='#DF0101' size = '10'>No</font>"), CancelaAction);
            alertDialog.Create();
            alertDialog.Show();
        }

        private void SaveAction(object sender, DialogClickEventArgs e)
        {
            thisConnection.Open();
            string cadena = "UPDATE tb_det_Etiqueta SET Estatus = 'C' WHERE emb_folio = '" + pedidocancelar.ToString() + "' AND Split = '" + split.ToString() + "'";
            SqlCommand cmd = new SqlCommand(cadena, thisConnection);
            cmd.ExecuteNonQuery();

            string cadenados = "UPDATE tb_det_split SET estatus = 'C' WHERE emb_folio = '" + pedidocancelar.ToString() + "' AND tarima = '" + split.ToString() + "'";
            SqlCommand cmddos = new SqlCommand(cadenados, thisConnection);
            cmddos.ExecuteNonQuery();

            string Cadena = "Select * From tb_det_split WHERE emb_folio = '" + pedidocancelar.ToString() + "' AND tarima = '" + split.ToString() + "' ";
            SqlDataAdapter da = new SqlDataAdapter(Cadena, thisConnection);
            DataSet ds = new DataSet();
            da.Fill(ds, "Ped");
            DataTable Ped = ds.Tables["Ped"];
            foreach (DataRow row in Ped.Rows)
            {
                if (row["tipo_rec"].ToString().Trim() == "PTC")
                    cadena = "UPDATE TB_DET_TRAZABILIDAD SET SURTIDO = SURTIDO - " + row["cajas"].ToString().Trim() + " WHERE PROD_CLAVE = '" + row["prod_clave"].ToString().Trim() + "' AND RECIBO = '" + row["no_lote"].ToString().Trim() + "' " +
                        "AND TIPO = 'PTC' AND TARIMA = '" + Convert.ToInt32(row["TARINI"].ToString().Trim()).ToString() + "' ";

                else
                    cadena = "UPDATE TB_DET_ETI_FINAL SET CAJAS_SUR = CAJAS_SUR - " + row["cajas"].ToString().Trim() + " WHERE CVE_PROD = '" + row["prod_clave"].ToString().Trim() + "' AND FOLIO = '" + row["no_lote"].ToString().Trim() + "' " +
                        "AND TARIMA = '" + Convert.ToInt32(row["TARINI"].ToString().Trim()).ToString() + "' ";
                cmd = new SqlCommand(cadena, thisConnection);
                cmd.ExecuteNonQuery();



            }


            string cade = "DELETE FROM Tb_Det_Split_ProdXPed WHERE pdn_folio = '" + pedidocan.Text.Trim() + "'";
            //MessageBox.Show(cadena);
            SqlCommand cmdss = new SqlCommand(cade, thisConnection);
            cmdss.ExecuteNonQuery();


            Android.Telephony.TelephonyManager mTelephonyMgr;
            mTelephonyMgr = (Android.Telephony.TelephonyManager)GetSystemService(TelephonyService);
            //IMEI number  
            string imei = mTelephonyMgr.DeviceId;


            string cadenas = "INSERT INTO TB_REGISTRO_MOVIMIENTOS(FECHA,NOM_COMPU,NOM_USU,TIPO_MOV,OP_CLAVE,FOLIO,DETALLE,SISTEMA,MOV_FOLIO) " +
                            "VALUES('" + System.DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "','CEL " + imei + "','" + crcancelar.Trim() + "','C','7.10','" +
                            pedidocancelar.ToString().Trim() + "','Cancelacion Split " + split.ToString() + "','SPLITCA','" + pedidocancelar.ToString().Trim() + "')";
            //MessageBox.Show(cadena);
            SqlCommand cmds = new SqlCommand(cadenas, thisConnection);
            cmds.ExecuteNonQuery();


            thisConnection.Close();

            db.Query<Mensajes>("delete from  [Mensajes] Where titulo = 'Error Al Guardar' AND mensaje = '" + pedidocancelar.ToString().Trim() + "'");


            Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
            alertDialog.SetTitle(Html.FromHtml("<font color='#DF0101' size = 10>Split Cancelado</font>"));
            alertDialog.SetIcon(Resource.Drawable.exito);
            alertDialog.SetMessage(Html.FromHtml("<font color='#FFFFFF' size = 10>Split Cancelado Correctamente!!! </font>"));
            alertDialog.SetCancelable(false);
            alertDialog.SetNeutralButton("Ok", delegate
            {
                alertDialog.Dispose();
                pedidocan.Text = "";
                cansplit.Text = "000|000";
                List<FlimStarInfo> lstFlimStar = ConsSplit();
                lstFlimStar.Clear();
                var gvObject = FindViewById<GridView>(Resource.Id.gvCtrCancel);
                gvObject.Adapter = new myGVItemAdapter(this, null);
                gvObject.Adapter = null;
                gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);

            });
            alertDialog.Show();

        }

        private void CancelaAction(object sender, DialogClickEventArgs e)
        {
            return;
        }

        List<FlimStarInfo> listItem = new List<FlimStarInfo>();

        List<FlimStarInfo> GetFlimStarInformation()
        {
            throw new NotImplementedException();
        }

        List<FlimStarInfo> ConsSplit()
        {
            string Existe = "N";
            int cantidadsplit = 0;
            thisConnection.Open();
            listItem.Clear();
            string contenido = "";
            //thisConnection.Open();
            string cadena = "Select DISTINCT(tarima) AS NoSplit from tb_det_split where emb_folio = '" + pedidocan.Text.Trim() + "' AND estatus != 'C'";
            SqlDataAdapter da = new SqlDataAdapter(cadena, thisConnection);
            DataSet ds = new DataSet();
            da.Fill(ds, "ConsPed");
            DataTable ConsPed = ds.Tables["ConsPed"];

            foreach (DataRow Row in ConsPed.Rows)
            {
                Existe = "S";
                listItem.Add(new FlimStarInfo()
                {
                    Name = "Split Numero: " + Row["NoSplit"].ToString().Trim(),
                    Age = "Para Cancelar de Clic Aqui",
                    ImageID = Resource.Drawable.producto
                });
                cantidadsplit++;
            }

            cansplit.Text = cantidadsplit.ToString();

            if (Existe != "S")
            {
                int etiquetas_afectadas = 0;
                string cade = "DELETE FROM Tb_Det_Etiqueta WHERE emb_folio = '" + pedidocan.Text.Trim() + "'";
                //MessageBox.Show(cadena);
                SqlCommand cmds = new SqlCommand(cade, thisConnection);
                etiquetas_afectadas = cmds.ExecuteNonQuery();


                if (etiquetas_afectadas == 0)
                {
                    db.Query<Mensajes>("delete from  [Mensajes] Where titulo = 'Error Al Guardar' AND mensaje = " + pedidocan.Text.Trim());


                    Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                    alertDialog.SetTitle(Html.FromHtml("<font color='#DF0101' size = 10>Pedido Sin Split/font>"));
                    alertDialog.SetIcon(Resource.Drawable.no);
                    alertDialog.SetCancelable(false);
                    alertDialog.SetMessage(Html.FromHtml("<font color='#FFFFFF' size = 10>El pedido: " + pedidocan.Text.Trim() + " No cuenta con split disponible</font>"));
                    alertDialog.SetNeutralButton("Ok", delegate
                    {
                        alertDialog.Dispose();
                    });
                    alertDialog.Show();
                }
                else
                {
                    if (db != null && pedidocancelar != null)
                    {
                        db.Query<Mensajes>("delete from  [Mensajes] Where titulo = 'Error Al Guardar' AND mensaje = '" + pedidocancelar.ToString().Trim() + "'");
                    }


                    Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                    alertDialog.SetTitle(Html.FromHtml("<font color='#A6FF34' size = 10>Etiquetas Liberadas/font>"));
                    alertDialog.SetIcon(Resource.Drawable.no);
                    alertDialog.SetCancelable(false);
                    alertDialog.SetMessage(Html.FromHtml("<font color='#89FF79' size = 10>Las Etiquetas del pedido: " + pedidocan.Text.Trim() + ", Han Sido Liberadas</font>"));
                    alertDialog.SetNeutralButton("Ok", delegate
                    {
                        alertDialog.Dispose();
                    });
                    alertDialog.Show();
                }


            }

            //LbxCons.Font = new Font(LbxCons.Font.Name, 7);   ;
            thisConnection.Close();

            return listItem;
        }

    }
}