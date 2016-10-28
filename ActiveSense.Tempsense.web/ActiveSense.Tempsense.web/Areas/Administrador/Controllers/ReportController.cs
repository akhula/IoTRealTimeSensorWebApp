﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Diagnostics;
using System.Data.SqlClient;
using System.Text;
using System.Web.UI.WebControls;
using ActiveSense.Tempsense.model.Modelo;
using ActiveSense.Tempsense.web.Controllers;
using ActiveSense.Tempsense.web.Models;
using ActiveSense.Tempsense.web.Helpers;
using System.Configuration;
using System.Web.Script.Serialization;

namespace ActiveSense.Tempsense.web.Areas.Administrador.Controllers
{
    [ActiveSenseAutorize("Administrador")]
    public class ReportController : GenericController
    {
        // GET: Administrador/Report
        public const int CANTIDAD_DISPOSITIVOS = 10;
        enum enumFiltroTiempo
        {
            Dia = 1440,
            Sesenta_min = 60,
            Treinta_min = 30,
            Veinte_min = 20,
            Diez_min = 10,
            Cinco_min = 5,
            Seleccione_Tiempo = 0
        };

        // [MedidorAuthorize]
        //GET:/Medida/
        public ActionResult Index(int id = 1, int idDispositivo = 0)
        {

            return View(Buscar(id, idDispositivo));
        }


        public ActionResult ListaMedidas(int id = 1, int idDispositivo = 0)
        {

            return PartialView(Buscar(id, idDispositivo));
        }

        [HttpPost]
        public ActionResult ObtenerDispositivoAsociado(string idEmpresa)
        {

            int idEmpresaT = Convert.ToInt32(idEmpresa);
            var lists = (dbActiveContext.Dispositivos.Where(x => x.EmpresaID == idEmpresaT)).ToList<Dispositivos>();

            List<DispositivoViewModel> datos = new List<DispositivoViewModel>();
            foreach (Dispositivos dist in lists)
            {
                var TipoMedida = String.Empty;
                DispositivoViewModel dispositivo = new DispositivoViewModel();
                dispositivo.idDispositivo = dist.DispositivoID;
                TipoMedida = dbActiveContext.TipoMedidas.Where(x => x.TipoMedidaID == dist.TipoMedidaID).FirstOrDefault().Nombre;
                dispositivo.tipoMedida = TipoMedida;
                dispositivo.nombreDispositivo = dist.Nombre + " ( "+ dispositivo.tipoMedida +" )";
                datos.Add(dispositivo);
            }

            JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
            string result = javaScriptSerializer.Serialize(datos);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public List<Medida> Buscar(int pageIndex, int idDispositivo)
        {
            Medida medida = new Medida();

            var listEmp = new SelectList(dbActiveContext.Empresas, "EmpresaID", "Nombre").ToList();
            listEmp.Insert(0, (new SelectListItem { Text = "Seleccione una empresa", Value = "0" }));
            ViewBag.Empresas = listEmp;

            var lists = (dbActiveContext.Dispositivos).ToList<Dispositivos>();
            List<DispositivoViewModel2> datos = new List<DispositivoViewModel2>();
            foreach (Dispositivos disp in lists)
            {
                var TipoMedida = String.Empty;
                DispositivoViewModel2 dispositivo = new DispositivoViewModel2();
                dispositivo.idDispositivo = disp.DispositivoID;
                TipoMedida = dbActiveContext.TipoMedidas.Where(x => x.TipoMedidaID == disp.TipoMedidaID).FirstOrDefault().Nombre;
                dispositivo.tipoMedida = TipoMedida;
                dispositivo.nombreDispositivo = disp.Nombre + " ( " + dispositivo.tipoMedida + " )";
                datos.Add(dispositivo);
            }
            var list = new SelectList(datos, "idDispositivo", "nombreDispositivo").ToList();
            list.Insert(0, (new SelectListItem { Text = "Seleccione el dispositivo", Value = "0" }));
            //Agregar Lista de Dispositivo
            ViewBag.DispositivoID = list;

            //agregar filtro de tiempo
            ViewBag.FiltroTiempo = new SelectList(Enum.GetValues(typeof(enumFiltroTiempo)).Cast<enumFiltroTiempo>().Select(v => new SelectListItem
            {
                Value = ((int)v).ToString(),
                Text = v.ToString()
            }).ToList(), "Value", "Text" );
            
            return null;
        }


        public JsonResult ObtenerDatosGrafico(int pageIndex, int idDispositivo, string fechaInicio, string fechaFin)
        {

            int start = Request["start"] != null ? Int16.Parse(Request["start"]) : 0;
            int lenght = Request["length"] != null ? Int16.Parse(Request["length"]) : 10;
            string fechaInicial = Request["fechaInicio"] != null ? Request["fechaInicio"] : "";
            string fechaFinal = Request["FechaFin"] != null ? Request["FechaFin"] : "";
            int filtroMedidaTiempo = Request["filtroTiempo"] != null ? Int16.Parse(Request["filtroTiempo"]) : 0;

            Medida medida = new Medida();
            int pageCount = 0;
            List<Medida> medidasList = null;

            if ( filtroMedidaTiempo <= 0)
            {
                medidasList = medida.Listar(start, lenght, out pageCount, idDispositivo, fechaInicial, fechaFin, "", "", filtroMedidaTiempo);
            }
            else {
                medidasList = medida.ListarPromedios(start, lenght, out pageCount, idDispositivo, fechaInicial, fechaFin, "", "", filtroMedidaTiempo);
            }
            

            List<double> temperaturaList = new List<double>();
            List<string> fechas = new List<string>();

            List<double> umbralInferior = new List<double>();
            List<double> umbralSuperior = new List<double>();

            List<double> toleranciaSuperiorList = new List<double>();
            List<double> toleranciaInferiorList = new List<double>();

            decimal umbraMax = 0;
            decimal umbraMin = 0;
            decimal toleranciaMin = 0;
            decimal toleranciaMax = 0;

            try
            { 
                 umbraMax = dbActiveContext.Umbrals.Where(p => p.DispositivoID == idDispositivo).FirstOrDefault().Temperatura_max;
                 umbraMin = dbActiveContext.Umbrals.Where(p => p.DispositivoID == idDispositivo).FirstOrDefault().Temperatura_min;
                 toleranciaMin = dbActiveContext.Umbrals.Where(p => p.DispositivoID == idDispositivo).FirstOrDefault().Tolerancia_min;
                 toleranciaMax = dbActiveContext.Umbrals.Where(p => p.DispositivoID == idDispositivo).FirstOrDefault().Tolerancia_max;
            }
            catch (Exception ex){ }

            foreach (Medida medidaTemp in medidasList)
            {
                temperaturaList.Add((double)medidaTemp.Valor);
                fechas.Add(medidaTemp.FechaHora.ToString());
                umbralInferior.Add((double)umbraMin);
                umbralSuperior.Add((double)umbraMax);
                toleranciaSuperiorList.Add((double)toleranciaMax);
                toleranciaInferiorList.Add((double)toleranciaMin);
            }

            var resultado = new JsonResult();
            resultado.Data = new
            {
                fechas = fechas.ToArray(),
                temperaturas = temperaturaList.ToArray(),
                umbralSuperior = umbralSuperior.ToArray(),
                umbralInferior = umbralInferior.ToArray(),
                toleranciaSuperiorList = toleranciaSuperiorList.ToArray(),
                toleranciaInferiorList = toleranciaInferiorList.ToArray(),
            };
            return resultado;

        }

        [HttpPost]
        public JsonResult ObtenerDatosTabla()
        {

            string search = Request["search[value]"];
            string draw = Request["draw"];

            int start = Request["start"] != null ? Int16.Parse(Request["start"]) : 0;
            int lenght = Request["length"] != null ? Int16.Parse(Request["length"]) : 10;

            int dispositivo = Request["idDispositivo"] != null ? Int16.Parse(Request["idDispositivo"]) : 0;

            string fechaInicial = Request["fechaInicio"] != null ? Request["fechaInicio"] : "";
            string fechaFinal = Request["FechaFin"] != null ? Request["FechaFin"] : "";

            int filtroMedidaTiempo = Request["filtroTiempo"] != null ? Int16.Parse(Request["filtroTiempo"]) : 0;

            Medida medida = new Medida();
            int pageCount = 0;

            List<Medida> medidasList = null;

            if (filtroMedidaTiempo <= 0)
            {
                medidasList = medida.Listar(start, lenght, out pageCount, dispositivo, fechaInicial, fechaFinal, "", "", filtroMedidaTiempo);
            }
            else
            {
                medidasList = medida.ListarPromedios(start, lenght, out pageCount, dispositivo, fechaInicial, fechaFinal, "", "", filtroMedidaTiempo);
            }


            List<double> temperaturaList = new List<double>();
            List<string> fechas = new List<string>();

            List<ReportModel> medidasModelList = new List<ReportModel>();
            ReportModel medidasmodel = null;

            Dictionary<int, string> nombresDic = new Dictionary<int, string>();

            foreach (Medida medidaTemp in medidasList)
            {
                medidasmodel = new ReportModel();
                medidasmodel.fecha = medidaTemp.FechaHora.ToString();
                medidasmodel.idDispositivo = medidaTemp.DispositivoID;
                medidasmodel.temperatura = medidaTemp.Valor.ToString();

                if (!nombresDic.ContainsKey(medidasmodel.idDispositivo))
                {
                    var nombre = dbActiveContext.Dispositivos.Where(dis => dis.DispositivoID == medidasmodel.idDispositivo).FirstOrDefault().Nombre;
                    nombresDic.Add(medidasmodel.idDispositivo, nombre);
                }
                medidasmodel.nombreDispositivo = nombresDic[medidasmodel.idDispositivo];

                medidasModelList.Add(medidasmodel);
            }

            var resultado = new JsonResult();
            resultado.Data = new { draw = draw, recordsTotal = pageCount, recordsFiltered = pageCount, data = medidasModelList.ToArray() };
            return resultado;
        }

    }


}
