using MVSystemApi.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MVSystemApi.Model_Negocio
{
    /// <summary>
    /// Servicio para gestionar notificaciones de WhatsApp
    /// Se comunica con el servidor Node.js que maneja la conexión WhatsApp
    /// </summary>
    public class WhatsAppService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<WhatsAppService> _logger;
        private readonly HttpClient _httpClient;
        private Process _nodeProcess;
        private readonly string _nodeServerUrl;

        public WhatsAppService(
            IConfiguration configuration,
            ILogger<WhatsAppService> logger,
            HttpClient httpClient)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClient;
            _nodeServerUrl = configuration["WhatsApp:NodeServerUrl"] ?? "http://localhost:3001";
        }

        /// <summary>
        /// Inicia el servidor Node.js de WhatsApp
        /// </summary>
        public async Task<bool> IniciarServidorWhatsApp()
        {
            try
            {
                var nodeExecutable = _configuration["WhatsApp:NodeExecutable"] ?? "node";
                var scriptPath = _configuration["WhatsApp:ScriptPath"] ?? "./WhatsAppNode/whatsapp-server.js";

                _logger.LogInformation($"Iniciando servidor WhatsApp en {scriptPath}...");

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = nodeExecutable,
                    Arguments = scriptPath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                _nodeProcess = new Process
                {
                    StartInfo = processStartInfo
                };

                _nodeProcess.OutputDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                    {
                        _logger.LogInformation($"WhatsApp Node: {args.Data}");
                    }
                };

                _nodeProcess.ErrorDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                    {
                        _logger.LogError($"WhatsApp Node Error: {args.Data}");
                    }
                };

                _nodeProcess.Start();
                _nodeProcess.BeginOutputReadLine();
                _nodeProcess.BeginErrorReadLine();

                // Esperar a que el servidor esté listo
                await Task.Delay(5000);

                _logger.LogInformation("Servidor WhatsApp iniciado correctamente");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al iniciar servidor WhatsApp: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Detiene el servidor Node.js
        /// </summary>
        public void DetenerServidorWhatsApp()
        {
            try
            {
                if (_nodeProcess != null && !_nodeProcess.HasExited)
                {
                    _nodeProcess.Kill();
                    _nodeProcess.Dispose();
                    _logger.LogInformation("Servidor WhatsApp detenido");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al detener servidor WhatsApp: {ex.Message}");
            }
        }

        /// <summary>
        /// Envía un mensaje de WhatsApp
        /// </summary>
        public async Task<bool> EnviarMensaje(string numeroTelefono, string mensaje)
        {
            try
            {
                var messageDto = new
                {
                    number = LimpiarNumeroTelefono(numeroTelefono),
                    message = mensaje
                };

                var jsonContent = JsonSerializer.Serialize(messageDto);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_nodeServerUrl}/send-message", content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Mensaje WhatsApp enviado a {numeroTelefono}");
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"Error al enviar mensaje WhatsApp: {errorContent}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Excepción al enviar mensaje WhatsApp: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Verifica si el servidor WhatsApp está listo
        /// </summary>
        public async Task<bool> VerificarEstado()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_nodeServerUrl}/status");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Envía notificación de tracking
        /// </summary>
        public async Task<bool> NotificarInteraccion(TrackingLog log)
        {
            try
            {
                var numeroDestino = _configuration["WhatsApp:NumeroNotificacion"];
                
                if (string.IsNullOrEmpty(numeroDestino))
                {
                    _logger.LogWarning("No hay número de WhatsApp configurado para notificaciones");
                    return false;
                }

                var mensaje = FormatearMensajeTracking(log);
                return await EnviarMensaje(numeroDestino, mensaje);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al notificar interacción: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Formatea el mensaje de tracking
        /// </summary>
        private string FormatearMensajeTracking(TrackingLog log)
        {
            return $@"🔔 *Alerta de Interacción - MVSystem*

👤 *Usuario:* {log.Usuario}
📦 *Módulo:* {log.Modulo}
⚡ *Acción:* {log.Accion}
🕐 *Fecha/Hora:* {log.FechaHora:dd/MM/yyyy HH:mm:ss}
🌐 *IP:* {log.DireccionIP ?? "N/A"}

{(string.IsNullOrEmpty(log.Metadata) ? "" : $"📋 *Detalles:* {log.Metadata}")}";
        }

        /// <summary>
        /// Limpia el número de teléfono al formato internacional
        /// </summary>
        private string LimpiarNumeroTelefono(string numero)
        {
            // Remover caracteres no numéricos
            var numeroLimpio = new string(numero.Where(char.IsDigit).ToArray());
            
            // Si no tiene código de país, agregar República Dominicana (+1809)
            if (!numeroLimpio.StartsWith("1") && numeroLimpio.Length == 10)
            {
                numeroLimpio = "1" + numeroLimpio;
            }

            return numeroLimpio;
        }
    }
}
