using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MVSystemApi.Model
{
    public static class StaticClass
    {
        #region PROPIEDADES
        static Boolean _Login;
        static Int32 _usuarioId;
        static String _usuarioLogin;
        static String _usuarioClaveEncr;
        static String _usuarioClave;
        static String _usuarioNombre;
        static String _EmpresaNombre;
        static String _SucursalNombre;
        static Int32? _usuarioSucursal;

        public static Int32? UsuarioSucursal
        {
            get { return _usuarioSucursal; }
            set { _usuarioSucursal = value; }
        }


        public static Boolean Login
        {
            get { return _Login; }
            set { _Login = value; }
        }

        public static Int32 UsuarioId
        {
            get { return _usuarioId; }
            set { _usuarioId = value; }
        }

        public static String UsuarioLogin
        {
            get { return _usuarioLogin; }
            set { _usuarioLogin = value; }
        }

        public static String UsuarioClaveEncr
        {
            get { return _usuarioClaveEncr; }
            set { _usuarioClaveEncr = value; }
        }

        public static String UsuarioClave
        {
            get { return _usuarioClave; }
            set { _usuarioClave = value; }
        }

        public static String UsuarioNombre
        {
            get { return _usuarioNombre; }
            set { _usuarioNombre = value; }
        }

        public static String EmpresaNombre
        {
            get { return _EmpresaNombre; }
            set { _EmpresaNombre = value; }
        }

        public static String SucursalNombre
        {
            get { return _SucursalNombre; }
            set { _SucursalNombre = value; }
        }
        #endregion

      

       
       
    }
}
