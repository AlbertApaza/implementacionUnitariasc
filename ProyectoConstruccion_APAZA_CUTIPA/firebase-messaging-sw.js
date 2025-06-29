// firebase-messaging-sw.js
importScripts('https://www.gstatic.com/firebasejs/10.11.0/firebase-app-compat.js');
importScripts('https://www.gstatic.com/firebasejs/10.11.0/firebase-messaging-compat.js');

const firebaseConfig = {
    apiKey: "AIzaSyDi1As7_ufAC9URZiuII8guJ2FjRR9LGpU",
    authDomain: "notificacionestest-7c1c8.firebaseapp.com",
    projectId: "notificacionestest-7c1c8",
    storageBucket: "notificacionestest-7c1c8.firebasestorage.app",
    messagingSenderId: "201347486772",
    appId: "1:201347486772:web:9988e7b9ba34e0f261a661"
};

firebase.initializeApp(firebaseConfig);
const messaging = firebase.messaging();

// Manejo de notificaciones en segundo plano
messaging.onBackgroundMessage(function (payload) {
    console.log('📱 Service Worker: Mensaje recibido en segundo plano:', payload);

    const { title, body } = payload.notification;

    // Determinar si es una alerta
    const isAlert = title.toLowerCase().includes('alerta') ||
        title.toLowerCase().includes('detectado') ||
        title.toLowerCase().includes('error');

    // Configurar opciones de notificación
    const notificationOptions = {
        body: body,
        icon: '/favicon.ico',
        badge: '/favicon.ico',
        tag: isAlert ? 'alert-notification' : 'info-notification',
        requireInteraction: isAlert, // Las alertas requieren interacción
        silent: false,
        data: {
            url: '/', // URL a abrir cuando se haga clic
            timestamp: Date.now(),
            isAlert: isAlert
        },
        actions: [
            {
                action: 'view',
                title: 'Ver Detalles',
                icon: '/favicon.ico'
            },
            {
                action: 'dismiss',
                title: 'Descartar'
            }
        ]
    };

    // Mostrar notificación del sistema
    return self.registration.showNotification(title, notificationOptions)
        .then(() => {
            console.log('✅ Notificación mostrada correctamente');

            // Enviar mensaje a todas las pestañas abiertas
            return self.clients.matchAll({ type: 'window', includeUncontrolled: true });
        })
        .then(clients => {
            console.log(`📤 Enviando mensaje a ${clients.length} cliente(s)`);

            clients.forEach(client => {
                client.postMessage({
                    type: 'FIREBASE_NOTIFICATION',
                    payload: payload,
                    timestamp: Date.now()
                });
            });
        })
        .catch(error => {
            console.error('❌ Error en Service Worker:', error);
        });
});

// Manejo de clics en notificaciones
self.addEventListener('notificationclick', function (event) {
    console.log('🖱️ Clic en notificación:', event.notification.tag);

    event.notification.close();

    if (event.action === 'dismiss') {
        return; // Solo cerrar la notificación
    }

    // Abrir o enfocar la aplicación
    event.waitUntil(
        self.clients.matchAll({ type: 'window', includeUncontrolled: true })
            .then(clients => {
                // Buscar una pestaña ya abierta
                for (const client of clients) {
                    if (client.url.includes(self.location.origin)) {
                        return client.focus();
                    }
                }

                // Si no hay pestañas abiertas, abrir una nueva
                return self.clients.openWindow('/');
            })
            .then(client => {
                if (client) {
                    // Enviar mensaje para procesar la notificación
                    client.postMessage({
                        type: 'NOTIFICATION_CLICKED',
                        data: event.notification.data
                    });
                }
            })
    );
});

// Manejo de errores
self.addEventListener('error', function (event) {
    console.error('❌ Error en Service Worker:', event.error);
});

self.addEventListener('unhandledrejection', function (event) {
    console.error('❌ Promise rechazado en Service Worker:', event.reason);
});

console.log('🔧 Service Worker de Firebase inicializado correctamente');