﻿using System.Web.Mvc;
using ActiveSense.Tempsense.model.Modelo;
using System.Configuration;

namespace ActiveSense.Tempsense.web.Controllers
{
    public class GenericController : Controller
    {
        public ActiveSenseContext dbActiveContext = new ActiveSenseContext(ConfigurationManager.ConnectionStrings["TempsenseConnection"].ConnectionString);

    }
}
