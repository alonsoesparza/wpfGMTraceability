using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wpfGMTraceability.Helpers
{
    public static class HttpStatusHelper
    {

        public static string GetStatusMessage(int statusCode)
        {
            switch (statusCode)
            {
                case 200:
                    return "✅ OK: La solicitud fue exitosa.";
                case 201:
                    return "✅ Created: El recurso fue creado correctamente.";
                case 400:
                    return "❌ Bad Request: La solicitud tiene errores de formato o datos inválidos.";
                case 401:
                    return "🔒 Unauthorized: No tienes autorización. ¿Token expirado?";
                case 403:
                    return "🚫 Forbidden: Tienes credenciales, pero no permiso.";
                case 404:
                    return "🔍 Not Found: El recurso no existe.";
                case 409:
                    return "⚠️ Conflict: El recurso ya existe o hay un conflicto lógico.";
                case 422:
                    return "🧩 Unprocessable Entity: El servidor no puede procesar los datos enviados.";
                case 500:
                    return "💥 Internal Server Error: Algo falló en el servidor.";
                default:
                    return $"❓ Código inesperado: {statusCode}";
            }
        }
    }
}
