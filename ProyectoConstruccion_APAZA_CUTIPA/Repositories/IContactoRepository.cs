using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ProyectoConstruccion_APAZA_CUTIPA.Models;

namespace ProyectoConstruccion_APAZA_CUTIPA.Repositories
{
    public interface IContactoRepository
    {
        List<clsContactoEmergencia> GetByUserId(int idUsuario, out string errorMessage);

        bool Add(clsContactoEmergencia contacto, out string errorMessage);
        clsContactoEmergencia GetById(int idContactoEmergencia, int idUsuario, out string errorMessage);

        bool Update(clsContactoEmergencia contacto, out string errorMessage);

        bool SetAsPrincipal(int idContactoEmergencia, int idUsuario, out string errorMessage);

        bool Delete(int idContactoEmergencia, int idUsuario, out string mensajeError);

    }
}