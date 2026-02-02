const express = require('express');
const { Client, LocalAuth } = require('whatsapp-web.js');
const qrcode = require('qrcode-terminal');
const QRCode = require('qrcode'); // Para generar QR como imagen
const cors = require('cors');
const bodyParser = require('body-parser');
require('dotenv').config();

const app = express();
const PORT = process.env.PORT || 3001;

// Middlewares
app.use(cors());
app.use(bodyParser.json());
app.use(bodyParser.urlencoded({ extended: true }));

// Estado del cliente WhatsApp
let whatsappClient = null;
let isReady = false;
let qrCodeGenerated = false;
let lastQRCode = ''; // Almacenar último QR para mostrarlo como imagen

// Inicializar cliente de WhatsApp - DESACTIVADO TEMPORALMENTE
console.log('🚀 Iniciando servicio de WhatsApp para MVSystem...');

/*
whatsappClient = new Client({
    authStrategy: new LocalAuth({
        clientId: 'mvsystem-tracking',
        dataPath: './.wwebjs_auth'
    }),
    puppeteer: {
        headless: true,
        args: [
            '--no-sandbox',
            '--disable-setuid-sandbox',
            '--disable-dev-shm-usage',
            '--disable-accelerated-2d-canvas',
            '--no-first-run',
            '--no-zygote',
            '--disable-gpu'
        ]
    }
});
*/

// ========== EVENT LISTENERS - DESACTIVADOS TEMPORALMENTE ==========
/*
// Evento: Generación de QR Code
whatsappClient.on('qr', (qr) => {
    console.log('\n📱 ESCANEA ESTE CÓDIGO QR CON TU WHATSAPP:\n');
    qrcode.generate(qr, { small: true });
    console.log('\n⚠️  Abre WhatsApp en tu teléfono > Dispositivos vinculados > Vincular dispositivo');
    console.log('⚠️  Escanea el código QR mostrado arriba');
    console.log('🌐 O visita: http://localhost:' + PORT + '/qr-image\n');
    lastQRCode = qr; // Guardar QR para mostrarlo como imagen
    qrCodeGenerated = true;
});

// Evento: Cliente autenticado
whatsappClient.on('authenticated', () => {
    console.log('✅ WhatsApp autenticado correctamente');
});

// Evento: Error de autenticación
whatsappClient.on('auth_failure', (msg) => {
    console.error('❌ Error de autenticación:', msg);
    console.log('💡 Elimina la carpeta .wwebjs_auth y vuelve a escanear el QR');
});

// Evento: Cliente listo
whatsappClient.on('ready', () => {
    console.log('✅ Cliente WhatsApp listo y conectado');
    console.log('📞 Servicio de notificaciones activo');
    isReady = true;
});

// Evento: Cliente desconectado
whatsappClient.on('disconnected', (reason) => {
    console.log('⚠️  Cliente desconectado:', reason);
    isReady = false;
});

// Evento: Mensaje recibido (opcional - para logs)
whatsappClient.on('message', async(msg) => {
    // Solo para debugging - puedes comentar esto en producción
    if (msg.body.toLowerCase() === 'ping') {
        await msg.reply('🤖 Pong! Servicio MVSystem activo');
    }
});

// Inicializar cliente
whatsappClient.initialize();
*/

// ========== ENDPOINTS REST API ==========

/**
 * GET /status
 * Verifica el estado del servicio
 */
app.get('/status', (req, res) => {
    res.json({
        success: true,
        ready: isReady,
        qrGenerated: qrCodeGenerated,
        message: isReady ? 'Servicio WhatsApp activo' : 'Esperando conexión WhatsApp'
    });
});

/**
 * POST /send-message
 * Envía un mensaje de WhatsApp
 * Body: { number: '18091234567', message: 'Texto del mensaje' }
 */
app.post('/send-message', async(req, res) => {
    try {
        const { number, message } = req.body;

        if (!number || !message) {
            return res.status(400).json({
                success: false,
                error: 'Faltan parámetros: number y message son requeridos'
            });
        }

        if (!isReady) {
            return res.status(503).json({
                success: false,
                error: 'Servicio WhatsApp no está listo. Escanea el código QR primero.'
            });
        }

        // Formatear número (agregar @c.us si no lo tiene)
        const chatId = number.includes('@c.us') ? number : `${number}@c.us`;

        // Verificar si el número existe
        const isRegistered = await whatsappClient.isRegisteredUser(chatId);
        if (!isRegistered) {
            return res.status(404).json({
                success: false,
                error: `El número ${number} no está registrado en WhatsApp`
            });
        }

        // Enviar mensaje
        await whatsappClient.sendMessage(chatId, message);

        console.log(`✉️  Mensaje enviado a ${number}`);

        res.json({
            success: true,
            message: 'Mensaje enviado correctamente',
            to: number
        });

    } catch (error) {
        console.error('❌ Error al enviar mensaje:', error);
        res.status(500).json({
            success: false,
            error: error.message
        });
    }
});

/**
 * POST /send-bulk
 * Envía mensajes masivos
 * Body: { numbers: ['1809...', '1829...'], message: 'Texto' }
 */
app.post('/send-bulk', async(req, res) => {
    try {
        const { numbers, message } = req.body;

        if (!numbers || !Array.isArray(numbers) || !message) {
            return res.status(400).json({
                success: false,
                error: 'Faltan parámetros: numbers (array) y message son requeridos'
            });
        }

        if (!isReady) {
            return res.status(503).json({
                success: false,
                error: 'Servicio WhatsApp no está listo'
            });
        }

        const results = [];

        for (const number of numbers) {
            try {
                const chatId = number.includes('@c.us') ? number : `${number}@c.us`;
                await whatsappClient.sendMessage(chatId, message);
                results.push({ number, success: true });

                // Delay para evitar bloqueos de WhatsApp
                await new Promise(resolve => setTimeout(resolve, 2000));
            } catch (error) {
                results.push({ number, success: false, error: error.message });
            }
        }

        res.json({
            success: true,
            message: 'Mensajes procesados',
            results
        });

    } catch (error) {
        console.error('❌ Error en envío masivo:', error);
        res.status(500).json({
            success: false,
            error: error.message
        });
    }
});

/**
 * GET /qr
 * Regenera el código QR (útil para reconectar)
 */
app.get('/qr', async(req, res) => {
    try {
        if (isReady) {
            return res.json({
                success: false,
                message: 'Ya estás conectado a WhatsApp'
            });
        }

        // Reiniciar cliente
        await whatsappClient.destroy();
        await whatsappClient.initialize();

        res.json({
            success: true,
            message: 'QR regenerado. Revisa la consola del servidor o visita /qr-image'
        });
    } catch (error) {
        res.status(500).json({
            success: false,
            error: error.message
        });
    }
});

/**
 * GET /qr-image
 * Muestra el código QR como imagen en el navegador
 */
app.get('/qr-image', async(req, res) => {
    try {
        if (isReady) {
            return res.send(`
                <html>
                    <head><title>WhatsApp QR - MVSystem</title></head>
                    <body style="display:flex;justify-content:center;align-items:center;height:100vh;flex-direction:column;font-family:Arial;">
                        <h2 style="color:green;">✅ WhatsApp ya está conectado</h2>
                        <p>No necesitas escanear el QR</p>
                        <a href="/status">Ver estado del servicio</a>
                    </body>
                </html>
            `);
        }

        if (!lastQRCode) {
            return res.send(`
                <html>
                    <head>
                        <title>WhatsApp QR - MVSystem</title>
                        <meta http-equiv="refresh" content="3">
                    </head>
                    <body style="display:flex;justify-content:center;align-items:center;height:100vh;flex-direction:column;font-family:Arial;">
                        <h2>⏳ Esperando código QR...</h2>
                        <p>La página se actualizará automáticamente</p>
                        <p style="color:gray;">Si no aparece en 30 segundos, recarga manualmente</p>
                    </body>
                </html>
            `);
        }

        // Generar QR como imagen
        const qrImageUrl = await QRCode.toDataURL(lastQRCode, {
            width: 400,
            margin: 2,
            color: {
                dark: '#000000',
                light: '#FFFFFF'
            }
        });

        res.send(`
            <html>
                <head>
                    <title>WhatsApp QR - MVSystem</title>
                    <style>
                        body {
                            display: flex;
                            justify-content: center;
                            align-items: center;
                            height: 100vh;
                            flex-direction: column;
                            font-family: Arial, sans-serif;
                            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                            color: white;
                            margin: 0;
                        }
                        .container {
                            background: white;
                            padding: 30px;
                            border-radius: 15px;
                            box-shadow: 0 10px 40px rgba(0,0,0,0.3);
                            text-align: center;
                            color: #333;
                        }
                        h1 { color: #667eea; margin-bottom: 10px; }
                        img { border: 5px solid #667eea; border-radius: 10px; margin: 20px 0; }
                        .instructions {
                            background: #f0f0f0;
                            padding: 15px;
                            border-radius: 8px;
                            margin-top: 20px;
                            text-align: left;
                        }
                        .instructions ol { margin: 10px 0; padding-left: 20px; }
                        .instructions li { margin: 8px 0; }
                    </style>
                </head>
                <body>
                    <div class="container">
                        <h1>📱 MVSystem WhatsApp Service</h1>
                        <p><strong>Escanea este código QR para conectar WhatsApp</strong></p>
                        <img src="${qrImageUrl}" alt="QR Code" />
                        <div class="instructions">
                            <strong>📋 Instrucciones:</strong>
                            <ol>
                                <li>Abre WhatsApp en tu teléfono</li>
                                <li>Ve a <strong>Configuración</strong> → <strong>Dispositivos vinculados</strong></li>
                                <li>Toca en <strong>"Vincular un dispositivo"</strong></li>
                                <li>Escanea este código QR</li>
                            </ol>
                        </div>
                        <p style="margin-top:20px;color:gray;font-size:12px;">
                            Esta página se cerrará automáticamente cuando esté conectado
                        </p>
                    </div>
                    <script>
                        // Verificar estado cada 3 segundos
                        setInterval(async () => {
                            try {
                                const response = await fetch('/status');
                                const data = await response.json();
                                if (data.ready) {
                                    location.reload();
                                }
                            } catch (error) {
                                console.error('Error verificando estado:', error);
                            }
                        }, 3000);
                    </script>
                </body>
            </html>
        `);
    } catch (error) {
        res.status(500).send(`
            <html>
                <body style="display:flex;justify-content:center;align-items:center;height:100vh;flex-direction:column;font-family:Arial;">
                    <h2 style="color:red;">❌ Error generando QR</h2>
                    <p>${error.message}</p>
                </body>
            </html>
        `);
    }
});

/**
 * POST /logout
 * Cierra sesión de WhatsApp
 */
app.post('/logout', async(req, res) => {
    try {
        if (!isReady) {
            return res.status(400).json({
                success: false,
                message: 'No hay sesión activa'
            });
        }

        await whatsappClient.logout();
        isReady = false;
        qrCodeGenerated = false;

        res.json({
            success: true,
            message: 'Sesión cerrada correctamente'
        });
    } catch (error) {
        res.status(500).json({
            success: false,
            error: error.message
        });
    }
});

// Endpoint raíz
app.get('/', (req, res) => {
    res.json({
        service: 'MVSystem WhatsApp Service',
        version: '1.0.0',
        status: isReady ? 'connected' : 'waiting',
        endpoints: {
            status: 'GET /status',
            sendMessage: 'POST /send-message',
            sendBulk: 'POST /send-bulk',
            qr: 'GET /qr',
            qrImage: 'GET /qr-image (Ver QR en navegador)',
            logout: 'POST /logout'
        }
    });
});

// Iniciar servidor
app.listen(PORT, () => {
    console.log(`\n🌐 Servidor Express escuchando en http://localhost:${PORT}`);
    console.log(`📡 Endpoints disponibles:`);
    console.log(`   - GET  http://localhost:${PORT}/status`);
    console.log(`   - GET  http://localhost:${PORT}/qr-image (👁️  Ver QR en navegador)`);
    console.log(`   - POST http://localhost:${PORT}/send-message`);
    console.log(`   - POST http://localhost:${PORT}/send-bulk`);
    console.log(`   - GET  http://localhost:${PORT}/qr`);
    console.log(`   - POST http://localhost:${PORT}/logout\n`);
});

// Manejo de cierre graceful
process.on('SIGINT', async() => {
    console.log('\n⚠️  Cerrando servicio...');
    if (whatsappClient) {
        await whatsappClient.destroy();
    }
    process.exit(0);
});

process.on('SIGTERM', async() => {
    console.log('\n⚠️  Cerrando servicio...');
    if (whatsappClient) {
        await whatsappClient.destroy();
    }
    process.exit(0);
});